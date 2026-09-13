using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Network
{
    /// <summary>
    ///     TCP link this server opens itself. The server side of the same protocol is
    ///     <see cref="NetworkServer"/> with its <see cref="NetworkSession"/>: a session is a client
    ///     that came to us, this one is a server we went to.
    ///
    ///     The servers of one world need it: the field server is the one that connects to the
    ///     channel, so the whole family link lives on this side of the socket. The incoming stream
    ///     is cut into packets by the very same <see cref="FrameReader"/>, so both directions of
    ///     the family protocol are framed the same way
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public class NetworkClient : IDisposable
    {
        // Size of one read from the socket. The family link carries short packets and few of them,
        // so a small buffer is enough and a whole frame still fits
        private const int ReceiveBufferSize = 8192;

        private readonly object _sendLock = new object();

        private Socket _socket;
        private CancellationTokenSource _receiveCancellation;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="endpoint">Address of the server to connect to</param>
        public NetworkClient(IPEndPoint endpoint)
        {
            Id = Guid.NewGuid();
            Endpoint = endpoint;
        }

        /// <summary>
        ///     Client Id
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     Address of the server this client connects to
        /// </summary>
        public IPEndPoint Endpoint { get; }

        /// <summary>
        ///     Is the link open?
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        ///     Opens the link and starts reading it
        /// </summary>
        /// <param name="cancellationToken">Cancels the waiting for the connection</param>
        /// <returns>True when the link is open</returns>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                return true;
            }

            Socket socket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await socket.ConnectAsync(Endpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                socket.Dispose();
                OnError(e.SocketErrorCode);

                return false;
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();

                return false;
            }

            _socket = socket;
            _receiveCancellation = new CancellationTokenSource();
            IsConnected = true;

            OnConnected();

            // The reading lives on its own task: everything this link does afterwards is a reaction
            // to what comes in, and the caller is free to go on with its own work
            _ = Task.Run(() => ReceiveLoopAsync(socket, _receiveCancellation.Token));

            return true;
        }

        /// <summary>
        ///     Closes the link
        /// </summary>
        /// <returns>True when there was something to close</returns>
        public bool Disconnect()
        {
            Socket socket;
            CancellationTokenSource cancellation;

            lock (_sendLock)
            {
                if (!IsConnected)
                {
                    return false;
                }

                IsConnected = false;
                socket = _socket;
                cancellation = _receiveCancellation;
                _socket = null;
                _receiveCancellation = null;
            }

            cancellation?.Cancel();

            try
            {
                socket?.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
                // The other side is gone already, nothing left to shut down politely
            }
            catch (ObjectDisposedException)
            {
                // The socket is closed already
            }

            socket?.Dispose();
            cancellation?.Dispose();

            OnDisconnected();

            return true;
        }

        /// <summary>
        ///     Sends a whole frame
        /// </summary>
        /// <param name="buffer">Frame with its length prefix</param>
        /// <returns>True when the frame went into the socket</returns>
        public bool Send(byte[] buffer)
        {
            lock (_sendLock)
            {
                if (!IsConnected || _socket == null)
                {
                    return false;
                }

                try
                {
                    // The family link carries a handful of short packets, so the send is done in
                    // place: there is no queue to grow and no thread to wake up
                    _socket.Send(buffer, SocketFlags.None);

                    return true;
                }
                catch (SocketException e)
                {
                    OnError(e.SocketErrorCode);
                }
                catch (ObjectDisposedException)
                {
                    // The link was closed while this frame was on its way out
                }
            }

            // The link is broken, and the reading task is about to notice it as well
            Disconnect();

            return false;
        }

        /// <summary>
        ///     Reads the link until it breaks or is closed
        /// </summary>
        private async Task ReceiveLoopAsync(Socket socket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[ReceiveBufferSize];
            FrameReader frameReader = new FrameReader();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int received = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                    // The other side closed the link
                    if (received <= 0)
                    {
                        break;
                    }

                    if (!ProcessFrames(frameReader, buffer, received))
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Disconnect was asked for
            }
            catch (ObjectDisposedException)
            {
                // Disconnect closed the socket under the read
            }
            catch (SocketException e)
            {
                OnError(e.SocketErrorCode);
            }

            Disconnect();
        }

        /// <summary>
        ///     Hands every whole packet of the segment to the handler
        /// </summary>
        /// <returns>False when the link is not worth reading any more</returns>
        private bool ProcessFrames(FrameReader frameReader, byte[] buffer, int size)
        {
            frameReader.Append(buffer, 0, size);

            FrameReader.Frame broken = default;

            try
            {
                while (frameReader.TryRead(out FrameReader.Frame frame))
                {
                    if (frame.IsBroken)
                    {
                        broken = frame;
                        break;
                    }

                    OnReceived(frame.Payload, 0, frame.Payload.Length);

                    // The handler is allowed to close the link, the rest of the stream is not ours
                    if (!IsConnected)
                    {
                        return false;
                    }
                }
            }
            finally
            {
                frameReader.Compact();
            }

            if (broken.IsBroken && broken.Dropped > 0)
            {
                OnFramingError(broken.BrokenSize, broken.Dropped);

                // The stream cannot be resynchronized without a packet boundary
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Handle link opened notification
        /// </summary>
        protected virtual void OnConnected() { }

        /// <summary>
        ///     Handle link closed notification
        /// </summary>
        protected virtual void OnDisconnected() { }

        /// <summary>
        ///     Handle one whole packet without its length prefix
        /// </summary>
        protected virtual void OnReceived(byte[] buffer, long offset, long size) { }

        /// <summary>
        ///     Handle invalid packet length notification
        /// </summary>
        /// <param name="packetSize">Packet length taken from the stream</param>
        /// <param name="dropped">How many bytes were thrown away</param>
        protected virtual void OnFramingError(int packetSize, long dropped) { }

        /// <summary>
        ///     Handle error notification
        /// </summary>
        protected virtual void OnError(SocketError error) { }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}

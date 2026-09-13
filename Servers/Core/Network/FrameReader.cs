using System;

namespace Core.Network
{
    /// <summary>
    ///     Collects whole packets out of an incoming TCP stream. TCP gives a byte stream, so a
    ///     single receive may hold a piece of a packet, several packets or both: received bytes
    ///     are stored here until a whole packet is collected.
    ///
    ///     A packet starts with the little endian length prefix which includes the prefix itself,
    ///     so the payload handed out is the frame without its first two bytes.
    ///
    ///     Usage: <see cref="Append"/> the fresh segment, take frames with <see cref="TryRead"/>
    ///     until it says there are none, and call <see cref="Compact"/> in a finally - the frames
    ///     already handed out are dropped only then, so a handler that throws does not get the
    ///     same frames once again together with the next segment
    /// </summary>
    public class FrameReader
    {
        /// <summary>
        ///     Size of the packet length prefix, the prefix is a part of the declared length
        /// </summary>
        public const int HeaderSize = 2;

        /// <summary>
        ///     The biggest packet length the 2 bytes prefix is able to express
        /// </summary>
        public const int MaxFrameSize = short.MaxValue;

        private readonly Buffer _buffer = new Buffer();

        // Size of the accumulator part which is already taken apart and must be dropped
        private long _position;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="initialSize">
        ///     Start size of the accumulator, a couple of usual client packets. The socket receive
        ///     buffer is way bigger, and reserving it per session would cost megabytes on a crowded
        ///     server, while the accumulator grows by itself and never holds more than one frame
        ///     plus one segment
        /// </param>
        public FrameReader(int initialSize = 1024)
        {
            _buffer.Reserve(initialSize);
        }

        /// <summary>
        ///     Stores the fresh segment, the tail of the previous one is already here
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void Append(byte[] buffer, long offset, long size)
        {
            _buffer.Append(buffer, offset, size);
        }

        /// <summary>
        ///     Takes the next frame out of the accumulator
        /// </summary>
        /// <param name="frame">Next whole frame, or the report of a broken length prefix</param>
        /// <returns>False when the accumulator holds no whole frame any more</returns>
        public bool TryRead(out Frame frame)
        {
            frame = default;

            // Take packets while the accumulator holds the length prefix and the whole frame
            if (_buffer.Size - _position < HeaderSize)
            {
                return false;
            }

            int packetSize = BitConverter.ToUInt16(_buffer.Data, (int)_position);

            // The length is impossible, the stream is desynchronized and cannot be resynchronized
            // without a packet boundary, so the rest of the accumulator is dropped instead of
            // reading out of it
            if (packetSize < HeaderSize || packetSize > MaxFrameSize)
            {
                frame = Frame.Broken(packetSize, _buffer.Size - _position);
                _position = _buffer.Size;

                return true;
            }

            // The rest of the frame is still on the way, wait for the next segment
            if (_buffer.Size - _position < packetSize)
            {
                return false;
            }

            byte[] payload = new byte[packetSize - HeaderSize];
            Array.Copy(_buffer.Data, _position + HeaderSize, payload, 0, payload.Length);
            _position += packetSize;

            frame = Frame.Packet(payload);

            return true;
        }

        /// <summary>
        ///     Drops the frames already handed out and keeps the tail for the next segment
        /// </summary>
        public void Compact()
        {
            if (_position <= 0)
            {
                return;
            }

            _buffer.Remove(0, _position);
            _position = 0;
        }

        /// <summary>
        ///     One result of <see cref="TryRead"/>: either a whole frame or a broken length prefix
        /// </summary>
        public readonly struct Frame
        {
            private Frame(byte[] payload, int brokenSize, long dropped)
            {
                Payload = payload;
                BrokenSize = brokenSize;
                Dropped = dropped;
            }

            /// <summary>
            ///     Frame without its length prefix, null when the length prefix is broken
            /// </summary>
            public byte[] Payload { get; }

            /// <summary>
            ///     Length taken from the stream, filled only for a broken prefix
            /// </summary>
            public int BrokenSize { get; }

            /// <summary>
            ///     How many bytes were thrown away because of the broken prefix
            /// </summary>
            public long Dropped { get; }

            /// <summary>
            ///     The stream is desynchronized and everything left in the accumulator is gone
            /// </summary>
            public bool IsBroken => Payload == null;

            internal static Frame Packet(byte[] payload)
            {
                return new Frame(payload, 0, 0);
            }

            internal static Frame Broken(int brokenSize, long dropped)
            {
                return new Frame(null, brokenSize, dropped);
            }
        }
    }
}

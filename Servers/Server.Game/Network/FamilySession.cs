using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Core.Network;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Interfaces;
using Packets.Core.Utilities;

namespace Server.Game.Network
{
    /// <summary>
    ///     Link this field server keeps to a channel of its world. The channel serves the players
    ///     and the servers of the world on one and the same port, so the frames here are built the
    ///     way the client builds them: the length prefix, the crypt byte, the packet number and
    ///     the code. The traffic between the servers is never encrypted - the cipher of the
    ///     original is there for the client, and both ends of this link are ours
    /// </summary>
    public class FamilySession : NetworkClient
    {
        private readonly IRegisterHandlerService _registerHandlerService;
        private readonly ILogger _logger;
        private readonly Action<FamilySession> _onOpened;
        private readonly Action<FamilySession> _onClosed;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="endpoint">Address of the channel</param>
        /// <param name="svrNo">Number of the channel in TblParmSvr</param>
        /// <param name="registerHandlerService"></param>
        /// <param name="logger"></param>
        /// <param name="onOpened">Called when the link is up and the login of the server may go out</param>
        /// <param name="onClosed">Called when the link is gone</param>
        public FamilySession(IPEndPoint endpoint, short svrNo, IRegisterHandlerService registerHandlerService, ILogger logger, Action<FamilySession> onOpened, Action<FamilySession> onClosed) : base(endpoint)
        {
            ChannelSvrNo = svrNo;
            _registerHandlerService = registerHandlerService;
            _logger = logger;
            _onOpened = onOpened;
            _onClosed = onClosed;
        }

        /// <summary>
        ///     Number of the channel this link goes to, TblParmSvr.mSvrNo
        /// </summary>
        public short ChannelSvrNo { get; }

        /// <summary>
        ///     The channel has taken this server into the world: it answered the login of the server
        /// </summary>
        public bool IsLogined { get; set; }

        /// <summary>
        ///     Sends a packet to the channel
        /// </summary>
        /// <param name="model">Model with the packet attribute on it</param>
        public void Send(object model)
        {
            PacketType packetType = model.GetType().GetCustomAttribute<ModelAttribute>().PacketType;

            byte[] data = _registerHandlerService.Parse(packetType, model);

            FormationPackage bodyPackage = new FormationPackage();
            bodyPackage.AddByte(0x01); // TODO number package
            bodyPackage.AddShort((short)packetType);
            bodyPackage.AddBytes(data, 0, data.Length);

            FormationPackage formationPackage = new FormationPackage();
            formationPackage.AddByte(0x00);
            formationPackage.AddBytes(bodyPackage.GetBytes());
            formationPackage.AddShort((short)(formationPackage.Size + 2), begin: true);

            // base.Send on purpose: this class declares Send(object), which hides the inherited
            // Send(byte[]) by name, so a plain Send(bytes) would call this method again with the
            // frame as the model and throw on the missing packet attribute. The channel side sends
            // the very same way, see LoginSession.Send
            base.Send(formationPackage.GetBytes());
        }

        /// <inheritdoc/>
        protected override void OnConnected()
        {
            _logger.LogInformation("Family link to channel {SvrNo} on {Endpoint} is open", ChannelSvrNo, Endpoint);

            _onOpened(this);
        }

        /// <inheritdoc/>
        protected override void OnDisconnected()
        {
            IsLogined = false;

            _logger.LogWarning("Family link to channel {SvrNo} on {Endpoint} is gone", ChannelSvrNo, Endpoint);

            _onClosed(this);
        }

        /// <inheritdoc/>
        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                FormationPackage formationPackage = new FormationPackage(buffer, offset, size);

                byte checkCrypt = formationPackage.ReadByte();

                if (checkCrypt == 1)
                {
                    _logger.LogError("Channel {SvrNo} sent an encrypted frame over the family link", ChannelSvrNo);
                    return;
                }

                byte packetNumber = formationPackage.ReadByte();
                short packetId = formationPackage.ReadShort();

                if (!Enum.IsDefined(typeof(PacketType), packetId))
                {
                    _logger.LogError("Packet {PacketId} from channel {SvrNo} is not defined", packetId, ChannelSvrNo);
                    return;
                }

                PacketType packetType = (PacketType)packetId;

                // The channel greets every connection with the key block for the client, this link
                // has no use for it
                if (packetType == PacketType.ConnectionClient)
                {
                    return;
                }

                object parserModel = _registerHandlerService.Parse(packetType, formationPackage.GetBytes());

                if (parserModel == null)
                {
                    _logger.LogError("Model after parser is null for packet {PacketId} from channel {SvrNo}", packetId, ChannelSvrNo);
                    return;
                }

                _registerHandlerService.InvokeHandler(packetType, this, parserModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <inheritdoc/>
        protected override void OnFramingError(int packetSize, long dropped)
        {
            _logger.LogWarning("Broken packet length {PacketSize} on the family link to channel {SvrNo}, {Dropped} bytes dropped",
                packetSize, ChannelSvrNo, dropped);
        }

        /// <inheritdoc/>
        protected override void OnError(SocketError error)
        {
            _logger.LogInformation("Family link to channel {SvrNo} reports {Error}", ChannelSvrNo, error);
        }
    }
}

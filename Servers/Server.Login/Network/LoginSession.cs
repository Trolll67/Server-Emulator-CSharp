using System;
using System.Net.Sockets;
using System.Reflection;
using Core.Network;
using Core.Network.Cryptography;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Interfaces;
using Packets.Core.Utilities;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Login;
using Server.Login.Services.Family;

namespace Server.Login.Network
{
    /// <summary>
    ///     Network login session
    /// </summary>
    public class LoginSession : NetworkSession
    {
        private ILogger<LoginSession> _logger;
        private IAuthorizationFactory _authorizationFactory;
        private IRegisterHandlerService _registerHandlerService;
        private FamilyRegistry _familyRegistry;
        private CertificationRegistry _certificationRegistry;

        /// <summary>
        ///     Feature flag of LoginSetting.EncryptOutgoingPackets: the frames of this session leave
        ///     encrypted. Off by default, then the session sends what it always sent - the crypt
        ///     byte 0x00 and the plain frame
        /// </summary>
        private bool _encryptOutgoing;

        /// <summary>
        ///     The welcome packet already left this session. Until that moment the frames go in the
        ///     clear even with <see cref="_encryptOutgoing"/> on: welcome is the packet that hands
        ///     the key block to the client, so an encrypted one would be noise the client has
        ///     nothing to decrypt with, and the connection would die before the login even starts.
        ///     Written and read on the thread that serves the session
        /// </summary>
        private bool _welcomeSent;

        #region Properties for login session

        /// <summary>
        ///     Session login model
        /// </summary>
        public SessionLoginModel SessionLogin { get; set; }

        /// <summary>
        ///     Key part of the welcome block this session handed to the client, kept for later use.
        ///     Filled only with LoginSetting.GenerateSessionKey on, null otherwise - then the client
        ///     got the prepared static block and there is nothing session specific to remember.
        ///     The cipher of the session does not use it: BlowfishCrypt runs on its own static key,
        ///     and rekeying the cipher waits until the live client proves it reads the sent block
        /// </summary>
        public byte[] CipherKey { get; set; }

        /// <summary>
        ///     Number of the server of this world that opened the link, null while the session
        ///     belongs to a client. CSession::mFamily of the original: the channel port serves
        ///     both the players and the servers of the world, and this is what tells them apart
        /// </summary>
        public short? FamilySvrNo { get; set; }

        #endregion

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="server"></param>
        public LoginSession(LoginServer server) : base(server)
        {

        }

        /// <summary>
        ///     Inicialize services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="authorizationFactory"></param>
        /// <param name="registerHandlerService"></param>
        /// <param name="familyRegistry"></param>
        /// <param name="certificationRegistry"></param>
        /// <param name="encryptOutgoing">Value of LoginSetting.EncryptOutgoingPackets, see <see cref="_encryptOutgoing"/></param>
        public void InicializeServices(ILogger<LoginSession> logger, IAuthorizationFactory authorizationFactory, IRegisterHandlerService registerHandlerService, FamilyRegistry familyRegistry, CertificationRegistry certificationRegistry, bool encryptOutgoing)
        {
            _logger = logger;
            _authorizationFactory = authorizationFactory;
            _registerHandlerService = registerHandlerService;
            _familyRegistry = familyRegistry;
            _certificationRegistry = certificationRegistry;
            _encryptOutgoing = encryptOutgoing;
        }

        /// <summary>
        ///     Event connection new client
        /// </summary>
        protected override void OnConnected()
        {
            _logger.LogInformation($"New client connected {Id}");

            // Send welcome packet, always in the clear: it carries the key block itself
            _authorizationFactory.SendWelcome(this);

            // The client has the key block now, the rest of the session may be encrypted
            _welcomeSent = true;
        }

        /// <summary>
        ///     Event disconnected client
        /// </summary>
        protected override void OnDisconnected()
        {
            _logger.LogInformation($"Client disconnected {Id}");

            // A server of the world went off the line: the roster has to stop showing it to the
            // clients right away, and its slot has to be free for the server to come back to
            if (FamilySvrNo.HasValue)
            {
                _familyRegistry.Disconnected(FamilySvrNo.Value, this);
            }

            // The player either took the key to a game server or gave up on the login screen.
            // Either way the channel has nothing left to hold for this account
            if (SessionLogin != null)
            {
                _certificationRegistry.Released(SessionLogin.UserNo, this);
            }

            // Save account in database
        }

        /// <summary>
        ///     Handle receive data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                FormationPackage formationPackage = new FormationPackage(buffer, offset, size);

                byte checkCrypt = formationPackage.ReadByte();

                if (checkCrypt == 1)
                {
                    formationPackage = new FormationPackage(BlowfishCrypt.Decrypt(formationPackage.GetBytes()));
                }

                byte packetNumber = formationPackage.ReadByte();
                short packetId = formationPackage.ReadShort();

                // Check packet is exist in enum
                if (!Enum.IsDefined(typeof(PacketType), packetId))
                {
                    _logger.LogError($"Packet {packetId} is not defined");
                    return;
                }

                PacketType packetType = (PacketType)packetId;

                // Parse byte array to model
                object parserModel = _registerHandlerService.Parse(packetType, formationPackage.GetBytes());

                // Check model is null after parser
                if (parserModel == null)
                {
                    _logger.LogError($"Model after parser is null for packet {packetId}");
                    return;
                }

                // Handle parse model
                _registerHandlerService.InvokeHandler(packetType, this, parserModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        ///     Handle broken packet length in the incoming stream
        /// </summary>
        /// <param name="packetSize"></param>
        /// <param name="dropped"></param>
        protected override void OnFramingError(int packetSize, long dropped)
        {
            _logger.LogWarning($"Broken packet length {packetSize} at login session {Id}, {dropped} bytes dropped");

            // The stream is desynchronized forever, feeding the handlers with garbage is worse
            // than asking the client to connect again
            Disconnect();
        }

        /// <summary>
        ///     Send message to client
        /// </summary>
        /// <param name="model"></param>
        public void Send(object model)
        {
            PacketType packetType = model.GetType().GetCustomAttribute<ModelAttribute>().PacketType;

            // Parse model to byte array
            byte[] data = _registerHandlerService.Parse(packetType, model);

            // The part of the frame the crypt byte covers. On the incoming path the whole rest of
            // the frame after the crypt byte goes through the cipher - the packet number, the opcode
            // and the payload - so the outgoing one is built and encrypted exactly the same way
            FormationPackage bodyPackage = new FormationPackage();
            bodyPackage.AddByte(0x01); // TODO number package
            bodyPackage.AddShort((short)packetType);
            bodyPackage.AddBytes(data, 0, data.Length);

            byte[] body = bodyPackage.GetBytes();

            // Welcome is out of the flag, see _welcomeSent
            bool encrypt = _encryptOutgoing && _welcomeSent;

            // The cipher is a stream one and keeps the length, so the layout of the frame is the
            // same with the flag on and off: the length prefix, the crypt byte, then the body
            FormationPackage formationPackage = new FormationPackage();
            formationPackage.AddByte(encrypt ? (byte)0x01 : (byte)0x00);
            formationPackage.AddBytes(encrypt ? BlowfishCrypt.Encrypt(body) : body);
            formationPackage.AddShort((short)(formationPackage.Size + 2), begin: true);

            base.Send(formationPackage.GetBytes());
        }

        /// <summary>
        ///     Handle error exception
        /// </summary>
        /// <param name="error"></param>
        protected override void OnError(SocketError error)
        {
            _logger.LogInformation($"Have error at login session. ErrorType with code {error}");
        }
    }
}

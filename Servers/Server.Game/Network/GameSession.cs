using Core.Network;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Reflection;
using Core.Network.Cryptography;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Interfaces;
using Packets.Core.Utilities;
using Server.Game.Core.Factories.Interfaces;
using System;
using System.Threading;
using Server.Game.Services;
using Server.Game.Models.Game;
using System.Collections.Generic;

namespace Server.Game.Network
{
    /// <summary>
    ///     Where the session stands in the login sequence. The original keeps the same two marks
    ///     on the user object - the account is certified and the character is in the world - and
    ///     the packet handlers refuse to work when the session is not in the expected state
    /// </summary>
    public enum GameSessionState
    {
        /// <summary>
        ///     The socket is open, nothing else happened yet: the session has no account behind it
        /// </summary>
        Connected = 0,

        /// <summary>
        ///     UspLoginUser accepted the account, the client sits on the character selection screen
        ///     and no character is chosen yet
        /// </summary>
        LoggedIn = 1,

        /// <summary>
        ///     The chosen character is loaded and placed into the world, the client plays
        /// </summary>
        InWorld = 2
    }

    /// <summary>
    ///     Network game session
    /// </summary>
    public class GameSession : NetworkSession
    {
        private ILogger<GameSession> _logger;
        private IAuthorizationFactory _authorizationFactory;
        private IRegisterHandlerService _registerHandlerService;
        private IdentificationService _identificationService;
        private LogoutService _logoutService;

        /// <summary>
        ///     Feature flag of GameSetting.EncryptOutgoingPackets: the frames of this session leave
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

        #region Properties for game session
        /// <summary>
        ///     Session game model
        /// </summary>
        public GSession Sessions { get; set; }

        /// <summary>
        ///     Characters model
        /// </summary>
        public List<GPc> Pcs { get; set; }

        /// <summary>
        ///     Character game model
        /// </summary>
        public GPc Pc { get; set; }

        /// <summary>
        ///     Stage of the login sequence the session reached, see <see cref="GameSessionState"/>.
        ///     Moved only by <see cref="MarkLoggedIn"/>, <see cref="EnterWorld"/> and <see cref="LeaveWorld"/>.
        ///     Written both from the thread that serves the packets of this session and from the
        ///     disconnect, without synchronization: a guard may read InWorld on a socket that is
        ///     already dying, so the checks must survive that race and never assume the socket is alive
        /// </summary>
        public GameSessionState State { get; private set; }

        /// <summary>
        ///     The character of this session is loaded and placed into the world
        /// </summary>
        public bool IsInWorld => State == GameSessionState.InWorld;

        /// <summary>
        ///     Key part of the welcome block this session handed to the client, kept for later use.
        ///     Filled only with GameSetting.GenerateSessionKey on, null otherwise - then the client
        ///     got the prepared static block and there is nothing session specific to remember.
        ///     The cipher of the session does not use it: BlowfishCrypt runs on its own static key,
        ///     and rekeying the cipher waits until the live client proves it reads the sent block
        /// </summary>
        public byte[] CipherKey { get; set; }

        /// <summary>
        ///     The session is already logged out in the databases, see <see cref="TryBeginLogout"/>
        /// </summary>
        private int _isLoggedOut;
        #endregion

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="server"></param>
        public GameSession(GameServer server) : base(server)
        {

        }

        /// <summary>
        ///     Inicialize services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="authorizationFactory"></param>
        /// <param name="registerHandlerService"></param>
        /// <param name="identificationService"></param>
        /// <param name="logoutService"></param>
        /// <param name="encryptOutgoing">Value of GameSetting.EncryptOutgoingPackets, see <see cref="_encryptOutgoing"/></param>
        public void InicializeServices(ILogger<GameSession> logger, IAuthorizationFactory authorizationFactory, IRegisterHandlerService registerHandlerService, IdentificationService identificationService, LogoutService logoutService, bool encryptOutgoing)
        {
            _logger = logger;
            _authorizationFactory = authorizationFactory;
            _registerHandlerService = registerHandlerService;
            _identificationService = identificationService;
            _logoutService = logoutService;
            _encryptOutgoing = encryptOutgoing;
        }

        /// <summary>
        ///     The account passed UspLoginUser: the session may ask for the selection screen
        ///     and choose a character
        /// </summary>
        public void MarkLoggedIn()
        {
            State = GameSessionState.LoggedIn;
        }

        /// <summary>
        ///     The character is loaded and the client already got 5117: from here the session
        ///     lives in the world and the world packets are allowed
        /// </summary>
        public void EnterWorld()
        {
            State = GameSessionState.InWorld;
        }

        /// <summary>
        ///     The session leaves the world - the entry failed and was rolled back, the character
        ///     logged out or the socket died. The account itself stays certified, so the session
        ///     falls back to the selection screen state
        /// </summary>
        public void LeaveWorld()
        {
            if (State == GameSessionState.InWorld)
            {
                State = GameSessionState.LoggedIn;
            }
        }

        /// <summary>
        ///     Claims the right to log the session out in the databases. The LogoutPcReq handler
        ///     and the disconnect can race on the same session from different threads, so only
        ///     the first caller receives true
        /// </summary>
        public bool TryBeginLogout()
        {
            return Interlocked.Exchange(ref _isLoggedOut, 1) == 0;
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

            // Drop the world state first: the guards of the world packets must stop letting
            // this session through before its character is written out. The loops that walk
            // the sessions are stopped by RemoveConnection below
            LeaveWorld();

            // Remove session in store
            _identificationService.RemoveConnection(this);

            // Save the character and clear the login mark in the databases (UspLogoutPc, UspLogoutUser)
            _logoutService.Logout(this);
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

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Received {PacketType} ({PacketId}), {Size} bytes", packetType, packetId, size);

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
            _logger.LogWarning($"Broken packet length {packetSize} at game session {Id}, {dropped} bytes dropped");

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

            // Клиент падает молча, а сервер об успешной отправке ничего не пишет: без этой строки
            // не видно, какой пакет он получил последним
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Sent {PacketType} ({PacketId}), {Size} bytes, payload {PayloadSize}", packetType, (short)packetType, formationPackage.Size, data.Length);

            base.Send(formationPackage.GetBytes());
        }

        public void SendOnlyBytesForDelevop(byte[] data)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Sent a recorded packet, {Size} bytes", data.Length);

            base.Send(data);
        }

        /// <summary>
        ///     Handle error exception
        /// </summary>
        /// <param name="error"></param>
        protected override void OnError(SocketError error)
        {
            _logger.LogInformation($"Have error at game session. ErrorType with code {error}");
        }
    }
}

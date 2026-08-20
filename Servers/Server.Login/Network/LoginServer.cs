using System;
using System.Net;
using System.Net.Sockets;
using Core.Network;
using Database.Fnl.Parm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Interfaces;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Settings;

namespace Server.Login.Network
{
    /// <summary>
    ///     Network login server
    /// </summary>
    public class LoginServer : NetworkServer
    {
        private readonly ILogger<LoginServer> _logger;
        private readonly ILogger<LoginSession> _loggerSession;
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly IRegisterHandlerService _registerHandlerService;
        private readonly LoginSetting _loginSetting;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="loggerSession"></param>
        /// <param name="authorizationFactory"></param>
        /// <param name="registerHandlerService"></param>
        /// <param name="loginSetting"></param>
        public LoginServer(ILogger<LoginServer> logger, ILogger<LoginSession> loggerSession, IAuthorizationFactory authorizationFactory, IRegisterHandlerService registerHandlerService, IFnlParmRepository parmRepository, IOptions<LoginSetting> loginSetting) : base(IPAddress.Parse(loginSetting.Value.ServerIp), 0)
        {
            _logger = logger;
            _loggerSession = loggerSession;
            _authorizationFactory = authorizationFactory;
            _registerHandlerService = registerHandlerService;
            _loginSetting = loginSetting.Value;

            ResolveListenPort(parmRepository, loginSetting.Value);

            WarnOnConflictingCryptoSettings();
        }

        /// <summary>
        ///     Both feature flags at once contradict each other: the welcome block is generated per
        ///     connection, while the traffic cipher still runs on the static key of BlowfishCrypt.
        ///     The client then drops the connection with nothing in the log to explain it, so at
        ///     least the reason is written out at start
        /// </summary>
        private void WarnOnConflictingCryptoSettings()
        {
            if (_loginSetting.GenerateSessionKey && _loginSetting.EncryptOutgoingPackets)
            {
                _logger.LogWarning("LoginSetting.GenerateSessionKey and LoginSetting.EncryptOutgoingPackets are both on: the client gets a generated key block while the outgoing traffic is encrypted with the static key, so it will most likely drop the connection. Leave one of the flags off");
            }
        }

        /// <summary>
        ///     Finds this server in TblParmSvr by its address and kind (channel), and binds to the
        ///     port stored there. TblParmSvr is the only source of the port: the same table tells the
        ///     client where to connect, so a port taken from anywhere else would only be reachable
        ///     by accident. A missing or empty row stops the server instead of silently listening
        ///     somewhere the client will never look
        /// </summary>
        private void ResolveListenPort(IFnlParmRepository parmRepository, LoginSetting setting)
        {
            ParmServerRow own = parmRepository.GetParmSvr(ParmServerType.Channel, setting.ServerIp);

            if (own == null)
            {
                throw Fatal($"TblParmSvr has no channel server (mType = {(byte)ParmServerType.Channel}) with mMajorIp = '{setting.ServerIp}'. " +
                            "Add the row, or point \"LoginSetting:ServerIp\" at the address the channel is registered under");
            }

            if (own.TcpPort <= 0)
            {
                throw Fatal($"Channel server {own.SvrNo} on {setting.ServerIp} has no listen port: TblParmSvr.mTcpPort is {own.TcpPort}");
            }

            UpdateEndpoint(new IPEndPoint(IPAddress.Parse(setting.ServerIp), own.TcpPort));
            _logger.LogInformation("Resolved listen port {Port} from TblParmSvr (channel server {SvrNo} on {Ip})", own.TcpPort, own.SvrNo, setting.ServerIp);
        }

        /// <summary>
        ///     Logs the reason and builds the exception that stops the host
        /// </summary>
        private InvalidOperationException Fatal(string message)
        {
            string text = "Login server cannot start: " + message;

            _logger.LogCritical(text);

            return new InvalidOperationException(text);
        }

        /// <summary>
        ///     Create new session for login server
        /// </summary>
        /// <returns></returns>
        protected override NetworkSession CreateSession()
        {
            LoginSession loginSession = new LoginSession(this);
            loginSession.InicializeServices(_loggerSession, _authorizationFactory, _registerHandlerService, _loginSetting.EncryptOutgoingPackets);

            return loginSession;
        }

        /// <summary>
        ///     Handle error exception
        /// </summary>
        /// <param name="error"></param>
        protected override void OnError(SocketError error)
        {
            _logger.LogInformation($"Have error at login server. ErrorType with code {error}");
        }
    }
}

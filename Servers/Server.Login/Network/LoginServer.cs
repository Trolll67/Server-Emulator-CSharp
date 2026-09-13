using System.Net;
using System.Net.Sockets;
using Core.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Interfaces;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Settings;
using Server.Login.Services;
using Server.Login.Services.Family;

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
        private readonly FamilyRegistry _familyRegistry;
        private readonly LoginSetting _loginSetting;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="loggerSession"></param>
        /// <param name="authorizationFactory"></param>
        /// <param name="registerHandlerService"></param>
        /// <param name="familyRegistry"></param>
        /// <param name="ownChannelInfo"></param>
        /// <param name="loginSetting"></param>
        public LoginServer(ILogger<LoginServer> logger, ILogger<LoginSession> loggerSession, IAuthorizationFactory authorizationFactory, IRegisterHandlerService registerHandlerService, FamilyRegistry familyRegistry, OwnChannelInfo ownChannelInfo, IOptions<LoginSetting> loginSetting) : base(IPAddress.Parse(loginSetting.Value.ServerIp), 0)
        {
            _logger = logger;
            _loggerSession = loggerSession;
            _authorizationFactory = authorizationFactory;
            _registerHandlerService = registerHandlerService;
            _familyRegistry = familyRegistry;
            _loginSetting = loginSetting.Value;

            // TblParmSvr is the only source of the port: the same table tells the client where to
            // connect, so a port taken from anywhere else would only be reachable by accident
            UpdateEndpoint(new IPEndPoint(IPAddress.Parse(ownChannelInfo.ServerIp), ownChannelInfo.TcpPort));

            WarnOnConflictingCryptoSettings();
        }

        /// <summary>
        ///     How many players are on the channel right now. The links of the servers of this world
        ///     come to the very same port and are not players, so they are left out of the count
        /// </summary>
        public int ClientSessions
        {
            get
            {
                int count = 0;

                foreach (NetworkSession session in Sessions.Values)
                {
                    if (session is LoginSession loginSession && !loginSession.FamilySvrNo.HasValue)
                    {
                        count++;
                    }
                }

                return count;
            }
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
        ///     Create new session for login server
        /// </summary>
        /// <returns></returns>
        protected override NetworkSession CreateSession()
        {
            LoginSession loginSession = new LoginSession(this);
            loginSession.InicializeServices(_loggerSession, _authorizationFactory, _registerHandlerService, _familyRegistry, _loginSetting.EncryptOutgoingPackets);

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

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

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="loggerSession"></param>
        /// <param name="authorizationFactory"></param>
        /// <param name="registerHandlerService"></param>
        /// <param name="loginSetting"></param>
        public LoginServer(ILogger<LoginServer> logger, ILogger<LoginSession> loggerSession, IAuthorizationFactory authorizationFactory, IRegisterHandlerService registerHandlerService, IFnlParmRepository parmRepository, IOptions<LoginSetting> loginSetting) : base(IPAddress.Parse(loginSetting.Value.ServerIp), loginSetting.Value.ServerPort)
        {
            _logger = logger;
            _loggerSession = loggerSession;
            _authorizationFactory = authorizationFactory;
            _registerHandlerService = registerHandlerService;

            ResolveListenPort(parmRepository, loginSetting.Value);
        }

        /// <summary>
        ///     Finds this server in TblParmSvr by its address and kind (channel), and binds to the
        ///     port stored there. Falls back to the port from loginsettings.json when there is no
        ///     matching row, so a misconfigured database does not prevent the server from starting.
        /// </summary>
        private void ResolveListenPort(IFnlParmRepository parmRepository, LoginSetting setting)
        {
            ParmServerRow own = parmRepository.GetParmSvr(ParmServerType.Channel, setting.ServerIp);

            if (own != null && own.TcpPort > 0)
            {
                UpdateEndpoint(new IPEndPoint(IPAddress.Parse(setting.ServerIp), own.TcpPort));
                _logger.LogInformation("Resolved listen port {Port} from TblParmSvr (channel server {SvrNo} on {Ip})", own.TcpPort, own.SvrNo, setting.ServerIp);

                return;
            }

            _logger.LogWarning("TblParmSvr has no channel server on {Ip}, using port {Port} from loginsettings.json", setting.ServerIp, setting.ServerPort);
        }

        /// <summary>
        ///     Create new session for login server
        /// </summary>
        /// <returns></returns>
        protected override NetworkSession CreateSession()
        {
            LoginSession loginSession = new LoginSession(this);
            loginSession.InicializeServices(_loggerSession, _authorizationFactory, _registerHandlerService);

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

using System;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Login.Models.Receive;
using Packets.Server.Login.Models.Send;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Core.Handlers.Interfaces;
using Server.Login.Models.Login;
using Server.Login.Network;

namespace Server.Login.Core.Handlers
{
    /// <inheritdoc />
    [Handler]
    public class ServersHandler : IServersHandler
    {
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly IServersFactory _serversFactory;
        private readonly ILogger<ServersHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="authorizationFactory"></param>
        /// <param name="serversFactory"></param>
        /// <param name="logger"></param>
        public ServersHandler(IAuthorizationFactory authorizationFactory, IServersFactory serversFactory, ILogger<ServersHandler> logger)
        {
            _authorizationFactory = authorizationFactory;
            _serversFactory = serversFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.ArsAuthReq)]
        public void ArsAuthHandle(LoginSession loginSession, ArsAuthReqModel arsAuthReqModel)
        {
            SessionLoginModel sessionLogin = loginSession.SessionLogin;

            // The client repeats the account it was certified with, both values have to match the session
            if (sessionLogin == null || sessionLogin.UserNo != arsAuthReqModel.AccountId ||
                !string.Equals(sessionLogin.UserId, arsAuthReqModel.Login, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Packet 3120 from an unknown session: it names mUserNo {arsAuthReqModel.AccountId} and login {arsAuthReqModel.Login}, the session holds {(sessionLogin == null ? "nothing" : sessionLogin.UserNo + " and " + sessionLogin.UserId)}");

                _authorizationFactory.SendError(loginSession, ServerErrorType.NoUser);
                return;
            }

            // The factory answers from the same list it sent in 3101, no second read of FNLParm
            if (!_serversFactory.IsKnownServer(arsAuthReqModel.ServerId))
            {
                _logger.LogWarning($"Account {sessionLogin.UserId} chose the server {arsAuthReqModel.ServerId}, which is not in the family list sent in 3101");

                _authorizationFactory.SendError(loginSession, ServerErrorType.IncorrectServer);
                return;
            }

            // The packet carries no session key: the key is issued once in CertifyUser and goes
            // to the client in 3101, and 3121 has room for nothing but the confirmation state
            // (see 3121_ArsAuthAck.cs)

            _serversFactory.SendArsAuth(loginSession);
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.RefreshServers)]
        public void RefreshServersHandle(LoginSession loginSession, RefreshServersModel refreshServersModel)
        {
            _serversFactory.SendRefreshedServers(loginSession);
        }
    }
}

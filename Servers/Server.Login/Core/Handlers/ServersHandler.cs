using System;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;
using Packets.Server.Login.Models.Receive;
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

            // A link of a server of this world has no business asking this, and the original
            // refuses a session that already belongs to one
            if (loginSession.FamilySvrNo.HasValue)
            {
                _logger.LogWarning("Server {SvrNo} asks about the phone confirmation as if it were a player", loginSession.FamilySvrNo);

                _authorizationFactory.SendNak(loginSession, PacketType.ArsAuthReq, NakErrorType.NoUserAlreadyLogined);
                return;
            }

            // The client repeats the account it was certified with, both values have to match the session
            if (sessionLogin == null || sessionLogin.UserNo != arsAuthReqModel.AccountId ||
                !string.Equals(sessionLogin.UserId, arsAuthReqModel.Login, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Packet 3120 from an unknown session: it names mUserNo {arsAuthReqModel.AccountId} and login {arsAuthReqModel.Login}, the session holds {(sessionLogin == null ? "nothing" : sessionLogin.UserNo + " and " + sessionLogin.UserId)}");

                _authorizationFactory.SendNak(loginSession, PacketType.ArsAuthReq, NakErrorType.NoUserNotLogin);
                return;
            }

            // The chosen server is looked up in the roster of this world, the one the list was
            // built from. A refusal here goes out as the common packet of a refusal, not as the
            // one of the login screen: the login itself has already gone through
            if (!_serversFactory.IsKnownServer(arsAuthReqModel.ServerId))
            {
                _logger.LogWarning($"Account {sessionLogin.UserId} chose the server {arsAuthReqModel.ServerId}, which is not in this world");

                _authorizationFactory.SendNak(loginSession, PacketType.ArsAuthReq, NakErrorType.FamilyNot);
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

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
        [HandlerAction(PacketType.SelectServer)]
        public void SelectServerHandle(LoginSession loginSession, SelectServerModel selectServerModel)
        {
            SessionLoginModel sessionLogin = loginSession.SessionLogin;

            // The client repeats the account it was certified with, both values have to match the session
            if (sessionLogin == null || sessionLogin.UserNo != selectServerModel.AccountId ||
                !string.Equals(sessionLogin.UserId, selectServerModel.Login, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Packet 3120 from an unknown session: it names mUserNo {selectServerModel.AccountId} and login {selectServerModel.Login}, the session holds {(sessionLogin == null ? "nothing" : sessionLogin.UserNo + " and " + sessionLogin.UserId)}");

                _authorizationFactory.SendError(loginSession, ServerErrorType.NoUser);
                return;
            }

            // The factory answers from the same list it sent in 3101, no second read of FNLParm
            if (!_serversFactory.IsKnownServer(selectServerModel.ServerId))
            {
                _logger.LogWarning($"Account {sessionLogin.UserId} chose the server {selectServerModel.ServerId}, which is not in the family list sent in 3101");

                _authorizationFactory.SendError(loginSession, ServerErrorType.IncorrectServer);
                return;
            }

            // Ключ сессии выдаётся один раз в CertifyUser и уходит клиенту в 3101. Перевыпуск здесь
            // невозможен: пакет 3121 поля для ключа не имеет (см. 3121_SelectedServer.cs), клиент
            // остался бы со старым значением.

            _serversFactory.SendSelectedServer(loginSession);
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.RefreshServers)]
        public void RefreshServersHandle(LoginSession loginSession, RefreshServersModel refreshServersModel)
        {
            _serversFactory.SendRefreshedServers(loginSession);
        }
    }
}

using System;
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

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="authorizationFactory"></param>
        /// <param name="serversFactory"></param>
        public ServersHandler(IAuthorizationFactory authorizationFactory, IServersFactory serversFactory)
        {
            _authorizationFactory = authorizationFactory;
            _serversFactory = serversFactory;
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
                _authorizationFactory.SendError(loginSession, ServerErrorType.NoUser);
                return;
            }

            // The factory answers from the same list it sent in 3101, no second read of FNLParm
            if (!_serversFactory.IsKnownServer(selectServerModel.ServerId))
            {
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

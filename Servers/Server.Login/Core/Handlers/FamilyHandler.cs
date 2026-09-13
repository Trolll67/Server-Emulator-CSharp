using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Family;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Core.Handlers.Interfaces;
using Server.Login.Network;
using Server.Login.Services.Family;

namespace Server.Login.Core.Handlers
{
    /// <inheritdoc />
    [Handler]
    public class FamilyHandler : IFamilyHandler
    {
        private readonly FamilyRegistry _familyRegistry;
        private readonly IFamilyFactory _familyFactory;
        private readonly ILogger<FamilyHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="familyRegistry"></param>
        /// <param name="familyFactory"></param>
        /// <param name="logger"></param>
        public FamilyHandler(FamilyRegistry familyRegistry, IFamilyFactory familyFactory, ILogger<FamilyHandler> logger)
        {
            _familyRegistry = familyRegistry;
            _familyFactory = familyFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.LoginFamilyReq)]
        public void LoginFamilyHandle(LoginSession loginSession, LoginFamilyReqModel loginFamilyReqModel)
        {
            // A link that already belongs to a server of the world, or a client session that has
            // already logged in, has no business asking for this
            if (loginSession.FamilySvrNo.HasValue || loginSession.SessionLogin != null)
            {
                _logger.LogWarning("Session {Session} asks to join the family twice, it is already server {SvrNo}",
                    loginSession.Id, loginSession.FamilySvrNo);

                _familyFactory.SendLoginFamilyNak(loginSession, loginFamilyReqModel.SvrNo, FamilyErrorType.FamilyAlreadyConnect);
                return;
            }

            FamilyErrorType? error = _familyRegistry.Connected(loginFamilyReqModel.SvrNo, loginSession);

            if (error.HasValue)
            {
                _familyFactory.SendLoginFamilyNak(loginSession, loginFamilyReqModel.SvrNo, error.Value);

                // The link is of no use to either side now, and leaving it open would keep a
                // stranger sitting on the channel port
                loginSession.Disconnect();
                return;
            }

            // The answer names this channel, so that the other side learns who is on this end
            // of the link. The original sends it even after a refusal, which leaves the refused
            // server believing it is in the world; here a refusal ends with the link closed and
            // the server trying again from the start
            _familyFactory.SendLoginFamilyAck(loginSession);
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.NotifySvrStateAck)]
        public void NotifySvrStateHandle(LoginSession loginSession, NotifySvrStateAckModel notifySvrStateAckModel)
        {
            // Only the server itself is allowed to say how loaded it is, and only over its own link
            if (loginSession.FamilySvrNo != notifySvrStateAckModel.SvrNo)
            {
                _logger.LogWarning("Session {Session} reports the state of server {SvrNo} while it is {Own}",
                    loginSession.Id, notifySvrStateAckModel.SvrNo, loginSession.FamilySvrNo);

                return;
            }

            _familyRegistry.SetState(notifySvrStateAckModel.SvrNo, notifySvrStateAckModel.MaxSesCnt, notifySvrStateAckModel.BusySesCnt);
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.KeepAliveNullReq)]
        public void KeepAliveHandle(LoginSession loginSession, KeepAliveNullReqModel keepAliveNullReqModel)
        {
            // Nothing to answer: the packet has done its job by arriving
        }
    }
}

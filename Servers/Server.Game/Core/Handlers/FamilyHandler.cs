using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Family;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Network;

namespace Server.Game.Core.Handlers
{
    /// <inheritdoc />
    [Handler]
    public class FamilyHandler : IFamilyHandler
    {
        private readonly ILogger<FamilyHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger"></param>
        public FamilyHandler(ILogger<FamilyHandler> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.LoginFamilyAck)]
        public void LoginFamilyAckHandle(FamilySession familySession, LoginFamilyAckModel loginFamilyAckModel)
        {
            familySession.IsLogined = true;

            _logger.LogInformation("Channel {SvrNo} took this server into the world, the players see it now", loginFamilyAckModel.SvrNo);
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.LoginFamilyNak)]
        public void LoginFamilyNakHandle(FamilySession familySession, LoginFamilyNakModel loginFamilyNakModel)
        {
            familySession.IsLogined = false;

            // The reason is almost always a row of TblParmSvr that does not match the machine the
            // server actually runs on, so it is worth naming out loud
            _logger.LogError("Channel {Channel} refused server {SvrNo}: {Reason}. Check the row of this server in TblParmSvr",
                familySession.ChannelSvrNo, loginFamilyNakModel.SvrNo, loginFamilyNakModel.Error);
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.NotifySvrStateAck)]
        public void NotifySvrStateHandle(FamilySession familySession, NotifySvrStateAckModel notifySvrStateAckModel)
        {
            // Nothing on the field server depends on how loaded the channel is yet, but the packet
            // belongs to the link and arrives on its own timer
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.KeepAliveNullReq)]
        public void KeepAliveHandle(FamilySession familySession, KeepAliveNullReqModel keepAliveNullReqModel)
        {
            // Nothing to answer: the packet has done its job by arriving
        }
    }
}

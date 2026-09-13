using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;
using Packets.Core.Models.Family;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Network;

namespace Server.Game.Core.Handlers
{
    /// <inheritdoc />
    [Handler]
    public class FamilyHandler : IFamilyHandler
    {
        private readonly GameServer _gameServer;
        private readonly ILogger<FamilyHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="gameServer"></param>
        /// <param name="logger"></param>
        public FamilyHandler(GameServer gameServer, ILogger<FamilyHandler> logger)
        {
            _gameServer = gameServer;
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

        /// <inheritdoc />
        [HandlerAction(PacketType.KickPcReq)]
        public void KickPcHandle(FamilySession familySession, KickPcReqModel kickPcReqModel)
        {
            // Throwing a player out by the name of the character is what the manager of the
            // original does, and there is no manager here to ask for it
            if (kickPcReqModel.IsPc)
            {
                _logger.LogWarning("Channel {Channel} asks to throw out the character {Name}, only accounts are thrown out here",
                    familySession.ChannelSvrNo, kickPcReqModel.PcName);

                return;
            }

            GameSession session = _gameServer.FindByAccount(kickPcReqModel.UserNo);

            if (session == null)
            {
                // The account has already left by itself, which is the usual end of a second login
                _logger.LogInformation("Channel {Channel} asks to throw account {UserNo} out, it is not on this server",
                    familySession.ChannelSvrNo, kickPcReqModel.UserNo);

                return;
            }

            _logger.LogInformation("Account {UserNo} is thrown out at the ask of channel {Channel}",
                kickPcReqModel.UserNo, familySession.ChannelSvrNo);

            // The player is told why before the world lets go of them, exactly in this order:
            // the original sends the notice and closes the session right after it
            session.Send(new KickPcAckModel { Reason = kickPcReqModel.Reason });
            session.Disconnect();
        }
    }
}

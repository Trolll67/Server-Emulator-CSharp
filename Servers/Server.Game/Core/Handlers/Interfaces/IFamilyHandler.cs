using Packets.Core.Models.Family;
using Server.Game.Network;

namespace Server.Game.Core.Handlers.Interfaces
{
    /// <summary>
    ///     Handler of the packets a channel of this world sends to the field server
    /// </summary>
    public interface IFamilyHandler
    {
        /// <summary>
        ///     The channel names itself back: this server is on the line of the world now
        /// </summary>
        /// <param name="familySession"></param>
        /// <param name="loginFamilyAckModel"></param>
        void LoginFamilyAckHandle(FamilySession familySession, LoginFamilyAckModel loginFamilyAckModel);

        /// <summary>
        ///     The channel refused to take this server into the world
        /// </summary>
        /// <param name="familySession"></param>
        /// <param name="loginFamilyNakModel"></param>
        void LoginFamilyNakHandle(FamilySession familySession, LoginFamilyNakModel loginFamilyNakModel);

        /// <summary>
        ///     The channel tells how loaded it is
        /// </summary>
        /// <param name="familySession"></param>
        /// <param name="notifySvrStateAckModel"></param>
        void NotifySvrStateHandle(FamilySession familySession, NotifySvrStateAckModel notifySvrStateAckModel);

        /// <summary>
        ///     The channel pings the link
        /// </summary>
        /// <param name="familySession"></param>
        /// <param name="keepAliveNullReqModel"></param>
        void KeepAliveHandle(FamilySession familySession, KeepAliveNullReqModel keepAliveNullReqModel);

        /// <summary>
        ///     The channel asks to throw an account out of this world
        /// </summary>
        /// <param name="familySession"></param>
        /// <param name="kickPcReqModel"></param>
        void KickPcHandle(FamilySession familySession, KickPcReqModel kickPcReqModel);
    }
}

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
        ///     A ping of the link. It comes over the link of a channel and from a game client alike,
        ///     and those two sessions share no base type, so the session is taken as an object
        /// </summary>
        /// <param name="session">FamilySession of a channel or GameSession of a player</param>
        /// <param name="keepAliveNullReqModel"></param>
        void KeepAliveHandle(object session, KeepAliveNullReqModel keepAliveNullReqModel);

        /// <summary>
        ///     The channel asks to throw an account out of this world
        /// </summary>
        /// <param name="familySession"></param>
        /// <param name="kickPcReqModel"></param>
        void KickPcHandle(FamilySession familySession, KickPcReqModel kickPcReqModel);
    }
}

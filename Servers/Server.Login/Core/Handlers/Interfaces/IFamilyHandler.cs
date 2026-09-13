using Packets.Core.Models.Family;
using Server.Login.Network;

namespace Server.Login.Core.Handlers.Interfaces
{
    /// <summary>
    ///     Handler of the packets the servers of this world send to the channel
    /// </summary>
    public interface IFamilyHandler
    {
        /// <summary>
        ///     A server of the world asks to be taken onto the line
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="loginFamilyReqModel"></param>
        void LoginFamilyHandle(LoginSession loginSession, LoginFamilyReqModel loginFamilyReqModel);

        /// <summary>
        ///     A server of the world tells how loaded it is
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="notifySvrStateAckModel"></param>
        void NotifySvrStateHandle(LoginSession loginSession, NotifySvrStateAckModel notifySvrStateAckModel);

        /// <summary>
        ///     A server of the world pings the link
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="keepAliveNullReqModel"></param>
        void KeepAliveHandle(LoginSession loginSession, KeepAliveNullReqModel keepAliveNullReqModel);
    }
}

using Packets.Server.Login.Models.Receive;
using Server.Login.Network;

namespace Server.Login.Core.Handlers.Interfaces
{
    /// <summary>
    ///     Servers handler
    /// </summary>
    public interface IServersHandler
    {
        /// <summary>
        ///     Phone confirmation handle: the client asks it about the server it has chosen
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="arsAuthReqModel"></param>
        void ArsAuthHandle(LoginSession loginSession, ArsAuthReqModel arsAuthReqModel);

        /// <summary>
        ///     Refresh servers handle
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="refreshServersModel"></param>
        void RefreshServersHandle(LoginSession loginSession, RefreshServersModel refreshServersModel);
    }
}

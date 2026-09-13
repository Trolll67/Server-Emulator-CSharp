using Packets.Core.Models.Family;
using Server.Login.Network;

namespace Server.Login.Core.Factories.Interfaces
{
    /// <summary>
    ///     Factory of the packets this channel sends to the servers of its world
    /// </summary>
    public interface IFamilyFactory
    {
        /// <summary>
        ///     Names this channel back to a server that has just joined the world
        /// </summary>
        /// <param name="loginSession"></param>
        void SendLoginFamilyAck(LoginSession loginSession);

        /// <summary>
        ///     Refuses a server that asked to join the world
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="svrNo">Number the server named itself with</param>
        /// <param name="error">Why it was refused</param>
        void SendLoginFamilyNak(LoginSession loginSession, short svrNo, FamilyErrorType error);

        /// <summary>
        ///     Pings every server of the world so that an unused link does not look like a broken one
        /// </summary>
        void BroadcastKeepAlive();

        /// <summary>
        ///     Tells the world how loaded this channel is
        /// </summary>
        void BroadcastOwnState();
    }
}

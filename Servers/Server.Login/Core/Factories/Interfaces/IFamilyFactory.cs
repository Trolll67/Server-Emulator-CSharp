using Packets.Core.Models.Common;
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
        ///     Asks the game servers of the world to throw an account out. The channel sends it
        ///     when the same account logs in a second time while it is still playing somewhere
        /// </summary>
        /// <param name="userNo">Account to throw out</param>
        /// <param name="addressNumber">Address the second login came from</param>
        /// <param name="reason">Why, the player is shown the text of it</param>
        void SendKick(int userNo, long addressNumber, NakErrorType reason);

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

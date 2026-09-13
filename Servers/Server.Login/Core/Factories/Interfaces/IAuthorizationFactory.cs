using Packets.Core.Enums;
using Packets.Core.Models.Common;
using Packets.Server.Login.Models.Send;
using Server.Login.Network;

namespace Server.Login.Core.Factories.Interfaces
{
    /// <summary>
    ///     Factory of auth packets
    /// </summary>
    public interface IAuthorizationFactory
    {
        /// <summary>
        ///     Sends welcome packet
        /// </summary>
        /// <param name="loginSession"></param>
        void SendWelcome(LoginSession loginSession);

        /// <summary>
        ///     Refuses a login with the packet of the login screen
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="error">Why the login was refused</param>
        void SendError(LoginSession loginSession, NakErrorType error);

        /// <summary>
        ///     Refuses a login with a refusal that carries more than the reason: the moment a ban
        ///     ends and the text the database has for it
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="loginServerErrorModel"></param>
        void SendError(LoginSession loginSession, LoginServerErrorModel loginServerErrorModel);

        /// <summary>
        ///     Refuses a request with the common packet of a refusal, the one every server of the
        ///     original answers with when the request is not the login itself
        /// </summary>
        /// <param name="loginSession"></param>
        /// <param name="requestType">Packet that was refused, the client is told which one</param>
        /// <param name="error">Why it was refused</param>
        void SendNak(LoginSession loginSession, PacketType requestType, NakErrorType error);
    }
}
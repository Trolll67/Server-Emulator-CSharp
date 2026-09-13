using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Login.Models.Send
{
    /// <summary>
    ///     Answers whether the chosen server wants a phone confirmation of the login.
    ///     CTrARSAuthAck of the original: one state field and nothing else
    /// </summary>
    [Model(PacketType.ArsAuthAck)]
    public class ArsAuthAckModel
    {
        public ArsAuthState State { get; set; } = ArsAuthState.None;
    }

    /// <summary>
    ///     EARSAuthState of the original. With the phone confirmation turned off the original
    ///     answers None, and the client goes on to the game server
    /// </summary>
    public enum ArsAuthState
    {
        /// <summary>
        ///     Nothing to confirm
        /// </summary>
        None = 0,

        /// <summary>
        ///     The call has been placed, the client waits
        /// </summary>
        Asking = 1,

        /// <summary>
        ///     The confirmation failed
        /// </summary>
        Nak = 2,

        /// <summary>
        ///     Confirmed earlier in this session
        /// </summary>
        Success = 3
    }
}
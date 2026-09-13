using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Common
{
    /// <summary>
    ///     Tells a player why the world is about to let go of them. The original sends it right
    ///     before it closes the session. CTrKickPcAck2 of the original
    /// </summary>
    [Model(PacketType.KickPcAck)]
    public class KickPcAckModel
    {
        /// <summary>
        ///     Why. The client looks the text up by the group and the number of the reason
        /// </summary>
        public NakErrorType Reason { get; set; }
    }
}

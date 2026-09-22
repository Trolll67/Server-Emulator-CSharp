using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Send.Character
{
    /// <summary>
    ///     CTrCharActionAck of the original: whose action it is and what the action is. It goes to
    ///     everyone who sees the character and to the character itself
    /// </summary>
    [Model(PacketType.CharActionAck)]
    public class CharActionAckModel
    {
        /// <summary>
        ///     mCharID of the original
        /// </summary>
        public UniqueId SessionGameId { get; set; }

        /// <summary>
        ///     mAction of the original
        /// </summary>
        public uint Action { get; set; }
    }
}

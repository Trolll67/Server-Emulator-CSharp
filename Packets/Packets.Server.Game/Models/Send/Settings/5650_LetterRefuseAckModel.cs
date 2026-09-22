using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrLetterRefuseAck of the original: the setting the character ends up with
    /// </summary>
    [Model(PacketType.LetterRefuseAck)]
    public class LetterRefuseAckModel
    {
        /// <summary>
        ///     mIsOn of the original
        /// </summary>
        public int IsOn { get; set; }
    }
}

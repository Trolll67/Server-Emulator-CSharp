using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrGiftBoxExistAck of the original: whether a gift box waits for this character
    /// </summary>
    [Model(PacketType.GiftBoxExistAck)]
    public class GiftBoxExistAckModel
    {
        /// <summary>
        ///     mIsExist of the original
        /// </summary>
        public int IsExist { get; set; }
    }
}

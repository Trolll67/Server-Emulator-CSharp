using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrCheckStoreListAck of the original: the number of the rows of the warehouse and then
    ///     that many of them. The array of the original holds three hundred rows, but only the ones
    ///     it counts travel - an empty warehouse is the count alone, four bytes, and that is what
    ///     the recorded answer of the original carries
    /// </summary>
    [Model(PacketType.CheckStoreListAck)]
    public class CheckStoreListAckModel
    {
        /// <summary>
        ///     mCnt of the original: how many rows of the warehouse follow
        /// </summary>
        public uint Count { get; set; }
    }
}

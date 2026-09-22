using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrStoreCountAck of the original: how many rows the warehouse of the account really
    ///     holds. It is the number of the database and not the one of the list packet, so it may
    ///     name more than a list is able to carry
    /// </summary>
    [Model(PacketType.StoreCountAck)]
    public class StoreCountAckModel
    {
        public uint Count { get; set; }
    }
}

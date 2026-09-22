using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrStoreListAck of the original: the whole personal warehouse. The packet is packed
    ///     tight - a row is twenty nine bytes with no alignment between the fields - and only the
    ///     rows that travel are counted, so an empty warehouse is the kind and the count alone
    /// </summary>
    [Model(PacketType.StoreListAck)]
    public class StoreListAckModel
    {
        /// <summary>
        ///     How many rows one packet carries at most, eMaxCntStoreList of the original. It is
        ///     the limit of the packet and not of the warehouse: the rows past it stay in the
        ///     database and the count packet still names them all
        /// </summary>
        public const int MaxRows = 300;

        /// <summary>
        ///     Which warehouse it is, EStoreType: zero is the one of the account, one and two are
        ///     the two levels of the guild warehouse
        /// </summary>
        public int StoreType { get; set; }

        public List<StoreRowModel> Rows { get; set; } = new List<StoreRowModel>();
    }

    /// <summary>
    ///     One row of the warehouse, CStore of the original
    /// </summary>
    public class StoreRowModel
    {
        public long SerialNo { get; set; }
        public int ItemNo { get; set; }
        public bool IsConfirm { get; set; }
        public byte Status { get; set; }
        public int Count { get; set; }
        public short CountUse { get; set; }
        public int Owner { get; set; }
        public int TermOfEffectivity { get; set; }
        public byte HoleCount { get; set; }
    }
}

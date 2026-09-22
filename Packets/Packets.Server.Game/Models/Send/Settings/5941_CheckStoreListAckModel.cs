using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrCheckStoreListAck of the original: the count of the rows and then that many rows of
    ///     two numbers each. This is not the full list - the row here is only what the thing is and
    ///     how many of it lie there, eight bytes, and the client asks for it the moment it is in
    ///     the world to know whether the warehouse holds anything at all
    /// </summary>
    [Model(PacketType.CheckStoreListAck)]
    public class CheckStoreListAckModel
    {
        public List<CheckStoreRowModel> Rows { get; set; } = new List<CheckStoreRowModel>();
    }

    /// <summary>
    ///     One row of the short list
    /// </summary>
    public class CheckStoreRowModel
    {
        public int ItemNo { get; set; }
        public int Count { get; set; }
    }
}

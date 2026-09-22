using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Receive
{
    /// <summary>
    ///     CTrStoreReq of the original: everything the warehouse is asked to do travels in this one
    ///     packet of a fixed length. Only the first <see cref="Count"/> of the slots carry anything
    /// </summary>
    [Model(PacketType.StoreReq)]
    public class StoreReqModel
    {
        /// <summary>
        ///     Things the action is about, mItemInfo[18]
        /// </summary>
        public List<StoreReqItemModel> Items { get; set; } = new List<StoreReqItemModel>();

        /// <summary>
        ///     What the client wants of the warehouse, ESTOREACTION of the original
        /// </summary>
        public StoreActionType Action { get; set; }

        /// <summary>
        ///     mCount of the original: how many of the slots above are filled
        /// </summary>
        public uint Count { get; set; }

        /// <summary>
        ///     Password of the warehouse the client typed, mStorePassword[9]. The original asks
        ///     for it on every action but the short check of the list
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    ///     One slot of the request, SItem of the original
    /// </summary>
    public class StoreReqItemModel
    {
        public long SerialNo { get; set; }
        public uint Count { get; set; }
        public int ItemNo { get; set; }
    }

    /// <summary>
    ///     ESTOREACTION of the original
    /// </summary>
    public enum StoreActionType
    {
        RequestList = 0,
        PushItem = 1,
        PullItem = 2,
        CheckRequestList = 3,
        PushRequest = 4,
        PopRequest = 5,
        Etc = 6
    }
}

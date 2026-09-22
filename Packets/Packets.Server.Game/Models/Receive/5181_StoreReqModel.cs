using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Receive
{
    /// <summary>
    ///     CTrStoreReq of the original: everything the warehouse is asked to do travels in this one
    ///     packet. Its length is fixed at 321 bytes - eighteen item slots of sixteen bytes, the
    ///     action, the count, the password of the warehouse and four numbers of the client check.
    ///     Only the action is read here: the rest belongs to the moves this server does not make yet
    /// </summary>
    [Model(PacketType.StoreReq)]
    public class StoreReqModel
    {
        /// <summary>
        ///     What the client wants of the warehouse, ESTOREACTION of the original
        /// </summary>
        public StoreActionType Action { get; set; }

        /// <summary>
        ///     mCount of the original: how many things the action is about
        /// </summary>
        public uint Count { get; set; }
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

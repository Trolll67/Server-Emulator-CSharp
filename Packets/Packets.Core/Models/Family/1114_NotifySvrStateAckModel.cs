using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Family
{
    /// <summary>
    ///     A server tells the family how loaded it is. Every server sends it on its own timer,
    ///     the channel keeps the last value and puts it into the server list it hands to clients.
    ///     CTrNotifySvrStateAck of the original: mSvrNo, mMaxSesCnt, mBusySesCnt
    /// </summary>
    [Model(PacketType.NotifySvrStateAck)]
    public class NotifySvrStateAckModel
    {
        /// <summary>
        ///     Number of the server the state belongs to
        /// </summary>
        public short SvrNo { get; set; }

        /// <summary>
        ///     How many sessions the server is able to hold
        /// </summary>
        public short MaxSesCnt { get; set; }

        /// <summary>
        ///     How many sessions of those are still free. The name is the one of the original,
        ///     and it is misleading there as well: the field carries the free ones, so the count
        ///     of the players on the server is the difference of the two
        /// </summary>
        public short BusySesCnt { get; set; }
    }
}

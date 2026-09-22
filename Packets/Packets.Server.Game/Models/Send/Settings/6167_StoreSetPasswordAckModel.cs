using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     CTrStoreSetPasswordAck of the original: the warehouse tells the client to ask the
    ///     player for its password, or answers that the password was set or cleared
    /// </summary>
    [Model(PacketType.StoreSetPasswordAck)]
    public class StoreSetPasswordAckModel
    {
        /// <summary>
        ///     What the window is for: zero and one mean the password stands in the way of
        ///     putting a thing in or taking it out, two asks to set one, three to clear it
        /// </summary>
        public int Flag { get; set; }

        /// <summary>
        ///     Whether the warehouse has a password at all
        /// </summary>
        public int IsSet { get; set; }
    }
}

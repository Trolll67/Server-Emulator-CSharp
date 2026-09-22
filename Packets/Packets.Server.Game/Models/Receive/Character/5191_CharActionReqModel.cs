using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Receive.Character
{
    /// <summary>
    ///     CTrCharActionReq of the original: one number, the action the character plays
    /// </summary>
    [Model(PacketType.CharActionReq)]
    public class CharActionReqModel
    {
        /// <summary>
        ///     mAction of the original
        /// </summary>
        public uint Action { get; set; }
    }
}

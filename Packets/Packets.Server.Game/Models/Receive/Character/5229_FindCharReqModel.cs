using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Receive.Character
{
    /// <summary>
    ///     CTrFindCharReq of the original: the number of the entity the client wants to know about.
    ///     The client sends it when it holds a number it has no data for - the answer is the same
    ///     packet that shows the entity when it comes into view
    /// </summary>
    [Model(PacketType.FindCharReq)]
    public class FindCharReqModel
    {
        /// <summary>
        ///     mWho of the original
        /// </summary>
        public UniqueId Who { get; set; }
    }
}

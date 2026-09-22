using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Send.Npc
{
    /// <summary>
    ///     CTrScriptProcAck of the original: the keeper the client is talking to, what it opened
    ///     and the number that goes with it - for the warehouse that number is what the keeper
    ///     charges for its work
    /// </summary>
    [Model(PacketType.ScriptProcAck)]
    public class ScriptProcAckModel
    {
        public UniqueId SessionGameId { get; set; }
        public ScriptAction Action { get; set; }
        public int Param { get; set; }
    }
}

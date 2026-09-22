using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Send.Npc
{
    /// <summary>
    ///     CTrScrDialogNoMsgAck of the original: the window of a keeper that says nothing of its
    ///     own. The original answers with it when the NPC has a dialog but no script behind it
    /// </summary>
    [Model(PacketType.ScrDialogNoMsgAck)]
    public class ScrDialogNoMsgAckModel
    {
        public int ScriptId { get; set; }
        public UniqueId SessionGameId { get; set; }
        public int Param { get; set; }
    }
}

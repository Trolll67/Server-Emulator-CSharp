using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Npc;

namespace Packets.Server.Game.Parsers.Send.Npc
{
    /// <summary>
    ///     Parser of CTrScrDialogNoMsgAck, twelve bytes of payload
    /// </summary>
    [ParserSend]
    public class ScrDialogNoMsgAck
    {
        [ParserAction(Core.Enums.PacketType.ScrDialogNoMsgAck)]
        public byte[] Parsing(ScrDialogNoMsgAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger(model.ScriptId);
            model.SessionGameId.Write(formationPackage);
            formationPackage.AddInteger(model.Param);

            return formationPackage.GetBytes();
        }
    }
}

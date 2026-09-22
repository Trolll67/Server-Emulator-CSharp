using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Npc;

namespace Packets.Server.Game.Parsers.Send.Npc
{
    /// <summary>
    ///     Parser of CTrScriptProcAck, twelve bytes of payload
    /// </summary>
    [ParserSend]
    public class ScriptProcAck
    {
        [ParserAction(Core.Enums.PacketType.ScriptProcAck)]
        public byte[] Parsing(ScriptProcAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            model.SessionGameId.Write(formationPackage);
            formationPackage.AddInteger((int)model.Action);
            formationPackage.AddInteger(model.Param);

            return formationPackage.GetBytes();
        }
    }
}

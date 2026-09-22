using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Character;

namespace Packets.Server.Game.Parsers.Send.Character
{
    /// <summary>
    ///     Parser of CTrCharActionAck: the identifier of the character and the action behind it,
    ///     eight bytes in all, exactly as the recorded answer of the original carries them
    /// </summary>
    [ParserSend]
    public class CharActionAck
    {
        [ParserAction(Core.Enums.PacketType.CharActionAck)]
        public byte[] Parsing(CharActionAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            model.SessionGameId.Write(formationPackage);
            formationPackage.AddUInteger(model.Action);

            return formationPackage.GetBytes();
        }
    }
}

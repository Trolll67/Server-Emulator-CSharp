using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrLetterRefuseAck
    /// </summary>
    [ParserSend]
    public class LetterRefuseAck
    {
        [ParserAction(Core.Enums.PacketType.LetterRefuseAck)]
        public byte[] Parsing(LetterRefuseAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger(model.IsOn);

            return formationPackage.GetBytes();
        }
    }
}

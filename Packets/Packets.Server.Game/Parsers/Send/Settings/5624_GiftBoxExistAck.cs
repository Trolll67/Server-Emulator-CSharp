using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrGiftBoxExistAck
    /// </summary>
    [ParserSend]
    public class GiftBoxExistAck
    {
        [ParserAction(Core.Enums.PacketType.GiftBoxExistAck)]
        public byte[] Parsing(GiftBoxExistAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger(model.IsExist);

            return formationPackage.GetBytes();
        }
    }
}

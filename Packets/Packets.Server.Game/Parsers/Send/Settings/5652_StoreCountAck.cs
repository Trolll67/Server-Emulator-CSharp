using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrStoreCountAck
    /// </summary>
    [ParserSend]
    public class StoreCountAck
    {
        [ParserAction(Core.Enums.PacketType.StoreCountAck)]
        public byte[] Parsing(StoreCountAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddUInteger(model.Count);

            return formationPackage.GetBytes();
        }
    }
}

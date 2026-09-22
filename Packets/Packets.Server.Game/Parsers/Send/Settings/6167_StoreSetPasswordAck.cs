using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrStoreSetPasswordAck, eight bytes of payload
    /// </summary>
    [ParserSend]
    public class StoreSetPasswordAck
    {
        [ParserAction(Core.Enums.PacketType.StoreSetPasswordAck)]
        public byte[] Parsing(StoreSetPasswordAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger(model.Flag);
            formationPackage.AddInteger(model.IsSet);

            return formationPackage.GetBytes();
        }
    }
}

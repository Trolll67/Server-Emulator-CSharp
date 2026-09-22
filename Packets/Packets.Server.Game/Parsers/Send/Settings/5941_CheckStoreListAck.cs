using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrCheckStoreListAck. Nothing follows the count while the warehouse holds
    ///     nothing: the rows are written here once this server keeps a warehouse of its own
    /// </summary>
    [ParserSend]
    public class CheckStoreListAck
    {
        [ParserAction(Core.Enums.PacketType.CheckStoreListAck)]
        public byte[] Parsing(CheckStoreListAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddUInteger(model.Count);

            return formationPackage.GetBytes();
        }
    }
}

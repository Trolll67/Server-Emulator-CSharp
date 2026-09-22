using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrCheckStoreListAck: the count and then eight bytes a row, packed tight
    /// </summary>
    [ParserSend]
    public class CheckStoreListAck
    {
        [ParserAction(Core.Enums.PacketType.CheckStoreListAck)]
        public byte[] Parsing(CheckStoreListAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddUInteger((uint)model.Rows.Count);

            foreach (CheckStoreRowModel row in model.Rows)
            {
                formationPackage.AddUInteger((uint)row.ItemNo);
                formationPackage.AddInteger(row.Count);
            }

            return formationPackage.GetBytes();
        }
    }
}

using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of CTrStoreListAck. Everything is packed: the row of the original is twenty nine
    ///     bytes and the compiler puts no padding into it, so nothing is aligned here either
    /// </summary>
    [ParserSend]
    public class StoreListAck
    {
        [ParserAction(Core.Enums.PacketType.StoreListAck)]
        public byte[] Parsing(StoreListAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger(model.StoreType);
            formationPackage.AddUInteger((uint)model.Rows.Count);

            foreach (StoreRowModel row in model.Rows)
            {
                formationPackage.AddLong(row.SerialNo);
                formationPackage.AddUInteger((uint)row.ItemNo);
                formationPackage.AddByte(row.IsConfirm ? (byte)1 : (byte)0);
                formationPackage.AddByte(row.Status);
                formationPackage.AddInteger(row.Count);
                formationPackage.AddUShort((ushort)row.CountUse);
                formationPackage.AddUInteger((uint)row.Owner);
                formationPackage.AddInteger(row.TermOfEffectivity);
                formationPackage.AddByte(row.HoleCount);
            }

            return formationPackage.GetBytes();
        }
    }
}

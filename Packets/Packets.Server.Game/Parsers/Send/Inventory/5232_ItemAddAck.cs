using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Inventory;

namespace Packets.Server.Game.Parsers.Send.Inventory
{
    /// <summary>
    ///     Parser for item add ack
    /// </summary>
    [ParserSend]
    public class ItemAddAck
    {
        /// <summary>
        ///     Bead slots of an item written after it: five records of three four-byte fields each
        /// </summary>
        private const int BeadSlotCount = 5;

        /// <summary>
        ///     Mark of a slot with no bead in it. It stands in the second field of a record, and it
        ///     is minus one and not zero: a zero reads as a bead of number zero, so a bag item
        ///     would go out with five slots the client counts as filled
        /// </summary>
        private const int NoBead = -1;

        [ParserAction(PacketType.ItemAddAck)]
        public byte[] Parsing(ItemAddAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            model.Item.Write(formationPackage);

            // Beads are not put into items anywhere yet, so every slot of every item goes out empty
            for (int slot = 0; slot < BeadSlotCount; slot++)
            {
                formationPackage.AddZeroBytes(4);
                formationPackage.AddInteger(NoBead);
                formationPackage.AddZeroBytes(4);
            }

            model.SessionGameId.Write(formationPackage);
            formationPackage.AddByte(model.Reason);

            return formationPackage.GetBytes();
        }
    }
}
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Inventory;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Parsers.Send.Inventory
{
    /// <summary>
    ///     Parser for existed item ack
    /// </summary>
    [ParserSend]
    public class DisplayedItem
    {
        [ParserAction(PacketType.ExistedItemAck)]
        public byte[] Parsing(ExistedItemAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddUShort((ushort)model.Items.Count);

            foreach (PublicItem item in model.Items)
            {
                // The tail of a record of the batch goes out with zeros: its offset here is not
                // confirmed - the single sample of the reference lays it two bytes off the packet
                // about a single item - and a constant by a wrong offset would overwrite a field
                // that is really there. Zeros until a second sample
                item.Write(formationPackage, 0);
            }

            return formationPackage.GetBytes();
        }
    }
}
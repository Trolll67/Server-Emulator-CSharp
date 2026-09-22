using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send;
using System;
using System.Linq;

namespace Packets.Server.Game.Parsers.Send
{
    /// <summary>
    ///     Парсер пакета подтверждения входа в мир(инвентарь)
    /// </summary>
    [ParserSend]
    public class CompleteEnterWorld
    {
        /// <summary>
        ///     How many slots the bag of a character has, eInv_MaxSize of the original. The array
        ///     of CPublicInven is declared for 240 there, but only 160 of them travel: the recorded
        ///     packet of the original is 9060 bytes of payload, which is the 92 bytes before the
        ///     inventory, its count and padding, and exactly 160 slots behind them
        /// </summary>
        private const int InventorySlots = 160;

        /// <summary>
        ///     Size of one slot, CGoods of the original with its padding
        /// </summary>
        private const int GoodsSize = 56;


        [ParserAction(Core.Enums.PacketType.CompleteEnterWorld)]
        public byte[] Parsing(CompleteEnterWorldModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            // CTrChoosePcAck of the original starts with mUnique, mMapNo and mPos and nothing
            // else: the eight bytes that used to stand here pushed the whole packet out by eight,
            // so the client read the map and the position out of the wrong place and put the
            // character off the map. With them gone the position lands on offset 8 and the attack
            // rate on offset 38, exactly where the recorded packet of the original has them
            model.SessionGameId.Write(formationPackage);         // mUnique
            formationPackage.AddInteger(model.MapNo);            // mMapNo
            model.Position.Write(formationPackage);              // mPos
            formationPackage.AddZeroBytes(18);                   // Не расшифрованные байты
            formationPackage.AddShort(model.AttackRate);
            formationPackage.AddShort(model.MoveRate);
            formationPackage.AddZeroBytes(2);                    // Не расшифрованные байты
            model.Position.Write(formationPackage);
            formationPackage.AddZeroBytes(4);                    // Не расшифрованные байты
            formationPackage.AddInteger(model.Reputation);       // Репутация
            formationPackage.AddZeroBytes(28);                   // Не расшифрованные байты

            // CPublicInven of the original: the count of the things, then the slots themselves.
            // Six bytes of padding stand between them - CGoods starts with a flag and holds a
            // serial number eight bytes wide, so the array is aligned to eight
            // The count never names more than travels: a bag that somehow holds more than the
            // slots would send the client reading past the end of the packet
            formationPackage.AddShort((short)Math.Min(model.Items.Count, InventorySlots));
            formationPackage.AddZeroBytes(6);

            // Every slot travels, not only the filled ones: the length of the packet does not
            // depend on how full the bag is. The original leaves the slots past the count as they
            // lay in its memory and the client reads none of them, so zeroes do just as well
            for (int i = 0; i < InventorySlots; i++)
            {
                var item = model.Items.ElementAtOrDefault(i);

                if (item != null)
                {
                    item.Write(formationPackage);
                }
                else
                {
                    formationPackage.AddZeroBytes(GoodsSize);
                }
            }

            return formationPackage.GetBytes();
        }
    }
}
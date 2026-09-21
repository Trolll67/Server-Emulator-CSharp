using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send;
using System.Linq;

namespace Packets.Server.Game.Parsers.Send
{
    /// <summary>
    ///     Парсер пакета подтверждения входа в мир(инвентарь)
    /// </summary>
    [ParserSend]
    public class CompleteEnterWorld
    {
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

            formationPackage.AddShort((short)model.Items.Count);// Количество вещей в инвентаре
            formationPackage.AddZeroBytes(6);                    // Не расшифрованные байты

            // Вещи в инвентаре
            for (int i = 0; i < 240; i++)
            {
                var item = model.Items.ElementAtOrDefault(i);

                if (item != null)
                {
                    item.Write(formationPackage);
                }
                else
                {
                    formationPackage.AddZeroBytes(56);
                }
            }

            formationPackage.AddZeroBytes(5);
            return formationPackage.GetBytes();
        }
    }
}
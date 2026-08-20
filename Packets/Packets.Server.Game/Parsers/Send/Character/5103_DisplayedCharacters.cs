using System;
using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Character;

namespace Packets.Server.Game.Parsers.Send.Character
{
    /// <summary>
    ///     Парсер пакета отображенного персонажа
    /// </summary>
    [ParserSend]
    public class DisplayedCharacters
    {
        [ParserAction(Core.Enums.PacketType.DisplayedCharacter)]
        public byte[] Parsing(DisplayedCharacterModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            model.Character.Write(formationPackage);

            // Хвост CTrEnteredPcAck: Flag, количество абнормальных состояний и их номера.
            // Счётчик однобайтовый, поэтому лишние состояния в пакет не попадают:
            // иначе клиент прочитает хвост как мусор
            byte countAbnormal = (byte)Math.Min(model.Abnormals.Count, byte.MaxValue);

            formationPackage.AddByte(model.IsTeleport ? (byte)1 : (byte)0);
            formationPackage.AddByte(countAbnormal);

            for (int i = 0; i < countAbnormal; i++)
            {
                formationPackage.AddInteger(model.Abnormals[i]);
            }

            return formationPackage.GetBytes();
        }
    }
}
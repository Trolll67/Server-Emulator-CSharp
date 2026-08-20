using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Character;

namespace Packets.Server.Game.Parsers.Send.Character
{
    /// <summary>
    ///     Парсер пакета завершения входа персонажа в мир
    /// </summary>
    [ParserSend]
    public class EnteredWorldAck
    {
        [ParserAction(Core.Enums.PacketType.EnteredWorldAck)]
        public byte[] Parsing(EnteredWorldAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            // Отправляется пустой пакет: на проводе только заголовок с номером пакета

            return formationPackage.GetBytes();
        }
    }
}

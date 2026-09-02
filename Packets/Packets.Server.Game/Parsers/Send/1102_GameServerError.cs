using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send;

namespace Packets.Server.Game.Parsers.Send
{
    /// <summary>
    ///     Парсер ошибки сервера
    /// </summary>
    [ParserSend]
    public class GameServerError
    {
        [ParserAction(Core.Enums.PacketType.GameServerError)]
        public byte[] Parsing(GameServerErrorModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            // Отправляем номер ошибки: эхо опкода запроса, код причины, восемь байт данных
            // запроса (для отказа на надевание там идентификатор вещи, иначе ноль) и признак
            // окна - пятнадцать байт нагрузки, семнадцать вместе с опкодом самого пакета
            formationPackage.AddShort((short)model.PacketType);
            formationPackage.AddUInteger(model.ErrorType.Code);
            formationPackage.AddULong(model.Etc);
            formationPackage.AddByte(model.IsMsgBox ? (byte)1 : (byte)0);

            return formationPackage.GetBytes();
        }
    }
}
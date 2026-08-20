using Packets.Core.Attributes;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Send.Character
{
    [Model(Core.Enums.PacketType.StopMoveCharacter)]
    public class StopMoveCharacterModel
    {
        public UniqueId SessionGameId { get; set; }

        /// <summary>
        ///     Авторитетная позиция сервера, на которую клиента возвращают
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        ///     Поле Flag структуры CTrStopMoveAck: 0 ставится, когда проверка движения выставила
        ///     внутренний признак полного отката, а не по самому факту запрета; отказ без этого
        ///     признака даёт 1, и следом инициатору уходит ресинк 5103
        /// </summary>
        public byte Flag { get; set; }
    }
}

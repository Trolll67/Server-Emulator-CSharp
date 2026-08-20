using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Send.Character
{
    /// <summary>
    ///     Model displayed character
    /// </summary>
    [Model(PacketType.DisplayedCharacter)]
    public class DisplayedCharacterModel
    {
        public DisplayedCharacterModel()
        {
            Abnormals = new List<int>();
        }

        public PublicPc Character { get; set; }

        /// <summary>
        ///     Поле Flag структуры CTrEnteredPcAck
        /// </summary>
        public bool IsTeleport { get; set; }

        /// <summary>
        ///     Номера наложенных на персонажа абнормальных состояний. Уходят переменным хвостом:
        ///     сначала их количество одним байтом, затем по int на каждое состояние
        /// </summary>
        public List<int> Abnormals { get; set; }
    }
}
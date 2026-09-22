using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Models.Send
{
    /// <summary>
    ///     Model complete enter world and items to inventory
    /// </summary>
    [Model(PacketType.CompleteEnterWorld)]
    public class CompleteEnterWorldModel
    {
        public CompleteEnterWorldModel()
        {
            Items = new List<ItemApiModel>();
        }

        public UniqueId SessionGameId { get; set; }

        /// <summary>
        ///     Map the character stands on, TblPcState.mMapNo
        /// </summary>
        public int MapNo { get; set; }

        public Vector3 Position { get; set; }

        /// <summary>
        ///     CPcDetail.mHomePos: the point the character is raised at, TblPc.mHomePosX/Y/Z
        /// </summary>
        public Vector3 HomePosition { get; set; }

        /// <summary>
        ///     CPcDetail.mLetterLimit: whether the character refuses letters,
        ///     TblPcState.mIsLetterLimit
        /// </summary>
        public int LetterLimit { get; set; }

        public short Reputation { get; set; }
        public List<ItemApiModel> Items { get; set; }
        public short MoveRate { get; set; }
        public short AttackRate { get; set; }
    }
}
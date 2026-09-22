using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Receive
{
    /// <summary>
    ///     CTrLetterRefuseReq of the original: one int, whether the character refuses letters
    /// </summary>
    [Model(PacketType.LetterRefuseReq)]
    public class LetterRefuseReqModel
    {
        /// <summary>
        ///     mIsOn of the original, TblPcState.mIsLetterLimit
        /// </summary>
        public int IsOn { get; set; }
    }
}

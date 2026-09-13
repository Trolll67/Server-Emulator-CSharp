using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Family
{
    /// <summary>
    ///     Answer to <see cref="LoginFamilyReqModel"/>: the other side names itself, and the sender
    ///     of the request counts the link as established. CTrLoginFamilyAck of the original
    /// </summary>
    [Model(PacketType.LoginFamilyAck)]
    public class LoginFamilyAckModel
    {
        /// <summary>
        ///     Number of the server that answers
        /// </summary>
        public short SvrNo { get; set; }
    }
}

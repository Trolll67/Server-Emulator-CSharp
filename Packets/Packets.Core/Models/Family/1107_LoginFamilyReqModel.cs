using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Family
{
    /// <summary>
    ///     A server of the world announces itself to another one right after it has connected.
    ///     CTrLoginFamilyReq of the original: mSvrNo, mVersion
    /// </summary>
    [Model(PacketType.LoginFamilyReq)]
    public class LoginFamilyReqModel
    {
        /// <summary>
        ///     Number of the server that connects, TblParmSvr.mSvrNo
        /// </summary>
        public short SvrNo { get; set; }

        /// <summary>
        ///     Build of the sender. The original logs it and lets the link live either way,
        ///     so nothing here refuses a link because of it
        /// </summary>
        public int Version { get; set; }
    }
}

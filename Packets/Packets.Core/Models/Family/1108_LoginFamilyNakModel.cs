using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Core.Models.Family
{
    /// <summary>
    ///     Refusal of a family login. CTrLoginFamilyNak of the original: mErrNo, mSvrNo
    /// </summary>
    [Model(PacketType.LoginFamilyNak)]
    public class LoginFamilyNakModel
    {
        /// <summary>
        ///     Why the server was not taken into the family
        /// </summary>
        public FamilyErrorType Error { get; set; }

        /// <summary>
        ///     Number of the server that was refused, as it named itself
        /// </summary>
        public short SvrNo { get; set; }
    }

    /// <summary>
    ///     Reasons the original refuses a family login with. The numbers are the error codes of the
    ///     original, which are hashes of the constant names, so they are filled in when the code
    ///     of the errors of the game server gets its own place. Until then the link logs the reason
    ///     by name and the number stays zero
    /// </summary>
    public enum FamilyErrorType
    {
        /// <summary>
        ///     There is no such server in TblParmSvr, or it is not family to this one
        /// </summary>
        FamilyNot = 0,

        /// <summary>
        ///     The address the server connected from is not the one TblParmSvr holds for it
        /// </summary>
        FamilyInvalidIp = 1,

        /// <summary>
        ///     The server is already on the line
        /// </summary>
        FamilyAlreadyConnect = 2
    }
}

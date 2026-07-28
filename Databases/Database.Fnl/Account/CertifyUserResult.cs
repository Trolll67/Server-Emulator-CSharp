using System;

namespace Database.Fnl.Account
{
    /// <summary>
    ///     Output of dbo.UspCertifyUser_CN
    /// </summary>
    public class CertifyUserResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - success, 1 - unknown account or wrong password,
        ///     3 - the account is not activated, 4 - already logged in, 5 - update failed,
        ///     9 - blocked, 92 - resource mismatch, 93 - address of a privileged account changed
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     TblUser.mUserNo, @pUserNo
        /// </summary>
        public int UserNo { get; set; }

        /// <summary>
        ///     World the account is currently logged into, @pWorldNo. Greater than zero means "in game"
        /// </summary>
        public short WorldNo { get; set; }

        /// <summary>
        ///     Error name, @pErrNoStr, for example eErrNoUserDiffPswd. Filled only when
        ///     <see cref="ReturnCode"/> is not zero: on success the procedure leaves there
        ///     its initial value 'eErrNoSqlInternalError' and never resets it
        /// </summary>
        public string ErrNo { get; set; }

        /// <summary>
        ///     TblUserBlock.mCertify, @pCertify - the moment the block expires, null when there is no block
        /// </summary>
        public DateTime? BlockedUntil { get; set; }

        /// <summary>
        ///     TblUserBlock.mCertifyReason, @pCertifyReason - reason of the block
        /// </summary>
        public string BlockReason { get; set; }

        /// <summary>
        ///     TblUser.mSecKeyTableUse, @pSecKeyTableUse - the account uses a security card
        /// </summary>
        public byte SecKeyTableUse { get; set; }

        /// <summary>
        ///     TblUser.mUserAuth, @pUserAuth: 0 - not activated, 1 - ordinary user, above - staff
        /// </summary>
        public byte UserAuth { get; set; }
    }
}

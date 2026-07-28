namespace Database.Fnl.Account
{
    /// <summary>
    ///     Output of dbo.UspUpdateCertifiedKey
    /// </summary>
    public class UpdateCertifiedKeyResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - the key is written,
        ///     1 - there is no such mUserNo, 2 - the update itself failed
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     TblUser.mUserId, @pUserId char(20) without the trailing spaces
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     TblUser.mUserAuth, @pUserAuth
        /// </summary>
        public byte UserAuth { get; set; }

        /// <summary>
        ///     TblUser.mSecKeyTableUse, @pSecKeyTableUse
        /// </summary>
        public byte SecKeyTableUse { get; set; }

        /// <summary>
        ///     TblUser.mPcBangLv, @pPCBangLv
        /// </summary>
        public int PcBangLv { get; set; }
    }
}

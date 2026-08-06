namespace Database.Fnl.Account
{
    /// <summary>
    ///     Output of dbo.UspLoginUser
    /// </summary>
    public class LoginUserResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - success (also when the same account re-enters the
        ///     same world from the same IP), 1 - no such mUserNo, 2 - wrong session key,
        ///     5 - the account is not activated, 7 - already logged into another world,
        ///     8 - update failed, 9 - blocked, 10 - the IP is already used by the same PC bang
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     TblUser.mUserId, @pUserId varchar(20) without the trailing spaces
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     TblUser.mUserAuth, @pUserAuth: 0 - not activated, 1 - ordinary user, above - staff
        /// </summary>
        public byte UserAuth { get; set; }

        /// <summary>
        ///     TblUser.mSecKeyTableUse, @pSecKeyState - the account uses a security card
        /// </summary>
        public byte SecKeyState { get; set; }

        /// <summary>
        ///     The session key actually written into TblUser.mCertifiedKey, @pNewCertifiedKeyOutput.
        ///     The procedure may adjust the requested key when it collides with the current one
        /// </summary>
        public int NewCertifiedKey { get; set; }

        /// <summary>
        ///     TblUser.mUseMacro, @pUseMacro - the macro usage counter of the account. UspLogoutUser
        ///     writes it back, so the session keeps it between login and logout
        /// </summary>
        public short UseMacro { get; set; }

        /// <summary>
        ///     Error name, @pErrNoStr, for example eErrNoUserDiffCertifiedKey. Filled only when
        ///     <see cref="ReturnCode"/> is not zero: on success the procedure leaves there
        ///     its initial value 'eErrNoSqlInternalError' and never resets it
        /// </summary>
        public string ErrNo { get; set; }
    }
}

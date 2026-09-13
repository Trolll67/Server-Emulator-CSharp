namespace Database.Fnl.Account
{
    /// <summary>
    ///     Access to the accounts database (FNLAccount) through the original stored procedures
    /// </summary>
    public interface IFnlAccountRepository
    {
        /// <summary>
        ///     Checks the account and writes the session key into TblUser, dbo.UspCertifyUser_CN
        /// </summary>
        /// <param name="request">Login data of the client</param>
        /// <returns>Result with the return code of the procedure, never null</returns>
        CertifyUserResult CertifyUser(CertifyUserRequest request);

        /// <summary>
        ///     Reissues the session key of an already certified account, dbo.UspUpdateCertifiedKey
        /// </summary>
        /// <param name="userNo">TblUser.mUserNo</param>
        /// <param name="certifiedKey">New value of TblUser.mCertifiedKey</param>
        /// <returns>Result with the return code of the procedure, never null</returns>
        UpdateCertifiedKeyResult UpdateCertifiedKey(int userNo, int certifiedKey);

        /// <summary>
        ///     Re-checks a certified account entering the world and rotates its session key,
        ///     dbo.UspLoginUser
        /// </summary>
        /// <param name="request">Login data of the client</param>
        /// <returns>Result with the return code of the procedure, never null</returns>
        LoginUserResult LoginUser(LoginUserRequest request);

        /// <summary>
        ///     Marks an account as logged out of its world, dbo.UspLogoutUser. The procedure
        ///     negates TblUser.mWorldNo, so a later login does not answer eErrNoUserLoginAnother
        /// </summary>
        /// <param name="userNo">TblUser.mUserNo</param>
        /// <param name="chatBlockApplyTime">Minutes to subtract from the chat block, 0 keeps it as is</param>
        /// <param name="useMacro">Value written back into TblUser.mUseMacro, from the login result</param>
        /// <returns>Result with the return code of the procedure, never null</returns>
        LogoutUserResult LogoutUser(int userNo, int chatBlockApplyTime, short useMacro);

        /// <summary>
        ///     Asks the block list of addresses about a client (dbo.UspIsValidIp). The original
        ///     runs it before it even looks at the account
        /// </summary>
        /// <param name="addressNumber">
        ///     Address the way the original counts it: the four parts packed in decimal,
        ///     so 192.168.0.1 becomes 192168000001
        /// </param>
        IpCheckResult IsValidIp(long addressNumber);

        /// <summary>
        ///     Reads the world an account belongs to (dbo.UspRetrieveWorldNo). The original asks
        ///     it before the login itself: an account of another world is only let in when that
        ///     world is a neighbour of this channel
        /// </summary>
        /// <param name="userId">Login of the account</param>
        RetrieveWorldNoResult RetrieveWorldNo(string userId);
    }
}

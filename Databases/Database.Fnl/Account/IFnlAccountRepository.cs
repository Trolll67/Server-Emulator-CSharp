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
    }
}

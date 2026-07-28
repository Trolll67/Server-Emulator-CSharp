namespace Server.Login.Models.Login
{
    /// <summary>
    ///     Session login model. There is no Sessions table in the original R2: the role of the
    ///     session token is played by TblUser.mCertifiedKey, which the channel writes into
    ///     FNLAccount and the field server checks afterwards
    /// </summary>
    public class SessionLoginModel
    {
        /// <summary>
        ///     TblUser.mUserNo of the certified account
        /// </summary>
        public int UserNo { get; set; }

        /// <summary>
        ///     TblUser.mUserId, the name the client logged in with
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     TblUser.mCertifiedKey, the key the client shows to the field server
        /// </summary>
        public int CertifiedKey { get; set; }

        /// <summary>
        ///     World the account is currently logged into, greater than zero means "already in game"
        /// </summary>
        public short WorldNo { get; set; }
    }
}

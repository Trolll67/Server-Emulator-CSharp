namespace Database.Fnl.Account
{
    /// <summary>
    ///     Input of dbo.UspCertifyUser_CN, one property per input parameter of the procedure.
    ///     The defaults are the ones the login slice always sends, see the comments below.
    /// </summary>
    public class CertifyUserRequest
    {
        /// <summary>
        ///     Account name, @pUserId varchar(20)
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     Password, @pUserPswd varchar(20). TblUser.mUserPswd keeps it in plain text,
        ///     hashing it here would break the contract of the original procedure
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     Client address, @pIp char(15). The procedure writes it into TblUser.mIp
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        ///     Client address in the numeric form, @pIpEX bigint. The original channel sends 0
        /// </summary>
        public long IpEx { get; set; }

        /// <summary>
        ///     Session key, @pCertifiedKey int. RAND() of the database is a float,
        ///     so the original generates the key outside of SQL and passes it in
        /// </summary>
        public int CertifiedKey { get; set; }

        /// <summary>
        ///     Resource checksum of the client matches the server one, @pIsEqualRsc bit.
        ///     With 0 the procedure rejects an ordinary user with eErrNoRscKeyNotEqual
        /// </summary>
        public bool IsEqualRsc { get; set; } = true;

        /// <summary>
        ///     PC bang level, @pPcBangLv int (EPcBangLv). There are no PC bangs in the slice
        /// </summary>
        public int PcBangLv { get; set; }

        /// <summary>
        ///     Create the account when it does not exist yet, @pIsAddUser bit.
        ///     The original takes it from the server option table, the slice keeps it off
        /// </summary>
        public bool IsAddUser { get; set; }

        /// <summary>
        ///     Compare the password, @pIsPwdCheck bit. The original takes it from option 54
        ///     "Certify To Password In DB" of TblParmSvrOp, the slice always checks
        /// </summary>
        public bool IsPwdCheck { get; set; } = true;

        /// <summary>
        ///     PC bang join code, @pJoinCode varchar(1)
        /// </summary>
        public string JoinCode { get; set; } = "N";

        /// <summary>
        ///     Channeling identifier, @pLoginChannelID char(1)
        /// </summary>
        public string LoginChannelId { get; set; } = "N";

        /// <summary>
        ///     The user is under the anti addiction rules, @pTired char(1)
        /// </summary>
        public string Tired { get; set; } = "N";

        /// <summary>
        ///     Chinese identity card number, @pChnSID char(33)
        /// </summary>
        public string ChnSid { get; set; } = string.Empty;
    }
}

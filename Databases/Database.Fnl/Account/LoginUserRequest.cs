namespace Database.Fnl.Account
{
    /// <summary>
    ///     Input of dbo.UspLoginUser, one property per input parameter of the procedure.
    ///     The procedure re-checks an already certified account when it enters the world:
    ///     it compares the session key, rotates it and writes the world into TblUser
    /// </summary>
    public class LoginUserRequest
    {
        /// <summary>
        ///     TblUser.mUserNo, @pUserNo int - the account certified earlier by UspCertifyUser
        /// </summary>
        public int UserNo { get; set; }

        /// <summary>
        ///     Session key the client presents, @pCertifiedKey int. It must match the
        ///     current TblUser.mCertifiedKey, otherwise the procedure returns eErrNoUserDiffCertifiedKey
        /// </summary>
        public int CertifiedKey { get; set; }

        /// <summary>
        ///     Client address, @pIp char(15). The procedure writes it into TblUser.mIp
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        ///     World the account enters, @pWorldNo smallint. The procedure writes it into TblUser.mWorldNo
        /// </summary>
        public short WorldNo { get; set; }

        /// <summary>
        ///     Client address in the numeric form, @pIpEX bigint. The original channel sends 0
        /// </summary>
        public long IpEx { get; set; }

        /// <summary>
        ///     PC bang level for the duplicate IP check, @pPcBangLvEX int. With zero the procedure
        ///     skips the "one IP per PC bang" rule, the slice keeps it off
        /// </summary>
        public int PcBangLvEx { get; set; }

        /// <summary>
        ///     The session belongs to a non client user, @pIsNonClt bit. The slice always sends 0
        /// </summary>
        public bool IsNonClt { get; set; }

        /// <summary>
        ///     Connected server type, @pSvrInfo tinyint: 0 - general server, 1 - Chaos Battle Server
        /// </summary>
        public byte SvrInfo { get; set; }

        /// <summary>
        ///     Session key to write instead of the current one, @pNewCertifiedKey int. RAND() of the
        ///     database is a float, so the original generates the key outside of SQL and passes it in
        /// </summary>
        public int NewCertifiedKey { get; set; }
    }
}

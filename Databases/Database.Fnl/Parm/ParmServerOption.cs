namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Numbers of the server options in TblParmSvrOp that the emulator relies on.
    ///     The original reads the whole table on startup and drives its behaviour by it
    /// </summary>
    public static class ParmServerOption
    {
        /// <summary>
        ///     "Certify To Password In DB": whether the password is compared against
        ///     TblUser.mUserPswd during authorization. When it is off, the original does not check the
        ///     password at all — the client does not even send it in a readable form, the check belongs
        ///     to the external billing service
        /// </summary>
        public const int CertifyToPasswordInDb = 54;
    }
}

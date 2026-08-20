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

        /// <summary>
        ///     "Do Not Account Automatic Creation": forbids creating an account that does not exist yet.
        ///     Worded the other way round than the rest, so the flag the certify procedure takes is its
        ///     negation: with the option off an unknown login creates the account, with it on the login
        ///     is refused. The original reads the same number, see CSqlUser::Certify of the channel
        /// </summary>
        public const int DoNotAccountAutomaticCreation = 53;
    }
}

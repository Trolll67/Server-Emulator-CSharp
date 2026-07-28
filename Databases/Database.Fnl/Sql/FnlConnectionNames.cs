namespace Database.Fnl.Sql
{
    /// <summary>
    ///     Names of the original R2 databases as they are written in the "ConnectionStrings" configuration section
    /// </summary>
    public static class FnlConnectionNames
    {
        /// <summary>
        ///     Accounts database (FNLAccount): users, certified keys, blocks
        /// </summary>
        public const string FnlAccount = "FnlAccount";

        /// <summary>
        ///     Parameters database (FNLParm): server list, game data tables
        /// </summary>
        public const string FnlParm = "FnlParm";
    }
}

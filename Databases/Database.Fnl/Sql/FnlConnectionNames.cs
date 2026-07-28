namespace Database.Fnl.Sql
{
    /// <summary>
    ///     Names of the original R2 databases. A name is the base name of the DSN file in the directory
    ///     <see cref="FnlDatabaseOptions.DsnDirectory"/>: "Account" means "Account.dsn".
    ///     The same name works as the key in the "ConnectionStrings" section when a connection string
    ///     is set explicitly instead of the DSN file
    /// </summary>
    public static class FnlConnectionNames
    {
        /// <summary>
        ///     Accounts database (FNLAccount): users, certified keys, blocks. Account.dsn
        /// </summary>
        public const string FnlAccount = "Account";

        /// <summary>
        ///     Parameters database (FNLParm): server list, game data tables. Parm.dsn
        /// </summary>
        public const string FnlParm = "Parm";
    }
}

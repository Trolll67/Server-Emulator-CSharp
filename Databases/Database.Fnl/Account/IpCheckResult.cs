namespace Database.Fnl.Account
{
    /// <summary>
    ///     What the block list of addresses says about a client
    /// </summary>
    public enum IpCheckResult
    {
        /// <summary>
        ///     The address is not on the list
        /// </summary>
        Allowed = 0,

        /// <summary>
        ///     The address is blocked, the login is refused
        /// </summary>
        Blocked = 1,

        /// <summary>
        ///     The list could not be read. The original answers such a login with an internal
        ///     error of the database instead of guessing
        /// </summary>
        Unreadable = 2
    }
}

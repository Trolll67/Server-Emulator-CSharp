namespace Database.Fnl.Account
{
    /// <summary>
    ///     Output of dbo.UspLogoutUser
    /// </summary>
    public class LogoutUserResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - success, 1 - the account is not marked as being
        ///     in a world (TblUser.mWorldNo is not positive), 2 and 3 - update failed
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     The account was not in a world, so there was nothing to log out. A second logout
        ///     of the same session ends here and is not an error
        /// </summary>
        public bool WasNotInWorld => ReturnCode == 1;
    }
}

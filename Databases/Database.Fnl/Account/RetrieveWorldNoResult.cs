namespace Database.Fnl.Account
{
    /// <summary>
    ///     World an account belongs to, the result of dbo.UspRetrieveWorldNo
    /// </summary>
    public class RetrieveWorldNoResult
    {
        /// <summary>
        ///     Return code of the procedure, zero when it has an answer
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     Whether the procedure answered at all
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     Number of the world as TblUser keeps it, sign included: positive while the account
        ///     is in a game of that world, negative once it has left it, zero when it has not
        ///     played anywhere yet
        /// </summary>
        public short WorldNo { get; set; }
    }
}

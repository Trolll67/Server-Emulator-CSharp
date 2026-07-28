namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Own server settings, one row of the result set of dbo.UspGetParmSvr.
    ///     The procedure returns 23 columns; only the ones the login slice needs are mapped here
    /// </summary>
    public class ParmServerRow
    {
        /// <summary>
        ///     Server number (TblParmSvr.mSvrNo), the key every other Parm procedure is called with
        /// </summary>
        public short SvrNo { get; set; }

        /// <summary>
        ///     World the server belongs to, 0 for the channel server
        /// </summary>
        public short WorldNo { get; set; }

        /// <summary>
        ///     TCP port the server listens on
        /// </summary>
        public int TcpPort { get; set; }

        /// <summary>
        ///     UDP port the server listens on
        /// </summary>
        public int UdpPort { get; set; }

        /// <summary>
        ///     Server description, the procedure already applies RTRIM to it
        /// </summary>
        public string Desc { get; set; }
    }
}

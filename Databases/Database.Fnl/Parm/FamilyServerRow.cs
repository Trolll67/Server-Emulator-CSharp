namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Neighbour server of the same family, one row of the result set of dbo.UspGetFamilyEx
    /// </summary>
    public class FamilyServerRow
    {
        /// <summary>
        ///     Server number (TblParmSvr.mSvrNo)
        /// </summary>
        public short SvrNo { get; set; }

        /// <summary>
        ///     Server kind, the caller decides which kinds it is interested in
        /// </summary>
        public ParmServerType Type { get; set; }

        /// <summary>
        ///     TCP port the server listens on
        /// </summary>
        public int TcpPort { get; set; }

        /// <summary>
        ///     UDP port the server listens on
        /// </summary>
        public int UdpPort { get; set; }

        /// <summary>
        ///     Address the clients connect to (TblParmSvr.mMajorIp)
        /// </summary>
        public string MajorIp { get; set; }

        /// <summary>
        ///     Server description shown in the client server list
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        ///     World the server belongs to
        /// </summary>
        public short WorldNo { get; set; }
    }
}

namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Extra mark on the server, TblParmSvr.mSvrInfo. The channel passes it to the client in
    ///     the server list. ESvrInfo of the original
    /// </summary>
    public enum ParmServerInfo
    {
        /// <summary>
        ///     Nothing special about the server
        /// </summary>
        None = 0,

        /// <summary>
        ///     Server of the Chaos battle
        /// </summary>
        ChaosBattle = 1,

        /// <summary>
        ///     Server with a restricted entrance
        /// </summary>
        Specific = 2
    }
}

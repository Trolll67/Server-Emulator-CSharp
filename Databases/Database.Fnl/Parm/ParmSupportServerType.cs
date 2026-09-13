namespace Database.Fnl.Parm
{
    /// <summary>
    ///     What kind of service the server offers, TblParmSvr.mSupportType. The channel passes the
    ///     value to the client in the server list as is, the client tells the two kinds apart in
    ///     its own way. ESupportServerType of the original
    /// </summary>
    public enum ParmSupportServerType
    {
        /// <summary>
        ///     Ordinary server
        /// </summary>
        Original = 1,

        /// <summary>
        ///     Server open to everyone
        /// </summary>
        Open = 2
    }
}

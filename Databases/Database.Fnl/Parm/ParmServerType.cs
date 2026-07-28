namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Server kind stored in TblParmSvr.mType.
    ///     Values are taken from the body of dbo.UspGetFamilyEx, where the procedure declares
    ///     @aChannel = 0, @aField = 1, @aMgr = 2, @aGateWay = 3 with the comment "ESvrType과 연결됨"
    ///     (linked to ESvrType of the original server)
    /// </summary>
    public enum ParmServerType : byte
    {
        /// <summary>
        ///     Channel server: authenticates users and hands out the server list
        /// </summary>
        Channel = 0,

        /// <summary>
        ///     Field server: the world itself
        /// </summary>
        Field = 1,

        /// <summary>
        ///     Manager server
        /// </summary>
        Manager = 2,

        /// <summary>
        ///     Gateway server
        /// </summary>
        Gateway = 3
    }
}

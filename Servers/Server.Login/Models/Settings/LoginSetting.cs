namespace Server.Login.Models.Settings
{
    /// <summary>
    ///     Config for settings login server
    /// </summary>
    public class LoginSetting
    {
        public short Id { get; set; }

        public string ServerIp { get; set; }
        public short ServerPort { get; set; }

        /// <summary>
        ///     Own number of the channel server in FNLParm.TblParmSvr. Used when the server can not be
        ///     found by <see cref="ServerIp"/>: the address the emulator listens on and TblParmSvr.mMajorIp
        ///     of the live database are not necessarily the same
        /// </summary>
        public short ChannelSvrNo { get; set; }
    }
}

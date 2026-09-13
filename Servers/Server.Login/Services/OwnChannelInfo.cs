using System;
using Database.Fnl.Parm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Login.Models.Settings;

namespace Server.Login.Services
{
    /// <summary>
    ///     This channel server's own identity, resolved once at startup from TblParmSvr by its
    ///     address and kind (channel): server number and listen port. TblParmSvr is the only source
    ///     of these - the same table tells the client where to connect, so a port taken from
    ///     anywhere else would only be reachable by accident, and the family of this channel is
    ///     looked up by the very same number. A missing row stops the server. Registered as a
    ///     singleton: the lookup runs once, not per packet
    /// </summary>
    public class OwnChannelInfo
    {
        public OwnChannelInfo(IFnlParmRepository parmRepository, IOptions<LoginSetting> loginSetting, ILogger<OwnChannelInfo> logger)
        {
            LoginSetting setting = loginSetting.Value;
            ParmServerRow own = parmRepository.GetParmSvr(ParmServerType.Channel, setting.ServerIp);

            if (own == null)
            {
                throw Fatal(logger, $"TblParmSvr has no channel server (mType = {(byte)ParmServerType.Channel}) with mMajorIp = '{setting.ServerIp}'. " +
                                    "Add the row, or point \"LoginSetting:ServerIp\" at the address the channel is registered under");
            }

            if (own.TcpPort <= 0)
            {
                throw Fatal(logger, $"Channel server {own.SvrNo} on {setting.ServerIp} has no listen port: TblParmSvr.mTcpPort is {own.TcpPort}");
            }

            SvrNo = own.SvrNo;
            WorldNo = own.WorldNo;
            TcpPort = own.TcpPort;
            ServerIp = setting.ServerIp;

            logger.LogInformation("Resolved own identity from TblParmSvr: channel server {SvrNo}, world {WorldNo}, port {Port} on {Ip}",
                SvrNo, WorldNo, TcpPort, ServerIp);
        }

        /// <summary>
        ///     Logs the reason and builds the exception that stops the host
        /// </summary>
        private static InvalidOperationException Fatal(ILogger<OwnChannelInfo> logger, string message)
        {
            string text = "Login server cannot start: " + message;

            logger.LogCritical(text);

            return new InvalidOperationException(text);
        }

        /// <summary>
        ///     Server number, TblParmSvr.mSvrNo
        /// </summary>
        public short SvrNo { get; }

        /// <summary>
        ///     World the channel belongs to, TblParmSvr.mWorldNo
        /// </summary>
        public short WorldNo { get; }

        /// <summary>
        ///     TCP port the channel listens on, TblParmSvr.mTcpPort
        /// </summary>
        public int TcpPort { get; }

        /// <summary>
        ///     Address the channel listens on, TblParmSvr.mMajorIp
        /// </summary>
        public string ServerIp { get; }
    }
}

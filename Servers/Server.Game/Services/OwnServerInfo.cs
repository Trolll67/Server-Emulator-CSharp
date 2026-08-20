using System;
using Database.Fnl.Parm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Models.Settings;

namespace Server.Game.Services
{
    /// <summary>
    ///     This game server's own identity, resolved once at startup from TblParmSvr by its address
    ///     and kind (field): server number, world number and listen port. TblParmSvr is the only
    ///     source of these: the channel server hands the client the very same row, so an identity
    ///     taken from anywhere else would put the server on a port nobody is told about. A missing
    ///     row stops the server. Registered as a singleton: the lookup runs once, not per packet.
    /// </summary>
    public class OwnServerInfo
    {
        public OwnServerInfo(IFnlParmRepository parmRepository, IOptions<GameSetting> gameSetting, ILogger<OwnServerInfo> logger)
        {
            GameSetting setting = gameSetting.Value;
            ParmServerRow own = parmRepository.GetParmSvr(ParmServerType.Field, setting.ServerIp);

            if (own == null)
            {
                throw Fatal(logger, $"TblParmSvr has no field server (mType = {(byte)ParmServerType.Field}) with mMajorIp = '{setting.ServerIp}'. " +
                                    "Add the row, or point \"GameSetting:ServerIp\" at the address the field server is registered under");
            }

            if (own.TcpPort <= 0)
            {
                throw Fatal(logger, $"Field server {own.SvrNo} on {setting.ServerIp} has no listen port: TblParmSvr.mTcpPort is {own.TcpPort}");
            }

            SvrNo = own.SvrNo;
            WorldNo = own.WorldNo;
            TcpPort = own.TcpPort;

            logger.LogInformation("Resolved own identity from TblParmSvr: field server {SvrNo}, world {WorldNo}, port {Port} on {Ip}",
                SvrNo, WorldNo, TcpPort, setting.ServerIp);
        }

        /// <summary>
        ///     Logs the reason and builds the exception that stops the host
        /// </summary>
        private static InvalidOperationException Fatal(ILogger<OwnServerInfo> logger, string message)
        {
            string text = "Game server cannot start: " + message;

            logger.LogCritical(text);

            return new InvalidOperationException(text);
        }

        /// <summary>
        ///     Server number, TblParmSvr.mSvrNo
        /// </summary>
        public short SvrNo { get; }

        /// <summary>
        ///     World the server belongs to, TblParmSvr.mWorldNo
        /// </summary>
        public short WorldNo { get; }

        /// <summary>
        ///     TCP port the server listens on, TblParmSvr.mTcpPort
        /// </summary>
        public int TcpPort { get; }
    }
}

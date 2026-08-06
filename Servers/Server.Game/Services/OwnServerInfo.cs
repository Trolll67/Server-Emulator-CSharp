using Database.Fnl.Parm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Models.Settings;

namespace Server.Game.Services
{
    /// <summary>
    ///     This game server's own identity, resolved once at startup from TblParmSvr by its address
    ///     and kind (field): server number, world number and listen port. Falls back to the values
    ///     in gamesettings.json when TblParmSvr has no matching row, so a missing entry does not stop
    ///     the server. Registered as a singleton: the lookup runs once, not per packet.
    /// </summary>
    public class OwnServerInfo
    {
        public OwnServerInfo(IFnlParmRepository parmRepository, IOptions<GameSetting> gameSetting, ILogger<OwnServerInfo> logger)
        {
            GameSetting setting = gameSetting.Value;
            ParmServerRow own = parmRepository.GetParmSvr(ParmServerType.Field, setting.ServerIp);

            if (own != null)
            {
                SvrNo = own.SvrNo;
                WorldNo = own.WorldNo;
                TcpPort = own.TcpPort > 0 ? own.TcpPort : setting.ServerPort;
                ResolvedFromDatabase = true;

                logger.LogInformation("Resolved own identity from TblParmSvr: field server {SvrNo}, world {WorldNo}, port {Port} on {Ip}",
                    SvrNo, WorldNo, TcpPort, setting.ServerIp);

                return;
            }

            SvrNo = setting.Id;
            WorldNo = setting.Id;
            TcpPort = setting.ServerPort;
            ResolvedFromDatabase = false;

            logger.LogWarning("TblParmSvr has no field server on {Ip}, using id {Id} and port {Port} from gamesettings.json",
                setting.ServerIp, setting.Id, setting.ServerPort);
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

        /// <summary>
        ///     True when the values came from TblParmSvr, false when they fell back to gamesettings.json
        /// </summary>
        public bool ResolvedFromDatabase { get; }
    }
}

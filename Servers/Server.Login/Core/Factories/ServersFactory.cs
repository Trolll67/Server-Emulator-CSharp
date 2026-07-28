using System.Collections.Generic;
using Database.Fnl.Parm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Server.Login.Models.Send;
using Packets.Server.Login.Models.Send.Models;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Settings;
using Server.Login.Network;

namespace Server.Login.Core.Factories
{
    /// <inheritdoc/>
    public class ServersFactory : IServersFactory
    {
        private readonly IFnlParmRepository _parmRepository;
        private readonly ILogger<ServersFactory> _logger;
        private readonly LoginSetting _loginSetting;

        /// <summary>
        ///     Server list of the channel, read from FNLParm once and kept here: the client asks for it
        ///     on every login, and TblParmSvr changes only when the servers themselves are reconfigured.
        ///     Sessions live on socket threads, so the built list is never touched again and a new read
        ///     only replaces the reference: volatile makes the filled list visible to the other threads
        /// </summary>
        private volatile List<ServerModel> _servers;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="parmRepository"></param>
        /// <param name="logger"></param>
        /// <param name="loginSetting"></param>
        public ServersFactory(IFnlParmRepository parmRepository, ILogger<ServersFactory> logger, IOptions<LoginSetting> loginSetting)
        {
            _parmRepository = parmRepository;
            _logger = logger;
            _loginSetting = loginSetting.Value;
        }

        /// <inheritdoc/>
        public void SendServers(LoginSession loginSession)
        {
            SendServersModel sendServersModel = new SendServersModel
            {
                AccountId = loginSession.SessionLogin.UserNo,
                SessionId = loginSession.SessionLogin.CertifiedKey,
                Servers = GetServers()
            };

            loginSession.Send(sendServersModel);
        }

        /// <inheritdoc/>
        public void SendRefreshedServers(LoginSession loginSession)
        {
            RefreshedServersModel refreshedServersModel = new RefreshedServersModel()
            {
                Servers = LoadServers()
            };

            loginSession.Send(refreshedServersModel);
        }

        /// <inheritdoc/>
        public void SendSelectedServer(LoginSession loginSession)
        {
            SelectedServerModel selectedServerModel = new SelectedServerModel();

            loginSession.Send(selectedServerModel);
        }

        /// <inheritdoc/>
        public bool IsKnownServer(short serverId)
        {
            // The very same list the client was given in 3101: what it sees and what it may choose
            // never disagree, and the choice costs no round trip to FNLParm
            foreach (ServerModel server in GetServers())
            {
                if (server.Id == serverId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Write all servers in the packet
        /// </summary>
        /// <returns></returns>
        private List<ServerModel> GetServers()
        {
            return _servers ?? LoadServers();
        }

        /// <summary>
        ///     Rereads the server list from FNLParm and caches it
        /// </summary>
        /// <returns>Field servers of the own family, empty list when the database is not readable</returns>
        private List<ServerModel> LoadServers()
        {
            List<ServerModel> serverModels = new List<ServerModel>();

            try
            {
                short svrNo = GetOwnSvrNo();

                foreach (FamilyServerRow server in _parmRepository.GetFamily(svrNo))
                {
                    // The family also carries the manager and the gateway, the client list is field servers only
                    if (server.Type != ParmServerType.Field)
                    {
                        continue;
                    }

                    ServerModel serverModel = new ServerModel
                    {
                        Id = server.SvrNo,
                        Name = server.Desc,
                        ServerIp = server.MajorIp,
                        ServerPort = (short)server.TcpPort,
                        Status = true, // ASSUMPTION: TblParmSvr has no "is alive" column, the channel pings the family instead
                        Hidden = false, // ASSUMPTION: no such column in TblParmSvr
                        Type = ServerType.Server, // ASSUMPTION: no such column in TblParmSvr
                        Congestion = CongestionType.Low // ASSUMPTION: the original counts the online users of the field server
                    };

                    serverModels.Add(serverModel);
                }
            }
            catch (SqlException e)
            {
                // UspGetFamilyEx raises 'Invalid SvrNo(%d)' for an unknown number instead of returning
                // an empty set. The client gets an empty list, the login server keeps running,
                // and the next packet tries the database again: the failed read is not cached
                _logger.LogError(e, "Can not read the server list from FNLParm, the client gets an empty list");

                return serverModels;
            }

            _servers = serverModels;

            return serverModels;
        }

        /// <summary>
        ///     Own number of the channel server: the original looks it up by its own address
        /// </summary>
        /// <returns>TblParmSvr.mSvrNo of this channel</returns>
        private short GetOwnSvrNo()
        {
            ParmServerRow channel = _parmRepository.GetParmSvr(ParmServerType.Channel, _loginSetting.ServerIp);

            if (channel != null)
            {
                return channel.SvrNo;
            }

            _logger.LogWarning($"TblParmSvr has no channel server on {_loginSetting.ServerIp}, taking ChannelSvrNo {_loginSetting.ChannelSvrNo} from loginsettings.json");

            return _loginSetting.ChannelSvrNo;
        }
    }
}

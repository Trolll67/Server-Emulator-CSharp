using System.Collections.Generic;
using Database.Fnl.Parm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Server.Login.Models.Send;
using Packets.Server.Login.Models.Send.Models;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Family;
using Server.Login.Models.Settings;
using Server.Login.Network;
using Server.Login.Services;
using Server.Login.Services.Family;

namespace Server.Login.Core.Factories
{
    /// <inheritdoc/>
    public class ServersFactory : IServersFactory
    {
        private readonly IFnlParmRepository _parmRepository;
        private readonly FamilyRegistry _familyRegistry;
        private readonly OwnChannelInfo _ownChannelInfo;
        private readonly ILogger<ServersFactory> _logger;
        private readonly LoginSetting _loginSetting;

        /// <summary>
        ///     Options of this channel from TblParmSvrOp, by option number. Read on the first login
        ///     and kept: the table changes only on a reconfiguration, and the original reads it once
        ///     at startup as well. Null until the first successful read, a failed read is not cached
        /// </summary>
        private volatile IReadOnlyDictionary<int, bool> _options;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="parmRepository"></param>
        /// <param name="familyRegistry"></param>
        /// <param name="ownChannelInfo"></param>
        /// <param name="logger"></param>
        /// <param name="loginSetting"></param>
        public ServersFactory(IFnlParmRepository parmRepository, FamilyRegistry familyRegistry, OwnChannelInfo ownChannelInfo, ILogger<ServersFactory> logger, IOptions<LoginSetting> loginSetting)
        {
            _parmRepository = parmRepository;
            _familyRegistry = familyRegistry;
            _ownChannelInfo = ownChannelInfo;
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
                Servers = GetServers()
            };

            loginSession.Send(refreshedServersModel);
        }

        /// <inheritdoc/>
        public void SendArsAuth(LoginSession loginSession)
        {
            // The phone confirmation is not reproduced here, and the original answers exactly this
            // when its own content flag is off: the state stays None and the client goes on
            ArsAuthAckModel arsAuthAckModel = new ArsAuthAckModel { State = ArsAuthState.None };

            loginSession.Send(arsAuthAckModel);
        }

        /// <inheritdoc/>
        public bool IsKnownServer(short serverId)
        {
            // The original looks the chosen server up in the very same roster the list was built
            // from, and takes anything that is in this world
            return _familyRegistry.IsKnown(serverId);
        }

        /// <summary>
        ///     Builds the client server list out of the roster of this world
        /// </summary>
        /// <returns>
        ///     Field servers of this world, the ones on the line marked as such. A server that is
        ///     configured but not running stays in the list and is shown as unavailable, which is
        ///     what the original does as well
        /// </returns>
        private List<ServerModel> GetServers()
        {
            List<ServerModel> serverModels = new List<ServerModel>();

            // The world also holds the manager and the gateway, the client list is field servers only
            foreach (FamilyServer server in _familyRegistry.GetServers(ParmServerType.Field))
            {
                serverModels.Add(new ServerModel
                {
                    Id = server.SvrNo,
                    Name = server.Desc,
                    ServerIp = server.MajorIp,
                    ServerPort = (short)server.TcpPort,
                    Status = server.IsConnected,
                    Congestion = GetCongestion(server),
                    Type = (ServerType)server.SupportType,
                    IsChaosBattle = server.SvrInfo == ParmServerInfo.ChaosBattle
                });
            }

            return serverModels;
        }

        /// <summary>
        ///     Turns the session counts a field server reports into the four step scale the client
        ///     draws. The steps and their order are the ones of the original: the emptiest server
        ///     is the lowest value, and the numbers the steps are counted against are per country
        ///     there and configurable here
        /// </summary>
        /// <param name="server">Server of this world with the state it last reported</param>
        private CongestionType GetCongestion(FamilyServer server)
        {
            // A server that has not told anything yet is holding nobody as far as we know
            int used = server.IsConnected ? server.UsedSessions : 0;

            if (used < _loginSetting.ServerLowLoadSessions)
            {
                return CongestionType.Low;
            }

            if (used < _loginSetting.ServerNormalLoadSessions)
            {
                return CongestionType.Medium;
            }

            return used >= server.MaxSesCnt - _loginSetting.ServerFullReserveSessions
                ? CongestionType.Maximum
                : CongestionType.High;
        }

        /// <inheritdoc/>
        public bool IsPasswordCheckedInDatabase()
        {
            // Refusing every login is worse than the original behaviour of this server, whose option
            // is off anyway, so an unreadable table leaves the check off
            return IsOptionOn(ParmServerOption.CertifyToPasswordInDb, false);
        }

        /// <inheritdoc/>
        public bool IsAccountCreatedOnLogin()
        {
            // The option forbids the creation, so the flag of the procedure is its negation.
            // An unreadable table counts as "forbidden": writing accounts into FNLAccount by
            // an option nobody could read is the one guess that is not safe to make
            return !IsOptionOn(ParmServerOption.DoNotAccountAutomaticCreation, true);
        }

        /// <summary>
        ///     Reads an option of this channel from the cached set of TblParmSvrOp
        /// </summary>
        /// <param name="opNo">Option number, see <see cref="ParmServerOption"/></param>
        /// <param name="whenUnreadable">Value to assume when FNLParm can not be read</param>
        /// <returns>TblParmSvrOp.mIsSetup of the option, false when the channel has no such row</returns>
        private bool IsOptionOn(int opNo, bool whenUnreadable)
        {
            IReadOnlyDictionary<int, bool> options = _options ?? LoadOptions();

            if (options == null)
            {
                return whenUnreadable;
            }

            return options.TryGetValue(opNo, out bool isSetup) && isSetup;
        }

        /// <summary>
        ///     Rereads the options of this channel from FNLParm and caches them
        /// </summary>
        /// <returns>Options by number, null when the database is not readable</returns>
        private IReadOnlyDictionary<int, bool> LoadOptions()
        {
            Dictionary<int, bool> options = new Dictionary<int, bool>();

            try
            {
                foreach (ParmServerOptionRow option in _parmRepository.GetServerOptions(_ownChannelInfo.SvrNo))
                {
                    options[option.OpNo] = option.IsSetup;
                }
            }
            catch (SqlException e)
            {
                // The failed read is not cached, the next login retries. Every caller decides
                // for itself what an unknown option means for it
                _logger.LogError(e, "Can not read the server options from FNLParm");

                return null;
            }

            _logger.LogInformation(
                options.TryGetValue(ParmServerOption.CertifyToPasswordInDb, out bool isPasswordChecked) && isPasswordChecked
                    ? "Option 'Certify To Password In DB' is on: the password is compared against TblUser"
                    : "Option 'Certify To Password In DB' is off: the password is not checked, as in the original");

            _logger.LogInformation(
                options.TryGetValue(ParmServerOption.DoNotAccountAutomaticCreation, out bool isCreationForbidden) && isCreationForbidden
                    ? "Option 'Do Not Account Automatic Creation' is on: an unknown login is refused"
                    : "Option 'Do Not Account Automatic Creation' is off: an unknown login creates the account");

            _options = options;

            return options;
        }

    }
}

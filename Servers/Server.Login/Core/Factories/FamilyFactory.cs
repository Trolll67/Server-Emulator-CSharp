using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Database.Fnl.Parm;
using Packets.Core.Models.Common;
using Packets.Core.Models.Family;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Settings;
using Server.Login.Network;
using Server.Login.Services;
using Server.Login.Services.Family;

namespace Server.Login.Core.Factories
{
    /// <inheritdoc/>
    public class FamilyFactory : IFamilyFactory
    {
        private readonly FamilyRegistry _familyRegistry;
        private readonly OwnChannelInfo _ownChannelInfo;
        private readonly LoginServer _loginServer;
        private readonly LoginSetting _loginSetting;
        private readonly ILogger<FamilyFactory> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public FamilyFactory(FamilyRegistry familyRegistry, OwnChannelInfo ownChannelInfo, LoginServer loginServer, IOptions<LoginSetting> loginSetting, ILogger<FamilyFactory> logger)
        {
            _familyRegistry = familyRegistry;
            _ownChannelInfo = ownChannelInfo;
            _loginServer = loginServer;
            _loginSetting = loginSetting.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void SendLoginFamilyAck(LoginSession loginSession)
        {
            loginSession.Send(new LoginFamilyAckModel { SvrNo = _ownChannelInfo.SvrNo });
        }

        /// <inheritdoc/>
        public void SendLoginFamilyNak(LoginSession loginSession, short svrNo, FamilyErrorType error)
        {
            loginSession.Send(new LoginFamilyNakModel { SvrNo = svrNo, Error = error });
        }

        /// <inheritdoc/>
        public void SendKick(int userNo, long addressNumber, NakErrorType reason)
        {
            KickPcReqModel kick = new KickPcReqModel
            {
                IsPc = false,
                UserNo = userNo,
                Reason = reason,
                AddressNumber = addressNumber
            };

            IReadOnlyList<LoginSession> sessions = _familyRegistry.GetSessions(ParmServerType.Field);

            foreach (LoginSession session in sessions)
            {
                session.Send(kick);
            }

            _logger.LogInformation("Asked {Count} game servers of this world to throw account {UserNo} out: {Reason}",
                sessions.Count, userNo, reason);
        }

        /// <inheritdoc/>
        public void BroadcastKeepAlive()
        {
            KeepAliveNullReqModel keepAlive = new KeepAliveNullReqModel();

            foreach (LoginSession session in _familyRegistry.GetSessions())
            {
                session.Send(keepAlive);
            }
        }

        /// <inheritdoc/>
        public void BroadcastOwnState()
        {
            IReadOnlyList<LoginSession> sessions = _familyRegistry.GetSessions();

            if (sessions.Count == 0)
            {
                return;
            }

            // The field of the free sessions is named for the busy ones in the original as well,
            // see the packet of the state: what travels is how much room is left
            short maxSessions = _loginSetting.MaxSessions;
            short freeSessions = (short)System.Math.Max(0, maxSessions - _loginServer.ClientSessions);

            NotifySvrStateAckModel state = new NotifySvrStateAckModel
            {
                SvrNo = _ownChannelInfo.SvrNo,
                MaxSesCnt = maxSessions,
                BusySesCnt = freeSessions
            };

            foreach (LoginSession session in sessions)
            {
                session.Send(state);
            }

            _logger.LogDebug("Told the world about {Used} of {Max} sessions in use", maxSessions - freeSessions, maxSessions);
        }
    }
}

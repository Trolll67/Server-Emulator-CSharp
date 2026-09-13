using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Database.Fnl.Parm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Interfaces;
using Packets.Core.Models.Family;
using Server.Game.Models.Settings;
using Server.Game.Network;

namespace Server.Game.Services.Family
{
    /// <summary>
    ///     Keeps this field server on the line of its world. The channel is the one that hands the
    ///     client the server list, and it only shows the servers it holds a link to - so a field
    ///     server that never calls its channel is a server no player is able to see.
    ///
    ///     Who connects to whom is the kinship of the original: the channel is the parent of a
    ///     field server, and a server opens the link to its parents itself. The address and the
    ///     port of the channel come from TblParmSvr, the same table the channel is registered in
    /// </summary>
    public class FamilyLinkService : IHostedService
    {
        private readonly IFnlParmRepository _parmRepository;
        private readonly OwnServerInfo _ownServerInfo;
        private readonly GameServer _gameServer;
        private readonly IRegisterHandlerService _registerHandlerService;
        private readonly ILogger<FamilyLinkService> _logger;
        private readonly GameSetting _gameSetting;

        private readonly List<FamilySession> _sessions = new List<FamilySession>();
        private readonly object _lock = new object();

        private CancellationTokenSource _cancellation;
        private Timer _timer;

        // A round that runs long must not be joined by the next one: connecting a link that is
        // already being connected would open a second socket to the same channel
        private int _ticking;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public FamilyLinkService(IFnlParmRepository parmRepository, OwnServerInfo ownServerInfo, GameServer gameServer, IRegisterHandlerService registerHandlerService, IOptions<GameSetting> gameSetting, ILogger<FamilyLinkService> logger)
        {
            _parmRepository = parmRepository;
            _ownServerInfo = ownServerInfo;
            _gameServer = gameServer;
            _registerHandlerService = registerHandlerService;
            _gameSetting = gameSetting.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellation = new CancellationTokenSource();

            foreach (FamilyServerRow channel in GetChannels())
            {
                if (!IPAddress.TryParse(channel.MajorIp, out IPAddress address))
                {
                    _logger.LogError("Channel {SvrNo} of this world has an address TblParmSvr can not be read as one", channel.SvrNo);
                    continue;
                }

                FamilySession session = new FamilySession(
                    new IPEndPoint(address, channel.TcpPort),
                    channel.SvrNo,
                    _registerHandlerService,
                    _logger,
                    OnLinkOpened,
                    OnLinkClosed);

                lock (_lock)
                {
                    _sessions.Add(session);
                }
            }

            if (_sessions.Count == 0)
            {
                _logger.LogWarning("This world has no channel server in TblParmSvr: nobody will show this server to the players");

                return Task.CompletedTask;
            }

            // The very first round happens right away, so the server is on the line at startup and
            // not one interval later
            TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, _gameSetting.FamilyKeepAliveSeconds));

            _timer = new Timer(Tick, null, TimeSpan.Zero, interval);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellation?.Cancel();
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();

            List<FamilySession> sessions;

            lock (_lock)
            {
                sessions = _sessions.ToList();
                _sessions.Clear();
            }

            foreach (FamilySession session in sessions)
            {
                session.Dispose();
            }

            _cancellation?.Dispose();

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Channels of this world out of TblParmSvr
        /// </summary>
        private IReadOnlyList<FamilyServerRow> GetChannels()
        {
            try
            {
                return _parmRepository.GetFamily(_ownServerInfo.SvrNo)
                    .Where(row => row.Type == ParmServerType.Channel)
                    .ToList();
            }
            catch (SqlException e)
            {
                // The server is able to run without the link, it just stays invisible in the list.
                // Falling over here would cost the players a world that otherwise works
                _logger.LogError(e, "Can not read the world of server {SvrNo} from FNLParm, the family link is not started", _ownServerInfo.SvrNo);

                return new List<FamilyServerRow>();
            }
        }

        /// <summary>
        ///     One round of the timer: reconnect what is down, tell the rest how loaded we are
        /// </summary>
        private void Tick(object state)
        {
            if (Interlocked.CompareExchange(ref _ticking, 1, 0) != 0)
            {
                return;
            }

            try
            {
                List<FamilySession> sessions;

                lock (_lock)
                {
                    sessions = _sessions.ToList();
                }

                foreach (FamilySession session in sessions)
                {
                    if (!session.IsConnected)
                    {
                        // The result is handled in the callbacks of the link itself
                        _ = session.ConnectAsync(_cancellation.Token);
                        continue;
                    }

                    if (!session.IsLogined)
                    {
                        continue;
                    }

                    session.Send(new KeepAliveNullReqModel());
                    session.Send(BuildState());
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can not reach the channels of this world");
            }
            finally
            {
                Interlocked.Exchange(ref _ticking, 0);
            }
        }

        /// <summary>
        ///     Names this server to the channel as soon as the link is up
        /// </summary>
        private void OnLinkOpened(FamilySession session)
        {
            session.Send(new LoginFamilyReqModel
            {
                SvrNo = _ownServerInfo.SvrNo,
                Version = _gameSetting.FamilyVersion
            });
        }

        /// <summary>
        ///     Nothing to clean up here: the next round of the timer connects the link again
        /// </summary>
        private void OnLinkClosed(FamilySession session)
        {
        }

        /// <summary>
        ///     How loaded this server is. The field of the free sessions is named for the busy ones
        ///     in the original as well, see the packet of the state: what travels is how much room
        ///     is left, and the channel counts the players out of the difference
        /// </summary>
        private NotifySvrStateAckModel BuildState()
        {
            short maxSessions = _gameSetting.MaxSessions;
            short freeSessions = (short)Math.Max(0, maxSessions - _gameServer.ConnectedSessions);

            return new NotifySvrStateAckModel
            {
                SvrNo = _ownServerInfo.SvrNo,
                MaxSesCnt = maxSessions,
                BusySesCnt = freeSessions
            };
        }
    }
}

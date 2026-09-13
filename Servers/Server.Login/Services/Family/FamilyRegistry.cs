using System.Collections.Generic;
using System.Linq;
using Database.Fnl.Parm;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Packets.Core.Models.Family;
using Server.Login.Models.Family;
using Server.Login.Network;

namespace Server.Login.Services.Family
{
    /// <summary>
    ///     Who of this world is on the line right now. The roster itself is read once from
    ///     TblParmSvr - the same procedure that the original calls at startup - and the servers
    ///     fill in the rest themselves: they open the family link, name their number and keep
    ///     telling how loaded they are.
    ///
    ///     This is what the client server list is built from. A server that is configured but not
    ///     running is in the roster and marked as not on the line, exactly as the original shows it.
    ///     CFamilyMgr of the original
    /// </summary>
    public class FamilyRegistry
    {
        private readonly IFnlParmRepository _parmRepository;
        private readonly OwnChannelInfo _ownChannelInfo;
        private readonly ILogger<FamilyRegistry> _logger;

        private readonly object _lock = new object();

        /// <summary>
        ///     Roster by server number, built on the first use. Null while TblParmSvr could not be
        ///     read: a failed read is not remembered, the next caller tries again
        /// </summary>
        private Dictionary<short, FamilyServer> _servers;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="parmRepository"></param>
        /// <param name="ownChannelInfo"></param>
        /// <param name="logger"></param>
        public FamilyRegistry(IFnlParmRepository parmRepository, OwnChannelInfo ownChannelInfo, ILogger<FamilyRegistry> logger)
        {
            _parmRepository = parmRepository;
            _ownChannelInfo = ownChannelInfo;
            _logger = logger;
        }

        /// <summary>
        ///     Takes a server of the world onto the line
        /// </summary>
        /// <param name="svrNo">Number the server named itself with</param>
        /// <param name="session">Family link the server came on</param>
        /// <returns>Null when the server is on the line now, otherwise the reason it was refused</returns>
        public FamilyErrorType? Connected(short svrNo, LoginSession session)
        {
            lock (_lock)
            {
                FamilyServer server = FindLocked(svrNo);

                if (server == null)
                {
                    _logger.LogWarning("Server {SvrNo} asked to join the family of channel {Channel}, TblParmSvr has no such neighbour",
                        svrNo, _ownChannelInfo.SvrNo);

                    return FamilyErrorType.FamilyNot;
                }

                // The original compares the address of the link with the one TblParmSvr holds for
                // that server: the number alone would let anybody who reaches the port claim to be
                // a field server of this world and get into the list the clients are given
                string address = GetAddress(session);

                if (!string.Equals(server.MajorIp, address))
                {
                    _logger.LogWarning("Server {SvrNo} asked to join the family from {Address}, TblParmSvr registers it on {Expected}",
                        svrNo, address, server.MajorIp);

                    return FamilyErrorType.FamilyInvalidIp;
                }

                if (server.Session != null)
                {
                    _logger.LogWarning("Server {SvrNo} asked to join the family while it is already on the line", svrNo);

                    return FamilyErrorType.FamilyAlreadyConnect;
                }

                server.Session = session;
                session.FamilySvrNo = svrNo;

                _logger.LogInformation("Server {SvrNo} ({Desc}) of world {World} is on the line", svrNo, server.Desc, server.WorldNo);

                return null;
            }
        }

        /// <summary>
        ///     Takes a server off the line when its link is gone
        /// </summary>
        /// <param name="svrNo">Number of the server</param>
        /// <param name="session">Link that is gone, only it is allowed to clear the slot</param>
        public void Disconnected(short svrNo, LoginSession session)
        {
            lock (_lock)
            {
                FamilyServer server = FindLocked(svrNo);

                // A link that lost the race to a fresh one of the same server must not clear the
                // slot the fresh one has taken
                if (server?.Session != session)
                {
                    return;
                }

                server.Session = null;
                server.MaxSesCnt = 0;
                server.BusySesCnt = 0;

                _logger.LogWarning("Server {SvrNo} ({Desc}) is off the line", svrNo, server.Desc);
            }
        }

        /// <summary>
        ///     Remembers how loaded a server of the world is
        /// </summary>
        /// <param name="svrNo">Number of the server</param>
        /// <param name="maxSesCnt">How many sessions it is able to hold</param>
        /// <param name="busySesCnt">How many of those are still free, see the packet of the state</param>
        public void SetState(short svrNo, short maxSesCnt, short busySesCnt)
        {
            lock (_lock)
            {
                FamilyServer server = FindLocked(svrNo);

                if (server == null)
                {
                    return;
                }

                server.MaxSesCnt = maxSesCnt;
                server.BusySesCnt = busySesCnt;
            }
        }

        /// <summary>
        ///     Servers of the world of the asked kind, with the state they last reported
        /// </summary>
        /// <param name="type">Kind of server to take</param>
        /// <returns>Copies, safe to read outside the lock</returns>
        public IReadOnlyList<FamilyServer> GetServers(ParmServerType type)
        {
            lock (_lock)
            {
                Dictionary<short, FamilyServer> servers = LoadLocked();

                if (servers == null)
                {
                    return new List<FamilyServer>();
                }

                return servers.Values
                    .Where(server => server.Type == type)
                    .OrderBy(server => server.SvrNo)
                    .Select(Copy)
                    .ToList();
            }
        }

        /// <summary>
        ///     Is there such a server in this world at all? The original asks its roster the very
        ///     same way and does not care whether the server is on the line: a server that is down
        ///     is shown as unavailable in the list, and the client does not offer it
        /// </summary>
        /// <param name="svrNo">Number of the server</param>
        /// <returns>True when the server is in the roster of this world</returns>
        public bool IsKnown(short svrNo)
        {
            lock (_lock)
            {
                return FindLocked(svrNo) != null;
            }
        }

        /// <summary>
        ///     Does this channel know a server of that world? An account belongs to a world, and
        ///     the original lets one of another world in only when a server of it is a neighbour
        /// </summary>
        /// <param name="worldNo">Number of the world the account belongs to</param>
        public bool HasWorld(short worldNo)
        {
            lock (_lock)
            {
                Dictionary<short, FamilyServer> servers = LoadLocked();

                if (servers == null)
                {
                    return false;
                }

                foreach (FamilyServer server in servers.Values)
                {
                    if (server.WorldNo == worldNo)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Links of every server of the world that is on the line
        /// </summary>
        /// <param name="type">Kind of server to take, null for every kind</param>
        /// <returns>Copies of the references, safe to use outside the lock</returns>
        public IReadOnlyList<LoginSession> GetSessions(ParmServerType? type = null)
        {
            lock (_lock)
            {
                Dictionary<short, FamilyServer> servers = LoadLocked();

                if (servers == null)
                {
                    return new List<LoginSession>();
                }

                return servers.Values
                    .Where(server => server.Session != null && (type == null || server.Type == type))
                    .Select(server => server.Session)
                    .ToList();
            }
        }

        /// <summary>
        ///     Finds a server of the world, loading the roster when it is the first call
        /// </summary>
        private FamilyServer FindLocked(short svrNo)
        {
            Dictionary<short, FamilyServer> servers = LoadLocked();

            if (servers == null)
            {
                return null;
            }

            return servers.TryGetValue(svrNo, out FamilyServer server) ? server : null;
        }

        /// <summary>
        ///     Reads the roster of the world out of TblParmSvr once
        /// </summary>
        /// <returns>Roster by server number, null when FNLParm can not be read</returns>
        private Dictionary<short, FamilyServer> LoadLocked()
        {
            if (_servers != null)
            {
                return _servers;
            }

            Dictionary<short, FamilyServer> servers = new Dictionary<short, FamilyServer>();

            try
            {
                foreach (FamilyServerRow row in _parmRepository.GetFamily(_ownChannelInfo.SvrNo))
                {
                    // The procedure gives back the whole world, this channel included
                    if (row.SvrNo == _ownChannelInfo.SvrNo)
                    {
                        continue;
                    }

                    servers[row.SvrNo] = new FamilyServer
                    {
                        SvrNo = row.SvrNo,
                        Type = row.Type,
                        WorldNo = row.WorldNo,
                        MajorIp = row.MajorIp,
                        TcpPort = row.TcpPort,
                        Desc = row.Desc,
                        SupportType = row.SupportType,
                        SvrInfo = row.SvrInfo
                    };
                }
            }
            catch (SqlException e)
            {
                // UspGetFamilyEx raises 'Invalid SvrNo(%d)' for an unknown number instead of
                // returning an empty set. The channel keeps running with an empty world, and the
                // next caller tries the database again: the failed read is not remembered
                _logger.LogError(e, "Can not read the world of channel {SvrNo} from FNLParm", _ownChannelInfo.SvrNo);

                return null;
            }

            _logger.LogInformation("World of channel {SvrNo}: {Count} neighbour servers in TblParmSvr", _ownChannelInfo.SvrNo, servers.Count);

            _servers = servers;

            return _servers;
        }

        /// <summary>
        ///     Copy of a roster entry for a reader outside the lock
        /// </summary>
        private static FamilyServer Copy(FamilyServer server)
        {
            return new FamilyServer
            {
                SvrNo = server.SvrNo,
                Type = server.Type,
                WorldNo = server.WorldNo,
                MajorIp = server.MajorIp,
                TcpPort = server.TcpPort,
                Desc = server.Desc,
                SupportType = server.SupportType,
                SvrInfo = server.SvrInfo,
                Session = server.Session,
                MaxSesCnt = server.MaxSesCnt,
                BusySesCnt = server.BusySesCnt
            };
        }

        /// <summary>
        ///     Address the link came from
        /// </summary>
        private static string GetAddress(LoginSession session)
        {
            return session.Socket?.RemoteEndPoint is System.Net.IPEndPoint endPoint
                ? endPoint.Address.ToString()
                : string.Empty;
        }
    }
}

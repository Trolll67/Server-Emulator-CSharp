using Database.Fnl.Parm;
using Server.Login.Network;

namespace Server.Login.Models.Family
{
    /// <summary>
    ///     One server of this channel's world. The static part comes from TblParmSvr and never
    ///     changes while the channel runs; the rest is filled by the server itself over the family
    ///     link: whether it is on the line at all and how loaded it is. CFamily of the original
    /// </summary>
    public class FamilyServer
    {
        /// <summary>
        ///     Server number, TblParmSvr.mSvrNo
        /// </summary>
        public short SvrNo { get; set; }

        /// <summary>
        ///     Kind of the server: only the field ones go into the list the client sees
        /// </summary>
        public ParmServerType Type { get; set; }

        /// <summary>
        ///     World the server belongs to
        /// </summary>
        public short WorldNo { get; set; }

        /// <summary>
        ///     Address the clients connect to, and the only address this server is allowed to
        ///     open the family link from
        /// </summary>
        public string MajorIp { get; set; }

        /// <summary>
        ///     Port the server listens on
        /// </summary>
        public int TcpPort { get; set; }

        /// <summary>
        ///     Name shown in the client server list
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        ///     Kind of service the server offers, goes to the client as is
        /// </summary>
        public ParmSupportServerType SupportType { get; set; }

        /// <summary>
        ///     Extra mark on the server, goes to the client as is
        /// </summary>
        public ParmServerInfo SvrInfo { get; set; }

        /// <summary>
        ///     Family link of this server, null while the server is not on the line. Written and
        ///     read under the lock of the registry
        /// </summary>
        public LoginSession Session { get; set; }

        /// <summary>
        ///     How many sessions the server is able to hold, as it told us last
        /// </summary>
        public short MaxSesCnt { get; set; }

        /// <summary>
        ///     How many of those are still free, as it told us last. The name is the one of the
        ///     original and means the opposite of what it says, see the packet of the state
        /// </summary>
        public short BusySesCnt { get; set; }

        /// <summary>
        ///     Is the server on the line right now?
        /// </summary>
        public bool IsConnected => Session != null;

        /// <summary>
        ///     How many sessions the server is holding right now
        /// </summary>
        public int UsedSessions => MaxSesCnt - BusySesCnt;
    }
}

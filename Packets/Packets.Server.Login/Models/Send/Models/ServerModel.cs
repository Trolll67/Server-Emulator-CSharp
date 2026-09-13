namespace Packets.Server.Login.Models.Send.Models
{
    /// <summary>
    ///     Server model
    /// </summary>
    public class ServerModel
    {
        public short Id { get; set; }

        public string Name { get; set; }

        /// <summary>
        ///     Is the server on the line? The original marks a server of the world that holds no
        ///     link to the channel as unavailable and still keeps it in the list
        /// </summary>
        public bool Status { get; set; }

        public CongestionType Congestion { get; set; }

        /// <summary>
        ///     TblParmSvr.mSupportType, four bytes on the wire
        /// </summary>
        public ServerType Type { get; set; }

        /// <summary>
        ///     TblParmSvr.mSvrInfo says the server is the one of the Chaos battle, four bytes
        ///     on the wire
        /// </summary>
        public bool IsChaosBattle { get; set; }

        public string ServerIp { get; set; }
        public short ServerPort { get; set; }
    }

    /// <summary>
    ///     What kind of service the server offers, ESupportServerType of the original
    /// </summary>
    public enum ServerType
    {
        Server = 1,
        OpenServer = 2
    }

    /// <summary>
    ///     How full the server is, the four steps the client draws. The original counts them from
    ///     the players the server is holding, so the emptiest server is the lowest value
    /// </summary>
    public enum CongestionType : byte
    {
        Low = 0x00,
        Medium = 0x01,
        High = 0x02,
        Maximum = 0x03
    }
}

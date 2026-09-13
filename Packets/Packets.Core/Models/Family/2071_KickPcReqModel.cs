using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;

namespace Packets.Core.Models.Family
{
    /// <summary>
    ///     Throw a player out of the world. The channel sends it to the servers of its world when
    ///     the same account logs in a second time, and the manager of the original sends it when
    ///     somebody is thrown out by hand. CTrKickPcReq3 of the original
    /// </summary>
    [Model(PacketType.KickPcReq)]
    public class KickPcReqModel
    {
        /// <summary>
        ///     Whom to throw out is said by the name of the character, not by the account
        /// </summary>
        public bool IsPc { get; set; }

        /// <summary>
        ///     Account to throw out, when the character is not named
        /// </summary>
        public int UserNo { get; set; }

        /// <summary>
        ///     Character to throw out, when it is named
        /// </summary>
        public string PcName { get; set; }

        /// <summary>
        ///     Why, the player is shown the text of it
        /// </summary>
        public NakErrorType Reason { get; set; }

        /// <summary>
        ///     Address the second login came from, the original carries it along for the logs
        /// </summary>
        public long AddressNumber { get; set; }
    }
}

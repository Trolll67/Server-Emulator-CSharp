using System;
using Server.Login.Network;

namespace Server.Login.Models.Family
{
    /// <summary>
    ///     An account this channel has certified and has not let go of yet. It lives from the
    ///     moment the login goes through until the player leaves the channel for a game server.
    ///     SChannelCertifyInfo of the original, kept by CCertificationMgr
    /// </summary>
    public class CertifiedAccount
    {
        /// <summary>
        ///     Account number, TblUser.mUserNo
        /// </summary>
        public int UserNo { get; set; }

        /// <summary>
        ///     Login of the account
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     Key the game server takes the player in by
        /// </summary>
        public int CertifiedKey { get; set; }

        /// <summary>
        ///     Address the login came from
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        ///     Session that holds the account, null when the player has already left the channel
        /// </summary>
        public LoginSession Session { get; set; }

        /// <summary>
        ///     When the login went through
        /// </summary>
        public DateTime CertifiedAt { get; set; }
    }
}

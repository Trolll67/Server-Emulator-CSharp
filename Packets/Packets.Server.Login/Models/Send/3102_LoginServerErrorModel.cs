using System;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;

namespace Packets.Server.Login.Models.Send
{
    /// <summary>
    ///     Refusal of a login. CTrCertifyUserNak of the original: the reason, the moment the
    ///     account stops being certified, a line of text for the player and the mark of a ban
    ///     that never ends. The original sends all of it as 227 bytes whatever the reason is
    /// </summary>
    [Model(PacketType.LoginServerError)]
    public class LoginServerErrorModel
    {
        /// <summary>
        ///     Why the login was refused
        /// </summary>
        public NakErrorType Error { get; set; }

        /// <summary>
        ///     Until when the account is certified. The original leaves it untouched on most of
        ///     the paths, only the ones about a ban fill it in
        /// </summary>
        public DateTime? EndCertify { get; set; }

        /// <summary>
        ///     Text the client shows instead of the one it has for the reason. Only the paths of
        ///     the publishers fill it in, everywhere else it stays empty
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        ///     The ban has no end
        /// </summary>
        public bool IsEternalSanction { get; set; }
    }
}

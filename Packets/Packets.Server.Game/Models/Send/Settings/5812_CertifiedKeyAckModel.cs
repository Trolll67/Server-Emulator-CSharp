using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     eCTrCertifiedKeyAck of the original: a single int, the session key the client is to hold
    ///     from now on. The world server rotates the key while logging the account in and sends the
    ///     new one here, so the client presents it on its next reconnection
    /// </summary>
    [Model(PacketType.CertifiedKeyAck)]
    public class CertifiedKeyAckModel
    {
        public int CertifiedKey { get; set; }
    }
}

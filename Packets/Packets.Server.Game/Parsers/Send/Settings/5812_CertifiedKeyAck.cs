using Packets.Core.Attributes;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Send.Settings;

namespace Packets.Server.Game.Parsers.Send.Settings
{
    /// <summary>
    ///     Parser of eCTrCertifiedKeyAck: the whole payload is the session key, one int
    /// </summary>
    [ParserSend]
    public class CertifiedKeyAck
    {
        [ParserAction(Core.Enums.PacketType.CertifiedKeyAck)]
        public byte[] Parsing(CertifiedKeyAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger(model.CertifiedKey);

            return formationPackage.GetBytes();
        }
    }
}

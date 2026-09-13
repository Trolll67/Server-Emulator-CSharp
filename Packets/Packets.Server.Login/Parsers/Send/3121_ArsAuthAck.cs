using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Login.Models.Send;

namespace Packets.Server.Login.Parsers.Send
{
    /// <summary>
    ///     Parser of the phone confirmation answer
    /// </summary>
    [ParserSend]
    public class ArsAuthAck
    {
        [ParserAction(PacketType.ArsAuthAck)]
        public byte[] Parsing(ArsAuthAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            formationPackage.AddInteger((int)model.State);

            return formationPackage.GetBytes();
        }
    }
}
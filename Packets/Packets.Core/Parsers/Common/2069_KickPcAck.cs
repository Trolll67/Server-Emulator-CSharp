using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;
using Packets.Core.Utilities;

namespace Packets.Core.Parsers.Common
{
    /// <summary>
    ///     Parser of the notice a player gets before the world lets go of them
    /// </summary>
    [ParserSend]
    public class KickPcAck
    {
        [ParserAction(PacketType.KickPcAck)]
        public byte[] Parsing(KickPcAckModel model)
        {
            FormationPackage formationPackage = new FormationPackage();

            // The client looks the text up by the group first and by the reason second
            formationPackage.AddInteger(model.Reason.MsgGroup);
            formationPackage.AddUInteger(model.Reason.Code);

            return formationPackage.GetBytes();
        }
    }
}

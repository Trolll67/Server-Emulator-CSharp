using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Receive.Character;

namespace Packets.Server.Game.Parsers.Receive.Character
{
    /// <summary>
    ///     Parser of CTrCharActionReq
    /// </summary>
    [ParserReceive]
    public class CharActionReq
    {
        [ParserAction(PacketType.CharActionReq)]
        public CharActionReqModel Parsing(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            return new CharActionReqModel { Action = formationPackage.ReadUInteger() };
        }
    }
}

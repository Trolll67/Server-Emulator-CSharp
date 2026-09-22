using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Receive.Character;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Structures;

namespace Packets.Server.Game.Parsers.Receive.Character
{
    /// <summary>
    ///     Parser of CTrFindCharReq
    /// </summary>
    [ParserReceive]
    public class FindCharReq
    {
        [ParserAction(PacketType.FindCharReq)]
        public FindCharReqModel Parsing(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            // Read fills the class out of the number itself, so the one handed to the constructor
            // is only a placeholder
            UniqueId who = new UniqueId(UniqueIdentifierType.Player);
            who.Read(formationPackage);

            return new FindCharReqModel { Who = who };
        }
    }
}

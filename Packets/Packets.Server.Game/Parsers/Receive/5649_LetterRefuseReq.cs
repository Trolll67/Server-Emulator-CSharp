using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Receive;

namespace Packets.Server.Game.Parsers.Receive
{
    /// <summary>
    ///     Parser of CTrLetterRefuseReq
    /// </summary>
    [ParserReceive]
    public class LetterRefuseReq
    {
        [ParserAction(PacketType.LetterRefuseReq)]
        public LetterRefuseReqModel Parsing(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            return new LetterRefuseReqModel { IsOn = formationPackage.ReadInteger() };
        }
    }
}

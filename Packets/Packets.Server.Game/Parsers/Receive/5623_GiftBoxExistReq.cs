using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Receive;

namespace Packets.Server.Game.Parsers.Receive
{
    /// <summary>
    ///     Parser of CTrGiftBoxExistReq: there is nothing to read, the packet carries no payload
    /// </summary>
    [ParserReceive]
    public class GiftBoxExistReq
    {
        [ParserAction(PacketType.GiftBoxExistReq)]
        public GiftBoxExistReqModel Parsing(byte[] data)
        {
            return new GiftBoxExistReqModel();
        }
    }
}

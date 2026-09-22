using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Receive;

namespace Packets.Server.Game.Parsers.Receive
{
    /// <summary>
    ///     Parser of CTrStoreReq. The action stands behind the eighteen item slots of the request,
    ///     which are sixteen bytes each, and the count stands behind the action
    /// </summary>
    [ParserReceive]
    public class StoreReq
    {
        /// <summary>
        ///     Where mFlag begins: SItem mItemInfo[18], sixteen bytes to a slot
        /// </summary>
        private const int ActionOffset = 18 * 16;

        [ParserAction(PacketType.StoreReq)]
        public StoreReqModel Parsing(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            formationPackage.ReadBytes(ActionOffset);

            return new StoreReqModel
            {
                Action = (StoreActionType)formationPackage.ReadInteger(),
                Count = formationPackage.ReadUInteger()
            };
        }
    }
}

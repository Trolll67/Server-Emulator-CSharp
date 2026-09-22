using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Utilities;
using Packets.Server.Game.Models.Receive;

namespace Packets.Server.Game.Parsers.Receive
{
    /// <summary>
    ///     Parser of CTrStoreReq. The packet is fixed at 321 bytes of payload and packed tight:
    ///     eighteen slots of sixteen bytes, then the action, the count of the slots that are
    ///     filled, the password of the warehouse and four numbers of the client check
    /// </summary>
    [ParserReceive]
    public class StoreReq
    {
        /// <summary>
        ///     How many slots one request carries
        /// </summary>
        private const int Slots = 18;

        /// <summary>
        ///     Size of one slot: the serial of the thing, how many of it, and what it is
        /// </summary>
        private const int SlotSize = 16;

        /// <summary>
        ///     Length of the password of the warehouse, mStorePassword[9]
        /// </summary>
        private const int PasswordSize = 9;

        [ParserAction(PacketType.StoreReq)]
        public StoreReqModel Parsing(byte[] data)
        {
            FormationPackage formationPackage = new FormationPackage(data);

            List<StoreReqItemModel> items = new List<StoreReqItemModel>();

            for (int i = 0; i < Slots; i++)
            {
                items.Add(new StoreReqItemModel
                {
                    SerialNo = formationPackage.ReadLong(),
                    Count = formationPackage.ReadUInteger(),
                    ItemNo = formationPackage.ReadInteger()
                });
            }

            StoreReqModel model = new StoreReqModel
            {
                Items = items,
                Action = (StoreActionType)formationPackage.ReadInteger(),
                Count = formationPackage.ReadUInteger(),
                Password = FormationPackageUtility.GetText(formationPackage.ReadBytes(PasswordSize), 0)
            };

            return model;
        }
    }
}

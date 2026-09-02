using Database.DataModel.Enums;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Models.Send.Action;
using Packets.Server.Game.Models.Send.Inventory;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;

namespace Server.Game.Core.Factories
{
    public class InventoryFactory : IInventoryFactory
    {
        /// <summary>
        ///     5232: a thing has been put into the bag of the character and the client has to draw
        ///     it there. The item is the one that lies in the bag and not the one that lay in the
        ///     world: the two carry different serial numbers and different counts after a pick-up
        ///     that merged the thing into a stack
        /// </summary>
        /// <param name="client">Session the packet is sent to</param>
        /// <param name="item">Item of the bag as it is after the change</param>
        /// <param name="uniqueId">Identifier the packet is about</param>
        /// <param name="reason">Why the thing got into the bag</param>
        public void SendItemAdd(GameSession client, GItem item, UniqueId uniqueId, Reason reason)
        {
            ItemAddAckModel itemAddModel = new ItemAddAckModel()
            {
                Item = CreateGoods(item),
                SessionGameId = uniqueId,
                Reason = (byte)reason
            };

            client.Send(itemAddModel);
        }

        /// <summary>
        ///     Item block of a packet about a thing of the bag. A thing that is not identified goes
        ///     out under the fake number of its parm row and with the normal status: that is how
        ///     the original keeps what has really dropped hidden until the thing is identified
        /// </summary>
        /// <param name="item">Item of the bag the block is filled from</param>
        private static ItemApiModel CreateGoods(GItem item)
        {
            return new ItemApiModel()
            {
                Flag = (byte)(item.IsConfirm ? 1 : 0),
                SerialNumber = item.SerialNumber,
                ItemId = item.IsConfirm ? item.Id : item.FakeId,
                Count = item.Count,
                EndTick = item.EndTick,
                ItemStatus = (byte)(item.IsConfirm ? item.Status : ItemStatusEnum.Normal),
                UseCount = item.UseCount,
                EatTime = item.EatTime,
                // The original puts here the minutes left until the term of the thing
                // ends, taken from its row in the DB of the player; we do not keep the
                // minutes - zero, the way the original has it for a thing without a term.
                // Filling it from the minutes of the procedure - together with the general
                // repair of EndTick and of the reading of the bag
                TermOfEffectivity = 0,
                ItemBind = (byte)item.ItemBind,
                Restore = item.Restore,
                Hole = item.Hole
            };
        }

        public void SendItemRemove(GameSession client, GItem gameItemModel, Reason reason)
        {
            ItemRemoveAckModel itemRemoveModel = new ItemRemoveAckModel()
            {
                Count = gameItemModel.Count,
                SerialNumber = gameItemModel.SerialNumber,
                SessionGameId = client.Pc.UniqueId,
                Reason = (byte)reason
            };

            client.Send(itemRemoveModel);
        }

        public void SendItemChangeTODOChangeAck(GameSession client, ItemChangeAckModel itemChangeAckModel)
        {
            ItemChangeAckModel itemChangeModel = new ItemChangeAckModel
            {
                SerialNumber = itemChangeAckModel.SerialNumber,
                ItemId = itemChangeAckModel.ItemId,
                IsCreate = itemChangeAckModel.IsCreate,
                Reason = itemChangeAckModel.Reason
            };

            client.Send(itemChangeModel);
        }
        
        public void SendItemUseAck(GameSession client, int ItemId)
        {
            ItemUseAckModel itemUseModel = new ItemUseAckModel
            {
                ItemId = ItemId
            };

            client.Send(itemUseModel);
        }

        public void SendCooldown(GameSession client, int ItemId)
        {
            ItemCooldownAckModel itemCooldownAckModel = new ItemCooldownAckModel()
            {
                ItemId = ItemId
            };
            client.Send(itemCooldownAckModel);
        }
    }
}

using Packets.Server.Game.Models.Send.Inventory;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Structures;
using Server.Game.Models.Game;
using Server.Game.Network;

namespace Server.Game.Core.Factories.Interfaces
{
    public interface IInventoryFactory
    {
        public void SendItemAdd(GameSession client, GItem itemGameModel, UniqueId uniqueId, Reason reason);
        public void SendItemRemove(GameSession client, GItem temGameModel, Reason reason);
        public void SendItemChangeTODOChangeAck(GameSession client, ItemChangeAckModel itemChangeAckModel);
        public void SendItemUseAck(GameSession client, int ItemId);
        public void SendCooldown(GameSession client, int ItemId);
    }
}

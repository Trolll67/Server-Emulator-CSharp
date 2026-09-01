using Packets.Server.Game.Enums;
using Packets.Server.Game.Models.Send.Inventory;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;

namespace Server.Game.Core.Factories
{
    public class EquipFactory : IEquipFactory
    {
        /// <summary>
        ///     5129: an item was put on. The packet carries the parm number of the item and its
        ///     serial - the client tells one worn item from another by the serial, so the field
        ///     must never hold anything else. The slot comes as a parameter and not off the item:
        ///     the truth about where the item ended up belongs to the equipment record the caller
        ///     wrote, and the same item type may go to either of the two ring slots.
        ///     One and the same call serves the owner and its neighbours - who the receivers are
        ///     is decided by the caller, which walks its own snapshot of the visible list
        /// </summary>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="sessionGameId">Unique identifier of the character that put the item on</param>
        /// <param name="item">Item that was put on</param>
        /// <param name="position">Slot the item was put into</param>
        public void SendEquip(GameSession clientTo, UniqueId sessionGameId, GItem item, ItemPositionType position)
        {
            EquipAckAllModel equipAckAllModel = new EquipAckAllModel
            {
                SessionGameId = sessionGameId,
                ItemId = item.Id,
                SerialNumber = item.SerialNumber,
                Position = position
            };

            clientTo.Send(equipAckAllModel);
        }

        /// <summary>
        ///     5131: an item was taken off. Neither the number nor the serial of the item is sent -
        ///     the emptied slot is enough for the client to strip it, so the caller does not have to
        ///     keep the item around. Goes to the same receivers as 5129
        /// </summary>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="sessionGameId">Unique identifier of the character that took the item off</param>
        /// <param name="position">Slot that was emptied</param>
        public void SendUnEquip(GameSession clientTo, UniqueId sessionGameId, ItemPositionType position)
        {
            UnEquipAckAllModel unEquipAckAllModel = new UnEquipAckAllModel
            {
                Position = position,
                SessionGameId = sessionGameId
            };

            clientTo.Send(unEquipAckAllModel);
        }
    }
}

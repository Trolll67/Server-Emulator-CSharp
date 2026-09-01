using Packets.Server.Game.Enums;
using Packets.Server.Game.Structures;
using Server.Game.Models.Game;
using Server.Game.Network;

namespace Server.Game.Core.Factories.Interfaces
{
    public interface IEquipFactory
    {
        void SendEquip(GameSession clientTo, UniqueId sessionGameId, GItem item, ItemPositionType position);

        void SendUnEquip(GameSession clientTo, UniqueId sessionGameId, ItemPositionType position);
    }
}

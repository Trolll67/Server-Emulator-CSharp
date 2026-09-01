using Packets.Server.Game.Structures;
using Server.Game.Network;

namespace Server.Game.Core.Factories.Interfaces
{
    public interface IMonsterActionFactory
    {
        void SendMoveToPoint(GameSession clientTo, UniqueId sessionGameId, Vector3 position, Vector3 pointPosition, byte flag, float velocity);

        void SendStopMoveMonster(GameSession clientTo, UniqueId sessionGameId, Vector3 position);
    }
}

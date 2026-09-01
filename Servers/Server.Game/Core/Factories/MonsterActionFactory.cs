using Packets.Server.Game.Models.Send.Character;
using Packets.Server.Game.Models.Send.MonsterNpc;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Network;

namespace Server.Game.Core.Factories
{
    public class MonsterActionFactory : IMonsterActionFactory
    {
        /// <summary>
        ///     5190: a monster walks to a point. The client is told both ends of the walk and the
        ///     speed, and interpolates the way between them on its own until the next packet about
        ///     this monster arrives - the server does not have to send every step it counts
        /// </summary>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="sessionGameId">Unique identifier of the monster that walks</param>
        /// <param name="position">Position the walk starts from</param>
        /// <param name="pointPosition">Point the monster walks to</param>
        /// <param name="flag">One while the monster is walking</param>
        /// <param name="velocity">Walking speed in units per second, the same the steps are cut by</param>
        public void SendMoveToPoint(GameSession clientTo, UniqueId sessionGameId, Vector3 position, Vector3 pointPosition, byte flag, float velocity)
        {
            DoMoveToAckModel movedCharactersModel = new DoMoveToAckModel
            {
                SessionGameId = sessionGameId,
                Position = position,
                PointPosition = pointPosition,
                Flag = flag,
                Velocity = velocity
            };

            clientTo.Send(movedCharactersModel);
        }

        /// <summary>
        ///     5326: a monster stops where the server holds it. The packet is the same one a stopped
        ///     character gets, but everything comes from the monster - it has no session of its own.
        ///     The flag is always zero: one is the code of a refused move the initiator gets a
        ///     resync after, and there is nobody to resync on the side of a monster
        /// </summary>
        /// <param name="clientTo">Session the packet is sent to</param>
        /// <param name="sessionGameId">Unique identifier of the monster that stopped</param>
        /// <param name="position">Position the monster stopped on</param>
        public void SendStopMoveMonster(GameSession clientTo, UniqueId sessionGameId, Vector3 position)
        {
            StopMoveCharacterModel stopMoveCharacterModel = new StopMoveCharacterModel
            {
                SessionGameId = sessionGameId,
                Position = position,
                Flag = 0
            };

            clientTo.Send(stopMoveCharacterModel);
        }
    }
}

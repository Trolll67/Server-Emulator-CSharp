using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Receive.Character;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Core.Systems;
using Server.Game.Models.Game;
using Server.Game.Network;
using Server.Game.Services.Database;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class CharacterActionHandler : ICharacterActionHandler
    {
        /// <summary>
        ///     Flag of 5326 for a refusal the move check marked as a full rollback: the client is put
        ///     back to the position of the server and nothing else follows
        /// </summary>
        private const byte StopMoveFlagRollback = 0;

        /// <summary>
        ///     Flag of 5326 for every other refusal: the client is put back and the initiator gets
        ///     5103 after it, so its own character is read from the server anew
        /// </summary>
        private const byte StopMoveFlagResync = 1;

        private readonly GameRepository _gameRepository;

        private readonly ICharacterActionFactory _characterActionFactory;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IAttackFactory _attackFactory;
        private readonly IVisibleFactory _visibleFactory;

        private readonly MoveSystem _moveSystem;

        private readonly ILogger<CharacterActionHandler> _logger;

        public CharacterActionHandler(GameRepository gameRepository, ICharacterActionFactory characterActionFactory, ICharacteristicFactory characteristicFactory, IAttackFactory attackFactory, IVisibleFactory visibleFactory, MoveSystem moveSystem, ILogger<CharacterActionHandler> logger)
        {
            _characteristicFactory = characteristicFactory;
            _characterActionFactory = characterActionFactory;
            _attackFactory = attackFactory;
            _visibleFactory = visibleFactory;
            _moveSystem = moveSystem;
            _gameRepository = gameRepository;
            _logger = logger;
        }

        [HandlerAction(PacketType.DoMoveReq)]
        public void MovingCharacters(GameSession client, DoMoveReqModel model)
        {
            // A move of a session that is not in the world: the character is either not chosen yet
            // or already on its way out, so there is nothing to move and nobody to tell about it
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            // The very first move means the client left the loading screen: the original clears its
            // loading mark here and stops treating the character as still loading
            // TODO: nothing reads GPc.LastLoadingTick and nothing else writes it, so clearing it
            // changes nothing today. It has to be set when the character is placed into the world
            // and read wherever a still loading character has to be spared - only then this line works
            client.Pc.LastLoadingTick = 0;

            // A character that may not move at all is not moved, but the client is still told where
            // the server sees it - otherwise it keeps walking on its side and the two positions drift
            if (!CanMove(client.Pc))
            {
                SendResynchronization(client);
                return;
            }

            Vector3 positionFrom = client.Pc.PositionCur;

            MoveResult moveResult = _moveSystem.CheckMove(client.Pc.MapNo, positionFrom, model.Position);

            if (moveResult != MoveResult.Accepted)
            {
                RefuseMove(client, moveResult, positionFrom, model.Position);
                return;
            }

            client.Pc.PositionCur = model.Position;
            client.Pc.DirectionSight = model.Direction;
            client.Pc.Action = model.Action;

            // We throw off the attack if we moved
            // TODO: the branch never fires today - the body of the attack handler is commented out,
            // so nothing ever sets GPc.AttackedUniqueIdentifier. Check the 5134 of a moving attacker
            // on a live client as soon as the attack is back
            if (client.Pc.AttackedUniqueIdentifier != null)
            {
                // The neighbours are the ones playing the attack of this character, so they are the
                // ones who have to stop it. 5326 was sent here before, which is the answer to a
                // refused move and not a stopped attack
                foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
                {
                    _attackFactory.SendEndAttack(visibleCharacterGame, client.Pc.UniqueId);
                }

                client.Pc.AttackedUniqueIdentifier = null;
            }

            // The mover itself is left out on purpose: it walks on its own side and waits only to be
            // stopped, an echo of its own move makes it jump back and forth
            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _characterActionFactory.SendMovedCharacters(visibleCharacterGame, client);
            }
        }

        [HandlerAction(PacketType.CharJumpReq)]
        public void JumpCharacter(GameSession client, CharJumpReqModel model)
        {
            client.Pc.DirectionSight = model.MoveDirection;
            client.Pc.Action = model.Action;

            _characterActionFactory.SendJumpCharacter(client, client);

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _characterActionFactory.SendJumpCharacter(visibleCharacterGame, client);
            }
        }

        [HandlerAction(PacketType.CharDirReq)]
        public void DirCharacter(GameSession client, CharDirectionReqModel model)
        {
            client.Pc.DirectionSight = model.Direction;

            _characterActionFactory.SendDirectionCharacter(client, client);

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _characterActionFactory.SendDirectionCharacter(visibleCharacterGame, client);
            }
        }

        [HandlerAction(PacketType.RespawnReq)]
        public void RespawnCharacter(GameSession client, RespawnReqModel model)
        {
            client.Pc.Simple.Hp = (short)(client.Pc.Ability.MaxHp / 2);
            client.Pc.Simple.Mp = (short)(client.Pc.Ability.MaxMp / 2);

            client.Pc.DeadTime = null;

            _characterActionFactory.SendRespawnCharacter(client, client);
            _characteristicFactory.SendHealthPointCharacteristics(client);
        }

        /// <summary>
        ///     Whether the state of the character lets it move at all. The original refuses the move
        ///     of a dead, of a frozen and of a trading character before it looks at the position
        /// </summary>
        /// <param name="pc">Character of the session that asked to move</param>
        private static bool CanMove(GPc pc)
        {
            if (pc.DeadTime != null)
            {
                return false;
            }

            // TODO: the freeze is not driven by anything yet - nothing writes GPc.Freeze - and the
            // personal shop of the character is not ported at all, so this refusal never fires today
            if (pc.Freeze != 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Answer a refused move: the neighbours and the mover are put back to the position the
        ///     server holds, and a refusal that is not a full rollback is followed by 5103
        /// </summary>
        /// <param name="client">Session that asked to move</param>
        /// <param name="moveResult">Answer of <see cref="MoveSystem.CheckMove"/></param>
        /// <param name="positionFrom">Position the server holds for the character</param>
        /// <param name="positionTo">Position the client asked to move to</param>
        private void RefuseMove(GameSession client, MoveResult moveResult, Vector3 positionFrom, Vector3 positionTo)
        {
            byte flag = moveResult == MoveResult.Forbidden ? StopMoveFlagRollback : StopMoveFlagResync;

            // A refused move looks to the player like a character nailed to the ground, and a badly
            // set threshold refuses every single move: without this line there is nothing in the
            // logs to tell one from a client that simply stopped walking
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                // A missing position is a refusal of its own, and the distance between it and
                // anything else is not a number
                float distanceSq = positionFrom == null || positionTo == null
                    ? float.NaN
                    : MoveSystem.GetDistance2DSq(positionFrom, positionTo);

                _logger.LogDebug("Move of character {PcNo} refused as {MoveResult}, distance squared {DistanceSq}, from {PositionFrom} to {PositionTo}",
                    client.Pc.Simple.PcNo, moveResult, distanceSq, positionFrom, positionTo);
            }

            // Unlike the accepted move, the refusal is not hidden from the mover: stopping it is the
            // whole point of the packet, and with a full rollback nothing else is sent to it
            _characterActionFactory.SendStopMoveCharacter(client, client, flag);

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _characterActionFactory.SendStopMoveCharacter(visibleCharacterGame, client, flag);
            }

            if (flag == StopMoveFlagResync)
            {
                SendResynchronization(client);
            }
        }

        /// <summary>
        ///     Repeat the character of the session to itself with 5103: the packet carries the
        ///     position of the server, so a client that ran away from it reads its own character anew
        /// </summary>
        /// <param name="client">Session that asked to move</param>
        private void SendResynchronization(GameSession client)
        {
            // The character is put at the position of the server at once: walking it there would
            // take it through the very ground the move was refused for, and the client would keep
            // its own idea of where it stands for the whole way
            // TODO: the list of the abnormal states of 5103 stays empty - the character keeps no
            // abnormal states yet
            _visibleFactory.SendDisplayedDetailsCharacter(client, client, isTeleport: true);
        }
    }
}

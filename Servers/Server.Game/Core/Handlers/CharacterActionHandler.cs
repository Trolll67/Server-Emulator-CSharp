using Database.DataModel.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Receive.Character;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Core.Systems;
using Server.Game.Models.Game;
using Server.Game.Models.Settings;
using Server.Game.Network;
using Packets.Server.Game.Enums;
using Server.Game.Services;
using Server.Game.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly GameSetting _gameSetting;

        private readonly ICharacterActionFactory _characterActionFactory;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IAttackFactory _attackFactory;
        private readonly IVisibleFactory _visibleFactory;

        /// <summary>
        ///     Map of the island a character starts on, eMapIslandGuinea of the original. Its
        ///     towns belong to the class of the character and not to the territory
        /// </summary>
        private const int StartingIslandMapNo = 1;

        /// <summary>
        ///     Source of the town the character is raised in when the territory names no point of
        ///     its own. Random is not thread safe and the respawns come off the socket threads
        /// </summary>
        private static readonly Random RespawnRandom = new Random();

        private readonly RegionService _regionService;
        private readonly IdentificationService _identificationService;
        private readonly MoveSystem _moveSystem;

        private readonly ILogger<CharacterActionHandler> _logger;

        public CharacterActionHandler(GameRepository gameRepository, IOptions<GameSetting> gameSetting, ICharacterActionFactory characterActionFactory, ICharacteristicFactory characteristicFactory, IAttackFactory attackFactory, IVisibleFactory visibleFactory, RegionService regionService, IdentificationService identificationService, MoveSystem moveSystem, ILogger<CharacterActionHandler> logger)
        {
            _characteristicFactory = characteristicFactory;
            _characterActionFactory = characterActionFactory;
            _attackFactory = attackFactory;
            _visibleFactory = visibleFactory;
            _regionService = regionService;
            _identificationService = identificationService;
            _moveSystem = moveSystem;
            _gameRepository = gameRepository;
            _gameSetting = gameSetting.Value;
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

            // We throw off the attack if we moved. The branch is live: 5133 marks the attacking
            // character (AttackHandler.BeginAttack), and a move of the attacker is one of the ways
            // the attack ends. The picture on a live client - a character that walks away and stops
            // swinging - is still unchecked
            if (client.Pc.AttackedUniqueIdentifier != null)
            {
                // Every place that drops an attack sends 5134 the same way: to the attacker and to
                // its neighbours - the attacker stops its own swing animation and waits to be told,
                // the neighbours are the ones playing the attack of this character. The attack tick
                // keeps to the same contract, so the client sees no difference between an attack
                // stopped by a move and one stopped by the service. 5326 was sent here before,
                // which is the answer to a refused move and not a stopped attack
                _attackFactory.SendEndAttack(client, client.Pc.UniqueId);

                foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
                {
                    _attackFactory.SendEndAttack(visibleCharacterGame, client.Pc.UniqueId);
                }

                client.Pc.AttackedUniqueIdentifier = null;
            }

            // The confirmation goes to everyone who sees the move, the mover among them: the
            // original does not leave the initiator out, and its client expects the answer to its
            // own request. A second copy is impossible - the list of visible characters is built
            // without the owner of the list (VisibleGameService.VisibleConnections), so the direct
            // send below is the only one the mover gets
            _characterActionFactory.SendMovedCharacters(client, client);

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

        [HandlerAction(PacketType.CharActionReq)]
        public void ActionCharacter(GameSession client, CharActionReqModel model)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            client.Pc.Action = model.Action;

            // The original carries the action to the neighbours of the character and to the
            // character itself: its own client waits for the answer before it plays the animation
            _characterActionFactory.SendCharAction(client, client);

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _characterActionFactory.SendCharAction(visibleCharacterGame, client);
            }
        }

        [HandlerAction(PacketType.FindCharReq)]
        public void FindCharacter(GameSession client, FindCharReqModel model)
        {
            if (!client.IsInWorld || client.Pc == null || model.Who == null)
            {
                return;
            }

            // The client holds a number it has nothing behind: the original looks the number up by
            // the class packed into it and answers with the very packet that shows the entity when
            // it comes into view. A number that belongs to nobody is a client that is late - the
            // entity is gone on our side already, and the original answers such a request with
            // nothing at all
            switch ((UniqueIdentifierType)model.Who.Class)
            {
                case UniqueIdentifierType.Player:
                    GameSession target = _identificationService.GetConnectionByUniqueIdentifier(model.Who);

                    if (target != null && target.IsInWorld && target.Pc != null)
                    {
                        _visibleFactory.SendDisplayedDetailsCharacter(target, client);
                    }

                    break;

                case UniqueIdentifierType.Monster:
                    GMonster unit = _identificationService.GetUnitByUniqueIdentifier(model.Who);

                    if (unit != null)
                    {
                        _visibleFactory.SendDisplayedDetailsUnit(client, unit);
                    }

                    break;

                case UniqueIdentifierType.Item:
                    GPublicItem item = _identificationService.GetItemByUniqueIdentifier(model.Who);

                    if (item != null)
                    {
                        _visibleFactory.SendDisplayedDetailsItem(client, item);
                    }

                    break;
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
            // A respawn of a session that is not in the world: there is no character to raise and
            // no neighbours to show it to
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            // Only a dead character is raised. Without this check 5141 of a living character heals
            // it down or up to a half of its maximum and teleports it to the respawn point, which
            // is a free heal and a free town portal for anyone who sends the packet by hand.
            // GPc.DeadTime is written by the death of a character (PlayerDeathSystem, called by the
            // intelligence of the monsters), so the path below is walked by every player a monster
            // kills. The picture on a live client - the death screen and the raise after it - is
            // still unchecked
            if (client.Pc.DeadTime == null)
            {
                return;
            }

            // The position is taken before anything is sent: 5142 carries the position of the
            // character, so the neighbours and the owner have to be told the new one at once
            MoveToRespawnPoint(client.Pc);

            client.Pc.Simple.Hp = client.Pc.Ability.MaxHp / 2;
            client.Pc.Simple.Mp = client.Pc.Ability.MaxMp / 2;

            client.Pc.DeadTime = null;

            _characterActionFactory.SendRespawnCharacter(client, client);

            // The neighbours keep the corpse standing until they are told about the respawn, and
            // they are the ones who have to move it to the respawn point
            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _characterActionFactory.SendRespawnCharacter(visibleCharacterGame, client);
            }

            _characteristicFactory.SendHealthPointCharacteristics(client);

            // 5142 alone leaves the client where it died: the resynchronization is what makes it
            // read its own character, and with it the respawn point of the server, anew
            SendResynchronization(client);
        }

        /// <summary>
        ///     Put the character at its respawn point, the way CPc::Respawn of the original does it:
        ///     the territory the character died in decides, not the character. The region layer of
        ///     the map paints the world into territories, and every territory holds the point its
        ///     dead are raised at.
        ///
        ///     A territory of zero is the one the original treats as "no territory of its own" and
        ///     sends to a town instead: on the starting islands to the town of the class, elsewhere
        ///     to one of the five towns of the first territory, picked at random.
        ///
        ///     The battle modes of the original - siege, arena, the guild war - keep respawn points
        ///     of their own, and none of them is ported yet
        /// </summary>
        /// <param name="pc">Character that asked to be raised</param>
        private void MoveToRespawnPoint(GPc pc)
        {
            int territoryNo = _regionService.GetTerritory(pc.PositionCur);

            TerritorySetting territory = _gameSetting.Territories
                .FirstOrDefault(candidate => candidate.No == territoryNo);

            // The territory of the death decides. Zero means the point is painted with nothing,
            // and the original falls through to a town for it
            if (territoryNo != 0 && territory?.Respawn != null && territory.Respawn.IsSet)
            {
                pc.PositionCur = ToVector(territory.Respawn);

                _logger.LogDebug("Character {PcNo} died in the territory {Territory} and is raised at its respawn point", pc.Simple.PcNo, territory.Name);

                return;
            }

            if (MoveToTownPoint(pc))
            {
                return;
            }

            // TblPc.mHomePosX/Y/Z. The original does not look at the home point of a character on
            // an ordinary death at all, but without the region layers there is nothing better to
            // fall back on than the point the character is bound to
            if (IsPositionSet(pc.Detail?.HomePos))
            {
                // A copy and not the instance itself: the position of the character is replaced on
                // every move, and the home point must not travel with it
                pc.PositionCur = new Vector3(pc.Detail.HomePos);

                // The home point carries its own map, and it is not always the one the character
                // died on: moving the point without the map left the server counting the character
                // on the map of its death while the client drew the one the point belongs to, and
                // every move was then checked against the bounds of the wrong map
                if (pc.Detail.HomeMapNo > 0)
                {
                    pc.MapNo = pc.Detail.HomeMapNo;
                }

                return;
            }

            // The start point of the class, the same setting the creation of a character is placed
            // at (CharacterHandler.CreateCharactersHandle). The classes of the two enums are the
            // same values under different names
            StartPosition startPosition = _gameSetting.StartPositions
                .FirstOrDefault(p => p.Class == (CharacterTypeEnum)pc.Simple.Class);

            if (startPosition == null)
            {
                _logger.LogWarning("No start position for class {Class} in GameSetting.StartPositions, character {PcNo} is raised where it died", pc.Simple.Class, pc.Simple.PcNo);
                return;
            }

            pc.PositionCur = new Vector3(startPosition.X, startPosition.Y, startPosition.Z);

            // TODO: the map is written into the character and from there into TblPcState, but the
            // client is not told that it changed - the packet of a field change is not ported yet,
            // so a start point on another map raises the character on the map it died on and moves
            // it to the other one only on the next login
            pc.MapNo = startPosition.Map;
        }

        /// <summary>
        ///     Whether the point is a real point and not an absent one. A position that was never
        ///     filled is either missing altogether or a zero of every coordinate, which is the
        ///     corner of the map and never a place a character is raised at
        /// </summary>
        /// <param name="position">Point read from the state of the character</param>
        private static bool IsPositionSet(Vector3 position)
        {
            return position != null && (position.X != 0f || position.Y != 0f || position.Z != 0f);
        }

        /// <summary>
        ///     Send the character to a town, STerritoryInfo::GetTownPos of the original. On the
        ///     starting islands the town is the one of the class of the character, and everywhere
        ///     else it is one of the five towns of the first territory, taken at random
        /// </summary>
        /// <param name="pc">Character that asked to be raised</param>
        /// <returns>True when the character was moved</returns>
        private bool MoveToTownPoint(GPc pc)
        {
            // The starting islands send a character to the town of its class, and the points of
            // those towns are the very ones a character of that class is created at
            if (pc.MapNo == StartingIslandMapNo)
            {
                StartPosition classTown = _gameSetting.StartPositions
                    .FirstOrDefault(candidate => candidate.Class == (CharacterTypeEnum)pc.Simple.Class);

                if (classTown != null)
                {
                    pc.PositionCur = new Vector3(classTown.X, classTown.Y, classTown.Z);
                    pc.MapNo = classTown.Map;

                    return true;
                }
            }

            List<PositionSetting> towns = _gameSetting.Territories
                .FirstOrDefault(candidate => candidate.No == 0)?.Towns?
                .Where(town => town != null && town.IsSet)
                .ToList();

            if (towns == null || towns.Count == 0)
            {
                return false;
            }

            // One of the five, the way the original takes it
            PositionSetting picked = towns[RespawnRandom.Next(towns.Count)];

            pc.PositionCur = ToVector(picked);

            return true;
        }

        /// <summary>
        ///     A point of the settings as the world holds it
        /// </summary>
        private static Vector3 ToVector(PositionSetting position)
        {
            return new Vector3(position.X, position.Y, position.Z);
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

            // The refusal goes to the mover as well as to its neighbours: stopping the mover is the
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

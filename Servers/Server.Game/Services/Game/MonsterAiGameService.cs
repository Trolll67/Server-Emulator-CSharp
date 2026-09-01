using Database.DataModel.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Packets.Server.Game.Models.Send.Attack;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Systems;
using Server.Game.Models.Game;
using Server.Game.Network;
using Server.Game.Services.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.Game
{
    /// <summary>
    ///     Intelligence of the monsters: one pass looks at every monster whose own moment has come and
    ///     decides what it does - stand at home, chase the one that hit it, swing at it or walk back to
    ///     the place it was spawned at. There is no packet of the client behind any of it: a monster is
    ///     drawn by its neighbours out of the very same packets a player is - the stop (5326), the walk
    ///     to a point (5190) and the swing (5132).
    ///     <para>
    ///     Everything the intelligence decides happens on the single thread of this job: the positions
    ///     of the monsters, their walks, their swings and the health they take off their victims are
    ///     written from here and nowhere else. The one thing that comes from another thread is the
    ///     target itself - the swing pass of the players registers the damage on the model of the
    ///     monster (GMonster.RegisterDamage) and that is what gives a monster somebody to fight - so
    ///     the target and the state of the fight are read and written under the lock of the model
    ///     </para>
    ///     <para>
    ///     The one position this pass does not write is the one of a respawned monster: the respawn
    ///     puts a monster back on its spot from a thread of its own. That write meets nobody - it
    ///     lands in the window where the monster is taken out of the IdentificationService, and this
    ///     pass only ever sees what the service hands out
    ///     </para>
    /// </summary>
    public class MonsterAiGameService : IHostedService
    {
        /// <summary>
        ///     How often the pass is repeated, in milliseconds. Overridable through
        ///     "GameSetting:JobIntervals:&lt;job name&gt;". It is the quantum of the intelligence: a
        ///     monster that fights is looked at as often as it swings and a monster that walks - as
        ///     often as it steps, and both of those moments are multiples of this one
        /// </summary>
        private const int TickIntervalMilliseconds = 100;

        /// <summary>
        ///     Pause between two steps of a walk, in milliseconds. The step itself is cut for exactly
        ///     this length, so the way the server moves the monster and the way the client
        ///     interpolates it out of 5190 come out the same
        /// </summary>
        private const int MoveTickIntervalMilliseconds = 400;

        /// <summary>
        ///     How far from the place it was spawned at a monster is willing to fight, in units. A
        ///     target that ran further than that - or a monster that chased it that far itself - ends
        ///     the fight: without a leash a chase across the whole map is a monster that never comes
        ///     back to its spot
        /// </summary>
        private const float LeashDistance = 5000f;

        /// <summary>
        ///     How many times in a row a chase may bring the monster nothing before it is given up.
        ///     A step that brought nothing is the target moving away from the point the monster
        ///     walks to and a step that left the monster where it stood - a monster that cannot
        ///     come any closer chases just as fruitlessly as one that is being run away from.
        ///     Together with <see cref="ChaseWithoutFightMilliseconds"/> it is what tells a target
        ///     that runs away from a target that stands and fights back: the chase is only given up
        ///     when both of them are over
        /// </summary>
        private const byte TargetMoveLimit = 9;

        /// <summary>
        ///     How long a chase may go on without a single landed hit, in milliseconds
        /// </summary>
        private const int ChaseWithoutFightMilliseconds = 11000;

        /// <summary>
        ///     Health 5132 of a swing against a player carries - always, on any health of the target.
        ///     A character learns its own health from 5146 (an absolute number), and the health of
        ///     somebody else is never drawn out of a swing: the percent in that field is the business
        ///     of a monster alone
        /// </summary>
        private const short HpAttackedPlayer = -1;

        /// <summary>
        ///     Flag of 5190 while the monster is walking
        /// </summary>
        private const byte MoveFlagWalking = 1;

        private readonly AttackSystem _attackSystem;
        private readonly MonsterMoveSystem _monsterMoveSystem;
        private readonly PlayerDeathSystem _playerDeathSystem;
        private readonly IAttackFactory _attackFactory;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IMonsterActionFactory _monsterActionFactory;
        private readonly IdentificationService _identificationService;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<MonsterAiGameService> _logger;

        /// <summary>
        ///     Source of the draws of the intelligence - which of the remembered attackers a monster
        ///     turns to next. Touched on the thread of the job only, so a single instance is enough
        /// </summary>
        private readonly Random _random;

        public MonsterAiGameService(AttackSystem attackSystem, MonsterMoveSystem monsterMoveSystem, PlayerDeathSystem playerDeathSystem, IAttackFactory attackFactory, ICharacteristicFactory characteristicFactory, IMonsterActionFactory monsterActionFactory, IdentificationService identificationService, PeriodicScheduler periodicScheduler, ILogger<MonsterAiGameService> logger)
        {
            _attackSystem = attackSystem;
            _monsterMoveSystem = monsterMoveSystem;
            _playerDeathSystem = playerDeathSystem;
            _attackFactory = attackFactory;
            _characteristicFactory = characteristicFactory;
            _monsterActionFactory = monsterActionFactory;
            _identificationService = identificationService;
            _periodicScheduler = periodicScheduler;
            _logger = logger;

            _random = new Random();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _periodicScheduler.Schedule(nameof(MonsterAi), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), MonsterAi);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     One pass of the intelligence: every monster that is alive and whose own moment has come
        ///     is asked what it does now
        /// </summary>
        private void MonsterAi()
        {
            try
            {
                var units = _identificationService.GetAllUnits();

                DateTime now = DateTime.Now;

                foreach (var unit in units)
                {
                    // A corpse decides nothing: it waits for the garbage and the respawn passes, and
                    // the respawn is what wipes the fight it died in
                    if (unit.DeadTime != null)
                    {
                        continue;
                    }

                    // The spots carry more than monsters - a merchant standing in a town is a unit of
                    // the same list - and nothing but a monster fights back
                    if (unit.ParmMon == null || unit.ParmMon.GbjClass != GbjClassEnum.Mon)
                    {
                        continue;
                    }

                    // A monster that swings waits out its attack rate, a monster that walks - its
                    // step, so most of the monsters are skipped by this line alone
                    if (unit.AiTickDateTime > now)
                    {
                        continue;
                    }

                    Tick(unit, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not tick monster intelligence");
            }
        }

        /// <summary>
        ///     One tick of one monster: what it is busy with is decided by the target it has and by
        ///     the state it is in, and both of them are read at once under the lock of the model - the
        ///     swing pass of the players may give a monster its target between two reads
        /// </summary>
        /// <param name="monster">Living monster whose moment has come</param>
        /// <param name="now">Moment of this pass</param>
        private void Tick(GMonster monster, DateTime now)
        {
            UniqueId targetUniqueId;
            MonsterAiState state;

            lock (monster.Aggro.SyncRoot)
            {
                targetUniqueId = monster.TargetUniqueId;
                state = monster.AiState;
            }

            if (targetUniqueId != null)
            {
                // Somebody hit the monster while it stood at home or walked back: the swing pass only
                // writes the target down, turning around is decided here
                if (state != MonsterAiState.Angry)
                {
                    BeginFight(monster, now);
                }

                Fight(monster, targetUniqueId, now);
                return;
            }

            switch (state)
            {
                // Angry without a target left: the last one the monster fought is gone and there is
                // nobody in the history to turn to
                case MonsterAiState.Angry:
                    GiveUpFight(monster, null);
                    break;

                case MonsterAiState.GoingHome:
                    GoHome(monster, now);
                    break;

                // Standing at home with nobody to fight. TODO: the active monsters look for a target
                // of their own here and the rest walk around their spot - both are written later
                case MonsterAiState.Idle:
                    // A monster that was killed in the middle of a chase comes back with the point
                    // it was walking to still written on it: the respawn wipes the fight it died in,
                    // and the walk is dropped here, silently - the clients have long forgotten the
                    // walk of a monster that died, and the respawned one is drawn to them anew
                    if (monster._PosTo != null)
                    {
                        monster._PosTo = null;
                    }

                    break;
            }
        }

        /// <summary>
        ///     The monster turns to the one it is going to fight: whatever it was walking to before -
        ///     another target or its own spot - is over
        /// </summary>
        /// <param name="monster">Monster that got a target</param>
        /// <param name="now">Moment of this pass</param>
        private void BeginFight(GMonster monster, DateTime now)
        {
            lock (monster.Aggro.SyncRoot)
            {
                monster.AiState = MonsterAiState.Angry;
            }

            StopWalk(monster);

            // The chase of a target that never lets the monster hit it is given up after
            // ChaseWithoutFightMilliseconds, and the fight has to start that time anew: the moment of
            // the last hit of a monster that has not fought yet lies in the very far past, and the
            // chase would be given up before its first step
            monster.LastFightDateTime = now;
        }

        /// <summary>
        ///     Tick of an angry monster: find the one it fights, swing at it when it is close enough
        ///     and walk up to it while it is not
        /// </summary>
        /// <param name="monster">Angry monster</param>
        /// <param name="targetUniqueId">Target of the monster, read off the model</param>
        /// <param name="now">Moment of this pass</param>
        private void Fight(GMonster monster, UniqueId targetUniqueId, DateTime now)
        {
            GameSession target = FindTarget(targetUniqueId);

            // Killed, logged out or simply gone: one of the attackers the monster still remembers is
            // taken instead, and an empty history leaves it without a target at all - the next pass
            // sees an angry monster with nobody to fight and sends it home
            if (target == null)
            {
                ChangeTarget(monster, now);
                return;
            }

            // The leash of the spot. Both ends of the fight are measured against the home of the
            // monster: a target that ran that far is not worth chasing, and a monster that got that
            // far itself has to come back whatever the target does
            if (IsOutOfLeash(monster, monster.PositionCur) || IsOutOfLeash(monster, target.Pc.PositionCur))
            {
                GiveUpFight(monster, targetUniqueId);
                return;
            }

            float distanceSq = MoveSystem.GetDistance2DSq(monster.PositionCur, target.Pc.PositionCur);

            // The reach of the monster is the one built out of its parm row, the same one the swings
            // of a player are checked against. The distance is taken on the plane and not in space:
            // the walk itself is cut on the plane, so a target that stands on a hill above the
            // monster must not be a target it walks towards forever
            if (distanceSq > monster._DistAttack * monster._DistAttack)
            {
                Chase(monster, target, now);
                return;
            }

            // Close enough: the walk is over and the neighbours are told where the monster stopped -
            // that is the packet the client draws the swings from
            StopWalk(monster);

            // The swing itself waits out the attack rate of the monster
            if (monster.AttackDateTime > now)
            {
                monster.AiTickDateTime = GetNextFightTick(monster.AttackDateTime, now);
                return;
            }

            Swing(monster, target, now);
        }

        /// <summary>
        ///     One swing of a monster at its target: the very same calculation a player swings with,
        ///     only the roles are the other way round
        /// </summary>
        /// <param name="monster">Attacking monster</param>
        /// <param name="victim">Session of the target, in the world and alive</param>
        /// <param name="now">Moment of this pass</param>
        private void Swing(GMonster monster, GameSession victim, DateTime now)
        {
            // The death of the monster is read again, right before the damage: the pass has looked
            // at a living monster, and the swings of the players run on a thread of their own and
            // may well have put it down in between. A corpse takes nothing off anybody
            if (monster.DeadTime != null)
            {
                return;
            }

            AttackResult result = _attackSystem.Attack(monster, victim.Pc);

            // A miss costs the monster its whole attack rate as well, so the next swing is put ahead
            // whatever this one came out as. The monster is looked at before that, though: a target
            // that runs away between two swings must not be given the whole rate of a head start
            monster.AttackDateTime = now.AddMilliseconds(AttackGameService.GetAttackRate(monster.AttackRate));
            monster.AiTickDateTime = GetNextFightTick(monster.AttackDateTime, now);

            // The health of a character is written by this pass alone (the regeneration writes it
            // from its own thread, and everything between the read and the write is a window where a
            // regenerated point rolls the damage of the swing back - the same race the monsters live
            // with already)
            int hp = victim.Pc.Simple.Hp;

            if (result.Damage > 0)
            {
                hp -= result.Damage;

                if (hp < 0)
                {
                    hp = 0;
                }

                victim.Pc.Simple.Hp = hp;

                // The chase paid off: the counters that give it up are about a target the monster
                // never reaches, and this one it does
                monster.LastFightDateTime = now;
                monster.TargetMoveCnt = 0;
            }

            SendAttacked(monster, victim, result.TypeHit);

            // The character learns its own health from 5146 and from nothing else, so a swing that
            // took nothing off - a miss - sends no packet and changes nothing. The counted health
            // goes into the packet itself: a point the recovery pass gives back in between must not
            // replace the number this hit decided on (the race with the recovery pass as such stays
            // as it is - it is the same one every hit of the world lives with)
            if (result.Damage > 0)
            {
                _characteristicFactory.SendHealthPointCharacteristics(victim, hp, victim.Pc.Simple.Mp);
            }

            // Only a swing that took health off can kill: a character may stand at zero health and
            // be alive (a miss against it must not kill it over and over)
            if (result.Damage <= 0 || hp > 0)
            {
                return;
            }

            _playerDeathSystem.KillPlayer(victim, monster);

            // A corpse is not somebody the monster keeps fighting: the killed one is forgotten and
            // one of the attackers that are still remembered is taken instead
            ChangeTarget(monster, now);
        }

        /// <summary>
        ///     A step of the monster towards its target and the packet the neighbours draw the walk
        ///     with. The chase is given up when it keeps bringing nothing - the target stepping away
        ///     from the point the monster walks to, or a step that gets the monster nowhere - and the
        ///     monster has not landed a single hit for a long while: that is a target that runs away
        ///     and not one that stands and fights back
        /// </summary>
        /// <param name="monster">Chasing monster</param>
        /// <param name="target">Session of the target, in the world and alive</param>
        /// <param name="now">Moment of this pass</param>
        private void Chase(GMonster monster, GameSession target, DateTime now)
        {
            Vector3 targetPosition = target.Pc.PositionCur;

            // The target stepped away from the point the monster was walking to. A target that stands
            // never moves that point, so the counter only grows while somebody is running
            if (monster._PosTo != null && !monster._PosTo.Equals(targetPosition) && monster.TargetMoveCnt < byte.MaxValue)
            {
                // The counter is reset by the model itself whenever the target changes, which happens
                // on the swing pass of the players: a stale value costs the monster one step of a
                // chase and nothing more
                monster.TargetMoveCnt++;
            }

            if (monster.TargetMoveCnt > TargetMoveLimit && monster.LastFightDateTime.AddMilliseconds(ChaseWithoutFightMilliseconds) < now)
            {
                GiveUpFight(monster, target.Pc.UniqueId);
                return;
            }

            // A chase is run and not walked: the monster goes with the whole move rate of its parm,
            // and the very same number goes out in the packet of the walk
            Vector3 positionBefore = monster.PositionCur;

            Walk(monster, targetPosition, MonsterMoveSystem.GetRunSpeed(monster.GetMoveRateOrg()), now);

            // A step that left the monster where it stood brought the chase nothing either - a parm
            // without a move rate at all is the plain case of it. Without this the monster would
            // stay angry forever: it never sets a point to walk to, so the shifts of the target are
            // never seen and the counter that ends the chase never grows
            if (monster.TargetMoveCnt < byte.MaxValue && positionBefore.Equals(monster.PositionCur))
            {
                monster.TargetMoveCnt++;
            }
        }

        /// <summary>
        ///     Tick of a monster on its way back: it walks to the place it was spawned at and stands
        ///     there the way it was born. The health it lost on the way is not given back - the
        ///     recovery pass raises it in its own time, exactly as it does for a monster nobody
        ///     touched
        /// </summary>
        /// <param name="monster">Monster that is going home</param>
        /// <param name="now">Moment of this pass</param>
        private void GoHome(GMonster monster, DateTime now)
        {
            // A monster without a spot has nowhere to walk back to: it stays where the fight left it
            if (monster.PositionDefault == null)
            {
                StopWalk(monster);
                SetIdle(monster);
                return;
            }

            // The way back is run the same way the chase was: a monster that gave a fight up does
            // not stroll home
            if (!Walk(monster, monster.PositionDefault, MonsterMoveSystem.GetRunSpeed(monster.GetMoveRateOrg()), now))
            {
                return;
            }

            // Home: the monster looks the way it looked when it was put into the world. The
            // direction goes out with the next packet about this monster and not with one of its
            // own - a stop carries the position alone
            StopWalk(monster);

            monster.DirectionSight = monster.DirectionSightDefault;

            SetIdle(monster);
        }

        /// <summary>
        ///     One step of a walk: the monster is moved by the length one tick covers, and the
        ///     neighbours are told about the walk once - on the tick the point is set or changed on.
        ///     One walk is one packet and the steps in between are silent: the client walks the
        ///     monster itself, from the position of the packet to the point of it with the speed of
        ///     it, and a packet on every step would only push the monster back where it already is.
        ///     That is why the position the packet carries is the one from before the step - the
        ///     moment the walk was decided on - and the speed is the one the monster really goes
        ///     with
        /// </summary>
        /// <param name="monster">Walking monster</param>
        /// <param name="pointPosition">Point the monster walks to</param>
        /// <param name="speed">Speed of the walk in units per second, <see cref="MonsterMoveSystem.GetRunSpeed"/></param>
        /// <param name="now">Moment of this pass</param>
        /// <returns>Whether the point is reached</returns>
        private bool Walk(GMonster monster, Vector3 pointPosition, float speed, DateTime now)
        {
            Vector3 positionFrom = monster.PositionCur;

            MonsterMoveStep step = _monsterMoveSystem.Step(positionFrom, pointPosition, speed, MoveTickIntervalMilliseconds);

            // A step that leaves the monster exactly where it stands is no walk at all - a parm
            // without a move rate, a point the monster is already on - and there is nothing to tell
            // the neighbours about. Everything else is a walk, the one shorter than a single step
            // among them: it is over on this very tick and it still goes out, otherwise the monster
            // would jump for everybody who sees it
            bool isMoved = !positionFrom.Equals(step.Position);

            if (isMoved && (monster._PosTo == null || !monster._PosTo.Equals(pointPosition)))
            {
                // A copy and not the point itself: the position of a player is replaced on every move
                // of it, and the point this monster walks to must not travel with the one it chases
                monster._PosTo = new Vector3(pointPosition);

                foreach (var visibleCharacterGame in monster.VisibleCharacterGames)
                {
                    _monsterActionFactory.SendMoveToPoint(visibleCharacterGame, monster.UniqueId, positionFrom, monster._PosTo, MoveFlagWalking, speed);
                }
            }

            monster.PositionCur = step.Position;

            // The point is reached: the caller stops the walk, and the stop is looked at on the
            // nearest pass and not one step later - a monster that has walked up to its target
            // swings at it right away
            if (step.IsArrived)
            {
                return true;
            }

            monster.AiTickDateTime = now.AddMilliseconds(MoveTickIntervalMilliseconds);

            return false;
        }

        /// <summary>
        ///     The monster stops where the server holds it. The packet goes out once, on the step from
        ///     walking to standing: a monster whose parm gives it no speed at all reaches every point
        ///     it walks to at once, and an unconditional stop would send a packet about it on every
        ///     single tick
        /// </summary>
        /// <param name="monster">Monster that stops</param>
        private void StopWalk(GMonster monster)
        {
            if (monster._PosTo == null)
            {
                return;
            }

            monster._PosTo = null;

            foreach (var visibleCharacterGame in monster.VisibleCharacterGames)
            {
                _monsterActionFactory.SendStopMoveMonster(visibleCharacterGame, monster.UniqueId, monster.PositionCur);
            }
        }

        /// <summary>
        ///     The swing of the monster as everybody who has to see it gets it: the victim first, the
        ///     neighbours of the monster after it. The victim may not be in the list of the monster at
        ///     all - the visibility pass rebuilds the lists once per tick and a swing lands in between
        ///     - so it is sent to directly and skipped in the list, and nobody gets the packet twice
        /// </summary>
        /// <param name="monster">Monster that swung</param>
        /// <param name="victim">Session of the target</param>
        /// <param name="typeHit">Miss, hit or critical hit</param>
        private void SendAttacked(GMonster monster, GameSession victim, TypeHit typeHit)
        {
            _attackFactory.SendAttacked(victim, monster.UniqueId, victim.Pc.UniqueId, typeHit, monster.PositionCur, HpAttackedPlayer);

            foreach (var visibleCharacterGame in monster.VisibleCharacterGames)
            {
                if (visibleCharacterGame == victim)
                {
                    continue;
                }

                _attackFactory.SendAttacked(visibleCharacterGame, monster.UniqueId, victim.Pc.UniqueId, typeHit, monster.PositionCur, HpAttackedPlayer);
            }
        }

        /// <summary>
        ///     Session of the target of a monster, null when there is nobody to fight any more
        /// </summary>
        /// <param name="targetUniqueId">Target of the monster, read off the model</param>
        private GameSession FindTarget(UniqueId targetUniqueId)
        {
            GameSession target = _identificationService.GetConnectionByUniqueIdentifier(targetUniqueId);

            // A session that left the world draws nothing and stands nowhere: for the monster it is
            // the same as a target that is gone altogether
            if (target == null || target.Pc == null || !target.IsInWorld || target.Pc.PositionCur == null)
            {
                return null;
            }

            // The lookup goes by the number of the identifier alone, and a number is handed out again
            // as soon as the one that held it is gone: the generation and the class are the only
            // things that tell the newcomer from the character the monster used to fight
            if (!UniqueId.IsSame(target.Pc.UniqueId, targetUniqueId))
            {
                return null;
            }

            // A corpse is not fought: it is raised by a request of its own, and until then it is
            // nobody the monster has business with
            if (target.Pc.DeadTime != null)
            {
                return null;
            }

            return target;
        }

        /// <summary>
        ///     Take the next of the attackers the monster remembers after the one it fought is gone.
        ///     The one that is gone is forgotten by the model itself; an empty history leaves the
        ///     monster without a target, and the next pass is what sends it home
        /// </summary>
        /// <param name="monster">Monster that lost its target</param>
        /// <param name="now">Moment of this pass</param>
        private void ChangeTarget(GMonster monster, DateTime now)
        {
            monster.ChangeTarget(_random);

            // Whatever the monster was walking to belonged to the target it does not have any more
            StopWalk(monster);

            // A fresh target is chased with its own patience and not with what is left of the one
            // before it
            monster.LastFightDateTime = now;

            // The new target is looked at on the nearest pass: there is nothing to wait out
            monster.AiTickDateTime = now;
        }

        /// <summary>
        ///     The monster gives the fight up: everything it remembers of it is dropped and it walks
        ///     back to its spot. The health is not touched - a monster that comes home wounded is
        ///     healed by the recovery pass and by nothing else
        /// </summary>
        /// <param name="monster">Monster that is done fighting</param>
        /// <param name="targetUniqueId">
        ///     Target the caller has given the fight up on, null when the monster had nobody to
        ///     fight at all
        /// </param>
        private void GiveUpFight(GMonster monster, UniqueId targetUniqueId)
        {
            lock (monster.Aggro.SyncRoot)
            {
                // The target is read again under the lock the swing pass of the players writes it
                // under: a monster with nobody to fight gets one from a hit that lands between the
                // decision of the caller and this line, and the reset would wipe a target nobody
                // has looked at yet - together with the hit that gave it. Only the fight the caller
                // has actually given up on is given up, everything else is left to the nearest pass
                if (monster.TargetUniqueId != null && !UniqueId.IsSame(monster.TargetUniqueId, targetUniqueId))
                {
                    return;
                }

                // The history goes with the target: a monster that gave up on a fight and walks home
                // is hit again by anybody who still wants it, and that hit is what gives it a target
                // anew. The state the reset leaves behind is the state of a monster that has just
                // been born, so the way home is put on top of it - and a hit that lands in between
                // is not lost either, it simply meets a monster that is on its way home with a
                // target. The pause before the next swing is the one thing the monster keeps: it has
                // just swung, and a fight it is dragged back into must not begin with a free hit
                monster.ResetFight(keepAttackDelay: true);

                monster.AiState = MonsterAiState.GoingHome;
            }

            // Outside the lock: the packets of the stop go to everybody who sees the monster, and
            // the swing pass of the players waits on that very lock to register a hit
            StopWalk(monster);
        }

        /// <summary>
        ///     The monster has nothing to do: it stands where it stands until somebody hits it
        /// </summary>
        /// <param name="monster">Monster that is done walking</param>
        private static void SetIdle(GMonster monster)
        {
            lock (monster.Aggro.SyncRoot)
            {
                monster.AiState = MonsterAiState.Idle;
            }
        }

        /// <summary>
        ///     Whether the point lies further from the spot of the monster than the fight reaches
        /// </summary>
        /// <param name="monster">Monster the spot of which the point is measured against</param>
        /// <param name="position">Point to measure</param>
        private static bool IsOutOfLeash(GMonster monster, Vector3 position)
        {
            // A monster without a spot is not held by anything: it was never put anywhere to come
            // back to
            if (monster.PositionDefault == null || position == null)
            {
                return false;
            }

            return MoveSystem.GetDistance2DSq(monster.PositionDefault, position) > LeashDistance * LeashDistance;
        }

        /// <summary>
        ///     When a monster that waits out its attack rate is looked at again: on the swing itself
        ///     or on the step of a chase, whichever of the two comes first. A monster that only woke
        ///     up for its own swings would be blind for the whole rate, and a target that walks off
        ///     in between would be gone by the time the monster opens its eyes
        /// </summary>
        /// <param name="attackDateTime">Moment the monster may swing again at</param>
        /// <param name="now">Moment of this pass</param>
        private static DateTime GetNextFightTick(DateTime attackDateTime, DateTime now)
        {
            DateTime stepDateTime = now.AddMilliseconds(MoveTickIntervalMilliseconds);

            return attackDateTime < stepDateTime ? attackDateTime : stepDateTime;
        }
    }
}

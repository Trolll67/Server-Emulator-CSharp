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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.Game
{
    /// <summary>
    ///     Attack game service: the auto attack of the characters. The request of the client (5133)
    ///     only marks the character as attacking (AttackHandler.BeginAttack), every swing itself is
    ///     played here - one pass looks at all the attacking characters, hits with the ones whose
    ///     swing is due and stops the attacks that lost their target
    /// </summary>
    public class AttackGameService : IHostedService
    {
        /// <summary>
        ///     How often the swing pass is repeated, in milliseconds. Overridable through
        ///     "GameSetting:JobIntervals:&lt;job name&gt;". A swing is a matter of seconds, so the pass
        ///     itself only decides how precise its moment is - and the neighbours learn about the hit
        ///     with the very same delay the visibility pass has
        /// </summary>
        private const int TickIntervalMilliseconds = 100;

        /// <summary>
        ///     Shortest pause between two swings of one character, in milliseconds. GChar.AttackRate is
        ///     read as milliseconds, and a parm without an attack rate gives a zero that would otherwise
        ///     let a character swing on every pass. Empirical value, not the original
        /// </summary>
        public const int MinimumAttackRateMilliseconds = 500;

        private readonly IAttackFactory _attackFactory;
        private readonly AttackSystem _attackSystem;
        private readonly ExpSystem _expSystem;
        private readonly IdentificationService _identificationService;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<AttackGameService> _logger;

        public AttackGameService(AttackSystem attackSystem, ExpSystem expSystem, IdentificationService identificationService, IAttackFactory attackFactory, PeriodicScheduler periodicScheduler, ILogger<AttackGameService> logger)
        {
            _attackSystem = attackSystem;
            _expSystem = expSystem;
            _identificationService = identificationService;
            _attackFactory = attackFactory;
            _periodicScheduler = periodicScheduler;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _periodicScheduler.Schedule(nameof(AttackConnections), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), AttackConnections);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Swings of all the attacking characters. The whole pass runs on the single thread of its
        ///     own job, so the hp of the monsters and their death are written from one place only
        /// </summary>
        private void AttackConnections()
        {
            try
            {
                // The attacking ones are the sessions that are in the world and were marked by 5133:
                // AttackedUniqueIdentifier is the flag of an attack in progress, the same one the move
                // handler drops when the attacker walks away
                var connections = _identificationService.GetAllConnections().Where(c => c.Pc != null && c.Pc.AttackedUniqueIdentifier != null);

                DateTime now = DateTime.Now;

                foreach (var connection in connections)
                {
                    // The state is read once: the request of the client comes from a network thread and
                    // may change the target - or drop the attack - in the middle of the pass
                    UniqueId targetUniqueId = connection.Pc.AttackedUniqueIdentifier;

                    if (targetUniqueId == null)
                    {
                        continue;
                    }

                    // The moment of the next swing, put AttackRate ahead by the previous one
                    if (connection.Pc.AttackDateTime > now)
                    {
                        continue;
                    }

                    Attack(connection, targetUniqueId, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not attack connections");
            }
        }

        /// <summary>
        ///     One swing of one character: everything that has to hold for the attack to go on is
        ///     checked, then the swing is calculated, applied and told about
        /// </summary>
        /// <param name="client">Attacking session</param>
        /// <param name="targetUniqueId">Target of the attack, read off the character</param>
        /// <param name="now">Moment of this pass</param>
        private void Attack(GameSession client, UniqueId targetUniqueId, DateTime now)
        {
            // A session that is not in the world any more has nobody to draw its swings for, so it is
            // left out of the packets: the attack of the character it used to have is only taken off
            // the neighbours
            if (!client.IsInWorld)
            {
                StopAttack(client, targetUniqueId, sendToAttacker: false);
                return;
            }

            // A dead attacker stops swinging: the client of a killed character keeps its auto attack
            // running until it is told the attack is over
            if (client.Pc.DeadTime != null)
            {
                StopAttack(client, targetUniqueId);
                return;
            }

            // Only monsters are attacked in this phase (PvP is not ported). A target that is not in the
            // identification service any more is a corpse the garbage pass has taken away, and a target
            // with a death time is a corpse that is still lying in the world
            GMonster target = _identificationService.GetUnitByUniqueIdentifier(targetUniqueId);

            if (target == null || target.DeadTime != null)
            {
                StopAttack(client, targetUniqueId);
                return;
            }

            // The list itself is replaced by the visibility pass and never edited in place, so the
            // reference taken here is a consistent snapshot of what the attacker sees
            if (!client.Pc.VisibleUnitGames.Contains(target))
            {
                StopAttack(client, targetUniqueId);
                return;
            }

            // The distance is checked on every swing and not once at the start: the client begins to
            // attack while it still walks up to the target and may as well walk away from it
            if (client.Pc.PositionCur.Distance(target.PositionCur) > client.Pc._DistAttack)
            {
                StopAttack(client, targetUniqueId);
                return;
            }

            AttackResult result = _attackSystem.Attack(client.Pc, target);

            // Current hp of the monster lives in Simple, which is replaced as a whole by the respawn:
            // taken into a local, the damage lands either on the monster we hit or on nobody at all.
            // The read and the write are kept next to each other on purpose - the recovery pass writes
            // the same field from its own thread, and everything in between is a window where a
            // regenerated hp rolls the damage of the swing back
            GPcSimple simple = target.Simple;
            int hpBefore = simple.Hp;
            int hp = hpBefore;

            if (result.Damage > 0)
            {
                hp -= result.Damage;

                if (hp < 0)
                {
                    hp = 0;
                }

                simple.Hp = hp;
            }

            // The packet carries the hp the target had before the swing: the bar of the client is
            // always one hit behind, which is why the first swing draws the bar the target had when
            // the fight started, without a branch of its own. A swing that left the target at zero
            // is the exception - an empty bar is drawn at once. The percent itself is the business
            // of the monster
            short hpAttacked = target.GetHpDisplayed(hp > 0 ? hpBefore : 0);

            // The attacker plays the swing itself, the neighbours are the ones who see it happen
            _attackFactory.SendAttacked(client, client.Pc.UniqueId, target.UniqueId, result.TypeHit, client.Pc.PositionCur, hpAttacked);

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _attackFactory.SendAttacked(visibleCharacterGame, client.Pc.UniqueId, target.UniqueId, result.TypeHit, client.Pc.PositionCur, hpAttacked);
            }

            // The next swing of this character. A rate that is not a sane number of milliseconds is
            // pulled up to the minimum, otherwise the pass would swing on every tick
            client.Pc.AttackDateTime = now.AddMilliseconds(GetAttackRate(client.Pc.AttackRate));

            // A swing that took hp off is remembered by the monster it landed on: a miss leaves no
            // trace, and a monster this swing has put down is left out - a corpse has nobody to
            // fight back. The register is written from this pass and read by the pass of the AI on
            // a thread of its own, and the two meet on the lock the model takes inside the call
            if (result.Damage > 0 && hp > 0)
            {
                target.RegisterDamage(client.Pc.UniqueId, result.Damage);
            }

            // Only a swing that took hp off can kill: a monster may stand at zero hp and be alive
            // (a parm without hp at all, a negative regeneration that took it under zero), and a miss
            // against such a monster must not kill it over and over and pay the experience every time
            if (result.Damage <= 0 || hp > 0)
            {
                return;
            }

            KillTarget(client, target, targetUniqueId, now);
        }

        /// <summary>
        ///     Death of the monster the swing has killed: it is fixed once, the experience is given out
        ///     once, and the corpse is left to the garbage and the respawn passes
        /// </summary>
        /// <param name="client">Session that killed the monster</param>
        /// <param name="target">Killed monster</param>
        /// <param name="targetUniqueId">Identifier the attack was started with</param>
        /// <param name="now">Moment of this pass</param>
        private void KillTarget(GameSession client, GMonster target, UniqueId targetUniqueId, DateTime now)
        {
            // The guard goes before the write and the whole branch: death is what pays the experience,
            // so a monster whose DeadTime is already set is a monster somebody else has killed - it
            // must not pay twice. A death time is written here and nowhere else (the respawn only
            // clears it back to null from its own thread), and this pass runs on a single thread, so
            // the check and the write hold together
            if (target.DeadTime != null)
            {
                StopAttack(client, targetUniqueId);
                return;
            }

            target.DeadTime = now;

            // 5137 carries the reputation of the killer: for a monster it goes out unchanged, changing
            // it belongs to PvP which is not ported
            int chaotic = client.Pc.Detail.Chaotic;
            ChaoticStatusType chaoticStatus = (ChaoticStatusType)client.Pc.Detail.ChaoticStatus;

            // Experience and the levels it brings, with everything the client has to be told about
            // them, go before the death of the monster: that is the order the original sends them in
            _expSystem.KillUnit(client, target);

            _attackFactory.SendDeadAttack(client, client.Pc.UniqueId, target.UniqueId, chaotic, chaoticStatus);

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _attackFactory.SendDeadAttack(visibleCharacterGame, client.Pc.UniqueId, target.UniqueId, chaotic, chaoticStatus);
            }

            // The corpse stays in the world until GarbageGameService takes it away and UnitGameService
            // brings the monster back - nothing of that is done here
            StopAttack(client, targetUniqueId);
        }

        /// <summary>
        ///     End of the attack: the character stops being an attacking one and everybody who draws its
        ///     swings is told about it. The very same contract the move handler keeps - 5134 to the
        ///     attacker and to its neighbours - so the client sees no difference between an attack
        ///     stopped by a move and one stopped by this pass
        /// </summary>
        /// <param name="client">Session whose attack ends</param>
        /// <param name="targetUniqueId">Target this pass has checked the attack against</param>
        /// <param name="sendToAttacker">Whether the attacker itself is told the attack is over</param>
        private void StopAttack(GameSession client, UniqueId targetUniqueId, bool sendToAttacker = true)
        {
            UniqueId attackedUniqueId = client.Pc.AttackedUniqueIdentifier;

            // Only the attack this pass has looked at is stopped. Between the checks and this line the
            // network thread could have dropped the attack itself (a move) or pointed it at another
            // target by a new 5133 - that attack has not been checked by anybody yet, and taking it
            // down here would kill an auto attack the client has every right to keep playing
            if (attackedUniqueId == null || attackedUniqueId.Id != targetUniqueId.Id)
            {
                return;
            }

            // The flag goes down before the packets: a send that throws must not leave a character
            // marked as attacking a target that is gone
            client.Pc.AttackedUniqueIdentifier = null;

            if (sendToAttacker)
            {
                _attackFactory.SendEndAttack(client, client.Pc.UniqueId);
            }

            foreach (var visibleCharacterGame in client.Pc.VisibleCharacterGames)
            {
                _attackFactory.SendEndAttack(visibleCharacterGame, client.Pc.UniqueId);
            }
        }

        /// <summary>
        ///     Pause between two swings of one attacker, in milliseconds: the attack rate built by
        ///     CalcSpeed out of the parm and the equipment, never shorter than the minimum. The
        ///     monsters swing by the very same rule, so the swing pass of the players and the
        ///     intelligence of the monsters ask this one and the same question
        /// </summary>
        /// <param name="attackRate">GChar.AttackRate of the attacker</param>
        public static int GetAttackRate(short attackRate)
        {
            if (attackRate < MinimumAttackRateMilliseconds)
            {
                return MinimumAttackRateMilliseconds;
            }

            return attackRate;
        }
    }
}

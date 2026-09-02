using Database.DataModel.Enums;
using Database.DataModel.Models;
using Packets.Server.Game.Structures;
using Server.Game.Network;
using System;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     What the monster is busy with. The state is read and written by the AI pass only, so a
    ///     monster changes what it does on one thread and nowhere else.
    ///     <para>
    ///     These are not the numbers the appearance packets carry in their action block: the packet
    ///     has a state of its own (a dead monster is one of its values, and the way home is none of
    ///     them), and translating one into the other belongs to whoever fills the packet
    ///     </para>
    /// </summary>
    public enum MonsterAiState
    {
        /// <summary>
        ///     Standing at home with nobody to fight. The state a monster is born and respawned in
        /// </summary>
        Idle,

        /// <summary>
        ///     Walking around its home on its own, at the strolling pace and with no target
        /// </summary>
        Stroll,

        /// <summary>
        ///     Chasing and hitting its target
        /// </summary>
        Angry,

        /// <summary>
        ///     Walking back to the place it was spawned at, after the fight is over
        /// </summary>
        GoingHome
    }

    /// <summary>
    ///     Monster of the world.
    ///     <para>
    ///     Current hp/mp are stored only in <see cref="GChar.Simple"/> (<c>Simple.Hp</c>, <c>Simple.Mp</c>),
    ///     their maximums - in <see cref="GChar.ParmMon"/> (<c>ParmMon.Hp</c>, <c>ParmMon.Mp</c>).
    ///     There is no separate hp/mp on the monster itself: everything that damages or heals
    ///     a monster works with <c>Simple</c>.
    ///     </para>
    ///     <para>
    ///     Death contract: a monster is dead while <see cref="GChar.DeadTime"/> is not null, alive while it is
    ///     null. Death is fixed once - by writing <c>DeadTime</c>, the corpse stays in the world and is removed
    ///     from <c>IdentificationService</c> by <c>GarbageGameService.GarbageUnits</c> (shortly before the
    ///     respawn or after <c>GameSetting.GarbageUnits</c>). Resurrection is done by
    ///     <c>UnitGameService.RespawnUnits</c>: <see cref="Respawn"/> milliseconds after <c>DeadTime</c> the
    ///     monster is removed from the identification service, restored by <see cref="_SetDefaultInfo"/>
    ///     (it resets <c>DeadTime</c>, hp and mp) and added back.
    ///     </para>
    /// </summary>
    public class GMonster : GChar
    {
        public GMonster()
        {
            VisibleCharacterGames = new List<GameSession>();
            VisibleItemGames = new List<GPublicItem>();
            VisibleUnitGames = new List<GMonster>();
            Aggro = new GAggroHistory();
        }

        /// <summary>
        ///     Hp in a packet for a monster whose bar the client must not draw
        /// </summary>
        public const short HpHidden = -1;

        // TODO: the monster does not yet keep its stomach, its flags and the time of its last death

        public int Respawn { get; set; }
        public Vector3 PositionDefault { get; set; }
        public float DirectionSightDefault { get; set; }

        public List<GameSession> VisibleCharacterGames { get; set; }
        public List<GPublicItem> VisibleItemGames { get; set; }
        public List<GMonster> VisibleUnitGames { get; set; }

        /// <summary>
        ///     Attackers the monster remembers and the damage each of them has dealt. Never replaced
        ///     by another one, not even by a respawn: the history is the lock the combat state of the
        ///     monster is kept behind, and a monster that swaps it would leave a thread holding the
        ///     lock of a history nobody reads any more. It is emptied instead, by
        ///     <see cref="ResetFight"/>
        /// </summary>
        public GAggroHistory Aggro { get; }

        /// <summary>
        ///     What the monster is busy with. The target it is busy with is the one of
        ///     <see cref="GChar.TargetUniqueId"/>, and how many times that target has moved away from
        ///     the place the monster went to is counted in <see cref="GChar.TargetMoveCnt"/> - both
        ///     are fields a character has anyway, and the AI is simply their first reader.
        ///     Written by the AI pass and by the fight reset (the respawn among others), always
        ///     under the lock of the aggro history - a reader outside the AI thread takes the
        ///     same lock
        /// </summary>
        public MonsterAiState AiState { get; set; }

        /// <summary>
        ///     When the AI is to look at this monster again. A monster in a fight is looked at as
        ///     often as it swings, a monster that walks - as often as it takes a step, and the pass
        ///     itself ticks faster than either of them, so every monster carries its own moment
        /// </summary>
        public DateTime AiTickDateTime { get; set; }

        /// <summary>
        ///     When the monster may swing again, one attack rate after its last swing. The zero date
        ///     of a monster that has not attacked yet lies in the past, so the first swing of a fight
        ///     goes out without waiting - the same contract the swings of a player keep
        /// </summary>
        public DateTime AttackDateTime { get; set; }

        /// <summary>
        ///     When the monster last landed a hit on its target. A chase that goes on and on without
        ///     a single hit is what tells a target that runs away from a target that fights back, so
        ///     the moment is kept next to the counter of the shifts of the target
        /// </summary>
        public DateTime LastFightDateTime { get; set; }

        /// <summary>
        ///     Hp of the monster the way the packets carry it: a percent of the full hp, not an absolute
        ///     value. The client draws the bar as fifteen segments of that percent, so an absolute hp would
        ///     keep the bar full until the monster drops under a hundred points - and a monster of the
        ///     first levels carries tens of thousands of them.
        ///     <para>
        ///     A monster whose parameter row keeps the bar off gets <see cref="HpHidden"/> instead of a
        ///     number - whatever its hp is, full or none. A monster that is already down and a monster with
        ///     a broken parm (the maximum is not positive) give 0.
        ///     </para>
        ///     <para>
        ///     The hp comes as an argument instead of being read from <c>Simple.Hp</c>: the swing pass
        ///     reports the value from before the hit, the display packets - the current one. The maximum is
        ///     always <c>ParmMon.Hp</c>, the same source <see cref="_SetDefaultInfo"/> fills the hp from
        ///     </para>
        /// </summary>
        /// <param name="hp">Hp to report</param>
        /// <returns>Percent of the full hp, 0 for a monster that is down, <see cref="HpHidden"/> for a hidden bar</returns>
        public short GetHpDisplayed(int hp)
        {
            if (!ParmMon.IsShowHp)
            {
                return HpHidden;
            }

            short maxHp = ParmMon.Hp;

            if (hp <= 0 || maxHp <= 0)
            {
                return 0;
            }

            // Hp above the maximum - an overheal or a parm changed under a living monster - draws the
            // same full bar, so it is held at a hundred and never leaves the range of a percent
            if (hp >= maxHp)
            {
                return 100;
            }

            return (short)(hp * 100 / maxHp);
        }

        /// <summary>
        ///     Move rate the walks of the monster are cut by. It is taken from the row of the shape
        ///     the monster wears right now and not from the one it was born with: a transformed
        ///     monster moves the way its shape does, and everything that walks a monster or tells
        ///     the neighbours about the walk has to take the number out of one and the same place.
        ///     A monster without a parm row at all does not move
        /// </summary>
        public short GetMoveRateOrg()
        {
            ParmMonster parm = ParmMonCur ?? ParmMon;

            if (parm == null)
            {
                return 0;
            }

            return parm.MoveRateOrg;
        }

        /// <summary>
        ///     Remember a hit the monster has taken: the damage goes into the aggro history, and a
        ///     monster that was fighting nobody starts fighting the one who hit it.
        ///     <para>
        ///     The target of a monster is sticky: while it has one, a hit of somebody else only fills
        ///     the history and never turns the monster around. Whom to fight next is decided by the
        ///     AI alone, and only once the target it has is gone (<see cref="ChangeTarget"/>) - so a
        ///     monster cannot be pulled off the one it fights by hitting it from the side
        ///     </para>
        ///     <para>
        ///     Called from the swing pass of the players while the AI reads the very same state, so
        ///     the history and the target are changed under one lock: the AI must never meet a
        ///     monster that has a target which is not in its history yet
        ///     </para>
        /// </summary>
        /// <param name="attacker">Identifier of the one who has hit</param>
        /// <param name="damage">Damage of the hit</param>
        /// <returns>Whether this hit is what gave the monster its target</returns>
        public bool RegisterDamage(UniqueId attacker, int damage)
        {
            if (attacker == null)
            {
                return false;
            }

            lock (Aggro.SyncRoot)
            {
                // The attacker is remembered whether the history had a place for it or not: a fifth
                // attacker is not written down, and it still is what the monster turns to when it is
                // fighting nobody
                Aggro.Register(attacker, damage);

                if (TargetUniqueId != null)
                {
                    return false;
                }

                TargetUniqueId = attacker;
                TargetMoveCnt = 0;

                return true;
            }
        }

        /// <summary>
        ///     Take the one the monster has found with its own eyes: a monster of a kind that hunts
        ///     gets its target from the pass of the intelligence and not from a hit. The one that is
        ///     taken is not written into the aggro history - the history holds the damage the monster
        ///     has taken, and a target that never touched it has nothing to be remembered for. That
        ///     is what tells the two kinds of target apart afterwards: a target with no record of its
        ///     own steps aside for anybody who really hits the monster, while one that has hit it is
        ///     as sticky as ever.
        ///     <para>
        ///     A monster that has a target already keeps it: the intelligence reads the target and
        ///     looks around on one thread, and a hit of the swing pass of the players may land in
        ///     between - a target that came with damage is worth more than one that came by sight
        ///     </para>
        /// </summary>
        /// <param name="target">Identifier of the one the monster has seen</param>
        /// <returns>Target of the monster after the call, null when it has nobody to fight</returns>
        public UniqueId SetTargetBySight(UniqueId target)
        {
            lock (Aggro.SyncRoot)
            {
                if (target != null && TargetUniqueId == null)
                {
                    TargetUniqueId = target;
                    TargetMoveCnt = 0;
                }

                return TargetUniqueId;
            }
        }

        /// <summary>
        ///     Pick the next target after the one the monster was fighting is gone - killed, out of
        ///     the world or given up on. The one that is gone is forgotten first, so it cannot be
        ///     drawn again, and one of the attackers that are still remembered is drawn at random:
        ///     the original does not pick the hardest hitter and neither do we.
        ///     <para>
        ///     The source of the draw is given by the caller so that a probe with a fixed seed picks
        ///     the same attacker every run, the same way the swings are drawn
        ///     </para>
        /// </summary>
        /// <param name="random">Source of the draw</param>
        /// <returns>Identifier of the new target, null when there is nobody left to fight</returns>
        public UniqueId ChangeTarget(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            lock (Aggro.SyncRoot)
            {
                Aggro.Remove(TargetUniqueId);

                GAggro next = Aggro.GetRandom(random);

                TargetUniqueId = next?.UniqueId;
                TargetMoveCnt = 0;

                return TargetUniqueId;
            }
        }

        /// <summary>
        ///     Drop everything the monster has of its fight: the attackers it remembers, the target
        ///     it was fighting, the counters and the moments of its own pass. The state it is left in
        ///     is the state of a monster that has just been spawned, so the same call serves the end
        ///     of a fight, the death of the monster and its respawn
        /// </summary>
        /// <param name="keepAttackDelay">
        ///     Whether the pause before the next swing survives the reset. A monster that gives a
        ///     fight up is still the same monster: it has just swung, and somebody who hits it again
        ///     right away must not be paid with a free swing out of turn. A monster that is put back
        ///     into the world is another one - it has never swung, and the reset gives it the empty
        ///     moment of a monster that has just been born
        /// </param>
        public void ResetFight(bool keepAttackDelay = false)
        {
            lock (Aggro.SyncRoot)
            {
                Aggro.Clear();

                // The walk dies with the fight: a point left over from a chase would otherwise
                // survive the respawn and ride into the appearance packet of the reborn monster,
                // sending it walking to the place its past life died at
                _PosTo = null;

                TargetUniqueId = null;
                TargetMoveCnt = 0;

                AiState = MonsterAiState.Idle;

                // The zero date lies in the past, so a monster that is put back on its feet is looked
                // at and may swing on the nearest pass instead of waiting out the rate of a fight
                // that is over
                AiTickDateTime = default;
                LastFightDateTime = default;

                if (!keepAttackDelay)
                {
                    AttackDateTime = default;
                }
            }
        }

        /// <summary>
        ///     Build the monster ability from its Detail. Idempotent: the base call resets the ability
        ///     and everything here is an assignment, so a repeated call after a respawn does not accumulate
        /// </summary>
        public new void CalcAbility()
        {
            base.CalcAbility();

            Ability.DDv = Detail.DDv;
            Ability.MDv = Detail.MDv;
            Ability.DDD = 0;
            Ability.RDv = Detail.RDv;
            Ability.MPv = Detail.MPv;
            Ability.RPv = Detail.RPv;
            Ability.DHit = Detail.Hit;
            Ability.DPv = (short)(AddDPV + Detail.DPv);
        }

        /// <summary>
        ///     Bring the monster to its default state: alive (<c>DeadTime = null</c>), full hp/mp from the parm,
        ///     ability recalculated from the parm and nothing left of the fight it died in. Used both
        ///     on world loading and on respawn, so repeated calls must give the same result
        /// </summary>
        /// <param name="parmMon"></param>
        public new void _SetDefaultInfo(ParmMonster parmMon)
        {
            base._SetDefaultInfo(parmMon);

            DeadTime = null;

            // A monster comes back with a clean sheet: the one that killed it is not somebody the
            // respawned monster has any business with, and a target left over from the last fight
            // would send it running after a player the moment it is put back in the world
            ResetFight();

            Simple = new GPcSimple()
            {
                Class = ParmMon.Class,
                Hp = ParmMon.Hp,
                Mp = ParmMon.Mp,
                Exp = ParmMon.Exp,
                NickName = ParmMon.Nm
            };

            Simple.SetStomach(70);

            Detail.DDv = ParmMon.DDv;
            Detail.RDv = ParmMon.RDv;
            Detail.MDv = ParmMon.MDv;

            Detail.DPv = ParmMon.DPv;
            Detail.RPv = ParmMon.RPv;
            Detail.MPv = ParmMon.MPv;

            Detail.Hit = 0;
            Detail.MinD = ParmMon.MinD;
            Detail.MaxD = ParmMon.MaxD;

            Transformed(parmMon);

            // Detail is filled, so the ability can be built from it: defences, evasion, hit
            CalcAbility();
        }
    }
}

using Database.DataModel.Enums;
using Packets.Server.Game.Models.Send.Attack;
using Server.Game.Models.Game;
using System;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     Combat characteristics of one side of a swing, taken off the character at the moment the
    ///     swing is calculated. One and the same snapshot answers for both roles a character can
    ///     play: the attacking half (accuracy, damage, dice of the weapon in hand, critical hit) and
    ///     the defending half (evasion, armour, resistance to critical hits). The role decides which
    ///     half is read, so a swing takes the accuracy of the attacker against the evasion of the
    ///     target and never mixes the two up.
    ///     <para>
    ///     A player and a monster both fit in here, but they are not read the same way: the evasion
    ///     and the critical resistance of a monster live in its parm row, while a player keeps them
    ///     in its calculated characteristics. The armour is taken off the characteristics for
    ///     everyone - a monster gets it filled from the parm when its abilities are built. The
    ///     asymmetry is written out as an explicit branch on purpose: it is how the original reads
    ///     the two, not an oversight to be straightened out
    ///     </para>
    /// </summary>
    public readonly struct CombatSnapshot
    {
        /// <summary>
        ///     Whether the weapon in the hand of this side puts its swing on the range way. Read of
        ///     the attacker only: the way is chosen by the one who swings, and the target answers on
        ///     that same way with its own evasion and armour
        /// </summary>
        public bool IsRange { get; init; }

        /// <summary>
        ///     Whether this side is a player. Only the unreachable PvP branch asks for it in this
        ///     phase - the target of a swing is always a monster
        /// </summary>
        public bool IsPlayer { get; init; }

        /// <summary>
        ///     Damage dice of the weapon in the hand, already picked for the way the swing goes
        ///     through. An unarmed character rolls the dice of its default weapon, so there is no
        ///     such thing as a swing without dice
        /// </summary>
        public GDice AttackDice { get; init; }

        /// <summary>
        ///     Accuracy of this side as an attacker on its own way, GPcAbility.DHit/RHit plus the
        ///     accuracy of the weapon in the hand. The accuracy of an item is summed into the
        ///     characteristics of its wearer as well, so a weapon counts twice here - that is how
        ///     the original lands a swing, and evening it out would put us off the balance of the
        ///     items. A monster is not counted twice: its own accuracy is zero and everything comes
        ///     off the default weapon
        /// </summary>
        public int Hit { get; init; }

        /// <summary>
        ///     The same accuracy against a player, GPcAbility.PvPDHIT/PvPRHIT plus the weapon. Never
        ///     used in this phase: nobody fills the PvP fields and the target of a swing is always a
        ///     monster
        /// </summary>
        public int PvPHit { get; init; }

        /// <summary>
        ///     Damage this side adds to every swing of its own, GPcAbility.DDD/RDD, picked for the
        ///     way the swing goes through. A monster carries a zero here and hits with the dice of
        ///     its default weapon alone
        /// </summary>
        public int Damage { get; init; }

        /// <summary>
        ///     Evasion of this side as a target, GPcAbility.DDv/RDv or the same numbers of the parm
        ///     row of a monster. Both ways are kept, because the way is chosen by the attacker
        /// </summary>
        public short DDv { get; init; }
        public short RDv { get; init; }

        /// <summary>
        ///     Armour of this side as a target, GPcAbility.DPv/RPv. Taken off the characteristics
        ///     for a player and a monster alike
        /// </summary>
        public short DPv { get; init; }
        public short RPv { get; init; }

        /// <summary>
        ///     Chance of a critical hit of this side, in points, GPcAbility.CriticalHit
        /// </summary>
        public short CriticalHit { get; init; }

        /// <summary>
        ///     How many points this side takes off the critical chance of whoever attacks it
        /// </summary>
        public short EnemySubCriticalHit { get; init; }

        /// <summary>
        ///     Damage this side adds to its own critical hits
        /// </summary>
        public short AddDDWhenCritical { get; init; }

        /// <summary>
        ///     Damage this side takes off a critical hit landed on it
        /// </summary>
        public short SubDDWhenCritical { get; init; }

        /// <summary>
        ///     Evasion this side answers a swing with, on the way the attacker swings
        /// </summary>
        /// <param name="isRange">Way of the attacker, CombatSnapshot.IsRange of the other side</param>
        public short GetEvasion(bool isRange)
        {
            return isRange ? RDv : DDv;
        }

        /// <summary>
        ///     Armour this side answers a swing with, on the way the attacker swings
        /// </summary>
        /// <param name="isRange">Way of the attacker, CombatSnapshot.IsRange of the other side</param>
        public short GetArmor(bool isRange)
        {
            return isRange ? RPv : DPv;
        }

        /// <summary>
        ///     Snapshot of a character, a player or a monster alike. The values are copied, so the
        ///     character may change while the swing is being calculated
        /// </summary>
        /// <param name="character">Character to read the characteristics of</param>
        public static CombatSnapshot Of(GChar character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            GPcAbility ability = character.Ability;
            GWeapon weapon = character.GetWeaponInHand();

            // The numbers are read off the row the character wears right now, the class - off the row
            // it was built with: a transformed player walks the world with the row of a monster, and
            // it still keeps its evasion in its own characteristics
            ParmMonster parm = character.ParmMonCur ?? character.ParmMon;
            ParmMonster parmBase = character.ParmMon ?? character.ParmMonCur;

            // A player keeps everything in its characteristics; everything else that walks the world
            // - a monster above all - keeps its evasion and its answer to critical hits in the parm
            // row it was built from
            bool isPlayer = parm == null || parmBase == null || parmBase.GbjClass == GbjClassEnum.Pc;

            bool isRange = weapon.IsRange;
            int weaponHit = weapon.AttackHit;

            return new CombatSnapshot
            {
                IsRange = isRange,
                IsPlayer = isPlayer,

                AttackDice = weapon.AttackDice,
                Hit = (isRange ? ability.RHit : ability.DHit) + weaponHit,
                PvPHit = (isRange ? ability.PvPRHIT : ability.PvPDHIT) + weaponHit,
                Damage = isRange ? ability.RDD : ability.DDD,

                DDv = isPlayer ? ability.DDv : parm.DDv,
                RDv = isPlayer ? ability.RDv : parm.RDv,

                DPv = ability.DPv,
                RPv = ability.RPv,

                CriticalHit = ability.CriticalHit,
                AddDDWhenCritical = ability.AddDDWhenCritical,

                EnemySubCriticalHit = isPlayer ? ability.EnemySubCriticalHit : parm.EnemySubCriticalHit,
                SubDDWhenCritical = isPlayer ? ability.SubDDWhenCritical : parm.SubDDWhenCritical
            };
        }
    }

    /// <summary>
    ///     Outcome of one swing: what the client is told in 5132 and how much health the target
    ///     loses. The damage is only reported, applying it is the business of the caller
    /// </summary>
    public readonly struct AttackResult
    {
        public AttackResult(TypeHit typeHit, int damage)
        {
            TypeHit = typeHit;
            Damage = damage;
        }

        /// <summary>
        ///     Miss, hit or critical hit, the field of AttackAckModel
        /// </summary>
        public TypeHit TypeHit { get; }

        /// <summary>
        ///     Damage of the swing, always zero on a miss and always above zero otherwise
        /// </summary>
        public int Damage { get; }
    }

    /// <summary>
    ///     Mathematics of a single swing: whether it lands, whether it is critical and how much
    ///     damage it deals. The system knows nothing about sessions, packets and services - it is
    ///     given two characters, reads a snapshot off each of them and answers with a result, while
    ///     the caller decides whose health to lower and what to send (the same split as in
    ///     MoveSystem).
    ///     <para>
    ///     A swing of a weapon goes one of two ways: a range weapon in the hand puts it on the range
    ///     way, everything else - a melee weapon, a spear, an empty hand - on the melee way. There is
    ///     no magic way here at all; magic is the business of skills, which are not a swing
    ///     </para>
    /// </summary>
    public class AttackSystem
    {
        /// <summary>
        ///     Highest percent a swing can land with. Even an attacker whose accuracy is far above
        ///     the evasion of the target misses one swing out of twenty
        /// </summary>
        public const int MaxHitPercent = 95;

        /// <summary>
        ///     Draw of the hit: a number from zero to one below this, compared against the percent.
        ///     There is no lower bound - an attacker whose accuracy is nothing against the evasion of
        ///     the target lands nothing
        /// </summary>
        public const int HitRollRange = 100;

        /// <summary>
        ///     Lowest number the critical draw can come out as. It is the bound of the draw and not
        ///     the bound of the threshold: the two happen to be the same number, but they answer for
        ///     different things and move apart the moment one of them is changed
        /// </summary>
        public const int MinCriticalRoll = 1;

        /// <summary>
        ///     Highest number the critical draw can come out as: the draw is a number from
        ///     MinCriticalRoll to this
        /// </summary>
        public const int CriticalRollRange = 200;

        /// <summary>
        ///     Threshold the critical draw has to reach when neither side has anything to do with
        ///     critical hits. Together with the range it gives one critical hit in two hundred
        ///     swings, and every point of the critical value moves the threshold by one
        /// </summary>
        public const int CriticalThresholdBase = 200;

        /// <summary>
        ///     Lowest the critical threshold can be pushed to. A threshold of one is reached by every
        ///     draw, so an attacker with enough critical value hits critically always - there is no
        ///     upper bound on the chance
        /// </summary>
        public const int MinCriticalThreshold = 1;

        /// <summary>
        ///     What a critical hit does to the damage of the swing: it doubles it, and only then the
        ///     critical addition of the attacker and the critical resistance of the target are
        ///     counted
        /// </summary>
        public const int CriticalDamageMultiplier = 2;

        /// <summary>
        ///     Part of the armour of the target that reaches the damage: the armour is halved before
        ///     it is taken off
        /// </summary>
        public const int ArmorDivider = 2;

        /// <summary>
        ///     Draw that decides where an odd half of the armour goes: zero or one, so an armour of
        ///     eleven takes off five or six points, half of the swings each
        /// </summary>
        public const int ArmorOddRollRange = 2;

        /// <summary>
        ///     Damage of a swing that landed but was eaten by the armour of the target: a hit is
        ///     always felt
        /// </summary>
        public const int MinDamage = 1;

        /// <summary>
        ///     Randomness of the swings. Not seeded on purpose; the overload that takes a Random is
        ///     there for the probes, which need a fixed seed to be repeatable
        /// </summary>
        private readonly Random _random;

        public AttackSystem()
        {
            _random = new Random();
        }

        /// <summary>
        ///     Calculate one swing of one character against another. Neither side is changed: the
        ///     health is lowered by the caller out of AttackResult.Damage
        /// </summary>
        /// <param name="offense">Attacking character</param>
        /// <param name="defense">Target of the swing</param>
        public AttackResult Attack(GChar offense, GChar defense)
        {
            return Attack(CombatSnapshot.Of(offense), CombatSnapshot.Of(defense), _random);
        }

        /// <summary>
        ///     The same swing with an explicit source of randomness, for a probe that needs the draws
        ///     to repeat
        /// </summary>
        /// <param name="offense">Attacking character</param>
        /// <param name="defense">Target of the swing</param>
        /// <param name="random">Source of randomness of this swing</param>
        public AttackResult Attack(GChar offense, GChar defense, Random random)
        {
            return Attack(CombatSnapshot.Of(offense), CombatSnapshot.Of(defense), random);
        }

        /// <summary>
        ///     Swing of two snapshots taken beforehand
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public AttackResult Attack(CombatSnapshot offense, CombatSnapshot defense)
        {
            return Attack(offense, defense, _random);
        }

        /// <summary>
        ///     Swing of two snapshots with an explicit source of randomness: with the same seed and
        ///     the same snapshots the answer is always the same. The draws are always taken in this
        ///     order - the critical hit, then the hit, then the dice of the weapon one by one, then
        ///     the odd half of the armour - and a swing that misses takes no further draw at all.
        ///     <para>
        ///     The critical draw is made before the hit draw and does not depend on it, but a miss
        ///     beats a critical hit: a swing that did not land deals nothing
        ///     </para>
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        /// <param name="random">Source of randomness of this swing</param>
        public AttackResult Attack(CombatSnapshot offense, CombatSnapshot defense, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            bool isCritical = random.Next(MinCriticalRoll, CriticalRollRange + 1) >= GetCriticalThreshold(offense, defense);

            if (random.Next(HitRollRange) >= GetHitPercent(offense, defense))
            {
                return new AttackResult(TypeHit.Miss, 0);
            }

            int damage = GetDamage(offense, defense, isCritical, random);

            return new AttackResult(isCritical ? TypeHit.Crit : TypeHit.Hit, damage);
        }

        /// <summary>
        ///     Percent of the swings that land: the accuracy of the attacker against the evasion of
        ///     the target, as a ratio and not as a difference, held under MaxHitPercent. A target
        ///     that evades nothing is hit as often as anything can be hit; an attacker whose accuracy
        ///     is far below the evasion gets a zero and lands nothing at all, which is a proper
        ///     outcome and not a floor to be raised. The levels of the two sides do not count here -
        ///     a swing of a weapon knows nothing about them
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public static int GetHitPercent(CombatSnapshot offense, CombatSnapshot defense)
        {
            int evasion = defense.GetEvasion(offense.IsRange);

            if (evasion <= 0)
            {
                return MaxHitPercent;
            }

            // Against a player the accuracy is taken off the PvP fields instead. The branch is
            // written out but never walked in this phase: the target of a swing is always a monster
            // and nobody fills those fields yet
            int hit = defense.IsPlayer ? offense.PvPHit : offense.Hit;

            int percent = hit * 100 / evasion;

            return percent > MaxHitPercent ? MaxHitPercent : percent;
        }

        /// <summary>
        ///     Number the critical draw has to reach for the swing to be a critical one: the base
        ///     threshold plus what the target takes off critical hits minus the critical value of the
        ///     attacker, never below MinCriticalThreshold. The lower the threshold, the more of the
        ///     draws reach it
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public static int GetCriticalThreshold(CombatSnapshot offense, CombatSnapshot defense)
        {
            int threshold = CriticalThresholdBase + defense.EnemySubCriticalHit - offense.CriticalHit;

            return threshold < MinCriticalThreshold ? MinCriticalThreshold : threshold;
        }

        /// <summary>
        ///     Damage of a swing that landed: the roll of the weapon plus the damage of the attacker,
        ///     doubled when the swing is critical, and the armour of the target taken off the whole
        ///     of it once at the very end. The dice are rolled on a critical hit as well - a critical
        ///     hit doubles what was rolled and does not replace the roll
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        /// <param name="isCritical">Whether the swing is a critical hit</param>
        /// <param name="random">Source of the draws of the dice and of the odd half of the armour</param>
        public static int GetDamage(CombatSnapshot offense, CombatSnapshot defense, bool isCritical, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            // Places where the original adds more to the same sum: a bonus against the race of the
            // target, the beads of both sides, elemental damage, the flat and the percent modifiers
            // of an instant damage change and the modifiers of a skill. None of them has data behind
            // it here, so every one of them is a zero and the sum comes out the same
            int raceBonus = 0;

            int damage = raceBonus + offense.AttackDice.Roll(random) + offense.Damage;

            if (isCritical)
            {
                damage = GetCriticalDamage(offense, defense, damage);
            }

            damage -= GetArmorTaken(defense.GetArmor(offense.IsRange), random);

            if (damage < MinDamage)
            {
                return MinDamage;
            }

            // The health of both sides is bounded by a short - GPcAbility.MaxHp for a player,
            // ParmMonster.Hp for a monster - and 5132 reports what is left of it in a short as well,
            // so a swing is never allowed to carry more than a short can hold. This is our own guard
            // against rubbish in the parm and not a rule of the original: without it a health that
            // wraps around would reach the client
            if (damage > short.MaxValue)
            {
                return short.MaxValue;
            }

            return damage;
        }

        /// <summary>
        ///     Damage of a critical hit: the usual damage doubled, plus what the attacker adds to its
        ///     critical hits, minus what the target takes off them. A target that resists critical
        ///     hits harder than the attacker hits them takes the usual damage and never less, so a
        ///     critical hit is never worse than a plain one
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        /// <param name="damage">Damage of the same swing without the critical hit</param>
        private static int GetCriticalDamage(CombatSnapshot offense, CombatSnapshot defense, int damage)
        {
            int critical = damage * CriticalDamageMultiplier + offense.AddDDWhenCritical - defense.SubDDWhenCritical;

            return critical > damage ? critical : damage;
        }

        /// <summary>
        ///     How much of the armour of the target the swing loses: half of it, and the odd point of
        ///     an odd armour by a draw. The draw is made on every swing that landed, whether the
        ///     armour is odd or not, so the order of the draws does not depend on the numbers of the
        ///     target
        /// </summary>
        /// <param name="armor">Armour of the target on the way the swing goes through</param>
        /// <param name="random">Source of the draw of the odd point</param>
        private static int GetArmorTaken(int armor, Random random)
        {
            return armor / ArmorDivider + random.Next(ArmorOddRollRange) * (armor % ArmorDivider);
        }
    }
}

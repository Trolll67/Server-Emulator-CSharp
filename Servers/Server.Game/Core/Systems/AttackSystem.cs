using Packets.Server.Game.Models.Send.Attack;
using Server.Game.Models.Game;
using System;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     Combat characteristics of one side of a swing, taken off the character at the moment the
    ///     swing is calculated. The names repeat the fields of GPcAbility/GPcDetail so a value can
    ///     be traced back to the place that fills it. Both a player and a monster fit in here: a
    ///     monster keeps its numbers in the very same Detail/Ability once its abilities are
    ///     calculated, which is the business of the spawn path and not of this system
    /// </summary>
    public readonly struct CombatSnapshot
    {
        /// <summary>
        ///     Melee, range and magic attack, GPcAbility.DDv/RDv/MDv
        /// </summary>
        public short DDv { get; init; }
        public short RDv { get; init; }
        public short MDv { get; init; }

        /// <summary>
        ///     Melee, range and magic defence, GPcAbility.DPv/RPv/MPv
        /// </summary>
        public short DPv { get; init; }
        public short RPv { get; init; }
        public short MPv { get; init; }

        /// <summary>
        ///     Melee, range and magic accuracy, GPcAbility.DHit/RHit/MHit
        /// </summary>
        public short DHit { get; init; }
        public short RHit { get; init; }
        public short MHit { get; init; }

        /// <summary>
        ///     Melee, range and magic evasion, GPcAbility.DDD/RDD/MDD
        /// </summary>
        public short DDD { get; init; }
        public short RDD { get; init; }
        public short MDD { get; init; }

        /// <summary>
        ///     Damage of the weapon itself, GPcDetail.MinD/MaxD. For a monster both come from the
        ///     parm (ParmMonster.MinD/MaxD); for a player the weapon does not fill them yet, so the
        ///     roll is a zero and the whole damage comes out of the attack values
        /// </summary>
        public short MinD { get; init; }
        public short MaxD { get; init; }

        /// <summary>
        ///     Chance of a critical hit of this side, in points, GPcAbility.CriticalHit
        /// </summary>
        public short CriticalHit { get; init; }

        /// <summary>
        ///     How many points this side takes off the critical chance of whoever attacks it,
        ///     GPcAbility.EnemySubCriticalHit
        /// </summary>
        public short EnemySubCriticalHit { get; init; }

        /// <summary>
        ///     Damage this side adds to its own critical hit, GPcAbility.AddDDWhenCritical
        /// </summary>
        public short AddDDWhenCritical { get; init; }

        /// <summary>
        ///     Damage this side takes off a critical hit landed on it, GPcAbility.SubDDWhenCritical
        /// </summary>
        public short SubDDWhenCritical { get; init; }

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
            GPcDetail detail = character.Detail;

            return new CombatSnapshot
            {
                DDv = ability.DDv,
                RDv = ability.RDv,
                MDv = ability.MDv,

                DPv = ability.DPv,
                RPv = ability.RPv,
                MPv = ability.MPv,

                DHit = ability.DHit,
                RHit = ability.RHit,
                MHit = ability.MHit,

                DDD = ability.DDD,
                RDD = ability.RDD,
                MDD = ability.MDD,

                MinD = detail.MinD,
                MaxD = detail.MaxD,

                CriticalHit = ability.CriticalHit,
                EnemySubCriticalHit = ability.EnemySubCriticalHit,
                AddDDWhenCritical = ability.AddDDWhenCritical,
                SubDDWhenCritical = ability.SubDDWhenCritical
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
    ///     Way the swing goes through: melee, range or magic. One and the same channel decides
    ///     whether the swing lands and how much it takes off the target, so the accuracy of one way
    ///     of attacking never lands the damage of another one
    /// </summary>
    public enum AttackChannel
    {
        /// <summary>
        ///     DHit against DDD, DDv against DPv
        /// </summary>
        Melee = 0,

        /// <summary>
        ///     RHit against RDD, RDv against RPv
        /// </summary>
        Range = 1,

        /// <summary>
        ///     MHit against MDD, MDv against MPv
        /// </summary>
        Magic = 2
    }

    /// <summary>
    ///     Mathematics of a single swing: whether it lands, whether it is critical and how much
    ///     damage it deals. The system knows nothing about sessions, packets and services - it is
    ///     given two snapshots of characteristics and answers with a result, while the caller
    ///     decides whose health to lower and what to send (the same split as in MoveSystem).
    ///     TODO: the formulas of the original (FieldW.exe) are not extracted yet, so everything
    ///     below is the semantics of the draft this file used to hold - the hit chance out of the
    ///     difference between accuracy and evasion, the critical chance out of the difference
    ///     between the critical values, the damage out of the weapon roll and the attack minus the
    ///     defence - with the numbers picked by hand. Every threshold is a named constant here, so
    ///     the formulas can be replaced in one place once the real ones are known (A1)
    /// </summary>
    public class AttackSystem
    {
        /// <summary>
        ///     Chance to land a swing when accuracy and evasion are equal. Empirical value, not the
        ///     original: it comes from the draft this file used to hold
        /// </summary>
        public const double BaseHitChance = 0.905;

        /// <summary>
        ///     Upper bound of the hit chance: an attacker with any accuracy still misses sometimes.
        ///     Empirical value, not the original
        /// </summary>
        public const double MaxHitChance = 0.98;

        /// <summary>
        ///     Lower bound of the hit chance: an attacker with hopeless accuracy still lands a swing
        ///     sometimes. Empirical value, not the original
        /// </summary>
        public const double MinHitChance = 0.10;

        /// <summary>
        ///     What one point of accuracy above the evasion of the target is worth. Empirical value,
        ///     not the original
        /// </summary>
        public const double HitChancePerPoint = 0.01;

        /// <summary>
        ///     What one point of accuracy below the evasion of the target costs. Missing accuracy
        ///     weighs more than spare accuracy, exactly as in the draft. Empirical value, not the
        ///     original
        /// </summary>
        public const double MissChancePerPoint = 0.015;

        /// <summary>
        ///     What one point of the critical value above the critical defence of the target is
        ///     worth. Empirical value, not the original
        /// </summary>
        public const double CriticalChancePerPoint = 0.01;

        /// <summary>
        ///     Upper bound of the critical chance: a critical hit stays an event and does not become
        ///     the usual outcome of a swing. Empirical value, not the original
        /// </summary>
        public const double MaxCriticalChance = 0.50;

        /// <summary>
        ///     Damage of a swing that landed but was eaten by the defence of the target: a hit is
        ///     always felt. Empirical value, not the original
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
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public AttackResult Attack(CombatSnapshot offense, CombatSnapshot defense)
        {
            return Attack(offense, defense, _random);
        }

        /// <summary>
        ///     The same swing with an explicit source of randomness: with the same seed and the same
        ///     snapshots the answer is always the same. The draws are always taken in this order -
        ///     the hit, then the critical, then the weapon - and a critical hit takes no weapon draw
        ///     at all, because it always rolls the top damage of the weapon
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

            AttackChannel channel = GetAttackChannel(offense, defense, out int hitMargin);

            if (random.NextDouble() >= GetHitChance(hitMargin))
            {
                return new AttackResult(TypeHit.Miss, 0);
            }

            bool isCritical = random.NextDouble() < GetCriticalChance(offense, defense);

            int damage = GetDamage(offense, defense, channel, isCritical, random);

            return new AttackResult(isCritical ? TypeHit.Crit : TypeHit.Hit, damage);
        }

        /// <summary>
        ///     Chance of the swing to land, from MinHitChance to MaxHitChance
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public static double GetHitChance(CombatSnapshot offense, CombatSnapshot defense)
        {
            GetAttackChannel(offense, defense, out int hitMargin);

            return GetHitChance(hitMargin);
        }

        /// <summary>
        ///     Chance of the swing to land, out of the accuracy the attacker has above the evasion
        ///     of the target
        /// </summary>
        /// <param name="hitMargin">Answer of GetAttackChannel for these two sides</param>
        public static double GetHitChance(int hitMargin)
        {
            if (hitMargin >= 0)
            {
                double chance = BaseHitChance + hitMargin * HitChancePerPoint;

                return chance > MaxHitChance ? MaxHitChance : chance;
            }
            else
            {
                double chance = BaseHitChance + hitMargin * MissChancePerPoint;

                return chance < MinHitChance ? MinHitChance : chance;
            }
        }

        /// <summary>
        ///     The way the attacker goes through: the one whose accuracy beats the evasion of the
        ///     target by the most points, together with that difference. The draft compared the
        ///     three the same way, but through an if/else chain that could not see the magic
        ///     difference once the range one lost to the melee one.
        ///     TODO: the original picks the way of attacking by the weapon in hand and not by the
        ///     best of the three; this is the first thing to revise once the real formulas are
        ///     known (A1)
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        /// <param name="hitMargin">By how many points the accuracy beats the evasion</param>
        public static AttackChannel GetAttackChannel(CombatSnapshot offense, CombatSnapshot defense, out int hitMargin)
        {
            AttackChannel channel = AttackChannel.Melee;

            hitMargin = offense.DHit - defense.DDD;

            int rangeMargin = offense.RHit - defense.RDD;
            if (rangeMargin > hitMargin)
            {
                channel = AttackChannel.Range;
                hitMargin = rangeMargin;
            }

            int magicMargin = offense.MHit - defense.MDD;
            if (magicMargin > hitMargin)
            {
                channel = AttackChannel.Magic;
                hitMargin = magicMargin;
            }

            return channel;
        }

        /// <summary>
        ///     By how many points the accuracy of the attacker beats the evasion of the target on
        ///     the way it attacks through
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public static int GetHitMargin(CombatSnapshot offense, CombatSnapshot defense)
        {
            GetAttackChannel(offense, defense, out int hitMargin);

            return hitMargin;
        }

        /// <summary>
        ///     Chance of a landed swing to be a critical one, from zero to MaxCriticalChance
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        public static double GetCriticalChance(CombatSnapshot offense, CombatSnapshot defense)
        {
            int margin = offense.CriticalHit - defense.EnemySubCriticalHit;

            if (margin <= 0)
            {
                return 0;
            }

            double chance = margin * CriticalChancePerPoint;

            return chance > MaxCriticalChance ? MaxCriticalChance : chance;
        }

        /// <summary>
        ///     Damage of a swing that landed: the roll of the weapon plus what the attack values
        ///     leave after the defence of the target, and the critical addition on top of it
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        /// <param name="channel">Way the swing goes through, the answer of GetAttackChannel</param>
        /// <param name="isCritical">Whether the swing is a critical hit</param>
        /// <param name="random">Source of the weapon roll</param>
        public static int GetDamage(CombatSnapshot offense, CombatSnapshot defense, AttackChannel channel, bool isCritical, Random random)
        {
            int damage = GetWeaponDamage(offense, isCritical, random) + GetAttackPower(offense, defense, channel);

            if (isCritical)
            {
                damage += GetCriticalDamage(offense, defense);
            }

            if (damage < MinDamage)
            {
                return MinDamage;
            }

            // The health of both sides is bounded by a short - GPcAbility.MaxHp for a player,
            // ParmMonster.Hp for a monster - and 5132 reports what is left of it in a short as
            // well, so a swing is never allowed to carry more than a short can hold: rubbish in the
            // parm must not turn into a health that wraps around on the way to the client
            if (damage > short.MaxValue)
            {
                return short.MaxValue;
            }

            return damage;
        }

        /// <summary>
        ///     What the attack value of the attacker leaves after the defence of the target on the
        ///     way the swing goes through. Only that one way counts: the accuracy that landed the
        ///     swing and the damage it deals belong together, so a melee hit does not carry the
        ///     magic attack of the attacker as well. A way that cannot get through the defence deals
        ///     nothing instead of healing the target
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        /// <param name="channel">Way the swing goes through, the answer of GetAttackChannel</param>
        public static int GetAttackPower(CombatSnapshot offense, CombatSnapshot defense, AttackChannel channel)
        {
            int power;

            switch (channel)
            {
                case AttackChannel.Range:
                    power = offense.RDv - defense.RPv;
                    break;
                case AttackChannel.Magic:
                    power = offense.MDv - defense.MPv;
                    break;
                default:
                    power = offense.DDv - defense.DPv;
                    break;
            }

            return power > 0 ? power : 0;
        }

        /// <summary>
        ///     Roll of the weapon of the attacker, MinD to MaxD inclusive. A critical hit takes the
        ///     top of the weapon without a draw, the way the draft took the top attack values on a
        ///     critical hit
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="isCritical">Whether the swing is a critical hit</param>
        /// <param name="random">Source of the roll</param>
        private static int GetWeaponDamage(CombatSnapshot offense, bool isCritical, Random random)
        {
            int minDamage = offense.MinD > 0 ? offense.MinD : 0;
            int maxDamage = offense.MaxD > minDamage ? offense.MaxD : minDamage;

            if (isCritical || minDamage == maxDamage)
            {
                return maxDamage;
            }

            return random.Next(minDamage, maxDamage + 1);
        }

        /// <summary>
        ///     What a critical hit adds on top of the usual damage: what the attacker adds to its
        ///     critical hits minus what the target takes off them. A target that resists critical
        ///     hits harder than the attacker hits them takes the usual damage and not less
        /// </summary>
        /// <param name="offense">Characteristics of the attacker</param>
        /// <param name="defense">Characteristics of the target</param>
        private static int GetCriticalDamage(CombatSnapshot offense, CombatSnapshot defense)
        {
            int damage = offense.AddDDWhenCritical - defense.SubDDWhenCritical;

            return damage > 0 ? damage : 0;
        }
    }
}

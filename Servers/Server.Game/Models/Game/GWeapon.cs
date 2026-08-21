using System;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Damage dice of a weapon, "XdY+Z": X dice of Y faces each and a flat Z on top of the sum.
    ///     A weapon with no dice at all is a valid one - it always deals its flat part and nothing
    ///     more
    /// </summary>
    public readonly struct GDice
    {
        /// <summary>
        ///     Dice of the roll, the X of "XdY+Z"
        /// </summary>
        /// <param name="count">How many dice are thrown</param>
        /// <param name="faces">How many faces every one of them has</param>
        /// <param name="plus">Flat addition to the sum of the dice</param>
        public GDice(int count, int faces, int plus)
        {
            Count = count;
            Faces = faces;
            Plus = plus;
        }

        /// <summary>
        ///     How many dice are thrown
        /// </summary>
        public int Count { get; }

        /// <summary>
        ///     How many faces every die has
        /// </summary>
        public int Faces { get; }

        /// <summary>
        ///     Flat addition, dealt whether the dice are thrown or not
        /// </summary>
        public int Plus { get; }

        /// <summary>
        ///     Lowest the roll can come out as: every die shows one
        /// </summary>
        public int Min => Count > 0 && Faces > 0 ? Plus + Count : Plus;

        /// <summary>
        ///     Highest the roll can come out as: every die shows its last face
        /// </summary>
        public int Max => Count > 0 && Faces > 0 ? Plus + (Count * Faces) : Plus;

        /// <summary>
        ///     Throw of the dice: the flat part plus one draw per die, every draw a number from one
        ///     to the count of faces. The draws are made one by one, in the order the dice are
        ///     counted, so a probe with a seeded source gets the same numbers every run. Dice with
        ///     no faces are not drawn for at all - a rubbish row in the database must not turn a
        ///     swing into an exception
        /// </summary>
        /// <param name="random">Source of the draws, one draw per die</param>
        public int Roll(Random random)
        {
            int damage = Plus;

            if (Faces <= 0)
            {
                return damage;
            }

            for (int die = 0; die < Count; die++)
            {
                damage += 1 + random.Next(Faces);
            }

            return damage;
        }
    }

    /// <summary>
    ///     Combat properties of the weapon a character holds: what it rolls for damage on every way
    ///     a swing can go through and how well it lands. Knows nothing about sessions, packets or
    ///     the database - only numbers and a roll - so the same model fits the weapon of a player,
    ///     the default weapon of a monster and a probe driven by a seeded source of randomness
    /// </summary>
    public class GWeapon
    {
        /// <summary>
        ///     Item type of a melee weapon. Damage dice are carried by three item types only: this
        ///     one, the range weapon and the spear. Everything else in the hand, an empty hand
        ///     included, swings the melee way
        /// </summary>
        public const int ItemTypeMeleeWeapon = 1;

        /// <summary>
        ///     Item type of a range weapon, the only type that puts a swing on the range way. The
        ///     name of the same number in ItemTypeEnum does not say so, and the enumeration is left
        ///     alone on purpose - the way a swing goes through is decided here and nowhere else
        /// </summary>
        public const int ItemTypeRangeWeapon = 18;

        /// <summary>
        ///     Item type of a spear. A melee weapon with a reach of its own, so it swings the melee
        ///     way just like the usual one
        /// </summary>
        public const int ItemTypeSpearWeapon = 20;

        /// <summary>
        ///     Melee, range and magic damage dice. The magic ones are never rolled by a swing of a
        ///     weapon - only skills go the magic way - but they are kept so the model holds
        ///     everything an item carries
        /// </summary>
        public GDice DDd { get; set; }
        public GDice RDd { get; set; }
        public GDice MDd { get; set; }

        /// <summary>
        ///     Melee, range and magic accuracy of the weapon itself. The very same accuracy is also
        ///     summed into the characteristics of the character off every equipped item, so a
        ///     weapon in the hand counts twice - that is how the original does it, and evening it
        ///     out would put us off the balance of the items
        /// </summary>
        public short DHit { get; set; }
        public short RHit { get; set; }
        public short MHit { get; set; }

        /// <summary>
        ///     Whether the weapon puts a swing on the range way
        /// </summary>
        public bool IsRange { get; set; }

        /// <summary>
        ///     Dice a swing of this weapon rolls: the range ones for a range weapon, the melee ones
        ///     for everything else
        /// </summary>
        public GDice AttackDice => IsRange ? RDd : DDd;

        /// <summary>
        ///     Accuracy a swing of this weapon lands with, taken off the same way as the dice
        /// </summary>
        public short AttackHit => IsRange ? RHit : DHit;

        /// <summary>
        ///     Whether an item of this type puts a swing on the range way. Asked of the numeric
        ///     item type and not of the enumeration, because the names in the enumeration do not
        ///     match the numbers the item table gives to weapons
        /// </summary>
        /// <param name="itemType">Numeric item type of the item in the hand</param>
        public static bool IsRangeType(int itemType)
        {
            return itemType == ItemTypeRangeWeapon;
        }

        /// <summary>
        ///     Default weapon, the one a monster and an unarmed player hit with. It is built out of
        ///     the parm row: the accuracy of the row goes to all three ways, and the damage of the
        ///     row turns into a single die - the low damage is the flat part and the spread up to
        ///     the high damage is the faces of the die. A row whose high damage is the same as the
        ///     low one - or, in a broken row, below it - deals the low damage and nothing else
        /// </summary>
        /// <param name="hit">Accuracy of the parm row</param>
        /// <param name="minDamage">Low damage of the parm row</param>
        /// <param name="maxDamage">High damage of the parm row</param>
        public static GWeapon CreateDefault(short hit, short minDamage, short maxDamage)
        {
            GDice dice = minDamage < maxDamage
                ? new GDice(1, maxDamage - minDamage, minDamage)
                : new GDice(0, 0, minDamage);

            return new GWeapon
            {
                DDd = dice,
                RDd = dice,
                MDd = dice,
                DHit = hit,
                RHit = hit,
                MHit = hit,
                IsRange = false
            };
        }
    }
}

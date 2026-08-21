using Database.DataModel.Enums;
using Database.DataModel.Models;
using Server.Game.Models.Game;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Item game model
    /// </summary>
    public class GItem : Item
    {
        public GItem()
            : base()
        {

        }

        public GItem(Item parmItem)
            : base()
        {
            Id = parmItem.Id;
            EquipType = parmItem.GetEquipType();
            Type = parmItem.Type;
            Name = parmItem.Name;
            DPv = parmItem.DPv;
            MPv = parmItem.MPv;
            RPv = parmItem.RPv;
            DHit = parmItem.DHit;
            DDd = parmItem.DDd;
            RHit = parmItem.RHit;
            MHit = parmItem.MHit;
            DDdDice = ParseDice(parmItem.DDd);
            RDdDice = ParseDice(parmItem.RDd);
            MDdDice = ParseDice(parmItem.MDd);
            Critical = parmItem.Critical;
            EnemySubCriticalHit = parmItem.EnemySubCriticalHit;
            AddDDWhenCritical = parmItem.AddDDWhenCritical;
            SubDDWhenCritical = parmItem.SubDDWhenCritical;
            Str = parmItem.Str;
            Dex = parmItem.Dex;
            Int = parmItem.Int;
            HpPlus = parmItem.HpPlus;
            HpRegen = parmItem.HpRegen;
            AddHpPotionRestore = parmItem.AddHpPotionRestore;
            AddMaxHpWhenTransform = parmItem.AddMaxHpWhenTransform;
            Mpplus = parmItem.Mpplus;
            MpRegen = parmItem.MpRegen;
            AddMpPotionRestore = parmItem.AddMpPotionRestore;
            AddMaxMpWhenTransform = parmItem.AddMaxMpWhenTransform;
            Weight = parmItem.Weight;
            AddWeight = parmItem.AddWeight;
            MaxStack = parmItem.MaxStack;
            AttackRate = parmItem.AttackRate;
            AddAttackRateWhenTransform = parmItem.AddAttackRateWhenTransform;
            MoveRate = parmItem.MoveRate;
            AddMoveRateWhenTransform = parmItem.AddMoveRateWhenTransform;
            AddShortAttackRange = parmItem.AddShortAttackRange;
            UseLevel = parmItem.UseLevel;
            UseInAttack = parmItem.UseInAttack;
            IsConfirm = parmItem.IsConfirm;

            TermOfValidity = parmItem.TermOfValidity;
        }
        public GItem(GItem model)
            : base()
        {
            Id = model.Id;
            EquipType = model.EquipType;
            Type = model.Type;
            Name = model.Name;

            

            DPv = model.DPv;
            MPv = model.MPv;
            RPv = model.RPv;
            DHit = model.DHit;
            DDd = model.DDd;
            RHit = model.RHit;
            MHit = model.MHit;
            DDdDice = model.DDdDice;
            RDdDice = model.RDdDice;
            MDdDice = model.MDdDice;
            Critical = model.Critical;
            EnemySubCriticalHit = model.EnemySubCriticalHit;
            AddDDWhenCritical = model.AddDDWhenCritical;
            SubDDWhenCritical = model.SubDDWhenCritical;
            Str = model.Str;
            Dex = model.Dex;
            Int = model.Int;
            HpPlus = model.HpPlus;
            HpRegen = model.HpRegen;
            AddHpPotionRestore = model.AddHpPotionRestore;
            AddMaxHpWhenTransform = model.AddMaxHpWhenTransform;
            Mpplus = model.Mpplus;
            MpRegen = model.MpRegen;
            AddMpPotionRestore = model.AddMpPotionRestore;
            AddMaxMpWhenTransform = model.AddMaxMpWhenTransform;
            Weight = model.Weight;
            AddWeight = model.AddWeight;
            MaxStack = model.MaxStack;
            AttackRate = model.AttackRate;
            AddAttackRateWhenTransform = model.AddAttackRateWhenTransform;
            MoveRate = model.MoveRate;
            AddMoveRateWhenTransform = model.AddMoveRateWhenTransform;
            AddShortAttackRange = model.AddShortAttackRange;
            UseLevel = model.UseLevel;
            UseInAttack = model.UseInAttack;

            SerialNumber = model.SerialNumber;
            EquipPos = model.EquipPos;
            Count = model.Count;
            IsConfirm = model.IsConfirm;
            EndTick = model.EndTick;
            UseCount = model.UseCount;
            EatTime = model.EatTime;
            TermOfValidity = model.TermOfValidity;
            ItemBind = model.ItemBind;
            Restore = model.Restore;
            Hole = model.Hole;
        }

        public ItemEquipTypeEnum EquipType { get; set; }

        /// <summary>
        ///     Melee, range and magic damage dice of the item, parsed off the "XdY+Z" strings of the
        ///     parm row. Every worn item gives the flat part of its dice to the damage of the
        ///     character, and the one in the hand rolls its dice on every swing
        /// </summary>
        public GDice DDdDice { get; set; }
        public GDice RDdDice { get; set; }
        public GDice MDdDice { get; set; }
        public ItemEquipTypeEnum? EquipPos { get; set; }
        public int Count { get; set; }
        public uint EndTick { get; set; }
        public short UseCount { get; set; }
        public uint EatTime { get; set; }
        public ItemBindTypeEnum ItemBind { get; set; }
        public byte Restore { get; set; }
        public byte Hole { get; set; }

        

        public ulong SerialNumber { get; set; }

        /// <summary>
        ///     Highest count of dice a parm string may hold. A swing rolls the dice one by one in
        ///     the middle of a fight, so a row with an absurd count would hang the hit instead of
        ///     dealing a lot of damage - anything above the bound is read as a broken row
        /// </summary>
        public const int MaxDiceCount = 100;

        /// <summary>
        ///     Highest count of faces a die of a parm string may have, taken for the same reason as
        ///     the bound of the count: a number this far out is a broken row and not balance
        /// </summary>
        public const int MaxDiceFaces = 1000;

        /// <summary>
        ///     Whether the item is a weapon of the melee way: the usual one or a spear. The types
        ///     of weapons are asked of GWeapon, because the way a swing goes through is decided
        ///     there and in one place only
        /// </summary>
        public bool IsMeleeWeapon => (int)Type == GWeapon.ItemTypeMeleeWeapon
            || (int)Type == GWeapon.ItemTypeSpearWeapon;

        /// <summary>
        ///     Whether the item is a weapon of the range way
        /// </summary>
        public bool IsRangeWeapon => GWeapon.IsRangeType((int)Type);

        /// <summary>
        ///     Whether the item is a weapon of the magic way. Nothing swings it - the magic dice
        ///     are rolled by skills only - but the character still keeps them apart from the rest
        /// </summary>
        public bool IsMagicWeapon => Type == ItemTypeEnum.Book;

        /// <summary>
        ///     Combat properties of the item, the ones a swing asks of the weapon in the hand:
        ///     three sets of dice, three accuracies and the way the item puts a swing through
        /// </summary>
        public GWeapon CreateWeapon()
        {
            return new GWeapon
            {
                DDd = DDdDice,
                RDd = RDdDice,
                MDd = MDdDice,
                DHit = DHit,
                RHit = RHit,
                MHit = MHit,
                IsRange = IsRangeWeapon
            };
        }

        /// <summary>
        ///     Damage dice out of a parm string of the "XdY+Z" shape: X dice of Y faces each and a
        ///     flat Z on top. The letter comes in either case and the parts may be spaced apart
        ///     ("2D3 +1"), the addition may be missing ("XdY"), and a row may hold the flat part
        ///     alone, with no dice and no sign at all ("1") - a couple of weapons are written that
        ///     way and their only point of damage must not be lost. The addition keeps its sign, so
        ///     a row that takes damage away instead of adding it is read as it is written.
        ///     Everything else - an empty string, a count or a face out of the bounds above, a part
        ///     that does not read as a number - gives dice that roll nothing: the parm is read once
        ///     at start-up, and one broken row must not take the load down
        /// </summary>
        /// <param name="dice">Dice string of the parm row</param>
        public static GDice ParseDice(string dice)
        {
            GDice none = new GDice(0, 0, 0);

            if (string.IsNullOrWhiteSpace(dice))
            {
                return none;
            }

            // No letter of the dice at all: the whole row is the flat part
            int letter = dice.IndexOfAny(new[] { 'd', 'D' });
            if (letter < 0)
            {
                return int.TryParse(dice, out int flat) && IsPlusInBounds(flat)
                    ? new GDice(0, 0, flat)
                    : none;
            }

            if (!int.TryParse(dice.Substring(0, letter), out int count))
            {
                return none;
            }

            // The tail after the letter holds the faces and, from the first sign on, the addition
            string facesPart = dice.Substring(letter + 1);
            int sign = facesPart.IndexOfAny(new[] { '+', '-' });
            int plus = 0;

            if (sign >= 0)
            {
                if (!int.TryParse(facesPart.Substring(sign), out plus))
                {
                    return none;
                }

                facesPart = facesPart.Substring(0, sign);
            }

            if (!int.TryParse(facesPart, out int faces))
            {
                return none;
            }

            if (count < 0 || count > MaxDiceCount || faces < 0 || faces > MaxDiceFaces
                || !IsPlusInBounds(plus))
            {
                return none;
            }

            return new GDice(count, faces, plus);
        }

        /// <summary>
        ///     Whether the flat part of the dice fits the characteristics of a character: they are
        ///     kept short-wide and the flat parts of the worn items are summed into them
        /// </summary>
        /// <param name="plus">Flat part read off the parm string</param>
        private static bool IsPlusInBounds(int plus)
        {
            return plus >= short.MinValue && plus <= short.MaxValue;
        }
    }
}

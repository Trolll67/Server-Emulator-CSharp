using Database.DataModel.Enums;
using Database.DataModel.Models;
using Packets.Server.Game.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Models.Game
{
    public class GChar : GObject
    {
        public GChar()
        {
            Detail = new GPcDetail();
            Ability = new GPcAbility();
            Simple = new GPcSimple();
            // A character always holds something: until the parm row is read the default weapon is
            // an empty one, so a swing rolls a zero instead of falling over a missing weapon
            WeaponDef = new GWeapon();
            //ParmMon = new ParmMonster();
            //ParmMonCur = new ParmMonster();
        }
        public float _DistAttack { get; set; }

        public GItem Weapon { get; set; }

        /// <summary>
        ///     Default weapon, built out of the parm row of the character. A monster hits with it
        ///     always, a player hits with it while the weapon slot is empty
        /// </summary>
        public GWeapon WeaponDef { get; private set; }

        /// <summary>
        ///     Combat properties of the item in the weapon slot, filled next to Weapon by the
        ///     calculation of the characteristics. Empty means an empty hand
        /// </summary>
        public GWeapon WeaponEquip { get; set; }
        public GPcDetail Detail { get; set; }
        public GPcAbility Ability { get; set; }
        public GPcSimple Simple { get; set; }
        public ParmMonster ParmMon { get; set; }
        public ParmMonster ParmMonCur { get; set; }
        public UniqueId TargetUniqueId { get; set; }
        //public UniqueId MainAttacker { get; set; }
        //public UniqueId LastAttacker { get; set; }
        //public UniqueId _FoundItem { get; set; }
        public Vector3 _PosTo { get; set; }
        public Vector3 _PosToLast { get; set; }
        public Vector3 _PosToStep { get; set; }
        public float DirectionSight { get; set; }


        public MonsterSpot _Spot { get; set; }
        public MonsterSpotGroup _SpotGroup { get; set; }

        public bool IsVsibleFirst { get; set; }
        public DateTime LastUpdateHpMp { get; set; }
        public DateTime? DeadTime { get; set; }

        public short AttackRate { get; set; }
        public float MoveRate { get; set; }

        public short AddTransformAttackRate { get; set; }
        public short AddTransformMoveRate { get; set; }

        public uint _CastingDelay { get; set; }


        public bool CantDie { get; set; }
        public byte FssActCalledCnt { get; set; }
        public byte TargetMoveCnt { get; set; }
        public byte IAD { get; set; }
        public short IRD { get; set; }
        public short TotDamageToTarget { get; set; }
        public short TotDamageFromMain { get; set; }
        public short TotDamageFromOther { get; set; }
        public short AddHp { get; set; }
        public short AddMp { get; set; }
        public short AddRegenHp { get; set; }
        public short AddRegenMp { get; set; }
        public short AddStr { get; set; }
        public short AddDex { get; set; }
        public short AddInt { get; set; }
        public short AddDDV { get; set; }
        public short AddMDV { get; set; }
        public short AddRDV { get; set; }
        public short AddDPV { get; set; }
        public short AddMPV { get; set; }
        public short AddRPV { get; set; }
        public short AddDD { get; set; }
        public short AddHit { get; set; }
        public short AddRDD { get; set; }
        public short AddRHit { get; set; }
        public short AddMDD { get; set; }
        public short AddMHit { get; set; }
        public short AddWeight { get; set; }
        public short AddMoveSpeed { get; set; }
        public short AddAttackSpeed { get; set; }
        public short BaseMoveSpeed { get; set; }
        public short AddHwRegenHp { get; set; }
        public short AddHwRegenMp { get; set; }
        public short _NextDamage { get; set; }
        public short ReduceUseMp { get; set; }
        public short AddReduceUseMp { get; set; }
        public short AddSkillHitRate { get; set; }
        public int _AbNoCriticalHitUp { get; set; }
        public PlaceEnum _HomePlace { get; set; }
        public int AiParam { get; set; }
        public uint _GuildAssNo { get; set; }
        public short _AddCastingDelay { get; set; }
        public uint _SpdAttackPerFire { get; set; }
        public uint TickLastFight { get; set; }
        public uint _TickStxUmZil { get; set; }
        public uint TickLastSkill { get; set; }
        public uint TickLastPotion { get; set; }
        public uint _TickLastSearchRace { get; set; }
        public float _SpdWalkPerSec { get; set; }
        public float _SpdWalkPerFire { get; set; }
        public float _SpdWalkPerFireSq { get; set; }
        public float _SpdRunPerHalfFire { get; set; }
        public float SpdHackMaxDist { get; set; }
        public float SpdHackMovedDist { get; set; }

        public uint AiRaidCndLastTick { get; set; }
        public int WeaponApplyAbnItemNo { get; set; }
        public uint WeaponApplyAbnItemEndTick { get; set; }
        public uint LastLoadingTick { get; set; }

        public uint _AbQFlagThreadId { get; set; }
        public int _BeAttachedAbQFlag { get; set; }
        public bool _IsAbsoluteHit { get; set; }
        public short PowerwordLust { get; set; }
        public short ArmorBreak { get; set; }
        public short DestructionOfInner { get; set; }

        private void Attack(GChar target)
        {

        }

        /// <summary>
        ///     Weapon in the hand: the equipped one when there is one, the default one otherwise.
        ///     Everything a swing asks of a weapon - the dice, the accuracy, the way it goes
        ///     through - is asked of the answer of this one, so an unarmed character needs no
        ///     special case anywhere else
        /// </summary>
        public GWeapon GetWeaponInHand()
        {
            return WeaponEquip ?? WeaponDef;
        }

        public void CalcSpeed()
        {
            AttackRate = AddAttackSpeed;
            MoveRate = BaseMoveSpeed + AddMoveSpeed;

            if ((ParmMon.ParmNo == 151 || ParmMon.ParmNo == 952))// ParmItemSmall.IsRange(Weapon.ItemSmall))
                AttackRate += ParmMon.AttackRateOrg;
            else
                AttackRate += Detail.AttackRate;

            if (ParmMon != ParmMonCur)
            {
                AttackRate += AddTransformAttackRate;
                MoveRate += AddTransformMoveRate;
            }

            _SpdAttackPerFire = (uint)(AttackRate / 100);
            _SpdWalkPerSec = (uint)(MoveRate * 0.5);
            _SpdWalkPerFire = (uint)(MoveRate * 0.5);
            _SpdRunPerHalfFire = (uint)(MoveRate * 0.5);
            _SpdWalkPerFireSq = (uint)(MoveRate * 0.5 * (MoveRate * 0.5));
            SpdHackMaxDist = (uint)(MoveRate * 30) + (MoveRate * 7);
        }

        public void CalcAbility()
        {
            Ability.Reset();
            Ability.MaxHp = ParmMon.Hp;
            Ability.MaxMp = ParmMon.Mp;
            Ability.HpRegen = AddRegenHp;
            Ability.MpRegen = AddRegenMp;
            Ability.HwHpRegen = AddHwRegenHp;
            Ability.HwMpRegen = AddHwRegenMp;
            // TODO: the rest of what the parm row gives is not applied - bonuses against
            // a race, wards, elemental attack and resistance, inflicting and resisting
            // abnormal states
        }
        public void _SetDefaultInfo(ParmMonster parmMon)
        {
            ParmMon = parmMon;
            ParmMonCur = parmMon;


            AddRegenHp = ParmMon.HpRegen;
            AddRegenMp = ParmMon.MpRegen;

            Transformed(parmMon);
        }

        public void Transformed(ParmMonster parmMonCur)
        {
            ParmMonCur = parmMonCur;

            if (ParmMon.GbjClass == GbjClassEnum.Pc)
                Simple.OldLevel = Simple.Level;

            if (ParmMon == ParmMonCur)
            {
                // The default weapon is rebuilt out of the current parm row: the accuracy and the
                // damage spread of the row are everything it takes. The row of a player class is
                // shaped the same way as the row of a monster, so one place covers both
                WeaponDef = GWeapon.CreateDefault(ParmMonCur.Hit, ParmMonCur.MinD, ParmMonCur.MaxD);

                if (ParmMon.GbjClass == GbjClassEnum.Mon)
                {
                    _DistAttack = ParmMonCur.DistMelee;
                }
                else if (ParmMon.GbjClass == GbjClassEnum.Pc)
                {
                    _DistAttack = ParmMonCur.DistMelee;// Weapon.Range;
                }

                BaseMoveSpeed = ParmMonCur.MoveRateOrg;
                Detail.AttackRate = ParmMonCur.AttackRateOrg;
                Detail.MoveRate = ParmMonCur.MoveRateOrg;
            }
            else
            {
                _DistAttack = ParmMonCur.DistMelee;
                // TODO: the transform ability table is not read - the transformed shape keeps
                // the attack and move rates of the character instead of its own
                if (ParmMonCur.AttackType == AttackTypeEnum.Long)
                {
                    _DistAttack += Weapon.AddLongAttackRange;
                }
                else
                {
                    _DistAttack += Weapon.AddShortAttackRange;
                }
            }

            _DistAttack += ParmMonCur.BodySz;
            _CastingDelay = (uint)ParmMonCur.CastingDelay / 100;

            CalcSpeed();
        }

        private void _CalcWaponDamage(GChar target)
        {

        }
        #region TODO
        // Sides of a character the model does not carry yet: flags, an AI hook, action
        // sequences, a navigation path, active modifiers and abnormal states, raid info,
        // party size level, skill casting and cooldown delays, reflected and cycle damage,
        // instant damage modifiers, extra bonuses against a race and wards
        #endregion
    }
}

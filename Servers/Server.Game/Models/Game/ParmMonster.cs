using Database.DataModel.Enums;
using Database.DataModel.Models;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    public class ParmMonster
    {
        public ParmMonster(Monster monster)
        {
            ParmNo = monster.Id;
            Level = monster.Level;
            Class = monster.Class;
            Exp = monster.Exp;
            DDv = monster.DDv;
            MDv = monster.MDv;
            RDv = monster.RDv;
            DPv = monster.DPv;
            MPv = monster.MPv;
            RPv = monster.RPv;
            Hit = monster.Hit;
            MinD = monster.MinDamage;
            MaxD = monster.MaxDamage;
            AttackRateOrg = monster.AttackRateOrg;
            MoveRateOrg = monster.MoveRateOrg;
            AttackRateNew = monster.AttackRateNew;
            MoveRateNew = monster.MoveRateNew;
            Hp = monster.HpMax;
            Mp = monster.MpMax;
            //ChgAddHpTong = monster.
            //ChgAddMpTong = monster.
            MoveRange = monster.MoveRange;
            GbjClass = monster.Type;
            RaceType = monster.RaceType;
            AiType = monster.AiType;
            CastingDelay = monster.CastingDelay;
            Chaotic = monster.Chaotic;
            DistSight = monster.SightRange;
            DistMelee = monster.AttackRange;
            DistSkill = monster.SkillRange;
            DistSightSq = monster.SightRange * monster.SightRange;
            DistMeleeSq = monster.AttackRange * monster.AttackRange;
            DistSkillSq = monster.SkillRange * monster.SkillRange;
            BodySz = monster.BodySize;
            BodySzSq = (float)((monster.BodySize * monster.Scale) * (monster.BodySize * monster.Scale));
            DetectTransF = monster.DetectTransformF;
            DetectTransP = monster.DetectTransformP;
            DetectChao = monster.DetectChaotic;
            MerchListIdBuy = monster.BuyMerchanId;
            MerchListIdSell = monster.SellMerchanId;
            MerchListIdCharge = monster.ChargeMerchanId;
            //ChgWeight = monster.Add
            //CountryBit = monster.
            HpRegen = monster.HpRegen;
            MpRegen = monster.MpRegen;
            IsShowHp = monster.IsShowHp;
            VolitionOfHonor = monster.VolitionOfHonor;
            IsAmpliableTermOfValidity = monster.IsAmpliableTermOfValidity;
            AttackType = monster.AttackType;
            TransType = monster.TransType;
            SubDDWhenCritical = monster.SubDDWhenCritical;
            EnemySubCriticalHit = monster.EnemySubCriticalHit;
            QuestMaking = monster.EventQuest;
            Nm = monster.Name;

            // Every group the monster drops from is copied off the reference: the chance of the
            // group belongs to the link "monster -> group" and the type ("on the ground" or
            // "into the bag of the killer") to the group itself, and the drop needs both. A link
            // that points at a group the reference does not hold is skipped - a hole in the parm
            // must not take the whole monster down
            DropGroups = new List<GDropGroup>();
            foreach (var dropGroup in monster.Drops)
            {
                if (dropGroup.DropGroup == null)
                {
                    continue;
                }

                GDropGroup dGroup = new GDropGroup
                {
                    DropGroupId = dropGroup.DropGroupId,
                    Percent = dropGroup.Percent,
                    DropGroupType = dropGroup.DropGroup.DropGroupType,
                    Items = new List<DropGroupItem>(dropGroup.DropGroup.Items)
                };
                DropGroups.Add(dGroup);
            }
        }

        public int ParmNo { get; set; }
        public int Level { get; set; }
        public PcClassEnum Class { get; set; }
        public ulong Exp { get; set; }
        public short DDv { get; set; }
        public short MDv { get; set; }
        public short RDv { get; set; }
        public short DPv { get; set; }
        public short MPv { get; set; }
        public short RPv { get; set; }
        public short Hit { get; set; }
        public short MinD { get; set; }
        public short MaxD { get; set; }
        public short AttackRateOrg { get; set; }
        public short MoveRateOrg { get; set; }
        public short AttackRateNew { get; set; }
        public short MoveRateNew { get; set; }
        public short Hp { get; set; }
        public short Mp { get; set; }
        public short ChgAddHpTong { get; set; }
        public short ChgAddMpTong { get; set; }
        public short MoveRange { get; set; }
        public GbjClassEnum GbjClass { get; set; }
        public RaceTypeEnum RaceType { get; set; }
        public AiTypeEnum AiType { get; set; }
        public short CastingDelay { get; set; }
        public short Chaotic { get; set; }
        public float DistSight { get; set; }
        public float DistMelee { get; set; }
        public float DistSkill { get; set; }
        public float DistSightSq { get; set; }
        public float DistMeleeSq { get; set; }
        public float DistSkillSq { get; set; }
        public int BodySz { get; set; }
        public float BodySzSq { get; set; }
        public short DetectTransF { get; set; }
        public short DetectTransP { get; set; }
        public short DetectChao { get; set; }
        public int MerchListIdBuy { get; set; }
        public int MerchListIdSell { get; set; }
        public int MerchListIdCharge { get; set; }
        public short ChgWeight { get; set; }
        public ulong CountryBit { get; set; }
        public short HpRegen { get; set; }
        public short MpRegen { get; set; }
        public bool IsShowHp { get; set; }
        public short VolitionOfHonor { get; set; }
        public bool IsAmpliableTermOfValidity { get; set; }
        public AttackTypeEnum AttackType { get; set; }
        public byte TransType { get; set; }
        public short SubDDWhenCritical { get; set; }
        public short EnemySubCriticalHit { get; set; }
        public byte QuestMaking { get; set; }
        public string Nm { get; set; }
        public List<int> NPCStatic { get; set; }
        public List<int> SameRace { get; set; }

        public List<GDropGroup> DropGroups { get; set; }

        // TODO: parts of the monster parm the model does not read yet - flags, AI hook,
        // dialog, per-monster equip rules, bonuses against a race, wards, elemental attack
        // and resistance, inflicting and resisting abnormal states
    }
}

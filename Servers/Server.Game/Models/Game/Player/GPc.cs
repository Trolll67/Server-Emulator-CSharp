using Database.DataModel.Enums;
using Packets.Server.Game.Structures;
using Server.Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Character game model
    /// </summary>
    public class GPc : GChar
    {
        public GPc()
            : base()
        {
            //Items = new List<ItemGameModel>();
            //Buffs = new List<BuffGameModel>();
            VisibleCharacterGames = new List<GameSession>();
            VisibleItemGames = new List<GPublicItem>();
            VisibleUnitGames = new List<GMonster>();
            Equip = new List<GPcEquip>();
            Inventory = new GPcInventory();
        }
        public List<GPcEquip> Equip { get; set; }
        public GPcInventory Inventory { get; set; }

        /// <summary>
        ///     Map the character is currently on (TblPcState.mMapNo). Kept from the loaded state so
        ///     the per-tick UspUpdatePos write does not reset the map to 0
        /// </summary>
        public int MapNo { get; set; }

        //public AttackTypeEnum AttackTarget { get; set; }
        public ushort AttackType { get; set; }
        public Vector3 AttackPosition { get; set; }
        public byte AttackFlag { get; set; }
        public bool checkAttack { get; set; }
        public DateTime AttackDateTime { get; set; }

        #region Etc
        public uint Action;
        public uint PartyNo;
        public uint LogoutTick;
        public uint TickEndBoa;
        public uint TickEndBoaEx;
        public uint SpdHackTick;
        public uint UserNo;
        public uint BlockChat;
        public uint BlockBoard;
        public byte PcBangLv;
        public uint ChatCnt;
        public uint ChatTick;
        public int NeedMoneyNo;
        public uint SpecialEffectTime;
        public short AddHidDDv;
        public short AddHidMDv;
        public short AddHidRDv;
        public short AddHidDPv;
        public short AddHidMPv;
        public short AddHidRPv;
        public short AddCriticalHit;
        public uint mSecurityTick;
        public uint BlockChatTime;
        public char PlusCode;
        public byte UseMacroTimes;
        public uint MacroTick;
        public int PreventAddictionPlayTm;
        public short IncExp;
        public short IncDropRate;
        public short AddPotionRestore;
        public short AddTransformMaxHP;
        public short AddTransformMaxMP;
        public uint TickLetterLimit;
        public int NewId;
        public short SkillHp;
        public byte TeamBattle;
        public bool IsCheckTeam;
        public bool PreventItemDrop;
        public ulong VisitTicketSerial;
        public short ReduceDmgPer;
        public short ReflectDmg;
        public short RestoreHpFromReduceDmg;
        public short HonorPtrProtect;
        public short HonorPtrAmp;
        public int TermOfVolitionOfHonor;
        public byte TotemDamage;
        public uint Freeze;
        public int IsQuestInit;
        public byte UserItemDrop;
        public byte UserAddPKDrop;
        public uint AddTransformTm;
        public short AddDDWhenCritical;
        public short InstAddMaxHP;
        public short InstAddDDD;
        public bool RecallToAgit;
        public short SubDescChaoticPercent;
        public short AddIncrChaoticPercent;
        public int PcBangGUID;
        public int AccountGUID;
        public uint LoginTick;
        public uint EmoticonTick;
        public short SubDDWhenCritical;
        public short EnemySubCriticalHit;
        public uint LootingTick;
        public int IsDialogEditMode;
        public int IsUseReturnNoItem;
        public int IsAutoStomach;
        public uint AutoLootingCnt;
        public int BeginHp;
        public int BeginMp;
        public short EventDungeonSerialNo;
        public int IsFreeFall;
        public int IsTeleportCenterTownRegistBullet;
        public uint __aServerTickAcummulate;
        public int Bomber;
        public uint CheckValidTime;
        public uint TeamRankNo;
        public uint mTeamRankGuildNo;
        public int IsCalendarReLoad;
        public bool IsLimitTimeInit;
        public bool IsARSAuth;
        public bool IsInValidHeightLog;
        public int IsUpdateLimitTime;
        public int ServantTotalAbility;
        public short PainKiller;
        public int IsPainKiller;
        public uint UmZilAttackTick;
        public int IsRegArena;
        public int IsBossBattleDead;
        public byte FierceBattleKillCnt;
        public int ArenaPanalty;
        //public long RestExp[3];
        //public long RestExpMax[2];

        //char UserId[21];
        //char CRMCode[17];
        //char JoinCode[2];
        //char BillNo[21];
        //int LimitPlayTime[2];
        //char mTeamRankGuildNm[17];
        #endregion

        #region Default fields
        //public List<GItem> Items { get; set; }
        //public List<GBuff> Buffs { get; set; }
        #endregion

        #region General fields
        /// <summary>
        ///     Unique identifier
        /// </summary>

        /// <summary>
        ///     Attacked unique identifier
        /// </summary>
        public UniqueId AttackedUniqueIdentifier { get; set; }
        #endregion

        #region Visible fields
        /// <summary>
        ///     Visible character game
        /// </summary>
        public List<GameSession> VisibleCharacterGames { get; set; }

        /// <summary>
        ///     Visible item game
        /// </summary>
        public List<GPublicItem> VisibleItemGames { get; set; }

        /// <summary>
        ///     Visible unit game
        /// </summary>
        public List<GMonster> VisibleUnitGames { get; set; }
        #endregion

        #region TODO
        // Subsystems of the character the model does not carry yet: personal shop, macros,
        // inventory and equipment as live objects, aggro history, teleports, exchange,
        // friends and chat filters, admin state, set-item bonuses, summons and servants,
        // skill trees, beads, achievements, quests, arena and ranking state. Each of them
        // arrives together with the mechanic that needs it
        #endregion

        public ErrorEnum EquipItem(ulong serialNo)
        {
            if (Simple.Hp < 1)
            {
                return ErrorEnum.CharAlreadyDie;
            }

            // TODO: a paralyzed or stunned character refuses to use an item with its own
            // error code - waits for abnormal states

            var item = Inventory.Items.FirstOrDefault(x => x.SerialNumber == serialNo);
            if (item == null)
            {
                //item = ServantInventory.FirstOrDefault(x => x.SerialNumber == serialNo);
                //if (item == null)
                //{
                //    return ErrorEnum.ItemNotExist;
                //}
            }

            // TODO: порт EquipItem не завершён
            return ErrorEnum.NotImplemented;
        }

        public new void CalcAbility()
        {
            // Everything below is summed over a cleared ability: the bonuses of the worn items go
            // in with +=, so a second call - a level up, a change of the equipment - has to give
            // the same numbers as the first one instead of doubling them
            Ability.Reset();

            Ability.Str = Simple.CalcStr();
            Ability.Dex = Simple.CalcDex();
            Ability.Int = Simple.CalcInt();
            Ability.DDv = 1;
            Ability.MDv = 1;
            Ability.RDv = 1;

            // Base regeneration of the class, exactly as GChar.CalcAbility takes it: AddRegenHp and
            // AddRegenMp are filled by _SetDefaultInfo from the parm of the class. The assignment
            // goes before the equipment, the worn items add their own regeneration to this base
            Ability.HpRegen = AddRegenHp;
            Ability.MpRegen = AddRegenMp;
            Ability.HwHpRegen = AddHwRegenHp;
            Ability.HwMpRegen = AddHwRegenMp;

            Ability.AddDDWhenCritical += AddDDWhenCritical;
            Ability.SubDDWhenCritical += SubDDWhenCritical;
            Ability.EnemySubCriticalHit += EnemySubCriticalHit;

            var addHpByItem = 0;
            var addMpByItem = 0;
            foreach (var equip in Equip)
            {
                var item = equip.Item;

                Ability.DDv += item.DDv;
                Ability.MDv += item.MDv;
                Ability.RDv += item.RDv;
                Ability.DPv += item.DPv;
                Ability.MPv += item.MPv;
                Ability.RPv += item.RPv;
                Ability.HidDDv += item.HDDv;
                Ability.HidMDv += item.HMDv;
                Ability.HidRDv += item.HRDv;
                Ability.HidDPv += item.HDPv;
                Ability.HidMPv += item.HMPv;
                Ability.HidRPv += item.HRPv;
                Ability.CriticalHit += item.Critical;
                Ability.AddDDWhenCritical += item.AddDDWhenCritical;
                Ability.SubDDWhenCritical += item.SubDDWhenCritical;
                Ability.EnemySubCriticalHit += item.EnemySubCriticalHit;
                Ability.DHit += item.DHit;
                Ability.RHit += item.RHit;
                Ability.MHit += item.MHit;
                Ability.Str += item.Str;
                Ability.Dex += item.Dex;
                Ability.Int += item.Int;
                Ability.HpRegen += item.HpRegen;
                Ability.MpRegen += item.MpRegen;
                addHpByItem += item.HpPlus;
                addMpByItem += item.Mpplus;

                // Damage of the character off the equipment: the flat part of the dice of every
                // worn item goes into the damage of its own way. The item that is itself the weapon
                // of that way is left out - a weapon in the hand puts its flat part into the roll
                // it makes on every swing, and taking it here as well would count it twice
                // TODO a blessed melee weapon gives one point of critical on top of that; the
                // status of a worn item is read nowhere yet
                if (!item.IsMeleeWeapon)
                {
                    Ability.DDD += (short)item.DDdDice.Plus;
                }

                if (!item.IsRangeWeapon)
                {
                    Ability.RDD += (short)item.RDdDice.Plus;
                }

                if (!item.IsMagicWeapon)
                {
                    Ability.MDD += (short)item.MDdDice.Plus;
                }

                // TODO: the rest of what a worn item gives is not applied yet - equip
                // penalties, bonuses against a race, wards, elemental attack and resistance,
                // inflicting and resisting abnormal states. Each waits for its mechanic
            }

            // The item in the weapon slot is what the character swings with: its dice, its accuracy
            // and the way a swing goes through are read off WeaponEquip on every hit. An empty slot
            // clears the field and the swing falls back to the default weapon of the class.
            // The slot an item is worn in (EquipPos) is filled by nothing on the way a character is
            // loaded, so the slot the item type belongs to is taken when it is missing
            var weaponEquip = Equip.FirstOrDefault(x =>
                (x.Item.EquipPos ?? x.Item.EquipType) == ItemEquipTypeEnum.Weapon);

            Weapon = weaponEquip?.Item;
            WeaponEquip = weaponEquip?.Item.CreateWeapon();

            // TODO: an abnormal state active on the character re-applies its ability change here
            Ability.DDv += AddDDV;
            Ability.MDv += AddMDV;
            Ability.RDv += AddRDV;
            Ability.DPv += AddDPV;
            Ability.MPv += AddMPV;
            Ability.RPv += AddRPV;
            Ability.DHit += AddHit;
            Ability.RHit += AddRHit;
            Ability.MHit += AddMHit;
            Ability.DDD += (short)(AddDD + InstAddDDD);
            Ability.RDD += AddRDD;
            Ability.MDD += AddMDD;

            _CalcAbility();

            Ability.HidDDv += (short)(AddHidDDv + 5);
            Ability.HidMDv += (short)(AddHidMDv + 5);
            Ability.HidRDv += (short)(AddHidRDv + 5);
            Ability.HidDPv += AddHidDPv;
            Ability.HidMPv += AddHidMPv;
            Ability.HidRPv += AddHidRPv;
            Ability.DDv += Ability.HidDDv;
            Ability.MDv += Ability.HidMDv;
            Ability.RDv += Ability.HidRDv;
            Ability.DPv += Ability.HidDPv;
            Ability.MPv += Ability.HidMPv;
            Ability.RPv += Ability.HidRPv;
            Ability.CriticalHit += (short)(AddCriticalHit + Ability.Dex / 10);
            // TODO: achievement bonuses to max hp are not applied - neither the base ones nor
            // the ones of the transformed shape
            if (ParmMon != ParmMonCur)
            {
                addHpByItem += AddTransformMaxHP;
            }

            CalcMaxHp(addHpByItem);
            if (Ability.MaxHp < Simple.Hp)
            {
                Simple.Hp = Ability.MaxHp;
            }

            if (Simple.Class == PcClassEnum.Wizard)
            {
                var addTransMaxMp = 0;
                if (ParmMon != ParmMonCur)
                {
                    addTransMaxMp += AddTransformMaxMP;
                }

                Ability.MaxMp = (short)(addMpByItem + addTransMaxMp + AddMp + 2 * (Ability.Int + Simple.Level + 15));
            }
            else
            {
                var addTransMaxMp = 0;
                if (ParmMon != ParmMonCur)
                {
                    addTransMaxMp += AddTransformMaxMP;
                }

                Ability.MaxMp = (short)(addMpByItem + addTransMaxMp + Simple.Level + AddMp + 2 * (Ability.Int + 15));
            }
            // TODO: achievement bonuses to max mp are not applied - neither the base ones nor
            // the ones of the transformed shape

            if (Simple.Level > 100u)
                Ability.MaxMp += (short)(10 * (Simple.Level - 100));

            if (Ability.MaxMp < Simple.Mp)
                Simple.Mp = Ability.MaxMp;

            Ability.MpRegen += (short)(Ability.Int / 4);
            CalcWeight();
        }

        private void _CalcAbility()
        {
            short pStrRate;
            short pDexRate;
            // TODO вынести ability классов в отдельную настройку
            if (Simple.Class == PcClassEnum.Fighter)
            {
                Ability.DHit += (short)((Ability.Str - 15) / 2 + Simple.Level / 6 + 1);
                Ability.RHit += (short)(Ability.Dex - 9);
                Ability.MHit += (short)(Ability.Int - 9);
                Ability.DDD += (short)((Ability.Str - 15) / 3 + 1);
                Ability.RDD += (short)(Ability.Dex / 10);
                Ability.MDD += (short)(Ability.Int / 10 + 1);
                Ability.PvPMHIT = Ability.MHit;
                pStrRate = 3;
                pDexRate = 15;

            }
            else if (Simple.Class == PcClassEnum.Dragoon)
            {
                Ability.DHit += (short)((Ability.Str - 10) / 2 + Simple.Level / 6 + 1);
                Ability.RHit += (short)(Ability.Dex - 15 + 1);
                Ability.MHit += (short)(Ability.Int - 10 + 1);
                Ability.DDD += (short)((Ability.Str) / 10 + 1);
                Ability.RDD += (short)((Ability.Dex - 15) / 3);
                Ability.MDD += (short)(Ability.Int / 10 + 1);
                Ability.PvPMHIT = Ability.MHit;
                pStrRate = 3;
                pDexRate = 15;

                var def = (short)((Ability.Dex - 15) / 3);
                Ability.DDv = def;
                Ability.RDv = def;
                Ability.MDv = def;
            }
            else if (Simple.Class == PcClassEnum.Wizard)
            {
                Ability.DHit += (short)((Ability.Str - 13) / 2 + Simple.Level / 6 + 1);
                Ability.RHit += (short)(Ability.Dex - 10 + 1);
                Ability.MHit += (short)(Ability.Int - 12 + 1);
                Ability.DDD += (short)((Ability.Str - 13) / 3 + 1);
                Ability.RDD += (short)(Ability.Dex / 10);
                Ability.MDD += (short)(Ability.Int / 3);
                Ability.PvPMHIT = Ability.MHit;
                pStrRate = 5;
                pDexRate = 15;
            }
            else if (Simple.Class == PcClassEnum.Assassin)
            {
                var hit = (short)((Ability.Str - 12) / 2 + Simple.Level / 6 + 1);
                Ability.DHit += hit;
                Ability.RHit += (short)(Ability.Dex - 9);
                Ability.MHit += hit;
                Ability.DDD += (short)((Ability.Str - 13) / 3 + 1);
                Ability.RDD += (short)(Ability.Dex / 10);
                Ability.MDD += (short)(Ability.Int / 3);
                Ability.PvPMHIT = Ability.MHit;
                pStrRate = 5;
                pDexRate = 3;

                var def = (short)((Ability.Dex - 13) / 4);
                Ability.DDv = def;
                Ability.RDv = def;
                Ability.MDv = def;
            }
            else
            {
                Ability.DHit += (short)((Ability.Str - 12) / 2 + Simple.Level / 6 + 1);
                Ability.RHit += (short)(Ability.Dex - 11);
                Ability.MHit += (short)(Ability.Int + Ability.Str - 12 - 10);
                Ability.DDD += (short)((Ability.Str - 12) / 3 + 1);
                Ability.RDD += (short)((Ability.Dex - 12) / 3 + 1);
                Ability.MDD += (short)(Ability.Int / 3);
                Ability.PvPMHIT = (short)((Ability.Str - 12) + (Ability.Int - 11) / 5 + 1);
                pStrRate = 5;
                pDexRate = 3;

                var def = (short)((Ability.Dex - 13) / 4);
                Ability.DDv = def;
                Ability.RDv = def;
                Ability.MDv = def;
            }
            CalcPvPHitRate(pStrRate, pDexRate);
            if (Simple.StomachStatus == StomachStatusEnum.Full)
            {
                ++Ability.DDD;
                ++Ability.RDD;
                ++Ability.MDD;
            }
            else if (Simple.StomachStatus == 0)
            {
                --Ability.DDD;
                --Ability.RDD;
                --Ability.MDD;
            }
        }

        public void CalcWeight()
        {
            var maxWeight = 0;
            if (Simple.Class == PcClassEnum.Fighter)
                maxWeight = AddWeight + 30 * (Ability.Str + 100) + Simple.Level * 25;
            else if (Simple.Class == PcClassEnum.Dragoon)
                maxWeight = AddWeight + 30 * (Ability.Str + 100) + Simple.Level * 15;
            else if (Simple.Class == PcClassEnum.Wizard)
                maxWeight = AddWeight + 30 * (Ability.Str + 100) + Simple.Level * 20;
            else if (Simple.Class == PcClassEnum.Assassin)
                maxWeight = AddWeight + 30 * (Ability.Str + 100) + Simple.Level * 20;
            else
                maxWeight = AddWeight + 30 * (Ability.Str + 100) + Simple.Level * 15;

            if (Simple.Level > 100 )
                maxWeight += 400 * (Simple.Level - 100);

            // TODO: the achievement bonus of the transformed shape does not raise the weight cap yet

            Inventory.SetMaxWeight(maxWeight);
        }

        public void CalcMaxHp(int addHpByItem)
        {
            if (Simple.Class == PcClassEnum.Fighter)
            {
                Ability.MaxHp = (short)(addHpByItem + AddHp + 3 * Ability.Str + 8 * (Simple.Level + 5));
            }
            else if (Simple.Class == PcClassEnum.Dragoon)
            {
                var v7 = Ability.Str + 2 * Simple.Level;
                var v8 = 2 * v7;
                Ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            else if (Simple.Class == PcClassEnum.Wizard)
            {
                var v7 = 3 * Ability.Str;
                var v8 = 7 * Simple.Level;
                Ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            else if (Simple.Class == PcClassEnum.Assassin)
            {
                var v7 = 3 * Ability.Str;
                var v8 = 7 * Simple.Level;
                Ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            else
            {
                var v7 = 3 * Ability.Str;
                var v8 = 7 * Simple.Level;
                Ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            // TODO: extras on top of max hp are not applied - a server-wide event bonus,
            // instant modifiers and the growth past the level cap
        }

        public void CalcPvPHitRate(short strRate, short dexRate)
        {
            var strHitRate = Simple.Level / strRate;
            if (strHitRate <= 15)
            {
                Ability.PvPDHIT = Ability.DHit;
                Ability.PvPRHIT = Ability.RHit;
            }
            else
            {
                Ability.PvPDHIT = (short)(Ability.DHit - strHitRate + 15);
                var dexHitRate = Simple.Level / dexRate;
                if (dexHitRate <= 15)
                    Ability.PvPRHIT = Ability.RHit;
                else
                    Ability.PvPRHIT = (short)(Ability.RHit - dexHitRate + 15);
            }
        }
    }
}

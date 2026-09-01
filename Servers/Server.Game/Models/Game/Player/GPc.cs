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

        #region Equipment
        /// <summary>
        ///     Check a request to put an item on and build the change it makes. Nothing of the
        ///     character moves here: the answer carries the whole list of worn items the character
        ///     is going to wear, and it starts wearing it only when ApplyEquipChange is called with
        ///     that answer. The operation is split that way on purpose - between the check and the
        ///     change the caller sees everything the change touches and is free to store it
        ///     wherever it has to be stored, and to drop the plan and leave the character untouched
        ///     when it cannot. The model itself knows nothing of sessions, packets or a database.
        ///     Which slot the item goes to is decided here, by the type of the item; the slot a
        ///     request names is not asked for at all
        /// </summary>
        /// <param name="serialNo">Serial number of the item of the inventory to put on</param>
        public GEquipChange EquipItem(ulong serialNo)
        {
            // A dead character keeps what it died in. Death is told by DeadTime and not by the hit
            // points: a character that has already asked to rise stands with its points restored
            // and is still dead until the answer comes
            if (DeadTime != null)
            {
                return GEquipChange.Refused(ErrorEnum.CharAlreadyDie);
            }

            // TODO: a paralyzed or stunned character refuses to change its equipment with its own
            // error code - waits for abnormal states

            // The worn items are read once into a snapshot: everything below decides over the
            // state the operation started with - the list is never changed in place
            List<GPcEquip> worn = Equip;

            GItem item = Inventory.Items.FirstOrDefault(x => x.SerialNumber == serialNo);
            if (item == null)
            {
                // TODO: the original looks for the item in the inventory of a servant as well -
                // waits for the servant
                return GEquipChange.Refused(ErrorEnum.ItemNotExist);
            }

            // TODO: the rest of what an item is refused for is not read anywhere yet - a seizure,
            // a ban of the region of the map, an expired term, the owner of the item, a broken or
            // sealed slot, a curse that does not let the item of an occupied slot go

            if (worn.Any(x => x.SerialNo == serialNo))
            {
                // The closest code we carry: the original tells "already worn" apart from the rest
                return GEquipChange.Refused(ErrorEnum.ItemNotUseStateOrPos);
            }

            ItemEquipTypeEnum pos = item.EquipType;
            if (pos == ItemEquipTypeEnum.NotEquipped)
            {
                return GEquipChange.Refused(ErrorEnum.ItemNotEquipSlot);
            }

            // Level and class of the row of the item. The original asks the item itself whether
            // this character may wear it and hands it the class and the level; ours are the only
            // two conditions a row carries
            if (item.UseLevel > Simple.Level || !IsClassAllowed(item.UseClass))
            {
                return GEquipChange.Refused(ErrorEnum.CantEquipSlot);
            }

            if (item.Type == ItemTypeEnum.Arrow)
            {
                // Arrows are worn in the hand of the shield and hang on the bow of the other hand:
                // with no bow there is nothing to shoot them off (the original also wants the
                // grade of the arrows to match the grade of the bow, and the column that carries
                // that grade is not established)
                GPcEquip weapon = worn.FirstOrDefault(x => x.Pos == ItemEquipTypeEnum.Weapon);
                if (weapon == null || !weapon.Item.IsRangeWeapon)
                {
                    return GEquipChange.Refused(ErrorEnum.ItemCantFindBow);
                }
            }

            // A spear takes both hands: it does not go on over a shield, and a shield does not go
            // on over it. Arrows share the hand of the shield and are not a shield
            if (item.Type == ItemTypeEnum.Spear
                && IsWorn(worn, ItemEquipTypeEnum.Shield, ItemTypeEnum.Shield))
            {
                return GEquipChange.Refused(ErrorEnum.ItemCantEquipSpear);
            }

            if (item.Type == ItemTypeEnum.Shield
                && IsWorn(worn, ItemEquipTypeEnum.Weapon, ItemTypeEnum.Spear))
            {
                return GEquipChange.Refused(ErrorEnum.ItemCantEquipShield);
            }

            // A ring goes to the free one of the two slots of rings, the first one first; with
            // both slots taken the item of the first one is swapped (which of the two the
            // original picks is not restored)
            if (pos == ItemEquipTypeEnum.Ring1
                && IsWorn(worn, ItemEquipTypeEnum.Ring1)
                && !IsWorn(worn, ItemEquipTypeEnum.Ring2))
            {
                pos = ItemEquipTypeEnum.Ring2;
            }

            // TODO: the rings of riding, of which the original wears one over the two slots at a
            // time, are not told apart - the mechanic they belong to is not here

            List<GPcEquip> taken = new List<GPcEquip>();
            GPcEquip current = worn.FirstOrDefault(x => x.Pos == pos);
            if (current != null)
            {
                // An occupied slot is swapped inside one operation: the item of the slot leaves
                // first - a bow taking the arrows with it - and the new one goes in after it, so
                // the caller hears of both halves of the swap at once
                CollectTaken(worn, current, taken);
            }

            GPcEquip equipped = CreateEquip(item, pos);

            List<GPcEquip> next = new List<GPcEquip>(worn.Count + 1);
            foreach (GPcEquip equip in worn)
            {
                if (!taken.Contains(equip))
                {
                    next.Add(equip);
                }
            }

            next.Add(equipped);

            return GEquipChange.Done(next, taken, equipped);
        }

        /// <summary>
        ///     Check a request to take the item of a slot off and build the change it makes. Like
        ///     the request to put an item on, this one only builds the plan - see EquipItem for the
        ///     contract between the check and the change
        /// </summary>
        /// <param name="pos">Slot to empty</param>
        public GEquipChange UnEquipItem(ItemEquipTypeEnum pos)
        {
            if (pos < ItemEquipTypeEnum.Weapon || pos > ItemEquipTypeEnum.Servant)
            {
                return GEquipChange.Refused(ErrorEnum.PosInvalid);
            }

            List<GPcEquip> worn = Equip;

            GPcEquip current = worn.FirstOrDefault(x => x.Pos == pos);
            if (current == null)
            {
                return GEquipChange.Refused(ErrorEnum.ItemNotEquipSlot);
            }

            if (DeadTime != null)
            {
                return GEquipChange.Refused(ErrorEnum.CharAlreadyDie);
            }

            // TODO: a paralyzed or stunned character keeps its equipment on, and a cursed item is
            // never taken off - waits for abnormal states and for a source of a curse

            List<GPcEquip> taken = new List<GPcEquip>();
            CollectTaken(worn, current, taken);

            List<GPcEquip> next = new List<GPcEquip>(worn.Count);
            foreach (GPcEquip equip in worn)
            {
                if (!taken.Contains(equip))
                {
                    next.Add(equip);
                }
            }

            return GEquipChange.Done(next, taken, null);
        }

        /// <summary>
        ///     Start wearing what a plan of a change names. The list of worn items is published
        ///     whole: a new list replaces the old one by a single write of the reference, so a
        ///     reader on another thread walks either the equipment before the change or the one
        ///     after it and never a list being rebuilt under it. Everything the change moves -
        ///     the characteristics, the weapon in the hand, the reach and the speeds - is rebuilt
        ///     right after that by the calculation of the characteristics.
        ///     A plan is applied once and by the thread that built it: a plan built before another
        ///     one was applied carries a list that no longer holds
        /// </summary>
        /// <param name="change">Plan built by EquipItem or UnEquipItem</param>
        public bool ApplyEquipChange(GEquipChange change)
        {
            if (change == null || !change.IsSuccess)
            {
                return false;
            }

            // The plan built this list from scratch and gave it to nobody else, so it is
            // published as it is - a copy would only make the plan and the model diverge by
            // reference for anyone comparing them
            Equip = (List<GPcEquip>)change.Worn;
            CalcAbility();

            return true;
        }

        /// <summary>
        ///     Records that leave the character when a given one is taken off: the record itself
        ///     and, when a bow leaves the hand of the weapon, the arrows worn in the other hand -
        ///     there is nothing left for them to hang on, and the caller has to hear of both
        /// </summary>
        /// <param name="worn">Snapshot of the worn items the operation started with</param>
        /// <param name="equip">Record being taken off</param>
        /// <param name="taken">List the records that leave are collected into</param>
        private static void CollectTaken(List<GPcEquip> worn, GPcEquip equip, List<GPcEquip> taken)
        {
            taken.Add(equip);

            if (equip.Pos != ItemEquipTypeEnum.Weapon || !equip.Item.IsRangeWeapon)
            {
                return;
            }

            GPcEquip arrows = worn.FirstOrDefault(x =>
                x.Pos == ItemEquipTypeEnum.Shield && x.Item.Type == ItemTypeEnum.Arrow);
            if (arrows != null)
            {
                taken.Add(arrows);
            }
        }

        /// <summary>
        ///     Record of a worn item, built whole: the slot is named here, once and for the whole
        ///     life of the record
        /// </summary>
        /// <param name="item">Item of the inventory the character puts on</param>
        /// <param name="pos">Slot the item goes to</param>
        private static GPcEquip CreateEquip(GItem item, ItemEquipTypeEnum pos)
        {
            return new GPcEquip
            {
                Item = item,
                SerialNo = item.SerialNumber,
                Pos = pos,
                Status = item.Status,
                IsConfirm = item.IsConfirm ? 1 : 0,
                IsEquip = true,
                IsSeal = false
            };
        }

        /// <summary>
        ///     Whether a slot of the snapshot is taken
        /// </summary>
        /// <param name="worn">Snapshot of the worn items</param>
        /// <param name="pos">Slot to look at</param>
        private static bool IsWorn(List<GPcEquip> worn, ItemEquipTypeEnum pos)
        {
            return worn.Any(x => x.Pos == pos);
        }

        /// <summary>
        ///     Whether a slot of the snapshot is taken by an item of a given type
        /// </summary>
        /// <param name="worn">Snapshot of the worn items</param>
        /// <param name="pos">Slot to look at</param>
        /// <param name="type">Item type the slot has to hold</param>
        private static bool IsWorn(List<GPcEquip> worn, ItemEquipTypeEnum pos, ItemTypeEnum type)
        {
            return worn.Any(x => x.Pos == pos && x.Item.Type == type);
        }

        /// <summary>
        ///     Whether the class of the character is among the ones the row of the item is written
        ///     for. The column is a mask of one bit per class, in the order the classes are
        ///     numbered, and a row that names no class at all restricts nothing
        /// </summary>
        /// <param name="useClass">Class mask of the row of the item</param>
        private bool IsClassAllowed(UseClassEnum useClass)
        {
            if (useClass == 0)
            {
                return true;
            }

            return ((int)useClass & (1 << (int)Simple.Class)) != 0;
        }
        #endregion

        /// <summary>
        ///     Serializes the writers of the recalc: a level up runs it on the swing thread and a
        ///     change of the equipment on the network thread of the session, and two interleaved
        ///     recalcs could publish the weapon of one and the abilities of the other. Readers do
        ///     not take it - they live on the published references
        /// </summary>
        private readonly object _recalcLock = new object();

        public new void CalcAbility()
        {
            lock (_recalcLock)
            {
                CalcAbilityLocked();
            }
        }

        private void CalcAbilityLocked()
        {
            // The characteristics are built into a set of their own and published at the very end
            // by one write of the reference: a swing, the ai of a monster or the regeneration that
            // reads them from another thread gets either the numbers of before the change or the
            // ones of after it and never a half-built set.
            // Everything below is summed over a cleared ability: the bonuses of the worn items go
            // in with +=, so a second call - a level up, a change of the equipment - has to give
            // the same numbers as the first one instead of doubling them
            GPcAbility ability = new GPcAbility();
            ability.Reset();

            ability.Str = Simple.CalcStr();
            ability.Dex = Simple.CalcDex();
            ability.Int = Simple.CalcInt();
            ability.DDv = 1;
            ability.MDv = 1;
            ability.RDv = 1;

            // Base regeneration of the class, exactly as GChar.CalcAbility takes it: AddRegenHp and
            // AddRegenMp are filled by _SetDefaultInfo from the parm of the class. The assignment
            // goes before the equipment, the worn items add their own regeneration to this base
            ability.HpRegen = AddRegenHp;
            ability.MpRegen = AddRegenMp;
            ability.HwHpRegen = AddHwRegenHp;
            ability.HwMpRegen = AddHwRegenMp;

            ability.AddDDWhenCritical += AddDDWhenCritical;
            ability.SubDDWhenCritical += SubDDWhenCritical;
            ability.EnemySubCriticalHit += EnemySubCriticalHit;

            var addHpByItem = 0;
            var addMpByItem = 0;
            var addAttackRateByItem = 0;
            var addMoveRateByItem = 0;
            var addWeightByItem = 0;

            // Snapshot of the worn items, taken once for the whole calculation: an equip operation
            // replaces the list whole, and reading it twice could read one half of the equipment
            // off the list of before the change and the other half off the list of after it
            List<GPcEquip> worn = Equip;
            foreach (var equip in worn)
            {
                var item = equip.Item;

                ability.DDv += item.DDv;
                ability.MDv += item.MDv;
                ability.RDv += item.RDv;
                ability.DPv += item.DPv;
                ability.MPv += item.MPv;
                ability.RPv += item.RPv;
                ability.HidDDv += item.HDDv;
                ability.HidMDv += item.HMDv;
                ability.HidRDv += item.HRDv;
                ability.HidDPv += item.HDPv;
                ability.HidMPv += item.HMPv;
                ability.HidRPv += item.HRPv;
                ability.CriticalHit += item.Critical;
                ability.AddDDWhenCritical += item.AddDDWhenCritical;
                ability.SubDDWhenCritical += item.SubDDWhenCritical;
                ability.EnemySubCriticalHit += item.EnemySubCriticalHit;
                ability.DHit += item.DHit;
                ability.RHit += item.RHit;
                ability.MHit += item.MHit;
                ability.Str += item.Str;
                ability.Dex += item.Dex;
                ability.Int += item.Int;
                ability.HpRegen += item.HpRegen;
                ability.MpRegen += item.MpRegen;
                addHpByItem += item.HpPlus;
                addMpByItem += item.Mpplus;
                addWeightByItem += item.AddWeight;

                // The rate of attack is the pause between two swings, so what a worn item carries
                // there is taken away from the rate of the character - a faster weapon shortens
                // the pause - while the speeds of movement are simply added up. Both are handed to
                // the calculation of the speeds below, which is the only place that reads them
                addAttackRateByItem += item.AttackRate;
                addMoveRateByItem += item.MoveRate;

                // Damage of the character off the equipment: the flat part of the dice of every
                // worn item goes into the damage of its own way. The item that is itself the weapon
                // of that way is left out - a weapon in the hand puts its flat part into the roll
                // it makes on every swing, and taking it here as well would count it twice
                // TODO a blessed melee weapon gives one point of critical on top of that; the
                // status of a worn item is read nowhere yet
                if (!item.IsMeleeWeapon)
                {
                    ability.DDD += (short)item.DDdDice.Plus;
                }

                if (!item.IsRangeWeapon)
                {
                    ability.RDD += (short)item.RDdDice.Plus;
                }

                if (!item.IsMagicWeapon)
                {
                    ability.MDD += (short)item.MDdDice.Plus;
                }

                // TODO: the rest of what a worn item gives is not applied yet - equip
                // penalties, bonuses against a race, wards, elemental attack and resistance,
                // inflicting and resisting abnormal states. Each waits for its mechanic
            }

            // The item in the weapon slot is what the character swings with: its dice, its accuracy
            // and the way a swing goes through are read off WeaponEquip on every hit. An empty slot
            // clears the field and the swing falls back to the default weapon of the class. The
            // slot is the one of the record of the equipment - the record knows where it is worn,
            // the item knows nothing about it
            var weaponEquip = worn.FirstOrDefault(x => x.Pos == ItemEquipTypeEnum.Weapon);

            // TODO: an abnormal state active on the character re-applies its ability change here
            ability.DDv += AddDDV;
            ability.MDv += AddMDV;
            ability.RDv += AddRDV;
            ability.DPv += AddDPV;
            ability.MPv += AddMPV;
            ability.RPv += AddRPV;
            ability.DHit += AddHit;
            ability.RHit += AddRHit;
            ability.MHit += AddMHit;
            ability.DDD += (short)(AddDD + InstAddDDD);
            ability.RDD += AddRDD;
            ability.MDD += AddMDD;

            _CalcAbility(ability);

            ability.HidDDv += (short)(AddHidDDv + 5);
            ability.HidMDv += (short)(AddHidMDv + 5);
            ability.HidRDv += (short)(AddHidRDv + 5);
            ability.HidDPv += AddHidDPv;
            ability.HidMPv += AddHidMPv;
            ability.HidRPv += AddHidRPv;
            ability.DDv += ability.HidDDv;
            ability.MDv += ability.HidMDv;
            ability.RDv += ability.HidRDv;
            ability.DPv += ability.HidDPv;
            ability.MPv += ability.HidMPv;
            ability.RPv += ability.HidRPv;
            ability.CriticalHit += (short)(AddCriticalHit + ability.Dex / 10);
            // TODO: achievement bonuses to max hp are not applied - neither the base ones nor
            // the ones of the transformed shape
            if (ParmMon != ParmMonCur)
            {
                addHpByItem += AddTransformMaxHP;
            }

            CalcMaxHp(ability, addHpByItem);
            // Hit points above the new cap are cut down to it. The cut is written from the thread
            // that recalculates the character, and it races with the damage dealt to it on another
            // one exactly the way the regeneration already does - the same race, accepted the same
            // way
            if (ability.MaxHp < Simple.Hp)
            {
                Simple.Hp = ability.MaxHp;
            }

            if (Simple.Class == PcClassEnum.Wizard)
            {
                var addTransMaxMp = 0;
                if (ParmMon != ParmMonCur)
                {
                    addTransMaxMp += AddTransformMaxMP;
                }

                ability.MaxMp = (short)(addMpByItem + addTransMaxMp + AddMp + 2 * (ability.Int + Simple.Level + 15));
            }
            else
            {
                var addTransMaxMp = 0;
                if (ParmMon != ParmMonCur)
                {
                    addTransMaxMp += AddTransformMaxMP;
                }

                ability.MaxMp = (short)(addMpByItem + addTransMaxMp + Simple.Level + AddMp + 2 * (ability.Int + 15));
            }
            // TODO: achievement bonuses to max mp are not applied - neither the base ones nor
            // the ones of the transformed shape

            if (Simple.Level > 100u)
                ability.MaxMp += (short)(10 * (Simple.Level - 100));

            if (ability.MaxMp < Simple.Mp)
                Simple.Mp = ability.MaxMp;

            ability.MpRegen += (short)(ability.Int / 4);

            // The set is whole: publish it, and behind it everything a swing reads next to it -
            // the item in the hand and the properties it swings with, both of them new objects.
            // Every published object is internally consistent, but the fields are separate
            // writes: a swing that falls into the middle may pair the new abilities with the old
            // weapon for that one hit - the accepted window of the recalc, gone by the next swing
            Ability = ability;

            AddAttackRateByItem = (short)addAttackRateByItem;
            AddMoveRateByItem = (short)addMoveRateByItem;
            AddWeightByItem = (short)addWeightByItem;

            Weapon = weaponEquip?.Item;
            WeaponEquip = weaponEquip?.Item.CreateWeapon();

            // The reach and the speeds are built off the item in the hand and off the rest of the
            // worn items, so they go after the weapon is published: the next swing after a change
            // of the weapon already measures the new distance and waits the new pause
            CalcDistAttack();
            CalcSpeed();
            CalcWeight(ability);
        }

        private void _CalcAbility(GPcAbility ability)
        {
            short pStrRate;
            short pDexRate;
            // TODO вынести ability классов в отдельную настройку
            if (Simple.Class == PcClassEnum.Fighter)
            {
                ability.DHit += (short)((ability.Str - 15) / 2 + Simple.Level / 6 + 1);
                ability.RHit += (short)(ability.Dex - 9);
                ability.MHit += (short)(ability.Int - 9);
                ability.DDD += (short)((ability.Str - 15) / 3 + 1);
                ability.RDD += (short)(ability.Dex / 10);
                ability.MDD += (short)(ability.Int / 10 + 1);
                ability.PvPMHIT = ability.MHit;
                pStrRate = 3;
                pDexRate = 15;

            }
            else if (Simple.Class == PcClassEnum.Dragoon)
            {
                ability.DHit += (short)((ability.Str - 10) / 2 + Simple.Level / 6 + 1);
                ability.RHit += (short)(ability.Dex - 15 + 1);
                ability.MHit += (short)(ability.Int - 10 + 1);
                ability.DDD += (short)((ability.Str) / 10 + 1);
                ability.RDD += (short)((ability.Dex - 15) / 3);
                ability.MDD += (short)(ability.Int / 10 + 1);
                ability.PvPMHIT = ability.MHit;
                pStrRate = 3;
                pDexRate = 15;

                var def = (short)((ability.Dex - 15) / 3);
                ability.DDv = def;
                ability.RDv = def;
                ability.MDv = def;
            }
            else if (Simple.Class == PcClassEnum.Wizard)
            {
                ability.DHit += (short)((ability.Str - 13) / 2 + Simple.Level / 6 + 1);
                ability.RHit += (short)(ability.Dex - 10 + 1);
                ability.MHit += (short)(ability.Int - 12 + 1);
                ability.DDD += (short)((ability.Str - 13) / 3 + 1);
                ability.RDD += (short)(ability.Dex / 10);
                ability.MDD += (short)(ability.Int / 3);
                ability.PvPMHIT = ability.MHit;
                pStrRate = 5;
                pDexRate = 15;
            }
            else if (Simple.Class == PcClassEnum.Assassin)
            {
                var hit = (short)((ability.Str - 12) / 2 + Simple.Level / 6 + 1);
                ability.DHit += hit;
                ability.RHit += (short)(ability.Dex - 9);
                ability.MHit += hit;
                ability.DDD += (short)((ability.Str - 13) / 3 + 1);
                ability.RDD += (short)(ability.Dex / 10);
                ability.MDD += (short)(ability.Int / 3);
                ability.PvPMHIT = ability.MHit;
                pStrRate = 5;
                pDexRate = 3;

                var def = (short)((ability.Dex - 13) / 4);
                ability.DDv = def;
                ability.RDv = def;
                ability.MDv = def;
            }
            else
            {
                ability.DHit += (short)((ability.Str - 12) / 2 + Simple.Level / 6 + 1);
                ability.RHit += (short)(ability.Dex - 11);
                ability.MHit += (short)(ability.Int + ability.Str - 12 - 10);
                ability.DDD += (short)((ability.Str - 12) / 3 + 1);
                ability.RDD += (short)((ability.Dex - 12) / 3 + 1);
                ability.MDD += (short)(ability.Int / 3);
                ability.PvPMHIT = (short)((ability.Str - 12) + (ability.Int - 11) / 5 + 1);
                pStrRate = 5;
                pDexRate = 3;

                var def = (short)((ability.Dex - 13) / 4);
                ability.DDv = def;
                ability.RDv = def;
                ability.MDv = def;
            }
            CalcPvPHitRate(ability, pStrRate, pDexRate);
            if (Simple.StomachStatus == StomachStatusEnum.Full)
            {
                ++ability.DDD;
                ++ability.RDD;
                ++ability.MDD;
            }
            else if (Simple.StomachStatus == 0)
            {
                --ability.DDD;
                --ability.RDD;
                --ability.MDD;
            }
        }

        public void CalcWeight(GPcAbility ability)
        {
            // What the worn items add to the cap comes in beside the addition of the character
            // itself, and it is written whole on every calculation - a bag or a belt raises the
            // cap while it is worn and stops raising it as soon as it comes off
            var addWeight = AddWeight + AddWeightByItem;

            var maxWeight = 0;
            if (Simple.Class == PcClassEnum.Fighter)
                maxWeight = addWeight + 30 * (ability.Str + 100) + Simple.Level * 25;
            else if (Simple.Class == PcClassEnum.Dragoon)
                maxWeight = addWeight + 30 * (ability.Str + 100) + Simple.Level * 15;
            else if (Simple.Class == PcClassEnum.Wizard)
                maxWeight = addWeight + 30 * (ability.Str + 100) + Simple.Level * 20;
            else if (Simple.Class == PcClassEnum.Assassin)
                maxWeight = addWeight + 30 * (ability.Str + 100) + Simple.Level * 20;
            else
                maxWeight = addWeight + 30 * (ability.Str + 100) + Simple.Level * 15;

            if (Simple.Level > 100 )
                maxWeight += 400 * (Simple.Level - 100);

            // TODO: the achievement bonus of the transformed shape does not raise the weight cap yet

            Inventory.SetMaxWeight(maxWeight);
        }

        public void CalcMaxHp(GPcAbility ability, int addHpByItem)
        {
            if (Simple.Class == PcClassEnum.Fighter)
            {
                ability.MaxHp = (short)(addHpByItem + AddHp + 3 * ability.Str + 8 * (Simple.Level + 5));
            }
            else if (Simple.Class == PcClassEnum.Dragoon)
            {
                var v7 = ability.Str + 2 * Simple.Level;
                var v8 = 2 * v7;
                ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            else if (Simple.Class == PcClassEnum.Wizard)
            {
                var v7 = 3 * ability.Str;
                var v8 = 7 * Simple.Level;
                ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            else if (Simple.Class == PcClassEnum.Assassin)
            {
                var v7 = 3 * ability.Str;
                var v8 = 7 * Simple.Level;
                ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            else
            {
                var v7 = 3 * ability.Str;
                var v8 = 7 * Simple.Level;
                ability.MaxHp = (short)(addHpByItem + AddHp + v7 + v8 + 40);
            }
            // TODO: extras on top of max hp are not applied - a server-wide event bonus,
            // instant modifiers and the growth past the level cap
        }

        public void CalcPvPHitRate(GPcAbility ability, short strRate, short dexRate)
        {
            var strHitRate = Simple.Level / strRate;
            if (strHitRate <= 15)
            {
                ability.PvPDHIT = ability.DHit;
                ability.PvPRHIT = ability.RHit;
            }
            else
            {
                ability.PvPDHIT = (short)(ability.DHit - strHitRate + 15);
                var dexHitRate = Simple.Level / dexRate;
                if (dexHitRate <= 15)
                    ability.PvPRHIT = ability.RHit;
                else
                    ability.PvPRHIT = (short)(ability.RHit - dexHitRate + 15);
            }
        }
    }
}

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

        /// <summary>
        ///     Keeper the character is talking to, __mTalkingNpc of the original. It is written the
        ///     moment a request of a script finds the keeper close enough, and closing the window
        ///     does not clear it: the original drops it only when the character is carried across
        ///     the world or leaves it, so every request of one conversation finds the keeper here.
        ///     Whoever reads it has to be ready for a keeper that is no longer in the world - the
        ///     reference outlives the entity and nothing here is told when the entity is gone.
        ///     TODO: the original clears it on a teleport as well; there is no teleport to clear it
        ///     from yet, and a character that logs in again is built anew anyway
        /// </summary>
        public GMonster TalkingNpc { get; set; }
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
        ///     request names is not asked for at all.
        ///     The checks are walked in the order of the original: the player is told the first
        ///     reason his request failed for and none of the ones behind it
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

            // Class and level of the row of the item, asked in that order: the original hands both
            // to the item itself and lets it tell whether this character may wear it, and ours are
            // the only two conditions a row carries
            if (!IsClassAllowed(item.UseClass))
            {
                return GEquipChange.Refused(ErrorEnum.ItemCantEquipLimitClass);
            }

            if (!IsLevelAllowed(item.UseLevel))
            {
                return GEquipChange.Refused(ErrorEnum.ItemCantEquipLimitLevel);
            }

            if (worn.Any(x => x.SerialNo == serialNo))
            {
                return GEquipChange.Refused(ErrorEnum.ItemEquipped);
            }

            ItemEquipTypeEnum pos = item.EquipType;
            if (pos == ItemEquipTypeEnum.NotEquipped)
            {
                return GEquipChange.Refused(ErrorEnum.ItemNotEquipSlot);
            }

            if (item.Type == ItemTypeEnum.Arrow)
            {
                // Arrows are worn in the hand of the shield and hang on the bow of the other
                // hand: with no bow there is nothing to shoot them off. The original also wants
                // the grade of the arrows to match the grade of the bow, and that grade is zero on
                // every bow and every kind of arrows of the parm - the check always passes there,
                // and what tells a kind of arrows from another is the class of the row, which is
                // asked above
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
            // A slot that is no slot of the equipment at all: the original reads such a request as
            // a broken one, writes it into its log and answers nothing at all
            if (pos < ItemEquipTypeEnum.Weapon || pos > ItemEquipTypeEnum.Servant)
            {
                return GEquipChange.Refused(ErrorEnum.PosInvalid);
            }

            List<GPcEquip> worn = Equip;

            GPcEquip current = worn.FirstOrDefault(x => x.Pos == pos);
            if (current == null)
            {
                return GEquipChange.Refused(ErrorEnum.ItemNotEquip);
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

        /// <summary>
        ///     Whether the level of the character is the one the row of the item asks for. The
        ///     column carries the requirement with a sign: a positive one asks for a level not
        ///     below it, a negative one - the rows written for the low levels - for a level not
        ///     above its modulus, and a row that asks for nothing carries a zero
        /// </summary>
        /// <param name="useLevel">Level requirement of the row of the item</param>
        private bool IsLevelAllowed(short useLevel)
        {
            if (useLevel == 0)
            {
                return true;
            }

            return useLevel > 0 ? Simple.Level >= useLevel : Simple.Level <= -useLevel;
        }
        #endregion

        #region Inventory
        /// <summary>
        ///     Serializes the writers of the bag. A plan of a change is built over a snapshot of
        ///     the rows and holds only until another plan is applied, and the caller writes the row
        ///     of the item into the database in between - so the whole sequence "build the plan,
        ///     store it, apply it" belongs to one writer at a time, the call of the database
        ///     included. Two writers reach one bag: the requests of the player on the network
        ///     thread of its session and the loot of a killed monster on the thread of the swings,
        ///     and a pair of them that overlapped would publish the list of one over the list of
        ///     the other and lose one of the two operations without a word.
        ///     A change of the equipment is written under it as well: it is built over a row of the
        ///     bag and moves the very rows of the slots a drop of a worn item is refused by, so an
        ///     item put on while the same item is being thrown away has to wait for the other
        ///     operation to end - see EquipHandler.
        ///     Readers do not take it - they live on the published reference of the list.
        ///     Order the locks of the character are taken in: this one first and the lock of the
        ///     recalc of the characteristics (_recalcLock) or the lock of the identification service
        ///     inside it, never the other way round. Both nestings already happen - a change of the
        ///     equipment ends in the recalc under this lock (ApplyEquipChange), and a pick-up takes
        ///     the thing out of the world under it (InventarHandler.ItemPickUp) - and the recalc
        ///     itself writes the weight of the bag, so it takes this lock around its own one and
        ///     never underneath it
        /// </summary>
        public readonly object InventoryLock = new object();

        /// <summary>
        ///     Check a request to take an item of the ground into the bag and build the change it
        ///     makes. Nothing of the character moves here: the answer carries the whole list of
        ///     rows the bag is going to hold, and the character holds it only when
        ///     ApplyInventoryChange is called with that answer. The split between the check and the
        ///     change is the one of the equipment - see EquipItem - and it is what lets the caller
        ///     create or move the row of the item in the database in between, and drop the plan
        ///     when it cannot.
        ///     The order of the checks is the one of the original: a player has to be told the
        ///     first reason his request failed for and not any of the ones behind it
        /// </summary>
        /// <param name="groundItem">Item lying on the ground the character reaches for</param>
        /// <param name="count">How many items of it the character takes</param>
        public GInventoryChange PickUpItem(GItem groundItem, int count)
        {
            if (groundItem == null)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemNotExist);
            }

            // A dead character picks nothing up. Death is told by DeadTime and not by the hit
            // points, exactly the way EquipItem reads it
            if (DeadTime != null)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.CharAlreadyDie);
            }

            // TODO: a paralyzed or stunned character takes nothing into its bag either, each with
            // its own error code - waits for abnormal states

            // Nothing at all, and more than one of an item that lies one to a row: the original
            // refuses both before it looks at the bag
            if (count <= 0 || (count > 1 && !GPcInventory.IsStackable(groundItem)))
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemInvalidCnt);
            }

            // The rows are read once into a snapshot: everything below decides over the state the
            // operation started with - the list is never changed in place
            List<GItem> items = Inventory.Items;

            GItem stack = GPcInventory.FindStack(items, groundItem);

            // A row is taken in only when there is nothing to merge into: a merge leaves the count
            // of the rows as it is and goes through a bag that is already full. The original lifts
            // its own "bag full" the same way as soon as it finds a row to merge into - and asks
            // the weight again right after, so a merge into a full bag is still refused when the
            // character cannot carry the pile
            if (stack == null && items.Count >= GPcInventory.MaxSize)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.InvFull);
            }

            // Counted wide: a row holds up to a billion items and the sum of two rows does not fit
            // the count of one. The cap of the row is asked before the weight, the way the original
            // asks it: a pile that breaks both is refused for the stack and not for the weight
            long leftCount = (stack == null ? 0L : stack.Count) + count;
            if (leftCount > GPcInventory.MaxStackCount)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemTooManyStackCnt);
            }

            // The whole pile goes on the character at once, and its weight is counted wide for the
            // same reason
            if (Inventory.Weight + (long)count * groundItem.Weight > Inventory.MaxWeight)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemTooHeavy);
            }

            // TODO: an item the original lets a character hold one of at a time is refused right
            // here with its own code - no column of ours says which items those are

            if (stack != null)
            {
                // The bag keeps every row it held: the merge only raises the count of one of them,
                // and the very list the plan was built over is published back
                return GInventoryChange.Merged(items, stack, count, (int)leftCount);
            }

            GItem added = CopyItem(groundItem, count);

            List<GItem> next = new List<GItem>(items.Count + 1);
            next.AddRange(items);
            next.Add(added);

            return GInventoryChange.Added(next, added, count);
        }

        /// <summary>
        ///     Check a request to throw an item of the bag away and build the change it makes. Like
        ///     the request to pick an item up, this one only builds the plan - see PickUpItem for
        ///     the contract between the check and the change
        /// </summary>
        /// <param name="serialNo">Serial number of the row of the bag</param>
        /// <param name="count">How many items of the row leave it</param>
        public GInventoryChange DropItem(ulong serialNo, int count)
        {
            List<GItem> items = Inventory.Items;

            // The row is looked up by its serial number and never by the number of the item: two
            // rows of one item lie in the bag side by side as soon as anything keeps them apart
            GItem item = items.FirstOrDefault(x => x.SerialNumber == serialNo);
            if (item == null)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemNotExist);
            }

            if (DeadTime != null)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.CharAlreadyDie);
            }

            // TODO: a paralyzed or stunned character throws nothing away, and neither a seized item
            // nor a bound one leaves the bag at all - waits for abnormal states and for a source of
            // the two flags

            // A worn item is not thrown away and is not taken off by itself: the record of the
            // equipment would be left hanging on a row that is no longer in the bag. The original
            // refuses the same way and asks this before it looks at the count of the request, so a
            // request that names a worn item hears of the item and not of its count
            if (Equip.Any(x => x.SerialNo == serialNo))
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemEquipped);
            }

            if (count <= 0 || count > item.Count)
            {
                return GInventoryChange.Refused(InventoryErrorEnum.ItemLack);
            }

            GItem ground = CopyItem(item, count);
            int leftCount = item.Count - count;

            if (leftCount > 0)
            {
                // A part of the stack leaves: the row stays where it is and the bag keeps the list
                // it already holds
                return GInventoryChange.Dropped(items, item, count, leftCount, ground);
            }

            List<GItem> next = new List<GItem>(items.Count);
            foreach (GItem row in items)
            {
                if (row != item)
                {
                    next.Add(row);
                }
            }

            return GInventoryChange.Dropped(next, item, count, 0, ground);
        }

        /// <summary>
        ///     Carry what a plan of a change of the bag names. The list of rows is published whole:
        ///     a new list replaces the old one by a single write of the reference, so a reader on
        ///     another thread walks either the bag before the change or the one after it. The count
        ///     of a row is written before that - a reader that already sees the new list sees the
        ///     row whole - and the weight of the character is summed up over the published bag.
        ///     A plan is applied once and by the thread that built it: a plan built before another
        ///     one was applied carries a list that no longer holds - which is why every writer of
        ///     the bag builds, stores and applies its plan under InventoryLock
        /// </summary>
        /// <param name="change">Plan built by PickUpItem or DropItem</param>
        public bool ApplyInventoryChange(GInventoryChange change)
        {
            if (change == null || !change.IsSuccess)
            {
                return false;
            }

            // A row that leaves the bag keeps the count it had: the item that goes to the ground
            // was built off it, and nothing reads the row itself any more
            if (change.Item != null && !change.IsRemoved)
            {
                change.Item.Count = change.LeftCount;
            }

            // The plan built the list of an addition or of a removal from scratch and gave it to
            // nobody else; a merge and a partial drop publish back the very list they were built
            // over. Either way the reference is written as it is
            Inventory.Items = (List<GItem>)change.Items;
            Inventory.RecalcWeight();

            return true;
        }

        /// <summary>
        ///     Carry a plan that waits for a serial number of the database: the row a pick-up adds
        ///     has none of its own until the stored procedure creates it, and the row a pick-up
        ///     merges into has to be the very row the procedure merged the stack into.
        ///     The procedure merges by the date the term of the row runs out at, which the bag does
        ///     not keep (GPcInventory.FindStack), so the two of them tell the stacks apart by
        ///     different conditions and disagree in both directions. Neither disagreement is
        ///     refused: the database is the arbiter of every merge, and the bag is brought over to
        ///     the row the answer names.
        ///     A plan that adds a row may come back with the serial number of a row the bag already
        ///     holds - the procedure merged what the bag kept apart. Two rows of one serial number
        ///     would leave the bag telling of something the database has never had, and every
        ///     request of the player names a row by that number, so the count goes into the row
        ///     that carries it and the row of the plan is dropped.
        ///     A plan that merges may come back with another serial number than the row it merges
        ///     into carries - the procedure kept apart what the bag merged, or merged the stack
        ///     into another row of the bag. The merge of the plan is not carried out at all and the
        ///     count goes where the answer points instead.
        ///     Whichever way it went, the row the bag ends up holding is the one under the serial
        ///     number of the answer - that is the row the caller tells the client of, and it reads
        ///     it back with <see cref="FindItem"/>
        /// </summary>
        /// <param name="change">Plan built by PickUpItem</param>
        /// <param name="serialNo">Serial number the database issued to the row</param>
        public bool ApplyInventoryChange(GInventoryChange change, ulong serialNo)
        {
            if (change == null || !change.IsSuccess)
            {
                return false;
            }

            if (change.IsAdded)
            {
                GItem stack = FindSerial(change.Items, serialNo, change.Item);
                if (stack != null)
                {
                    return ApplyDatabaseMerge(change, stack);
                }

                change.Item.SerialNumber = serialNo;
            }
            else if (change.IsMerged && change.Item.SerialNumber != serialNo)
            {
                return ApplyDatabaseSerial(change, serialNo);
            }

            return ApplyInventoryChange(change);
        }

        /// <summary>
        ///     Carry a plan of an addition the database merged into a row the bag already held. The
        ///     count of the plan goes into that row and the list published is the one of the plan
        ///     without the row it added, so the bag holds as many rows as it did before.
        ///     What the plan carries is left telling of the row the count went into - the serial
        ///     number of the database and the count that row ended up with - so a reader of the
        ///     plan and the bag tell one story; the caller of the pick-up reads the row of the bag
        ///     itself by that serial number (<see cref="FindItem"/>), because the other way a plan
        ///     may end - the merge the database did not merge - leaves the plan pointing at a row
        ///     the count never went into
        /// </summary>
        /// <param name="change">Plan built by PickUpItem that takes a new row in</param>
        /// <param name="stack">Row of the bag the database merged the item into</param>
        private bool ApplyDatabaseMerge(GInventoryChange change, GItem stack)
        {
            AddToStack(stack, change.Count);

            List<GItem> next = new List<GItem>(change.Items.Count - 1);
            foreach (GItem row in change.Items)
            {
                if (row != change.Item)
                {
                    next.Add(row);
                }
            }

            change.Item.SerialNumber = stack.SerialNumber;
            change.Item.Count = stack.Count;

            Inventory.Items = next;
            Inventory.RecalcWeight();

            return true;
        }

        /// <summary>
        ///     Carry a plan of a merge the database answered another serial number to. Nothing of
        ///     the merge has happened yet - the plan only named it - so the row the plan was going
        ///     to raise is simply left with the count it holds, and the count goes where the answer
        ///     of the procedure points.
        ///     It points at a row of the bag when the procedure merged the stack into another row
        ///     than the plan picked: the bag holds two rows the plan cannot tell apart - they differ
        ///     by the date the term runs out at and by nothing the bag keeps - and the procedure
        ///     picked the other one of them. Otherwise it points at a row of its own the procedure
        ///     created, and the bag takes that row in beside the one the plan merged into: the copy
        ///     is taken off that row, because the two carry the same number of the item, the same
        ///     status, the same binding and the same term - everything a merge of the bag is made
        ///     by - and differ in the one thing the bag does not keep at all.
        ///     The bag may end up holding one row more than its cap that way: the checks of a
        ///     pick-up let a merge through a full bag, and the row the procedure created exists
        ///     whatever the cap of the bag says - a row dropped here would be a row nobody ever
        ///     gets back
        /// </summary>
        /// <param name="change">Plan built by PickUpItem that goes into a row of the bag</param>
        /// <param name="serialNo">Serial number of the row the database put the item into</param>
        private bool ApplyDatabaseSerial(GInventoryChange change, ulong serialNo)
        {
            GItem stack = FindSerial(change.Items, serialNo, change.Item);

            if (stack == null)
            {
                // The row of the procedure holds what went into it and nothing else: the count of
                // the plan is the whole count the item was stored with
                GItem added = CopyItem(change.Item, change.Count);
                added.SerialNumber = serialNo;

                List<GItem> next = new List<GItem>(change.Items.Count + 1);
                next.AddRange(change.Items);
                next.Add(added);

                Inventory.Items = next;
                Inventory.RecalcWeight();

                return true;
            }

            AddToStack(stack, change.Count);

            // A merge publishes back the very list it was built over, and this one adds no row to
            // it: the reference is written as it is, the way the plan of a merge is applied
            Inventory.Items = (List<GItem>)change.Items;
            Inventory.RecalcWeight();

            return true;
        }

        /// <summary>
        ///     Put a count into a row of the bag. Counted wide and clipped at the cap of a row: a
        ///     count that lands in a row the plan did not look at as at a stack of its own has been
        ///     asked by nobody whether the two counts fit one row
        /// </summary>
        /// <param name="stack">Row the count goes into</param>
        /// <param name="count">Count that goes in</param>
        private static void AddToStack(GItem stack, int count)
        {
            long total = (long)stack.Count + count;

            stack.Count = total > GPcInventory.MaxStackCount ? GPcInventory.MaxStackCount : (int)total;
        }

        /// <summary>
        ///     Row of the bag under a given serial number, none when the bag holds no such row. The
        ///     serial number is the one thing a row of the bag and a row of the database are held
        ///     together by, so a caller that has just stored a change of the bag reads the row it
        ///     has to tell the client of by the serial number the procedure answered with - see
        ///     ApplyInventoryChange, which puts the count wherever that number points
        /// </summary>
        /// <param name="serialNo">Serial number of the row of the database</param>
        public GItem FindItem(ulong serialNo)
        {
            return Inventory.Items.FirstOrDefault(x => x.SerialNumber == serialNo);
        }

        /// <summary>
        ///     Row of a list under a given serial number, none when no row carries it. The row a
        ///     plan is about to add is left out by its reference and never by its number: a thing
        ///     somebody else threw away lies in the world under the serial number of the row it came
        ///     off, and the copy the bag takes in carries that number until the database issues its
        ///     own
        /// </summary>
        /// <param name="items">Rows to look through</param>
        /// <param name="serialNo">Serial number to look for</param>
        /// <param name="skip">Row that is not looked at</param>
        private static GItem FindSerial(IReadOnlyList<GItem> items, ulong serialNo, GItem skip)
        {
            foreach (GItem row in items)
            {
                if (row != skip && row.SerialNumber == serialNo)
                {
                    return row;
                }
            }

            return null;
        }

        /// <summary>
        ///     Copy of an item that is about to live on its own: the row a bag takes in, or the
        ///     item a drop puts on the ground beside the row it came off. What the copy constructor
        ///     of the item leaves behind is written here - the status of the item is one of those,
        ///     and both the merge of the stacks and the packets of the item read it
        /// </summary>
        /// <param name="source">Item the copy is taken off</param>
        /// <param name="count">Count the copy holds</param>
        private static GItem CopyItem(GItem source, int count)
        {
            GItem copy = new GItem(source);

            copy.Status = source.Status;
            copy.Count = count;

            return copy;
        }
        #endregion

        /// <summary>
        ///     Serializes the writers of the recalc: a level up runs it on the swing thread and a
        ///     change of the equipment on the network thread of the session, and two interleaved
        ///     recalcs could publish the weapon of one and the abilities of the other. Readers do
        ///     not take it - they live on the published references.
        ///     It is the inner of the two locks of the character: it is taken under InventoryLock
        ///     and nothing is ever taken under it - see the order written down beside that lock
        /// </summary>
        private readonly object _recalcLock = new object();

        public new void CalcAbility()
        {
            // The lock of the bag is taken around the one of the recalc and never underneath it:
            // the calculation ends by writing the cap of the weight, the sum of the bag and the
            // state of the load, and those three belong to the writers of the bag - a pick-up that
            // has just raised the weight must not be overwritten by a recalc that started before
            // it. The paths that reach the recalc from under the lock of the bag - a change of the
            // equipment - only take it a second time
            lock (InventoryLock)
            {
                lock (_recalcLock)
                {
                    CalcAbilityLocked();
                }
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

        /// <summary>
        ///     Write the weight cap of the character off its characteristics and rebuild the weight
        ///     of the bag behind it. Every field of the weight is written here, and every one of
        ///     them belongs to the writers of the bag, so the call goes under InventoryLock -
        ///     CalcAbility, the only caller, takes it
        /// </summary>
        /// <param name="ability">Set of the characteristics being built</param>
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

            // The weight the bag adds up to is summed here as well: the calculation runs when the
            // character is read out of the database and on every change of the equipment, and
            // until it does the character carries a bag of rows that weigh nothing - and every
            // check against the cap of the weight would be made over that nothing
            Inventory.RecalcWeight();
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

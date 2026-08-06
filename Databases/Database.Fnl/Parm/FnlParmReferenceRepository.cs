using System.Collections.Generic;
using Database.DataModel.Enums;
using Database.DataModel.Models;
using Database.Fnl.Sql;
using Microsoft.Data.SqlClient;

namespace Database.Fnl.Parm
{
    /// <inheritdoc/>
    public class FnlParmReferenceRepository : IFnlParmReferenceRepository
    {
        /// <summary>
        ///     Ordinal of the link column in every result set of dbo.UspGetParmItem. The base DT_Item
        ///     columns are identical across the seven sets, only the trailing link table differs, and
        ///     the link id always sits right after ITermOfValidityMi
        /// </summary>
        private const int ItemLinkOrdinal = 32;

        /// <summary>
        ///     Ordinals of the two link columns in every result set of dbo.UspGetParmMonsterEx. Most
        ///     sets emit a single link id and a constant 100 in the second slot; the drop set emits
        ///     the drop group in the first and the drop chance in the second
        /// </summary>
        private const int MonsterLinkOrdinal = 19;
        private const int MonsterLinkSecondOrdinal = 20;

        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="connectionFactory"></param>
        public FnlParmReferenceRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Item> GetItems()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmItem");

            // @pShopClass only drives the computed IIsPShop flag (a column we do not map), the item
            // body is the same for every shop class, so 0 is passed here
            StoredProcedure.AddInTinyInt(command, "@pShopClass", 0);

            connection.Open();

            List<Item> items = new List<Item>();
            Dictionary<int, Item> byId = new Dictionary<int, Item>();

            using SqlDataReader reader = command.ExecuteReader();

            // Set 0: DT_Item LEFT JOIN DT_ItemSkill, the base row plus the item -> skill link
            while (reader.Read())
            {
                int id = IntOrZero(reader, 0);

                if (!byId.TryGetValue(id, out Item item))
                {
                    item = ReadItem(reader);
                    byId.Add(id, item);
                    items.Add(item);
                }

                if (!reader.IsDBNull(ItemLinkOrdinal))
                {
                    item.Skills.Add(new Skill { Id = reader.GetInt32(ItemLinkOrdinal) });
                }
            }

            // Set 1: item -> attribute add
            ReadItemLinks(reader, byId, (item, linkId) => item.AttributeAdds.Add(new AttributeAdd { Id = linkId }));
            // Set 2: item -> attribute resist
            ReadItemLinks(reader, byId, (item, linkId) => item.AttributeResists.Add(new AttributeResist { Id = linkId }));
            // Set 3: item -> slain
            ReadItemLinks(reader, byId, (item, linkId) => item.Slains.Add(new Slain { Id = linkId }));
            // Set 4: item -> protect
            ReadItemLinks(reader, byId, (item, linkId) => item.Protects.Add(new Protect { Id = linkId }));
            // Set 5: item -> abnormal add
            ReadItemLinks(reader, byId, (item, linkId) => item.AbnormalAdds.Add(new AbnormalAdd { Id = linkId }));
            // Set 6: item -> abnormal resist
            ReadItemLinks(reader, byId, (item, linkId) => item.AbnormalResists.Add(new AbnormalResist { Id = linkId }));

            return items;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Monster> GetMonsters()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmMonsterEx");

            connection.Open();

            List<Monster> monsters = new List<Monster>();
            Dictionary<int, Monster> byId = new Dictionary<int, Monster>();

            using SqlDataReader reader = command.ExecuteReader();

            // Set 0: DT_Monster LEFT JOIN DT_MonsterSlain, the base row plus the monster -> slain link
            while (reader.Read())
            {
                int id = IntOrZero(reader, 0);

                if (!byId.TryGetValue(id, out Monster monster))
                {
                    monster = ReadMonster(reader);
                    byId.Add(id, monster);
                    monsters.Add(monster);
                }

                if (!reader.IsDBNull(MonsterLinkOrdinal))
                {
                    monster.Slains.Add(new Slain { Id = reader.GetInt32(MonsterLinkOrdinal) });
                }
            }

            // Set 1: monster -> protect
            ReadMonsterLinks(reader, byId, (monster, linkId) => monster.Protects.Add(new Protect { Id = linkId }));

            // Set 2: monster -> drop group, the second link column is the drop chance
            reader.NextResult();
            while (reader.Read())
            {
                if (!byId.TryGetValue(IntOrZero(reader, 0), out Monster monster) || reader.IsDBNull(MonsterLinkOrdinal))
                {
                    continue;
                }

                monster.Drops.Add(new MonsterDrop
                {
                    MonsterId = monster.Id,
                    DropGroupId = reader.GetInt32(MonsterLinkOrdinal),
                    Percent = ByteOrZero(reader, MonsterLinkSecondOrdinal)
                });
            }

            // Set 3: monster -> attribute add
            ReadMonsterLinks(reader, byId, (monster, linkId) => monster.AttributeAdds.Add(new AttributeAdd { Id = linkId }));
            // Set 4: monster -> attribute resist
            ReadMonsterLinks(reader, byId, (monster, linkId) => monster.AttributeResists.Add(new AttributeResist { Id = linkId }));
            // Set 5: monster -> abnormal add
            ReadMonsterLinks(reader, byId, (monster, linkId) => monster.AbnormalAdds.Add(new AbnormalAdd { Id = linkId }));
            // Set 6: monster -> abnormal resist
            ReadMonsterLinks(reader, byId, (monster, linkId) => monster.AbnormalResists.Add(new AbnormalResist { Id = linkId }));

            return monsters;
        }

        /// <inheritdoc/>
        public IReadOnlyList<MonsterRoleRow> GetMonsterRoles()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmMonsterRole");

            connection.Open();

            List<MonsterRoleRow> rows = new List<MonsterRoleRow>();

            // MID, mRole
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new MonsterRoleRow
                {
                    MonsterId = IntOrZero(reader, 0),
                    Role = (NpcRoleEnum)ShortOrZero(reader, 1)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<MonsterSpot> GetMonsterSpots()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmMonsterSpot");

            connection.Open();

            List<MonsterSpot> rows = new List<MonsterSpot>();

            // mGID, mMID, mCnt, mTick, mDir, mVarRespawnTick, mIsEvent (mIsEvent has no POCO field)
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new MonsterSpot
                {
                    GroupId = IntOrZero(reader, 0),
                    MonsterId = IntOrZero(reader, 1),
                    Cnt = IntOrZero(reader, 2),
                    Tick = IntOrZero(reader, 3),
                    Dir = DoubleOrZero(reader, 4),
                    VarRespawnTick = IntOrZero(reader, 5)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<MonsterSpotGroup> GetMonsterSpotGroups()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmMonsterSpotGroup");

            connection.Open();

            List<MonsterSpotGroup> rows = new List<MonsterSpotGroup>();
            HashSet<int> seen = new HashSet<int>();

            // mGID, mPosX, mPosY, mPosZ, mRadius, mMID. The join repeats a group once per monster on
            // that spot, so the mMID column is dropped and duplicate groups are collapsed by id
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = IntOrZero(reader, 0);

                if (!seen.Add(id))
                {
                    continue;
                }

                rows.Add(new MonsterSpotGroup
                {
                    Id = id,
                    PosX = DoubleOrZero(reader, 1),
                    PosY = DoubleOrZero(reader, 2),
                    PosZ = DoubleOrZero(reader, 3),
                    Radius = DoubleOrZero(reader, 4)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Abnormal> GetAbnormals()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmAbnormal");

            connection.Open();

            List<Abnormal> rows = new List<Abnormal>();
            HashSet<int> seen = new HashSet<int>();

            // AID, AType, ALevel, APercent, MID (module link), AName, ATime, AGrade, ARemovable, ACopyable.
            // The LEFT JOIN to DT_AbnormalModule repeats an abnormal once per module, so it is collapsed
            // by id; the module link and the two type flags have no field on the Abnormal POCO
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = IntOrZero(reader, 0);

                if (!seen.Add(id))
                {
                    continue;
                }

                rows.Add(new Abnormal
                {
                    Id = id,
                    Type = (AbnormalTypeEnum)IntOrZero(reader, 1),
                    Level = ByteOrZero(reader, 2),
                    Percent = ByteOrZero(reader, 3),
                    Desc = TrimmedString(reader, 5),
                    Time = IntOrZero(reader, 6),
                    Grade = ByteOrZero(reader, 7)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<AbnormalAdd> GetAbnormalAdds()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmAbnormalAdd");

            connection.Open();

            List<AbnormalAdd> rows = new List<AbnormalAdd>();

            // AID, AType, ALevel, APercent, AName (AName has no POCO field)
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new AbnormalAdd
                {
                    Id = IntOrZero(reader, 0),
                    Type = (AbnormalTypeEnum)IntOrZero(reader, 1),
                    Level = ByteOrZero(reader, 2),
                    Percent = ByteOrZero(reader, 3)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<AbnormalResist> GetAbnormalResists()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmAbnormalResist");

            connection.Open();

            List<AbnormalResist> rows = new List<AbnormalResist>();

            // AID, AType, ALevel, APercent, AName (AName has no POCO field)
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new AbnormalResist
                {
                    Id = IntOrZero(reader, 0),
                    Type = (AbnormalTypeEnum)IntOrZero(reader, 1),
                    Level = ByteOrZero(reader, 2),
                    Percent = ByteOrZero(reader, 3)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Skill> GetSkills()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmSkill");

            connection.Open();

            List<Skill> skills = new List<Skill>();
            Dictionary<int, Skill> byId = new Dictionary<int, Skill>();

            using SqlDataReader reader = command.ExecuteReader();

            // Set 0: DT_Skill LEFT JOIN DT_SkillAttribute, the base row plus the skill -> attribute link
            while (reader.Read())
            {
                int id = IntOrZero(reader, 0);

                if (!byId.TryGetValue(id, out Skill skill))
                {
                    skill = ReadSkill(reader);
                    byId.Add(id, skill);
                    skills.Add(skill);
                }

                if (!reader.IsDBNull(SkillLinkOrdinal))
                {
                    skill.Attributes.Add(new Attribute { Id = reader.GetInt32(SkillLinkOrdinal) });
                }
            }

            // Set 1: skill -> abnormal
            ReadSkillLinks(reader, byId, (skill, linkId) => skill.Abnormals.Add(new Abnormal { Id = linkId }));
            // Set 2: skill -> slain
            ReadSkillLinks(reader, byId, (skill, linkId) => skill.Slains.Add(new Slain { Id = linkId }));

            return skills;
        }

        /// <inheritdoc/>
        public IReadOnlyList<DropItem> GetDropItems()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmDrop");

            connection.Open();

            List<DropItem> rows = new List<DropItem>();

            // DDrop, DItem, DNumber, DStatus, DIsEvent
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new DropItem
                {
                    Id = IntOrZero(reader, 0),
                    ItemId = IntOrZero(reader, 1),
                    Count = ShortOrZero(reader, 2),
                    Status = (ItemStatusEnum)ByteOrZero(reader, 3),
                    IsEvent = BoolOrFalse(reader, 4)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<DropGroup> GetDropGroups()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmDropGroup");

            connection.Open();

            List<DropGroup> groups = new List<DropGroup>();
            Dictionary<int, DropGroup> byId = new Dictionary<int, DropGroup>();

            // DGroup, DDrop, DPercent, DName, DDropType (DName and DDropType have no field on the POCO).
            // One row per (group, drop item), so rows are folded into the group's Items list
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int groupId = IntOrZero(reader, 0);

                if (!byId.TryGetValue(groupId, out DropGroup group))
                {
                    group = new DropGroup { DropGroupId = groupId };
                    byId.Add(groupId, group);
                    groups.Add(group);
                }

                group.Items.Add(new DropGroupItem
                {
                    DropItemId = IntOrZero(reader, 1),
                    Percent = FloatOrZero(reader, 2)
                });
            }

            return groups;
        }

        /// <inheritdoc/>
        public IReadOnlyList<ExpRow> GetExps()
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmExpTable");

            connection.Open();

            List<ExpRow> rows = new List<ExpRow>();

            // ELevel, EExp, ERestExpRate
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new ExpRow
                {
                    Level = ShortOrZero(reader, 0),
                    Exp = LongOrZero(reader, 1),
                    RestExpRate = IntOrZero(reader, 2)
                });
            }

            return rows;
        }

        /// <summary>
        ///     Ordinal of the link column in every result set of dbo.UspGetParmSkill: the linked
        ///     attribute/abnormal/slain id always sits right after SSpellNum
        /// </summary>
        private const int SkillLinkOrdinal = 4;

        /// <summary>
        ///     Advances the reader to the next result set of dbo.UspGetParmItem and hands every
        ///     non-null link id to <paramref name="attach"/> together with the already built item.
        ///     Only IID and the link column are touched, the base body was read from the first set
        /// </summary>
        private static void ReadItemLinks(SqlDataReader reader, Dictionary<int, Item> byId, System.Action<Item, int> attach)
        {
            reader.NextResult();

            while (reader.Read())
            {
                if (!byId.TryGetValue(IntOrZero(reader, 0), out Item item) || reader.IsDBNull(ItemLinkOrdinal))
                {
                    continue;
                }

                attach(item, reader.GetInt32(ItemLinkOrdinal));
            }
        }

        /// <summary>
        ///     Same as <see cref="ReadItemLinks"/> for dbo.UspGetParmMonsterEx (single link column sets)
        /// </summary>
        private static void ReadMonsterLinks(SqlDataReader reader, Dictionary<int, Monster> byId, System.Action<Monster, int> attach)
        {
            reader.NextResult();

            while (reader.Read())
            {
                if (!byId.TryGetValue(IntOrZero(reader, 0), out Monster monster) || reader.IsDBNull(MonsterLinkOrdinal))
                {
                    continue;
                }

                attach(monster, reader.GetInt32(MonsterLinkOrdinal));
            }
        }

        /// <summary>
        ///     Same as <see cref="ReadItemLinks"/> for dbo.UspGetParmSkill
        /// </summary>
        private static void ReadSkillLinks(SqlDataReader reader, Dictionary<int, Skill> byId, System.Action<Skill, int> attach)
        {
            reader.NextResult();

            while (reader.Read())
            {
                if (!byId.TryGetValue(IntOrZero(reader, 0), out Skill skill) || reader.IsDBNull(SkillLinkOrdinal))
                {
                    continue;
                }

                attach(skill, reader.GetInt32(SkillLinkOrdinal));
            }
        }

        /// <summary>
        ///     Maps the shared DT_Item body of a dbo.UspGetParmItem row. Ordinal 32 is the link column
        ///     and is handled by the caller. Columns the procedure does not select (IDesc, IUseMsg,
        ///     IDropEffect, IGetItemFeedback, IAddMpPotionRestore) keep their POCO default
        /// </summary>
        private static Item ReadItem(SqlDataReader reader)
        {
            return new Item
            {
                Id = IntOrZero(reader, 0),
                Type = (ItemTypeEnum)IntOrZero(reader, 1),
                Level = ByteOrZero(reader, 2),
                DDv = ShortOrZero(reader, 3),
                MDv = ShortOrZero(reader, 4),
                RDv = ShortOrZero(reader, 5),
                DPv = ShortOrZero(reader, 6),
                MPv = ShortOrZero(reader, 7),
                RPv = ShortOrZero(reader, 8),
                DHit = ShortOrZero(reader, 9),
                DDd = TrimmedString(reader, 10),
                RHit = ShortOrZero(reader, 11),
                RDd = TrimmedString(reader, 12),
                MHit = ShortOrZero(reader, 13),
                MDd = TrimmedString(reader, 14),
                HpPlus = ShortOrZero(reader, 15),
                Mpplus = ShortOrZero(reader, 16),
                Str = ShortOrZero(reader, 17),
                Dex = ShortOrZero(reader, 18),
                Int = ShortOrZero(reader, 19),
                MaxStack = IntOrZero(reader, 20),
                Weight = ShortOrZero(reader, 21),
                UseType = (UseTypeEnum)IntOrZero(reader, 22),
                UseNum = IntOrZero(reader, 23),
                Recycle = IntOrZero(reader, 24),
                HpRegen = ByteOrZero(reader, 25),
                MpRegen = ByteOrZero(reader, 26),
                AttackRate = ByteOrZero(reader, 27),
                MoveRate = ByteOrZero(reader, 28),
                Critical = ByteOrZero(reader, 29),
                TermOfValidity = ShortOrZero(reader, 30),
                TermOfValidityMi = ShortOrZero(reader, 31),
                // 32 = link column
                Name = TrimmedString(reader, 33),
                Status = (ItemStatusEnum)ByteOrZero(reader, 34),
                FakeId = IntOrZero(reader, 35),
                FakeName = TrimmedString(reader, 36),
                Range = ShortOrZero(reader, 37),
                UseClass = (UseClassEnum)ByteOrZero(reader, 38),
                UseLevel = ShortOrZero(reader, 39),
                UseEternal = ByteOrZero(reader, 40),
                UseDelay = IntOrZero(reader, 41),
                UseInAttack = ByteOrZero(reader, 42) != 0,
                IsEvent = BoolOrFalse(reader, 43),
                IsIndict = BoolOrFalse(reader, 44),
                HDDv = ShortOrZero(reader, 45),
                HMDv = ShortOrZero(reader, 46),
                HRDv = ShortOrZero(reader, 47),
                HDPv = ShortOrZero(reader, 48),
                HMPv = ShortOrZero(reader, 49),
                HRPv = ShortOrZero(reader, 50),
                AddWeight = ShortOrZero(reader, 51),
                SubType = (ItemSubTypeEnum)ShortOrZero(reader, 52),
                IsCharge = BoolOrFalse(reader, 53),
                NationOp = LongOrZero(reader, 54),
                // 55 = IIsPShop computed flag, no POCO field
                PshopItemType = ByteOrZero(reader, 56),
                QuestNo = IntOrZero(reader, 57),
                IsTest = BoolOrFalse(reader, 58),
                QuestNeedCnt = ByteOrZero(reader, 59),
                ContentsLv = ByteOrZero(reader, 60),
                IsConfirm = BoolOrFalse(reader, 61),
                IsSealable = BoolOrFalse(reader, 62),
                AddDDWhenCritical = ShortOrZero(reader, 63),
                SealRemovalNeedCnt = ByteOrZero(reader, 64),
                IsPracticalPeriod = BoolOrFalse(reader, 65),
                IsReceiveTown = BoolOrFalse(reader, 66),
                IsReinforceDestroy = BoolOrFalse(reader, 67),
                AddHpPotionRestore = ShortOrZero(reader, 68),
                AddMaxHpWhenTransform = ShortOrZero(reader, 69),
                AddMaxMpWhenTransform = ShortOrZero(reader, 70),
                AddAttackRateWhenTransform = ShortOrZero(reader, 71),
                AddMoveRateWhenTransform = ShortOrZero(reader, 72),
                SupportType = ByteOrZero(reader, 73),
                TermOfValidityLv = ShortOrZero(reader, 74),
                IsUseableUtgwsvr = BoolOrFalse(reader, 75),
                AddShortAttackRange = ShortOrZero(reader, 76),
                AddLongAttackRange = ShortOrZero(reader, 77),
                WeaponPoisonType = ShortOrZero(reader, 78),
                SubDDWhenCritical = ShortOrZero(reader, 79),
                EnemySubCriticalHit = ShortOrZero(reader, 80),
                IsPartyDrop = BoolOrFalse(reader, 81),
                MaxBeadHoleCount = ByteOrZero(reader, 82),
                SubTypeOption = IntOrZero(reader, 83)
                // 84 = mIsDeleteArenaSvr, no POCO field
            };
        }

        /// <summary>
        ///     Maps the shared DT_Monster body of a dbo.UspGetParmMonsterEx row. Ordinals 19 and 20 are
        ///     the link columns and are handled by the caller. mWMapIconType is not selected by the
        ///     procedure and keeps its POCO default
        /// </summary>
        private static Monster ReadMonster(SqlDataReader reader)
        {
            return new Monster
            {
                Id = IntOrZero(reader, 0),
                Class = (PcClassEnum)IntOrZero(reader, 1),
                Exp = (ulong)IntOrZero(reader, 2),
                DDv = ShortOrZero(reader, 3),
                MDv = ShortOrZero(reader, 4),
                RDv = ShortOrZero(reader, 5),
                DPv = ShortOrZero(reader, 6),
                MPv = ShortOrZero(reader, 7),
                RPv = ShortOrZero(reader, 8),
                Hit = ShortOrZero(reader, 9),
                MinDamage = ShortOrZero(reader, 10),
                MaxDamage = ShortOrZero(reader, 11),
                AttackRateOrg = ShortOrZero(reader, 12),
                MoveRateOrg = ShortOrZero(reader, 13),
                AttackRateNew = ShortOrZero(reader, 14),
                MoveRateNew = ShortOrZero(reader, 15),
                HpMax = ShortOrZero(reader, 16),
                MpMax = ShortOrZero(reader, 17),
                MoveRange = ShortOrZero(reader, 18),
                // 19, 20 = link columns
                Name = TrimmedString(reader, 21),
                Type = (GbjClassEnum)ShortOrZero(reader, 22),
                RaceType = (RaceTypeEnum)ShortOrZero(reader, 23),
                AiType = (AiTypeEnum)ShortOrZero(reader, 24),
                AiEx = (AiTypeEnum)IntOrZero(reader, 25),
                CastingDelay = ShortOrZero(reader, 26),
                Chaotic = ShortOrZero(reader, 27),
                SameRace1 = IntOrZero(reader, 28),
                SameRace2 = IntOrZero(reader, 29),
                SameRace3 = IntOrZero(reader, 30),
                SameRace4 = IntOrZero(reader, 31),
                SightRange = IntOrZero(reader, 32),
                AttackRange = IntOrZero(reader, 33),
                SkillRange = IntOrZero(reader, 34),
                BodySize = IntOrZero(reader, 35),
                DetectTransformF = ShortOrZero(reader, 36),
                DetectTransformP = ShortOrZero(reader, 37),
                DetectChaotic = ShortOrZero(reader, 38),
                IsResistTransF = BoolOrFalse(reader, 39),
                IsEvent = BoolOrFalse(reader, 40),
                Level = IntOrZero(reader, 41),
                IsTest = BoolOrFalse(reader, 42),
                HpNew = ShortOrZero(reader, 43),
                MpNew = ShortOrZero(reader, 44),
                BuyMerchanId = IntOrZero(reader, 45),
                SellMerchanId = IntOrZero(reader, 46),
                ChargeMerchanId = IntOrZero(reader, 47),
                TransformWeight = ShortOrZero(reader, 48),
                NationOp = LongOrZero(reader, 49),
                Scale = DoubleOrZero(reader, 50),
                HpRegen = ShortOrZero(reader, 51),
                MpRegen = ShortOrZero(reader, 52),
                ContentsLv = ByteOrZero(reader, 53),
                IsEventTest = BoolOrFalse(reader, 54),
                IsShowHp = BoolOrFalse(reader, 55),
                SupportType = ByteOrZero(reader, 56),
                VolitionOfHonor = ShortOrZero(reader, 57),
                IsAmpliableTermOfValidity = BoolOrFalse(reader, 58),
                AttackType = (AttackTypeEnum)ByteOrZero(reader, 59),
                TransType = ByteOrZero(reader, 60),
                SubDDWhenCritical = ShortOrZero(reader, 61),
                EnemySubCriticalHit = ShortOrZero(reader, 62),
                EventQuest = ByteOrZero(reader, 63)
            };
        }

        /// <summary>
        ///     Maps the shared DT_Skill body of a dbo.UspGetParmSkill row. Ordinal 4 is the link column
        ///     and is handled by the caller. Columns the procedure does not select (SDesc, mAnimation,
        ///     mCastingSpeed, mSkillEffect, mCAmShakeWhenHit, mCriticalEffectWhenHit, mActiveWeapon,
        ///     mIsCancel) keep their POCO default
        /// </summary>
        private static Skill ReadSkill(SqlDataReader reader)
        {
            return new Skill
            {
                Id = IntOrZero(reader, 0),
                HitPlus = ShortOrZero(reader, 1),
                MpperUse = ShortOrZero(reader, 2),
                SpellNum = ShortOrZero(reader, 3),
                // 4 = link column
                Name = TrimmedString(reader, 5),
                HpperUse = ShortOrZero(reader, 6),
                ChaoUse = ShortOrZero(reader, 7),
                ApplyRadius = ShortOrZero(reader, 8),
                ApplyCnt = ShortOrZero(reader, 9),
                ApplyRace = ByteOrZero(reader, 10),
                Type = ShortOrZero(reader, 11),
                CastingDelay = ShortOrZero(reader, 12),
                ConsumeItem = IntOrZero(reader, 13),
                ConsumeItemCnt = ByteOrZero(reader, 14),
                ConsumeItem2 = IntOrZero(reader, 15),
                ConsumeItemCnt2 = ByteOrZero(reader, 16),
                ActiveType = ShortOrZero(reader, 17),
                CoolTime = IntOrZero(reader, 18),
                CastingGroup = ShortOrZero(reader, 19),
                CoolTimeGroup = ShortOrZero(reader, 20),
                IsAttack = BoolOrFalse(reader, 21)
            };
        }

        /// <summary>
        ///     Reads a string column, char/nvarchar values come back padded with spaces
        /// </summary>
        private static string TrimmedString(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).TrimEnd();
        }

        /// <summary>
        ///     Reads a tinyint column, null (nullable column with no value) comes back as 0
        /// </summary>
        private static byte ByteOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (byte)0 : reader.GetByte(ordinal);
        }

        /// <summary>
        ///     Reads a smallint column, null comes back as 0
        /// </summary>
        private static short ShortOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (short)0 : reader.GetInt16(ordinal);
        }

        /// <summary>
        ///     Reads an int column, null comes back as 0
        /// </summary>
        private static int IntOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }

        /// <summary>
        ///     Reads a bigint column, null comes back as 0
        /// </summary>
        private static long LongOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0L : reader.GetInt64(ordinal);
        }

        /// <summary>
        ///     Reads a float (8-byte) column, null comes back as 0
        /// </summary>
        private static double DoubleOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0d : reader.GetDouble(ordinal);
        }

        /// <summary>
        ///     Reads a real (4-byte) column, null comes back as 0
        /// </summary>
        private static float FloatOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0f : reader.GetFloat(ordinal);
        }

        /// <summary>
        ///     Reads a bit column, null comes back as false
        /// </summary>
        private static bool BoolOrFalse(SqlDataReader reader, int ordinal)
        {
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }
    }
}

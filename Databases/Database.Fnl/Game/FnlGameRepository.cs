using System.Collections.Generic;
using Database.Fnl.Sql;
using Microsoft.Data.SqlClient;

namespace Database.Fnl.Game
{
    /// <inheritdoc/>
    public class FnlGameRepository : IFnlGameRepository
    {
        private const string ListPcProcedure = "dbo.UspListPc";
        private const string CreatePcProcedure = "dbo.UspCreatePc";
        private const string DeletePcProcedure = "dbo.UspDeletePcEx";
        private const string LoginPcProcedure = "dbo.UspLoginPc";
        private const string LogoutPcProcedure = "dbo.UspLogoutPc";
        private const string GetPcDetailProcedure = "dbo.UspGetPcDetail";
        private const string GetPcEquipProcedure = "dbo.UspGetPcEquip";
        private const string GetPcItemProcedure = "dbo.UspGetPcItem";
        private const string GetListAbnormalProcedure = "dbo.UspGetListAbnormal";
        private const string UpdatePosProcedure = "dbo.UspUpdatePos";
        private const string SetEquipProcedure = "dbo.UspSetEquip";
        private const string ResetEquipProcedure = "dbo.UspResetEquip";
        private const string PushItemProcedure = "dbo.UspPushItem";
        private const string PopItemProcedure = "dbo.UspPopItem";
        private const string EraseItemProcedure = "dbo.UspEraseItem";

        /// <summary>
        ///     Owner an item that lies on the ground is kept under. It is not a character: the
        ///     inventory reader answers with an empty list for the character numbers of the npc and
        ///     of the dropped items, so a row parked here belongs to nobody until somebody picks
        ///     the item up
        /// </summary>
        public const int DroppedItemOwner = 1;

        /// <summary>
        ///     Column ordinals of the dbo.UspListPc result set: a.mSlot, a.mNo
        /// </summary>
        private const int ListPcSlot = 0;
        private const int ListPcNo = 1;

        /// <summary>
        ///     Column ordinals of the dbo.UspGetPcDetail result set, in the order of its final SELECT.
        ///     The guild columns come from a LEFT JOIN and are null for a character in no guild
        /// </summary>
        private const int DetailNm = 0;
        private const int DetailClass = 1;
        private const int DetailSex = 2;
        private const int DetailHead = 3;
        private const int DetailFace = 4;
        private const int DetailBody = 5;
        private const int DetailLevel = 6;
        private const int DetailHpAdd = 7;
        private const int DetailHp = 8;
        private const int DetailMpAdd = 9;
        private const int DetailMp = 10;
        private const int DetailExp = 11;
        private const int DetailStomach = 12;
        private const int DetailMapNo = 13;
        private const int DetailPosX = 14;
        private const int DetailPosY = 15;
        private const int DetailPosZ = 16;
        private const int DetailHomeMapNo = 17;
        private const int DetailHomePosX = 18;
        private const int DetailHomePosY = 19;
        private const int DetailHomePosZ = 20;
        private const int DetailGuildNo = 21;
        private const int DetailNickNm = 22;
        private const int DetailGuildGrade = 23;
        private const int DetailPkCnt = 24;
        private const int DetailChaotic = 25;
        private const int DetailDiscipleNo = 26;
        private const int DetailDiscipleType = 27;
        private const int DetailIsLetterLimit = 28;
        private const int DetailIsPreventItemDrop = 29;
        private const int DetailFlag = 30;

        /// <summary>
        ///     Column ordinals of the dbo.UspGetPcEquip result set: a.mSlot, a.mSerialNo,
        ///     b.mItemNo, b.mBindingType
        /// </summary>
        private const int EquipSlot = 0;
        private const int EquipSerialNo = 1;
        private const int EquipItemNo = 2;
        private const int EquipBindingType = 3;

        /// <summary>
        ///     Column ordinals of the dbo.UspGetPcItem result set. Both UNION ALL branches project
        ///     the same fifteen columns; the two date columns are DATEDIFF(minute, now, endDate)
        /// </summary>
        private const int ItemSerialNo = 0;
        private const int ItemItemNo = 1;
        private const int ItemEndDate = 2;
        private const int ItemCnt = 3;
        private const int ItemIsConfirm = 4;
        private const int ItemStatus = 5;
        private const int ItemCntUse = 6;
        private const int ItemIsSeizure = 7;
        private const int ItemApplyAbnItemNo = 8;
        private const int ItemApplyAbnItemEndDate = 9;
        private const int ItemOwner = 10;
        private const int ItemPracticalPeriod = 11;
        private const int ItemBindingType = 12;
        private const int ItemRestoreCnt = 13;
        private const int ItemHoleCount = 14;

        /// <summary>
        ///     Column ordinals of the dbo.UspPushItem result set: the error code, the serial the
        ///     item ended up with and DATEDIFF(minute, now, endDate) of that row
        /// </summary>
        private const int PushItemErrNo = 0;
        private const int PushItemSerialNo = 1;
        private const int PushItemEndDate = 2;

        /// <summary>
        ///     Column ordinals of the dbo.UspGetListAbnormal result set:
        ///     mParmNo, mLeftTime, mAbParmNo, mRestoreCnt
        /// </summary>
        private const int AbnormalParmNo = 0;
        private const int AbnormalLeftTime = 1;
        private const int AbnormalAbParmNo = 2;
        private const int AbnormalRestoreCnt = 3;

        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="connectionFactory"></param>
        public FnlGameRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc/>
        public IReadOnlyList<PcListRow> ListPc(int userNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, ListPcProcedure);

            StoredProcedure.AddInInt(command, "@pUserNo", userNo);

            connection.Open();

            List<PcListRow> rows = new List<PcListRow>();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new PcListRow
                {
                    Slot = reader.GetByte(ListPcSlot),
                    No = reader.GetInt32(ListPcNo)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public CreatePcResult CreatePc(CreatePcRequest request)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, CreatePcProcedure);

            // Order and types are taken from sys.parameters of the live procedure
            StoredProcedure.AddInInt(command, "@pOwner", request.Owner);
            StoredProcedure.AddInTinyInt(command, "@pSlot", request.Slot);
            StoredProcedure.AddInChar(command, "@pNm", 12, request.Nm);
            StoredProcedure.AddInTinyInt(command, "@pClass", request.Class);
            StoredProcedure.AddInTinyInt(command, "@pSex", request.Sex);
            StoredProcedure.AddInTinyInt(command, "@pHead", request.Head);
            StoredProcedure.AddInTinyInt(command, "@pFace", request.Face);
            StoredProcedure.AddInTinyInt(command, "@pBody", request.Body);
            StoredProcedure.AddInInt(command, "@pHomeMap", request.HomeMap);
            StoredProcedure.AddInReal(command, "@pHomeX", request.HomeX);
            StoredProcedure.AddInReal(command, "@pHomeY", request.HomeY);
            StoredProcedure.AddInReal(command, "@pHomeZ", request.HomeZ);

            SqlParameter pcNo = StoredProcedure.AddOutInt(command, "@pPcNo");
            SqlParameter level = StoredProcedure.AddOutSmallInt(command, "@pLevel");
            SqlParameter errNoStr = StoredProcedure.AddOutVarChar(command, "@pErrNoStr", 50);

            connection.Open();
            command.ExecuteNonQuery();

            CreatePcResult result = new CreatePcResult
            {
                ReturnCode = StoredProcedure.ReturnValue(command),
                PcNo = GetInt32(pcNo),
                Level = GetInt16(level)
            };

            // On success @pErrNoStr keeps its initial 'eErrNoSqlInternalError', read it only on failure
            if (!result.IsSuccess)
            {
                result.ErrNo = StoredProcedure.GetTrimmedString(errNoStr);
            }

            return result;
        }

        /// <inheritdoc/>
        public DeletePcResult DeletePc(int owner, int pcNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, DeletePcProcedure);

            StoredProcedure.AddInInt(command, "@pOwner", owner);
            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);

            SqlParameter nm = StoredProcedure.AddOutChar(command, "@pNm", 12);
            SqlParameter guildNo = StoredProcedure.AddOutInt(command, "@pGuildNo");
            SqlParameter guildGrade = StoredProcedure.AddOutTinyInt(command, "@pGuildGrade");

            connection.Open();
            command.ExecuteNonQuery();

            return new DeletePcResult
            {
                ReturnCode = StoredProcedure.ReturnValue(command),
                Nm = StoredProcedure.GetTrimmedString(nm),
                GuildNo = GetInt32(guildNo),
                GuildGrade = GetByte(guildGrade)
            };
        }

        /// <inheritdoc/>
        public void LoginPc(int userNo, int pcNo, string ip)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, LoginPcProcedure);

            StoredProcedure.AddInInt(command, "@pUserNo", userNo);
            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);
            StoredProcedure.AddInChar(command, "@pIp", 15, ip);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        public void LogoutPc(int userNo, int pcNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, LogoutPcProcedure);

            StoredProcedure.AddInInt(command, "@pUserNo", userNo);
            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        public PcDetailRow GetPcDetail(int pcNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, GetPcDetailProcedure);

            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            // No row means the character does not exist or is already flagged for deletion
            if (!reader.Read())
            {
                return null;
            }

            return new PcDetailRow
            {
                Nm = GetTrimmedString(reader, DetailNm),
                Class = reader.GetByte(DetailClass),
                Sex = reader.GetByte(DetailSex),
                Head = reader.GetByte(DetailHead),
                Face = reader.GetByte(DetailFace),
                Body = reader.GetByte(DetailBody),
                Level = reader.GetInt16(DetailLevel),
                HpAdd = reader.GetInt32(DetailHpAdd),
                Hp = reader.GetInt32(DetailHp),
                MpAdd = reader.GetInt32(DetailMpAdd),
                Mp = reader.GetInt32(DetailMp),
                Exp = reader.GetInt64(DetailExp),
                Stomach = reader.GetInt16(DetailStomach),
                MapNo = reader.GetInt32(DetailMapNo),
                PosX = reader.GetFloat(DetailPosX),
                PosY = reader.GetFloat(DetailPosY),
                PosZ = reader.GetFloat(DetailPosZ),
                HomeMapNo = reader.GetInt32(DetailHomeMapNo),
                HomePosX = reader.GetFloat(DetailHomePosX),
                HomePosY = reader.GetFloat(DetailHomePosY),
                HomePosZ = reader.GetFloat(DetailHomePosZ),
                GuildNo = GetInt32OrZero(reader, DetailGuildNo),
                NickNm = GetTrimmedString(reader, DetailNickNm),
                GuildGrade = GetByteOrZero(reader, DetailGuildGrade),
                PkCnt = reader.GetInt32(DetailPkCnt),
                Chaotic = reader.GetInt32(DetailChaotic),
                DiscipleNo = reader.GetInt32(DetailDiscipleNo),
                DiscipleType = reader.GetByte(DetailDiscipleType),
                IsLetterLimit = reader.GetBoolean(DetailIsLetterLimit),
                IsPreventItemDrop = reader.GetBoolean(DetailIsPreventItemDrop),
                Flag = reader.GetInt16(DetailFlag)
            };
        }

        /// <inheritdoc/>
        public IReadOnlyList<PcEquipRow> GetPcEquip(int pcNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, GetPcEquipProcedure);

            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);

            connection.Open();

            List<PcEquipRow> rows = new List<PcEquipRow>();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new PcEquipRow
                {
                    Slot = reader.GetInt32(EquipSlot),
                    SerialNo = reader.GetInt64(EquipSerialNo),
                    ItemNo = reader.GetInt32(EquipItemNo),
                    BindingType = reader.GetByte(EquipBindingType)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<PcItemRow> GetPcItem(int pcNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, GetPcItemProcedure);

            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);

            connection.Open();

            List<PcItemRow> rows = new List<PcItemRow>();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new PcItemRow
                {
                    SerialNo = reader.GetInt64(ItemSerialNo),
                    ItemNo = reader.GetInt32(ItemItemNo),
                    EndDateMinutes = GetInt32OrZero(reader, ItemEndDate),
                    Cnt = reader.GetInt32(ItemCnt),
                    IsConfirm = reader.GetBoolean(ItemIsConfirm),
                    Status = reader.GetByte(ItemStatus),
                    CntUse = reader.GetInt16(ItemCntUse),
                    IsSeizure = reader.GetBoolean(ItemIsSeizure),
                    ApplyAbnItemNo = reader.GetInt32(ItemApplyAbnItemNo),
                    // mApplyAbnItemEndDate is nullable, so its DATEDIFF is null when there is no abnormal
                    ApplyAbnItemEndDateMinutes = GetInt32OrZero(reader, ItemApplyAbnItemEndDate),
                    Owner = reader.GetInt32(ItemOwner),
                    PracticalPeriod = reader.GetInt32(ItemPracticalPeriod),
                    BindingType = reader.GetByte(ItemBindingType),
                    RestoreCnt = reader.GetByte(ItemRestoreCnt),
                    HoleCount = reader.GetByte(ItemHoleCount)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public IReadOnlyList<PcAbnormalRow> GetListAbnormal(int pcNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, GetListAbnormalProcedure);

            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);

            connection.Open();

            List<PcAbnormalRow> rows = new List<PcAbnormalRow>();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new PcAbnormalRow
                {
                    ParmNo = reader.GetInt32(AbnormalParmNo),
                    LeftTime = reader.GetInt32(AbnormalLeftTime),
                    AbParmNo = reader.GetInt32(AbnormalAbParmNo),
                    RestoreCnt = reader.GetByte(AbnormalRestoreCnt)
                });
            }

            return rows;
        }

        /// <inheritdoc/>
        public void UpdatePos(int pcNo, int hp, int mp, int mapNo, float x, float y, float z, short stomach)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, UpdatePosProcedure);

            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);
            StoredProcedure.AddInInt(command, "@pHp", hp);
            StoredProcedure.AddInInt(command, "@pMp", mp);
            StoredProcedure.AddInInt(command, "@pMapNo", mapNo);
            StoredProcedure.AddInReal(command, "@pPosX", x);
            StoredProcedure.AddInReal(command, "@pPosY", y);
            StoredProcedure.AddInReal(command, "@pPosZ", z);
            StoredProcedure.AddInSmallInt(command, "@pStomach", stomach);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        public int SetEquip(int pcNo, long serialNo, int slot)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, SetEquipProcedure);

            // Order and types are taken from sys.parameters of the live procedure
            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);
            StoredProcedure.AddInBigInt(command, "@pSerial", serialNo);
            StoredProcedure.AddInInt(command, "@pSlot", slot);

            connection.Open();
            command.ExecuteNonQuery();

            return StoredProcedure.ReturnValue(command);
        }

        /// <inheritdoc/>
        public int ResetEquip(int pcNo, int slot)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, ResetEquipProcedure);

            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);
            StoredProcedure.AddInInt(command, "@pSlot", slot);

            connection.Open();
            command.ExecuteNonQuery();

            return StoredProcedure.ReturnValue(command);
        }

        /// <inheritdoc/>
        public PushItemRow PushItem(int pcNo, long serialNo, int itemNo, int validDay, int cnt, short cntUse,
            bool isConfirm, byte status, bool isStack, bool isCharge = false, int practicalPeriod = 0,
            byte bindingType = 0, byte restoreCnt = 0)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, PushItemProcedure);

            // Order and types are taken from sys.parameters of the live procedure
            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);
            StoredProcedure.AddInBigInt(command, "@pSerial", serialNo);
            StoredProcedure.AddInInt(command, "@pItemNo", itemNo);
            StoredProcedure.AddInInt(command, "@pValidDay", validDay);
            StoredProcedure.AddInInt(command, "@pCnt", cnt);
            StoredProcedure.AddInSmallInt(command, "@pCntUse", cntUse);
            StoredProcedure.AddInBit(command, "@pIsConfirm", isConfirm);
            StoredProcedure.AddInTinyInt(command, "@pStatus", status);
            StoredProcedure.AddInBit(command, "@pIsStack", isStack);
            StoredProcedure.AddInBit(command, "@pIsCharge", isCharge);
            StoredProcedure.AddInInt(command, "@pPracticalPeriod", practicalPeriod);
            StoredProcedure.AddInTinyInt(command, "@pBindingType", bindingType);
            StoredProcedure.AddInTinyInt(command, "@pRestoreCnt", restoreCnt);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            // The outcome is a result set of its own, the procedure sends it on every path it has
            if (!reader.Read())
            {
                throw new System.InvalidOperationException(
                    $"Stored procedure '{PushItemProcedure}' answered with no row");
            }

            return new PushItemRow
            {
                ErrorCode = reader.GetInt32(PushItemErrNo),
                SerialNo = GetSerialNo(reader, PushItemSerialNo),
                // The lifetime is counted from a date the failure paths may leave unset
                EndDateMinutes = GetInt32OrZero(reader, PushItemEndDate)
            };
        }

        /// <inheritdoc/>
        public PopItemResult PopItem(int pcNo, long serialNo, int cnt, int cachingCnt, bool isSpend, bool isStack)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, PopItemProcedure);

            // Order and types are taken from sys.parameters of the live procedure, @pIsSpend is
            // a tinyint there and not a bit
            StoredProcedure.AddInInt(command, "@pPcNo", pcNo);
            StoredProcedure.AddInBigInt(command, "@pSerial", serialNo);
            StoredProcedure.AddInInt(command, "@pCnt", cnt);
            StoredProcedure.AddInInt(command, "@pCachingCnt", cachingCnt);
            StoredProcedure.AddInTinyInt(command, "@pIsSpend", isSpend ? (byte)1 : (byte)0);
            StoredProcedure.AddInBit(command, "@pIsStack", isStack);

            SqlParameter serialNew = StoredProcedure.AddOutBigInt(command, "@pSerialNew");

            connection.Open();
            command.ExecuteNonQuery();

            return new PopItemResult
            {
                ReturnCode = StoredProcedure.ReturnValue(command),
                SerialNoNew = GetInt64(serialNew)
            };
        }

        /// <inheritdoc/>
        public int EraseItem(long serialNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlGame);
            using SqlCommand command = StoredProcedure.Create(connection, EraseItemProcedure);

            StoredProcedure.AddInBigInt(command, "@pSerial", serialNo);

            connection.Open();
            command.ExecuteNonQuery();

            return StoredProcedure.ReturnValue(command);
        }

        /// <summary>
        ///     Reads an int output parameter, the procedure leaves it untouched (NULL) on the error paths
        /// </summary>
        private static int GetInt32(SqlParameter parameter)
        {
            return IsEmpty(parameter) ? 0 : (int)parameter.Value;
        }

        /// <summary>
        ///     Reads a bigint output parameter
        /// </summary>
        private static long GetInt64(SqlParameter parameter)
        {
            return IsEmpty(parameter) ? 0L : (long)parameter.Value;
        }

        /// <summary>
        ///     Reads a smallint output parameter
        /// </summary>
        private static short GetInt16(SqlParameter parameter)
        {
            return IsEmpty(parameter) ? (short)0 : (short)parameter.Value;
        }

        /// <summary>
        ///     Reads a tinyint output parameter
        /// </summary>
        private static byte GetByte(SqlParameter parameter)
        {
            return IsEmpty(parameter) ? (byte)0 : (byte)parameter.Value;
        }

        /// <summary>
        ///     The parameter carries no value: the procedure did not reach the assignment
        /// </summary>
        private static bool IsEmpty(SqlParameter parameter)
        {
            return parameter?.Value == null || parameter.Value == System.DBNull.Value;
        }

        /// <summary>
        ///     Reads an int column that may be null (a LEFT JOIN column or a DATEDIFF over a null date)
        /// </summary>
        private static int GetInt32OrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }

        /// <summary>
        ///     Reads an item serial column: the serial counter of the database is a signed 32-bit
        ///     identity, and the procedures carry its value both as an int and as a bigint
        /// </summary>
        private static long GetSerialNo(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0L : System.Convert.ToInt64(reader.GetValue(ordinal));
        }

        /// <summary>
        ///     Reads a tinyint column that may be null (a LEFT JOIN column)
        /// </summary>
        private static byte GetByteOrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (byte)0 : reader.GetByte(ordinal);
        }

        /// <summary>
        ///     Reads a string column, char(N) values come back padded with spaces, LEFT JOIN
        ///     columns come back null
        /// </summary>
        private static string GetTrimmedString(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).TrimEnd();
        }
    }
}

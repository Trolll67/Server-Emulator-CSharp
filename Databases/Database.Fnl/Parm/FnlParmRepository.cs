using System.Collections.Generic;
using Database.Fnl.Sql;
using Microsoft.Data.SqlClient;

namespace Database.Fnl.Parm
{
    /// <inheritdoc/>
    public class FnlParmRepository : IFnlParmRepository
    {
        /// <summary>
        ///     Column ordinals of the dbo.UspGetParmSvr result set.
        ///     Read by ordinal on purpose: the procedure returns RTRIM([mDesc]) and two computed
        ///     columns without any alias, so they simply have no name to read them by
        /// </summary>
        private const int ParmSvrSvrNo = 0;
        private const int ParmSvrWorldNo = 1;
        private const int ParmSvrTcpPort = 9;
        private const int ParmSvrUdpPort = 10;
        private const int ParmSvrDesc = 14;

        /// <summary>
        ///     Column ordinals of the dbo.UspGetFamilyEx result set.
        ///     RTRIM([mMajorIp]) is selected twice: the original binds the first copy as the client
        ///     address (__mPcIp) and the second one as the internal address (__mSvrIp), see
        ///     FnlFw::CSqlParmSvr::__PrepareSelectFamily. In the shipped TblParmSvr both are mMajorIp,
        ///     so only the first one is mapped
        /// </summary>
        private const int FamilySvrNo = 0;
        private const int FamilyType = 1;
        private const int FamilyTcpPort = 2;
        private const int FamilyUdpPort = 3;
        private const int FamilyMajorIp = 4;
        private const int FamilyDesc = 6;
        private const int FamilyWorldNo = 7;

        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="connectionFactory"></param>
        public FnlParmRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc/>
        public ParmServerRow GetParmSvr(ParmServerType type, string majorIp)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetParmSvr");

            StoredProcedure.AddInTinyInt(command, "@pType", (byte)type);
            StoredProcedure.AddInChar(command, "@pMajorIp", 15, majorIp);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            // No row means there is no valid server of that kind on that address, that is not an error
            if (!reader.Read())
            {
                return null;
            }

            return new ParmServerRow
            {
                SvrNo = reader.GetInt16(ParmSvrSvrNo),
                WorldNo = GetInt16OrZero(reader, ParmSvrWorldNo),
                TcpPort = GetPort(reader, ParmSvrTcpPort),
                UdpPort = GetPort(reader, ParmSvrUdpPort),
                Desc = GetTrimmedString(reader, ParmSvrDesc)
            };
        }

        /// <inheritdoc/>
        public IReadOnlyList<FamilyServerRow> GetFamily(short svrNo)
        {
            using SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlParm);
            using SqlCommand command = StoredProcedure.Create(connection, "dbo.UspGetFamilyEx");

            StoredProcedure.AddInSmallInt(command, "@pSvrNo", svrNo);

            connection.Open();

            List<FamilyServerRow> rows = new List<FamilyServerRow>();

            // An unknown svrNo makes the procedure raise 'Invalid SvrNo(%d)' with severity 11,
            // the SqlException is left to the caller on purpose
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new FamilyServerRow
                {
                    SvrNo = reader.GetInt16(FamilySvrNo),
                    Type = (ParmServerType)reader.GetByte(FamilyType),
                    TcpPort = GetPort(reader, FamilyTcpPort),
                    UdpPort = GetPort(reader, FamilyUdpPort),
                    MajorIp = GetTrimmedString(reader, FamilyMajorIp),
                    Desc = GetTrimmedString(reader, FamilyDesc),
                    WorldNo = GetInt16OrZero(reader, FamilyWorldNo)
                });
            }

            return rows;
        }

        /// <summary>
        ///     Reads a port column, char(N)/smallint columns of TblParmSvr are never null
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ordinal"></param>
        /// <returns>
        ///     Port as an unsigned value: mTcpPort and mUdpPort are smallint, so everything above
        ///     32767 is stored negative. Casting back to short gives the original bytes again
        /// </returns>
        private static int GetPort(SqlDataReader reader, int ordinal)
        {
            return (ushort)reader.GetInt16(ordinal);
        }

        /// <summary>
        ///     Reads a smallint column, TblParmSvr.mWorldNo is nullable
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ordinal"></param>
        /// <returns>Column value or 0 when it is null</returns>
        private static short GetInt16OrZero(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (short)0 : reader.GetInt16(ordinal);
        }

        /// <summary>
        ///     Reads a string column, char(N) values come back padded with spaces
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="ordinal"></param>
        /// <returns>Value without trailing spaces or null</returns>
        private static string GetTrimmedString(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).TrimEnd();
        }
    }
}

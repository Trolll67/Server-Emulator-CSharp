using System;
using Database.Fnl.Sql;
using Microsoft.Data.SqlClient;

namespace Database.Fnl.Account
{
    /// <inheritdoc/>
    public class FnlAccountRepository : IFnlAccountRepository
    {
        /// <summary>
        ///     The Chinese variant is used on purpose: UspCertifyUser_KR additionally requires
        ///     TblUser.mAccountGuid, and on the live FNLAccount it is zero for every row,
        ///     so _KR would reject any login with eErrNoAuthInvalid
        /// </summary>
        private const string CertifyUserProcedure = "dbo.UspCertifyUser_CN";

        private const string UpdateCertifiedKeyProcedure = "dbo.UspUpdateCertifiedKey";

        private const string LoginUserProcedure = "dbo.UspLoginUser";

        private const string LogoutUserProcedure = "dbo.UspLogoutUser";

        private readonly ISqlConnectionFactory _connectionFactory;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="connectionFactory"></param>
        public FnlAccountRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc/>
        public CertifyUserResult CertifyUser(CertifyUserRequest request)
        {
            using (SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlAccount))
            using (SqlCommand command = StoredProcedure.Create(connection, CertifyUserProcedure))
            {
                // Order and types are taken from sys.parameters of the live procedure
                StoredProcedure.AddInVarChar(command, "@pUserId", 20, request.UserId);
                StoredProcedure.AddInVarChar(command, "@pUserPswd", 20, request.Password);
                StoredProcedure.AddInChar(command, "@pIp", 15, request.Ip);
                StoredProcedure.AddInBigInt(command, "@pIpEX", request.IpEx);
                StoredProcedure.AddInInt(command, "@pCertifiedKey", request.CertifiedKey);
                StoredProcedure.AddInBit(command, "@pIsEqualRsc", request.IsEqualRsc);
                StoredProcedure.AddInInt(command, "@pPcBangLv", request.PcBangLv);

                SqlParameter userNo = StoredProcedure.AddOutInt(command, "@pUserNo");
                SqlParameter worldNo = StoredProcedure.AddOutSmallInt(command, "@pWorldNo");
                SqlParameter errNoStr = StoredProcedure.AddOutVarChar(command, "@pErrNoStr", 50);
                SqlParameter certify = StoredProcedure.AddOutDateTime(command, "@pCertify");
                SqlParameter certifyReason = StoredProcedure.AddOutVarChar(command, "@pCertifyReason", 200);
                SqlParameter secKeyTableUse = StoredProcedure.AddOutTinyInt(command, "@pSecKeyTableUse");
                SqlParameter userAuth = StoredProcedure.AddOutTinyInt(command, "@pUserAuth");

                StoredProcedure.AddInBit(command, "@pIsAddUser", request.IsAddUser);
                StoredProcedure.AddInBit(command, "@pIsPwdCheck", request.IsPwdCheck);
                StoredProcedure.AddInVarChar(command, "@pJoinCode", 1, request.JoinCode);
                StoredProcedure.AddInChar(command, "@pLoginChannelID", 1, request.LoginChannelId);
                StoredProcedure.AddInChar(command, "@pTired", 1, request.Tired);
                StoredProcedure.AddInChar(command, "@pChnSID", 33, request.ChnSid);

                connection.Open();
                command.ExecuteNonQuery();

                CertifyUserResult result = new CertifyUserResult
                {
                    ReturnCode = StoredProcedure.ReturnValue(command),
                    UserNo = GetInt32(userNo),
                    WorldNo = GetInt16(worldNo),
                    BlockedUntil = GetDateTime(certify),
                    BlockReason = StoredProcedure.GetTrimmedString(certifyReason),
                    SecKeyTableUse = GetByte(secKeyTableUse),
                    UserAuth = GetByte(userAuth)
                };

                // On success @pErrNoStr still holds its initial 'eErrNoSqlInternalError':
                // the procedure sets it once at the start and never resets it
                if (!result.IsSuccess)
                {
                    result.ErrNo = StoredProcedure.GetTrimmedString(errNoStr);
                }

                return result;
            }
        }

        /// <inheritdoc/>
        public UpdateCertifiedKeyResult UpdateCertifiedKey(int userNo, int certifiedKey)
        {
            using (SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlAccount))
            using (SqlCommand command = StoredProcedure.Create(connection, UpdateCertifiedKeyProcedure))
            {
                StoredProcedure.AddInInt(command, "@pUserNo", userNo);
                StoredProcedure.AddInInt(command, "@pCertifiedKey", certifiedKey);

                SqlParameter userId = StoredProcedure.AddOutChar(command, "@pUserId", 20);
                SqlParameter userAuth = StoredProcedure.AddOutTinyInt(command, "@pUserAuth");
                SqlParameter secKeyTableUse = StoredProcedure.AddOutTinyInt(command, "@pSecKeyTableUse");
                SqlParameter pcBangLv = StoredProcedure.AddOutInt(command, "@pPCBangLv");

                connection.Open();
                command.ExecuteNonQuery();

                return new UpdateCertifiedKeyResult
                {
                    ReturnCode = StoredProcedure.ReturnValue(command),
                    UserId = StoredProcedure.GetTrimmedString(userId),
                    UserAuth = GetByte(userAuth),
                    SecKeyTableUse = GetByte(secKeyTableUse),
                    PcBangLv = GetInt32(pcBangLv)
                };
            }
        }

        /// <inheritdoc/>
        public LoginUserResult LoginUser(LoginUserRequest request)
        {
            using (SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlAccount))
            using (SqlCommand command = StoredProcedure.Create(connection, LoginUserProcedure))
            {
                // Order and types are taken from sys.parameters of the live procedure.
                // Every parameter is bound in the declaration order, even the outputs the
                // login slice does not read, to keep the positional contract of the original
                StoredProcedure.AddInInt(command, "@pUserNo", request.UserNo);
                StoredProcedure.AddInInt(command, "@pCertifiedKey", request.CertifiedKey);
                StoredProcedure.AddInChar(command, "@pIp", 15, request.Ip);
                StoredProcedure.AddInSmallInt(command, "@pWorldNo", request.WorldNo);
                StoredProcedure.AddInBigInt(command, "@pIpEX", request.IpEx);
                StoredProcedure.AddInInt(command, "@pPcBangLvEX", request.PcBangLvEx);
                StoredProcedure.AddInBit(command, "@pIsNonClt", request.IsNonClt);

                SqlParameter userId = StoredProcedure.AddOutVarChar(command, "@pUserId", 20);
                SqlParameter userAuth = StoredProcedure.AddOutTinyInt(command, "@pUserAuth");
                SqlParameter errNoStr = StoredProcedure.AddOutVarChar(command, "@pErrNoStr", 50);
                StoredProcedure.AddOutInt(command, "@pLeftChat");
                StoredProcedure.AddOutDateTime(command, "@pEndBoard");
                StoredProcedure.AddOutInt(command, "@pPcBangLv");
                SqlParameter useMacro = StoredProcedure.AddOutSmallInt(command, "@pUseMacro");
                StoredProcedure.AddOutInt(command, "@pIpEXUserNo");
                StoredProcedure.AddOutChar(command, "@pJoinCode", 1);
                StoredProcedure.AddOutChar(command, "@pTired", 1);
                StoredProcedure.AddOutChar(command, "@pChnSID", 33);
                StoredProcedure.AddOutBit(command, "@pNewId");

                StoredProcedure.AddInTinyInt(command, "@pSvrInfo", request.SvrInfo);
                StoredProcedure.AddInInt(command, "@pNewCertifiedKey", request.NewCertifiedKey);

                SqlParameter newCertifiedKey = StoredProcedure.AddOutInt(command, "@pNewCertifiedKeyOutput");
                SqlParameter secKeyState = StoredProcedure.AddOutTinyInt(command, "@pSecKeyState");

                connection.Open();
                command.ExecuteNonQuery();

                LoginUserResult result = new LoginUserResult
                {
                    ReturnCode = StoredProcedure.ReturnValue(command),
                    UserId = StoredProcedure.GetTrimmedString(userId),
                    UserAuth = GetByte(userAuth),
                    SecKeyState = GetByte(secKeyState),
                    NewCertifiedKey = GetInt32(newCertifiedKey),
                    UseMacro = GetInt16(useMacro)
                };

                // On success @pErrNoStr still holds its initial 'eErrNoSqlInternalError':
                // the procedure sets it once at the start and never resets it
                if (!result.IsSuccess)
                {
                    result.ErrNo = StoredProcedure.GetTrimmedString(errNoStr);
                }

                return result;
            }
        }

        /// <inheritdoc/>
        public LogoutUserResult LogoutUser(int userNo, int chatBlockApplyTime, short useMacro)
        {
            using (SqlConnection connection = _connectionFactory.Create(FnlConnectionNames.FnlAccount))
            using (SqlCommand command = StoredProcedure.Create(connection, LogoutUserProcedure))
            {
                StoredProcedure.AddInInt(command, "@pUserNo", userNo);
                StoredProcedure.AddInInt(command, "@pUserChatBlockApplyTime", chatBlockApplyTime);
                StoredProcedure.AddInSmallInt(command, "@pUseMacro", useMacro);

                connection.Open();
                command.ExecuteNonQuery();

                return new LogoutUserResult
                {
                    ReturnCode = StoredProcedure.ReturnValue(command)
                };
            }
        }

        /// <summary>
        ///     Reads an int output parameter, the procedures leave it untouched (NULL) on the error paths
        /// </summary>
        private static int GetInt32(SqlParameter parameter)
        {
            return IsEmpty(parameter) ? 0 : (int)parameter.Value;
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
        ///     Reads a datetime output parameter, NULL means there is no block on the account
        /// </summary>
        private static DateTime? GetDateTime(SqlParameter parameter)
        {
            return IsEmpty(parameter) ? (DateTime?)null : (DateTime)parameter.Value;
        }

        /// <summary>
        ///     The parameter carries no value: the procedure did not reach the assignment
        /// </summary>
        private static bool IsEmpty(SqlParameter parameter)
        {
            return parameter?.Value == null || parameter.Value == DBNull.Value;
        }
    }
}

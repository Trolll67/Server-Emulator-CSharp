using System;
using System.Net;
using Database.Fnl.Account;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Login.Models.Receive;
using Packets.Server.Login.Models.Send;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Core.Handlers.Interfaces;
using Server.Login.Models.Login;
using Server.Login.Network;

namespace Server.Login.Core.Handlers
{
    /// <inheritdoc />
    [Handler]
    public class AuthorizationHandler : IAuthorizationHandler
    {
        /// <summary>
        ///     Source of the session keys: RAND() of the database is a float, so the original channel
        ///     generates mCertifiedKey outside of SQL and passes it into UspCertifyUser_CN
        /// </summary>
        private static readonly Random CertifiedKeyRandom = new Random();

        private readonly IFnlAccountRepository _accountRepository;
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly IServersFactory _serversFactory;
        private readonly ILogger<AuthorizationHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="accountRepository"></param>
        /// <param name="authorizationFactory"></param>
        /// <param name="serversFactory"></param>
        /// <param name="logger"></param>
        public AuthorizationHandler(IFnlAccountRepository accountRepository, IAuthorizationFactory authorizationFactory, IServersFactory serversFactory, ILogger<AuthorizationHandler> logger)
        {
            _accountRepository = accountRepository;
            _authorizationFactory = authorizationFactory;
            _serversFactory = serversFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.AuthorizationLogin)]
        public void AuthorizationLoginHandle(LoginSession loginSession, AuthorizationLoginModel authorizationLoginModel)
        {
            int certifiedKey = NextCertifiedKey();

            CertifyUserResult certifyUser;

            try
            {
                certifyUser = _accountRepository.CertifyUser(new CertifyUserRequest
                {
                    UserId = authorizationLoginModel.Login,
                    Password = authorizationLoginModel.Password,
                    Ip = GetClientIp(loginSession),
                    CertifiedKey = certifiedKey
                });
            }
            catch (SqlException e)
            {
                // FNLAccount is down or the login timed out. Without an answer the client hangs on the
                // login screen forever, so the session gets the same code as an unknown account
                // and the login server keeps running
                _logger.LogError(e, $"Can not certify {authorizationLoginModel.Login}, FNLAccount is not readable");

                _authorizationFactory.SendError(loginSession, ServerErrorType.NoUser);
                return;
            }
            catch (InvalidOperationException e)
            {
                // SqlConnectionFactory throws it when the connection string of FNLAccount is not set.
                // The string itself is never written anywhere: it carries the sa password
                _logger.LogError(e, $"Can not certify {authorizationLoginModel.Login}, the connection to FNLAccount is not configured");

                _authorizationFactory.SendError(loginSession, ServerErrorType.NoUser);
                return;
            }

            // Only the return code is trustworthy: on the error paths the procedure leaves
            // its output parameters with whatever they happened to hold
            if (!certifyUser.IsSuccess)
            {
                _authorizationFactory.SendError(loginSession, GetErrorType(certifyUser));
                return;
            }

            loginSession.SessionLogin = new SessionLoginModel
            {
                UserNo = certifyUser.UserNo,
                UserId = authorizationLoginModel.Login,
                CertifiedKey = certifiedKey,
                WorldNo = certifyUser.WorldNo
            };

            _logger.LogInformation($"Account {authorizationLoginModel.Login} certified with mUserNo {certifyUser.UserNo}");

            _serversFactory.SendServers(loginSession);
        }

        /// <summary>
        ///     Turns the error of UspCertifyUser_CN into the code the client understands
        /// </summary>
        /// <param name="certifyUser">Result with a non zero return code</param>
        /// <returns></returns>
        private ServerErrorType GetErrorType(CertifyUserResult certifyUser)
        {
            switch (certifyUser.ErrNo)
            {
                case "eErrNoUserNotExistId3":
                    return ServerErrorType.NoUser;

                case "eErrNoUserDiffPswd":
                    return ServerErrorType.PasswordWrong;

                case "eErrNoUserChkAlreadyLogined":
                case "eErrNoUserLoginAnother":
                    return ServerErrorType.NoUserLoginAnother;

                default:
                    // Blocks, not activated accounts and resource mismatches have no own code in the packet
                    _logger.LogWarning($"UspCertifyUser_CN answered {certifyUser.ErrNo} with the return code {certifyUser.ReturnCode}");
                    return ServerErrorType.NoUser;
            }
        }

        /// <summary>
        ///     Address of the client, the procedure writes it into TblUser.mIp char(15)
        /// </summary>
        /// <param name="loginSession"></param>
        /// <returns></returns>
        private static string GetClientIp(LoginSession loginSession)
        {
            return loginSession.Socket?.RemoteEndPoint is IPEndPoint endPoint ? endPoint.Address.ToString() : string.Empty;
        }

        /// <summary>
        ///     Generates the next session key. Random is not thread safe and the sessions are served
        ///     by the socket threads, so the generator is used under a lock
        /// </summary>
        /// <returns>Positive value for TblUser.mCertifiedKey</returns>
        private static int NextCertifiedKey()
        {
            lock (CertifiedKeyRandom)
            {
                return CertifiedKeyRandom.Next(1, int.MaxValue);
            }
        }
    }
}

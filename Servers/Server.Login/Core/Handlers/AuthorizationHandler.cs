using System;
using System.Net;
using Database.Fnl.Account;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Core.Models.Common;
using Packets.Server.Login.Models.Receive;
using Packets.Server.Login.Models.Send;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Core.Handlers.Interfaces;
using Server.Login.Models.Login;
using Server.Login.Models.Settings;
using Server.Login.Network;
using Server.Login.Services;
using Server.Login.Services.Family;

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
        private readonly FamilyRegistry _familyRegistry;
        private readonly OwnChannelInfo _ownChannelInfo;
        private readonly LoginSetting _loginSetting;
        private readonly ILogger<AuthorizationHandler> _logger;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public AuthorizationHandler(IFnlAccountRepository accountRepository, IAuthorizationFactory authorizationFactory, IServersFactory serversFactory, FamilyRegistry familyRegistry, OwnChannelInfo ownChannelInfo, IOptions<LoginSetting> loginSetting, ILogger<AuthorizationHandler> logger)
        {
            _accountRepository = accountRepository;
            _authorizationFactory = authorizationFactory;
            _serversFactory = serversFactory;
            _familyRegistry = familyRegistry;
            _ownChannelInfo = ownChannelInfo;
            _loginSetting = loginSetting.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        [HandlerAction(PacketType.AuthorizationLogin)]
        public void AuthorizationLoginHandle(LoginSession loginSession, AuthorizationLoginModel authorizationLoginModel)
        {
            // The order of the checks below is the order of the original, and it is the order the
            // player sees: the first reason that fires is the one that reaches the login screen

            // A session that is already certified, or one that belongs to a server of this world,
            // has no business logging in. The original answers such a session with nothing at all
            if (loginSession.SessionLogin != null || loginSession.FamilySvrNo.HasValue)
            {
                _logger.LogWarning("Session {Session} sends the login twice, it is already {Who}",
                    loginSession.Id,
                    loginSession.FamilySvrNo.HasValue ? "server " + loginSession.FamilySvrNo : "account " + loginSession.SessionLogin?.UserId);

                return;
            }

            // The packet has to be put together the way the client puts it together
            NakErrorType broken = authorizationLoginModel.CheckIntegrity(_loginSetting.MaxLoginLength, _loginSetting.MaxPasswordLength);

            if (broken != null)
            {
                _logger.LogWarning("Login packet from {Ip} is not whole: {Reason}", GetClientIp(loginSession), broken);

                _authorizationFactory.SendError(loginSession, broken);
                return;
            }

            // Build of the client. The check is off while the expected build is not configured:
            // the number lives in the files of the client, and a wrong one here locks everybody out
            if (_loginSetting.ClientVersion != 0 && authorizationLoginModel.Version != _loginSetting.ClientVersion)
            {
                _logger.LogWarning("Account {Login} runs the build {Actual}, this channel takes {Expected}",
                    authorizationLoginModel.Login, authorizationLoginModel.Version, _loginSetting.ClientVersion);

                _authorizationFactory.SendError(loginSession, NakErrorType.VerInvalid);
                return;
            }

            string ip = GetClientIp(loginSession);

            if (!IsAddressAllowed(loginSession, authorizationLoginModel.Login, ip))
            {
                return;
            }

            if (!IsWorldReachable(loginSession, authorizationLoginModel.Login))
            {
                return;
            }

            Certify(loginSession, authorizationLoginModel, ip);
        }

        /// <summary>
        ///     Asks the block list of addresses about the client, as the original does before it
        ///     looks at the account at all
        /// </summary>
        /// <returns>True when the login may go on</returns>
        private bool IsAddressAllowed(LoginSession loginSession, string login, string ip)
        {
            IpCheckResult check;

            try
            {
                check = _accountRepository.IsValidIp(GetAddressNumber(loginSession));
            }
            catch (SqlException e)
            {
                // An older FNLAccount has no such procedure at all. Refusing every login because
                // of a list that does not exist is worse than letting them through, so the reason
                // is written down and the login goes on
                _logger.LogError(e, "Can not ask the block list of addresses about {Ip}, the login of {Login} goes on", ip, login);

                return true;
            }

            if (check == IpCheckResult.Allowed)
            {
                return true;
            }

            NakErrorType error = check == IpCheckResult.Blocked ? NakErrorType.IpBlocked : NakErrorType.SqlInternalError;

            _logger.LogInformation("Account {Login} is refused from {Ip}: {Reason}", login, ip, error);

            _authorizationFactory.SendError(loginSession, error);

            return false;
        }

        /// <summary>
        ///     An account belongs to a world, and this channel serves one. The original lets an
        ///     account of another world in only when it knows a server of that world
        /// </summary>
        /// <returns>True when the login may go on</returns>
        private bool IsWorldReachable(LoginSession loginSession, string login)
        {
            RetrieveWorldNoResult world;

            try
            {
                world = _accountRepository.RetrieveWorldNo(login);
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Can not read the world of {Login} from FNLAccount, the login goes on", login);

                return true;
            }

            if (!world.IsSuccess)
            {
                _logger.LogError("UspRetrieveWorldNo answered {Code} for {Login}", world.ReturnCode, login);

                _authorizationFactory.SendError(loginSession, NakErrorType.SqlInternalError);
                return false;
            }

            // An account that has not been to any world yet, and one of this very world, are both
            // at home here
            if (world.WorldNo == 0 || world.WorldNo == _ownChannelInfo.WorldNo || _familyRegistry.HasWorld(world.WorldNo))
            {
                return true;
            }

            _logger.LogInformation("Account {Login} belongs to world {World}, this channel serves {Own} and knows no server of that world",
                login, world.WorldNo, _ownChannelInfo.WorldNo);

            _authorizationFactory.SendError(loginSession, NakErrorType.NoUserLoginAnother);

            return false;
        }

        /// <summary>
        ///     The login itself, through the procedure of the original
        /// </summary>
        private void Certify(LoginSession loginSession, AuthorizationLoginModel authorizationLoginModel, string ip)
        {
            int certifiedKey = NextCertifiedKey();

            CertifyUserResult certifyUser;

            try
            {
                certifyUser = _accountRepository.CertifyUser(new CertifyUserRequest
                {
                    UserId = authorizationLoginModel.Login,
                    Password = authorizationLoginModel.Password,
                    Ip = ip,
                    IpEx = GetAddressNumber(loginSession),
                    CertifiedKey = certifiedKey,

                    // Option 54 'Certify To Password In DB' of TblParmSvrOp. With it off the client
                    // does not send a readable password at all, so checking it would reject every login
                    IsPwdCheck = _serversFactory.IsPasswordCheckedInDatabase(),

                    // Option 53 'Do Not Account Automatic Creation' of the same table, negated:
                    // with it off the procedure creates the account instead of answering
                    // eErrNoUserNotExistId3, which is how the original channel fills an empty TblUser
                    IsAddUser = _serversFactory.IsAccountCreatedOnLogin()
                });
            }
            catch (SqlException e)
            {
                // FNLAccount is down or the login timed out. Without an answer the client hangs on the
                // login screen forever, so the session gets the reason the original uses for a
                // database that did not answer, and the login server keeps running
                _logger.LogError(e, $"Can not certify {authorizationLoginModel.Login}, FNLAccount is not readable");

                _authorizationFactory.SendError(loginSession, NakErrorType.SqlInternalError);
                return;
            }
            catch (InvalidOperationException e)
            {
                // SqlConnectionFactory throws it when the connection string of FNLAccount is not set.
                // The string itself is never written anywhere: it carries the sa password
                _logger.LogError(e, $"Can not certify {authorizationLoginModel.Login}, the connection to FNLAccount is not configured");

                _authorizationFactory.SendError(loginSession, NakErrorType.SqlInternalError);
                return;
            }

            // Only the return code is trustworthy: on the error paths the procedure leaves
            // its output parameters with whatever they happened to hold
            if (!certifyUser.IsSuccess)
            {
                SendCertifyError(loginSession, authorizationLoginModel.Login, certifyUser);
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
        ///     Turns the answer of UspCertifyUser_CN into the refusal the client shows. The original
        ///     does not know the reasons of the procedure by name either: it counts the number of
        ///     whatever name came back and sends that, and the client finds the text by the number.
        ///     So every reason the procedure has ever been taught reaches the player without being
        ///     listed anywhere here
        /// </summary>
        private void SendCertifyError(LoginSession loginSession, string login, CertifyUserResult certifyUser)
        {
            NakErrorType error = string.IsNullOrWhiteSpace(certifyUser.ErrNo)
                ? NakErrorType.NoUserNotExistId
                : NakErrorType.FromName(certifyUser.ErrNo.Trim());

            // Без этой строки отказ виден только на клиенте: пакет 3102 несёт один код ошибки
            // и не говорит, что именно ответила процедура
            _logger.LogInformation($"Account {login} is not certified: UspCertifyUser_CN answered {certifyUser.ErrNo} with the return code {certifyUser.ReturnCode}, the client gets {error}");

            _authorizationFactory.SendError(loginSession, new LoginServerErrorModel
            {
                Error = error,

                // The procedure fills both of these on the paths about a ban, and the client shows
                // the text of the procedure instead of the one it has for the reason
                EndCertify = certifyUser.BlockedUntil,
                Reason = certifyUser.BlockReason
            });
        }

        /// <summary>
        ///     Address of the client, the procedure writes it into TblUser.mIp char(15)
        /// </summary>
        private static string GetClientIp(LoginSession loginSession)
        {
            return loginSession.Socket?.RemoteEndPoint is IPEndPoint endPoint ? endPoint.Address.ToString() : string.Empty;
        }

        /// <summary>
        ///     Address of the client the way the original counts it for the database: the four
        ///     parts packed in decimal, so 192.168.0.1 becomes 192168000001
        /// </summary>
        private static long GetAddressNumber(LoginSession loginSession)
        {
            if (!(loginSession.Socket?.RemoteEndPoint is IPEndPoint endPoint))
            {
                return 0;
            }

            byte[] parts = endPoint.Address.MapToIPv4().GetAddressBytes();

            return ((parts[0] * 1000L + parts[1]) * 1000L + parts[2]) * 1000L + parts[3];
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

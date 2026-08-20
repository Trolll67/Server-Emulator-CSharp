using System;
using System.Linq;
using System.Net;
using Database.Fnl.Account;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Receive;
using Packets.Server.Game.Models.Send;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Models.Settings;
using Server.Game.Network;
using Server.Game.Services;
using Server.Game.Services.Database;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class AuthorizationHandler : IAuthorizationHandler
    {
        /// <summary>
        ///     Generator of the session keys the world server rotates on every UspLoginUser call,
        ///     the same way the login server issues the first key in UspCertifyUser_CN
        /// </summary>
        private static readonly System.Random CertifiedKeyRandom = new System.Random();

        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly ICharacterFactory _characterFactory;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IErrorFactory _commonFactory;
        private readonly IFnlAccountRepository _accountRepository;
        private readonly GameRepository _gameRepository;
        private readonly DBGameMappingService _dbGameMappingService;
        private readonly ParmRepository _parmRepository;
        private readonly IdentificationService _identificationService;
        private readonly OwnServerInfo _ownServerInfo;
        private readonly LogoutService _logoutService;
        private readonly ILogger<AuthorizationHandler> _logger;

        public AuthorizationHandler(IAuthorizationFactory authorizationFactory, ICharacterFactory characterFactory, ICharacteristicFactory characteristicFactory, IErrorFactory commonFactory, IFnlAccountRepository accountRepository, GameRepository gameRepository, DBGameMappingService dbGameMappingService, ParmRepository parmRepository, IdentificationService identificationService, OwnServerInfo ownServerInfo, LogoutService logoutService, ILogger<AuthorizationHandler> logger)
        {
            _authorizationFactory = authorizationFactory;
            _characterFactory = characterFactory;
            _characteristicFactory = characteristicFactory;
            _commonFactory = commonFactory;
            _accountRepository = accountRepository;
            _gameRepository = gameRepository;
            _dbGameMappingService = dbGameMappingService;
            _parmRepository = parmRepository;
            _identificationService = identificationService;
            _ownServerInfo = ownServerInfo;
            _logoutService = logoutService;
            _logger = logger;
        }

        [HandlerAction(PacketType.LoginUserReq)]
        public void Authorization(GameSession client, LoginUserReqModel model)
        {
            int userNo = (int)model.AccountId;

            // The world server no longer reads a Sessions table: the session key issued by the login
            // server (mCertifiedKey) is re-checked through dbo.UspLoginUser. The 5100 packet carries
            // mUserNo in AccountId and mCertifiedKey in SessionId (see Server.Login AuthorizationHandler)
            LoginUserResult loginResult;

            try
            {
                loginResult = _accountRepository.LoginUser(new LoginUserRequest
                {
                    UserNo = userNo,
                    CertifiedKey = model.SessionId,
                    Ip = GetClientIp(client),
                    WorldNo = _ownServerInfo.WorldNo,
                    SvrInfo = 0, // general server, not the Chaos Battle Server
                    IpEx = 0,
                    PcBangLvEx = 0,
                    IsNonClt = false,

                    // UspLoginUser unconditionally overwrites TblUser.mCertifiedKey with this value
                    // even before it compares the keys; passing zero would leave a key no later
                    // re-authorization can match, so the key is rotated like the original does
                    NewCertifiedKey = NextCertifiedKey()
                });
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                // FNLAccount is down, the connection is not configured, or the procedure itself failed
                // — UspLoginUser reaches into FNLBilling, so the login of the database matters there too.
                // Without an answer the client hangs on the loading screen forever, so it gets the same
                // code as a wrong session key and the world server keeps running
                _logger.LogError(e, $"Can not log mUserNo {userNo} into world {_ownServerInfo.WorldNo}, UspLoginUser failed");

                _commonFactory.SendServerError(client, PacketType.LoginUserReq, GameServerErrorType.NoUserNotLogin, true);
                return;
            }

            // Only the return code is trustworthy: a wrong key or an unknown account fails here
            if (!loginResult.IsSuccess)
            {
                _commonFactory.SendServerError(client, PacketType.LoginUserReq, GameServerErrorType.NoUserNotLogin, true);
                return;
            }

            // Build the domain session from the procedure result and load the selection screen
            GSession sessionGame = new GSession();
            _dbGameMappingService.MapSession(sessionGame, loginResult, userNo, _ownServerInfo.SvrNo);

            client.Sessions = sessionGame;

            // The client may have dropped the socket while UspLoginUser was running: by that
            // moment the disconnect had no session to log out, so the mark set by the procedure
            // has to be cleared here, or the account stays "in the world" forever
            if (!client.IsConnected)
            {
                _logoutService.Logout(client);
                return;
            }

            client.Pcs = _gameRepository.GetPcsByAccountId(userNo);

            _authorizationFactory.SendServerTime(client);
            _authorizationFactory.SendGameConfiguration(client);
            _characterFactory.SendInformationCharacters(client);
        }

        [HandlerAction(PacketType.ChoosePcReq)]
        public void EnterWorld(GameSession client, ChoosePcReqModel model)
        {
            // The chosen character must belong to the account's selection list
            GPc characterGame = client.Pcs.FirstOrDefault(c => c.Simple.PcNo == model.PcNo);

            if (characterGame == null)
            {
                _commonFactory.SendServerError(client, PacketType.ChoosePcReq, GameServerErrorType.NoCharInvalidNo, true);
                return;
            }

            // Load the selected character on the way into the world: UspLoginPc + the loader procedures
            client.Pc = _gameRepository.LoadPc(client.Sessions.AccountId, (int)model.PcNo, GetClientIp(client));

            if (client.Pc == null)
            {
                _commonFactory.SendServerError(client, PacketType.ChoosePcReq, GameServerErrorType.NoCharInvalidNo, true);
                return;
            }

            // Register session
            _identificationService.AddConnection(client);

            // Get exp by level TODO
            GExp expGame = _parmRepository.GetExpByLvl(client.Pc.Simple.Level);

            _authorizationFactory.SendCompleteEnterWorld(client);
            _characteristicFactory.SendInformationAbilityCharacteristics(client);
            _characteristicFactory.SendHealthPointCharacteristics(client);
            _characteristicFactory.SendSpeedCharacteristics(client, client);
            _characteristicFactory.SendInfoWeight(client);
            _characteristicFactory.SendInfoExp(client, expGame);
        }

        /// <summary>
        ///     Generates the next session key. Random is not thread safe and the sessions are
        ///     served by the socket threads, so the generator is used under a lock
        /// </summary>
        /// <returns>Positive value for TblUser.mCertifiedKey</returns>
        private static int NextCertifiedKey()
        {
            lock (CertifiedKeyRandom)
            {
                return CertifiedKeyRandom.Next(1, int.MaxValue);
            }
        }

        /// <summary>
        ///     Address of the client, the procedures write it into TblUser.mIp / TblPc.mIp
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static string GetClientIp(GameSession client)
        {
            return client.Socket?.RemoteEndPoint is IPEndPoint endPoint ? endPoint.Address.ToString() : string.Empty;
        }

        [HandlerAction(PacketType.LogoutPcReq)]
        public void Logout(GameSession client, LogoutPcReqModel model)
        {
            // Leave the world registry first so the autosave and the visibility loops stop
            // touching the character while it is being written out, the same order the
            // disconnect path follows
            _identificationService.RemoveConnection(client);

            // Save the character and clear the login marks (UspLogoutPc, UspLogoutUser), then
            // close the session: the databases already treat the account as offline, so a socket
            // kept open would be a session the world no longer knows about
            _logoutService.Logout(client);
            client.Disconnect();
        }
    }
}
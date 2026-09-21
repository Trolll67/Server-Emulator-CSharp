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
using Packets.Server.Game.Models.Send.Character;
using Packets.Core.Models.Common;
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
            // 5100 belongs to a fresh socket only. A second login on a session that already chose
            // a character would hand out one more selection screen and let a second 5116 through:
            // the world registry would then hold two entries for one socket and the first character
            // would stay in it forever, saved by the autosave and never logged out
            if (client.State != GameSessionState.Connected)
            {
                _commonFactory.SendServerError(client, PacketType.LoginUserReq, NakErrorType.NoUserChkAlreadyLogined, true);
                return;
            }

            int userNo = (int)model.AccountId;

            // The world server no longer reads a Sessions table: the session key issued by the login
            // server (mCertifiedKey) is re-checked through dbo.UspLoginUser. The 5100 packet carries
            // mUserNo in AccountId and mCertifiedKey in SessionId (see Server.Login AuthorizationHandler)
            LoginUserResult loginResult;

            // The rotated key is kept: the procedure writes it into TblUser.mCertifiedKey and the
            // client is told the new value through CertifiedKeyAck (5812), so it presents this one
            // on its next reconnection to a world server
            int newCertifiedKey = NextCertifiedKey();

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
                    NewCertifiedKey = newCertifiedKey
                });
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                // FNLAccount is down, the connection is not configured, or the procedure itself failed
                // — UspLoginUser reaches into FNLBilling, so the login of the database matters there too.
                // Without an answer the client hangs on the loading screen forever, so it gets the same
                // code as a wrong session key and the world server keeps running
                _logger.LogError(e, $"Can not log mUserNo {userNo} into world {_ownServerInfo.WorldNo}, UspLoginUser failed");

                _commonFactory.SendServerError(client, PacketType.LoginUserReq, NakErrorType.NoUserNotLogin, true);
                return;
            }

            // Only the return code is trustworthy: a wrong key or an unknown account fails here
            if (!loginResult.IsSuccess)
            {
                _commonFactory.SendServerError(client, PacketType.LoginUserReq, NakErrorType.NoUserNotLogin, true);
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

            // The account is certified, the client is going to the selection screen: from here
            // 5116 is the only step left before the world
            client.MarkLoggedIn();

            // The order is the one of the original right after a successful login: the new session
            // key, the server tick, the contents block, then the character selection screen
            _authorizationFactory.SendCertifiedKey(client, newCertifiedKey);
            _authorizationFactory.SendServerTime(client);
            _authorizationFactory.SendGameConfiguration(client);
            _characterFactory.SendInformationCharacters(client);
        }

        [HandlerAction(PacketType.ChoosePcReq)]
        public void EnterWorld(GameSession client, ChoosePcReqModel model)
        {
            // 5116 has a place only between the account login and the world: a session that never
            // passed 5100 has no selection list, and a session already in the world would load a
            // second character over the one it plays
            if (client.State != GameSessionState.LoggedIn)
            {
                SendChoosePcNak(client);
                return;
            }

            // The chosen character must belong to the account's selection list
            GPc characterGame = client.Pcs.FirstOrDefault(c => c.Simple.PcNo == model.PcNo);

            if (characterGame == null)
            {
                SendChoosePcNak(client);
                return;
            }

            int userNo = client.Sessions.AccountId;
            int pcNo = (int)model.PcNo;
            GPc characterLoaded;

            // Load the selected character on the way into the world: UspLoginPc + the loader
            // procedures. UspLoginPc marks the character online before the loaders run, so every
            // failure from here on has to be rolled back with UspLogoutPc - otherwise the character
            // stays online in the database and the next attempt is refused
            try
            {
                characterLoaded = _gameRepository.LoadPc(userNo, pcNo, GetClientIp(client));
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                _logger.LogError(e, $"Can not load character {pcNo} of account {userNo} into the world");

                RollbackLoginPc(userNo, pcNo);
                SendChoosePcNak(client);
                return;
            }

            if (characterLoaded == null)
            {
                // UspLoginPc has already run, but the loader procedures returned nothing
                RollbackLoginPc(userNo, pcNo);
                SendChoosePcNak(client);
                return;
            }

            // Get exp by level TODO
            // Taken before 5117: the lookup throws when the parameter table has no row for the
            // level, and after 5117 that turns into "the character is here" followed by a Nak
            GExp expGame;

            try
            {
                expGame = _parmRepository.GetExpByLvl(characterLoaded.Simple.Level);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"No experience row for level {characterLoaded.Simple.Level} of character {pcNo}");

                RollbackLoginPc(userNo, pcNo);
                SendChoosePcNak(client);
                return;
            }

            client.Pc = characterLoaded;

            // The client may have dropped the socket while the loaders were running: at that moment
            // the disconnect had no character to log out, so the mark of UspLoginPc is cleared here
            if (!client.IsConnected)
            {
                client.Pc = null;
                RollbackLoginPc(userNo, pcNo);
                return;
            }

            try
            {
                // Register session. AddConnection also hands out the unique identifier 5117 carries,
                // so the world registry is filled one step earlier than in the original
                // TODO split the identifier allocation from the registration in IdentificationService
                // to place the character into the world strictly after 5117
                _identificationService.AddConnection(client);

                // The disconnect could have run completely between the check above and this
                // registration: its RemoveConnection found nothing back then, so the fresh entry
                // would stay in the registry forever, autosaved on behalf of a socket that is gone
                if (!client.IsConnected)
                {
                    RollbackEnterWorld(client, userNo, pcNo);
                    return;
                }

                // 5117 goes first: it carries the character itself, the client builds the world around it
                _authorizationFactory.SendCompleteEnterWorld(client);

                // The character is announced, from here the session counts as being in the world
                client.EnterWorld();

                _characteristicFactory.SendInformationAbilityCharacteristics(client);
                _characteristicFactory.SendHealthPointCharacteristics(client);
                _characteristicFactory.SendSpeedCharacteristics(client, client);
                _characteristicFactory.SendInfoWeight(client);
                _characteristicFactory.SendInfoExp(client, expGame);

                // 5102 closes the sequence: the client leaves the loading screen only after it
                client.Send(new EnteredWorldAckModel());
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Can not put character {pcNo} of account {userNo} into the world");

                RollbackEnterWorld(client, userNo, pcNo);
                SendChoosePcNak(client);
            }
        }

        /// <summary>
        ///     Undoes a half finished entry into the world in the reverse order: the session stops
        ///     counting as being in the world for the guards, leaves the world registry, forgets the
        ///     character and gives up the online mark UspLoginPc has set
        /// </summary>
        /// <param name="client">Session that failed to enter</param>
        /// <param name="userNo">Account number, @pUserNo</param>
        /// <param name="pcNo">Chosen character number, @pPcNo</param>
        private void RollbackEnterWorld(GameSession client, int userNo, int pcNo)
        {
            client.LeaveWorld();
            _identificationService.RemoveConnection(client);
            client.Pc = null;

            RollbackLoginPc(userNo, pcNo);
        }

        /// <summary>
        ///     Clears the online mark of a character the world failed to take in (UspLogoutPc).
        ///     Never throws: the answer to the client matters more than the second database failure
        /// </summary>
        /// <param name="userNo">Account number, @pUserNo</param>
        /// <param name="pcNo">Chosen character number, @pPcNo</param>
        private void RollbackLoginPc(int userNo, int pcNo)
        {
            try
            {
                _gameRepository.LogoutPc(userNo, pcNo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Can not roll UspLoginPc back for character {pcNo} of account {userNo}");
            }
        }

        /// <summary>
        ///     The common answer to a failed character choice: 1102 with the opcode of 5116 echoed
        ///     back, the client returns to the selection screen
        /// </summary>
        /// <param name="client"></param>
        private void SendChoosePcNak(GameSession client)
        {
            _commonFactory.SendServerError(client, PacketType.ChoosePcReq, NakErrorType.NoCharInvalidNo, true);
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
            // The session no longer plays: the state is dropped for the guards of the world
            // packets, and the registry entry is removed so the autosave and the visibility
            // loops stop touching the character while it is being written out - the same order
            // the disconnect path follows
            client.LeaveWorld();
            _identificationService.RemoveConnection(client);

            // Save the character and clear the login marks (UspLogoutPc, UspLogoutUser), then
            // close the session: the databases already treat the account as offline, so a socket
            // kept open would be a session the world no longer knows about
            _logoutService.Logout(client);
            client.Disconnect();
        }
    }
}
using System.Linq;
using System.Net;
using Database.Fnl.Account;
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
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly ICharacterFactory _characterFactory;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IErrorFactory _commonFactory;
        private readonly IFnlAccountRepository _accountRepository;
        private readonly GameRepository _gameRepository;
        private readonly DBGameMappingService _dbGameMappingService;
        private readonly ParmRepository _parmRepository;
        private readonly IdentificationService _identificationService;
        private readonly GameSetting _gameSetting;

        public AuthorizationHandler(IAuthorizationFactory authorizationFactory, ICharacterFactory characterFactory, ICharacteristicFactory characteristicFactory, IErrorFactory commonFactory, IFnlAccountRepository accountRepository, GameRepository gameRepository, DBGameMappingService dbGameMappingService, ParmRepository parmRepository, IdentificationService identificationService, IOptions<GameSetting> gameSetting)
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
            _gameSetting = gameSetting.Value;
        }

        [HandlerAction(PacketType.LoginUserReq)]
        public void Authorization(GameSession client, LoginUserReqModel model)
        {
            int userNo = (int)model.AccountId;

            // The world server no longer reads a Sessions table: the session key issued by the login
            // server (mCertifiedKey) is re-checked through dbo.UspLoginUser. The 5100 packet carries
            // mUserNo in AccountId and mCertifiedKey in SessionId (see Server.Login AuthorizationHandler)
            LoginUserResult loginResult = _accountRepository.LoginUser(new LoginUserRequest
            {
                UserNo = userNo,
                CertifiedKey = model.SessionId,
                Ip = GetClientIp(client),
                WorldNo = _gameSetting.Id,
                SvrInfo = 0, // general server, not the Chaos Battle Server
                IpEx = 0,
                PcBangLvEx = 0,
                IsNonClt = false,
                NewCertifiedKey = 0
            });

            // Only the return code is trustworthy: a wrong key or an unknown account fails here
            if (!loginResult.IsSuccess)
            {
                _commonFactory.SendServerError(client, PacketType.LoginUserReq, GameServerErrorType.NoUserNotLogin, true);
                return;
            }

            // Build the domain session from the procedure result and load the selection screen
            GSession sessionGame = new GSession();
            _dbGameMappingService.MapSession(sessionGame, loginResult, userNo, _gameSetting.Id);

            client.Sessions = sessionGame;
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
            // TODO Logout
        }
    }
}
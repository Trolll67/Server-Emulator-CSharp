using Packets.Server.Game.Models.Receive.Character;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using System;
using System.Linq;
using Server.Game.Network;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Send;
using Server.Game.Models.Game;
using Server.Game.Models.Settings;
using Server.Game.Services.Database;
using Database.DataModel.Enums;
using Database.Fnl.Game;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class CharacterHandler : ICharacterHandler
    {
        /// <summary>
        ///     Slots of the selection screen, TblPc.mSlot is 0..2
        /// </summary>
        private const byte SlotCount = 3;

        /// <summary>
        ///     Length of TblPc.mNm, char(12): a longer name would be cut by the procedure
        /// </summary>
        private const int NameMaxLength = 12;

        /// <summary>
        ///     UspCreatePc: the slot is already taken
        /// </summary>
        private const int CreateSlotBusy = 1;

        /// <summary>
        ///     UspCreatePc: a character with that name already exists
        /// </summary>
        private const int CreateNameExists = 2;

        /// <summary>
        ///     UspDeletePcEx: the character still belongs to a guild
        /// </summary>
        private const int DeleteGuildMember = 2;

        /// <summary>
        ///     UspDeletePcEx: the character is a side of an apprenticeship
        /// </summary>
        private const int DeleteApprenticeship = 6;

        /// <summary>
        ///     UspDeletePcEx: too many characters were deleted already
        /// </summary>
        private const int DeleteTooMany = 7;

        /// <summary>
        ///     UspDeletePcEx: the character still has lots on the auction
        /// </summary>
        private const int DeleteAuctionLot = 8;

        private readonly ILogger<CharacterHandler> _logger;
        private readonly GameSetting _gameSetting;
        private readonly GameRepository _gameRepository;
        private readonly ICharacterFactory _characterFactory;
        private readonly IErrorFactory _errorFactory;

        public CharacterHandler(ILogger<CharacterHandler> logger, IOptions<GameSetting> gameSetting, GameRepository gameRepository, ICharacterFactory characterFactory, IErrorFactory errorFactory)
        {
            _logger = logger;
            _gameSetting = gameSetting.Value;
            _gameRepository = gameRepository;
            _characterFactory = characterFactory;
            _errorFactory = errorFactory;
        }

        [HandlerAction(PacketType.CreatePcReq)]
        public void CreateCharactersHandle(GameSession client, CreatePcReqModel model)
        {
            // 5118 belongs to the selection screen: a socket that never passed 5100 has no account
            // behind it, so there is nothing to own the character and no list to put it into
            if (client.State == GameSessionState.Connected)
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoUserNotLogin, true);
                return;
            }

            // A session that already plays has left the selection screen behind: the original
            // refuses such a request with eErrNoUserAlreadyEnterField, the closest code of this
            // enum is the one about a session that is logged in already
            if (client.IsInWorld)
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoUserChkAlreadyLogined, true);
                return;
            }

            // The values the client is able to send at all. Anything outside these ranges is a
            // broken or forged packet - eErrNoTrBrokenIntegrity in the original - and it must be
            // stopped before the procedure writes it into tinyint columns
            if (model.Slot >= SlotCount || model.Class >= (byte)PcClassEnum.Cnt)
            {
                _logger.LogWarning("Account {AccountId} asked to create a character in slot {Slot} of class {Class}, request rejected", client.Sessions.AccountId, model.Slot, model.Class);
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.HackerDetected, true);
                return;
            }

            // Check if character more 3
            if (client.Pcs.Count() >= SlotCount)
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoCharInvalidSlot, true);
                return;
            }

            // Check if slot not empty
            if (client.Pcs.Any(c => c.Simple.Slot == model.Slot)) // TODO проверка на удаленного персонажа
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoUserCharSlotBusy, true);
                return;
            }

            // The name comes from a fixed 13 byte field of the packet, so it may be empty, longer
            // than the column or filled with anything at all - eErrNoCharInvalidNm in the original.
            // The enum has no value for it yet, the closest one is about an invalid character field.
            // The name is checked as it came: the parser cuts the padding of the field at the first
            // zero byte and nothing else is stripped, so a name with spaces around it is a refusal
            // and not something the server silently repairs
            string name = model.Name;

            if (!IsValidName(name))
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoCharInvalidNo, true);
                return;
            }

            // The unique name check lives in the database, UspCreatePc reports it by return code 2;
            // here only the characters of this account are known, they are checked without a query
            if (client.Pcs.Any(c => string.Equals(c.Simple.NickName, name, StringComparison.OrdinalIgnoreCase)))
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoCharAlreadyExistNm, true);
                return;
            }

            // Стартовые карта и позиция зависят от класса, значения задаются в gamesettings.json.
            // Класс без стартовой точки считается закрытым контентом - последняя проверка оригинала
            StartPosition startPosition = _gameSetting.StartPositions
                .FirstOrDefault(p => p.Class == (CharacterTypeEnum)model.Class);

            if (startPosition == null)
            {
                // Класс не описан в конфиге — это ошибка настройки сервера. В оригинале сюда
                // приходит eErrNoContentsNotSupport, но его числовой код пока не известен
                _logger.LogError("No start position for class {Class} in GameSetting.StartPositions, character creation rejected", model.Class);
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.UnknownError, true);
                return;
            }

            // Create the character through UspCreatePc. The unique name check and the slot check
            // live inside the procedure, the business outcome is reported by the return code
            CreatePcResult result;

            try
            {
                result = _gameRepository.CreatePc(new CreatePcRequest
                {
                    Owner = client.Sessions.AccountId,
                    Slot = model.Slot,
                    Nm = name,
                    Class = model.Class,
                    Sex = model.Sex,
                    Head = model.Head,
                    Face = model.Face,
                    Body = model.TypeBody,
                    HomeMap = startPosition.Map,
                    HomeX = startPosition.X,
                    HomeY = startPosition.Y,
                    HomeZ = startPosition.Z
                });
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                // FNLGame is down or the procedure itself failed: without an answer the client
                // keeps waiting on the creation screen forever
                _logger.LogError(e, $"Can not create a character for account {client.Sessions.AccountId}, UspCreatePc failed");

                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.UnknownError, true);
                return;
            }

            if (!result.IsSuccess)
            {
                // A negative return code never comes from the body of the procedure: it is the
                // internal failure the original reports as eErrNoSqlInternalError. A code above
                // zero is a business answer and carries the error name in @pErrNoStr
                if (result.ReturnCode < 0)
                {
                    _logger.LogError("UspCreatePc failed internally for account {AccountId}, return code {ReturnCode}", client.Sessions.AccountId, result.ReturnCode);
                    _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.UnknownError, true);
                    return;
                }

                _logger.LogInformation("UspCreatePc refused a character of account {AccountId}: {ErrNo}, return code {ReturnCode}", client.Sessions.AccountId, result.ErrNo, result.ReturnCode);
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, MapCreateError(result.ReturnCode), true);
                return;
            }

            // Build the freshly created character from the loader procedures for the client.
            // The row is written by now, so every failure of the loaders is answered instead of
            // being left to the catch of the session: without an answer the client would hang
            // on the creation screen with the character already in the database
            GPc gamePc;

            try
            {
                gamePc = _gameRepository.GetPc(result.PcNo, model.Slot);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Character {result.PcNo} of account {client.Sessions.AccountId} was created but its loading failed");

                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.UnknownError, true);
                return;
            }

            if (gamePc == null)
            {
                // The row is written, but the loaders gave nothing back: the character shows up
                // on the next visit of the selection screen, this attempt can only be refused
                _logger.LogError("Character {PcNo} of account {AccountId} was created but can not be loaded back", result.PcNo, client.Sessions.AccountId);
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.UnknownError, true);
                return;
            }

            client.Pcs.Add(gamePc);

            // TODO стартовые скилл-паки при создании не выдаются: персонаж появляется без базовых
            // умений своего класса, их раздача - отдельная задача

            // TODO Str/Dex/Int пакета 5119 в оригинале считаются из класса при уровне 1, а не
            // читаются из созданной записи; фабрика берёт их из pc.Ability, см. CharacterFactory
            _characterFactory.SendCompleteCreateCharacters(client, gamePc);
        }

        [HandlerAction(PacketType.DeletePcReq)]
        public void DeleteCharactersHandle(GameSession client, DeletePcReqModel model)
        {
            // 5120 belongs to the selection screen, the same way 5118 does
            if (client.State == GameSessionState.Connected)
            {
                _errorFactory.SendServerError(client, PacketType.DeletePcReq, GameServerErrorType.NoUserNotLogin, true);
                return;
            }

            // A character that is in the world must not be deleted under itself: the world holds
            // it, the autosave writes it back and the deletion would leave the session playing a
            // character that no longer exists. The original answers eErrNoUserAlreadyEnterField
            if (client.IsInWorld)
            {
                _errorFactory.SendServerError(client, PacketType.DeletePcReq, GameServerErrorType.NoUserChkAlreadyLogined, true);
                return;
            }

            GPc pcGame = client.Pcs.FirstOrDefault(c => c.Simple.PcNo == model.PcNo);

            if (pcGame == null)
            {
                _errorFactory.SendServerError(client, PacketType.DeletePcReq, GameServerErrorType.NoCharCannotDel, true);
                return;
            }

            // Delete the character through UspDeletePcEx, the business outcome is the return code
            DeletePcResult result;

            try
            {
                result = _gameRepository.DeletePc(client.Sessions.AccountId, (int)model.PcNo);
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                _logger.LogError(e, $"Can not delete character {model.PcNo} of account {client.Sessions.AccountId}, UspDeletePcEx failed");

                _errorFactory.SendServerError(client, PacketType.DeletePcReq, GameServerErrorType.UnknownError, true);
                return;
            }

            if (!result.IsSuccess)
            {
                // A negative return code is an internal failure of the procedure, a positive one
                // is a business refusal with its own reason
                if (result.ReturnCode < 0)
                {
                    _logger.LogError("UspDeletePcEx failed internally for character {PcNo}, return code {ReturnCode}", model.PcNo, result.ReturnCode);
                    _errorFactory.SendServerError(client, PacketType.DeletePcReq, GameServerErrorType.UnknownError, true);
                    return;
                }

                // A guild member, a side of an apprenticeship, the exceeded delete limit and the
                // lots left on the auction are separate eErrNo* answers in the original. This enum
                // has a value for none of them, so every business refusal goes out as the common
                // one and the reasons stay apart in the log only
                _logger.LogInformation("UspDeletePcEx refused character {PcNo}: {Reason}, return code {ReturnCode}", model.PcNo, DescribeDeleteReason(result.ReturnCode), result.ReturnCode);
                _errorFactory.SendServerError(client, PacketType.DeletePcReq, GameServerErrorType.NoCharCannotDel, true);
                return;
            }

            // Delete character from client
            client.Pcs.Remove(pcGame);

            // TODO участникам календарных договорённостей удалённого персонажа оригинал шлёт
            // уведомление; рассылка не реализована, договорённости остаются висеть

            _characterFactory.SendCompleteDeleteCharacters(client, pcGame);
        }

        /// <summary>
        ///     The name has to fit TblPc.mNm char(12) and carry nothing but ASCII letters and
        ///     digits: the field of the packet is fixed and may hold spaces, quotes or control
        ///     bytes, the original refuses all of that with eErrNoCharInvalidNm. The name is not
        ///     repaired on the way: a name with spaces around it is refused, not trimmed.
        ///     Non-ASCII letters are refused too - mNm is a single byte column and the procedure
        ///     converts the parameter by the collation of the database, so anything outside ASCII
        ///     may silently turn into question marks; allow wider ranges only after the collation
        ///     of the live FNLGame is confirmed
        /// </summary>
        /// <param name="name">Name as it came in the packet</param>
        /// <returns>True when the name may be handed to the procedure</returns>
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > NameMaxLength)
            {
                return false;
            }

            foreach (char symbol in name)
            {
                bool isAsciiLetterOrDigit = (symbol >= '0' && symbol <= '9')
                    || (symbol >= 'A' && symbol <= 'Z')
                    || (symbol >= 'a' && symbol <= 'z');

                if (!isAsciiLetterOrDigit)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Turns a business return code of UspCreatePc into the error the client shows
        /// </summary>
        /// <param name="returnCode">RETURN code of the procedure, above zero</param>
        /// <returns>Error type of packet 1102</returns>
        private static GameServerErrorType MapCreateError(int returnCode)
        {
            switch (returnCode)
            {
                case CreateSlotBusy:
                    return GameServerErrorType.NoUserCharSlotBusy;

                case CreateNameExists:
                    return GameServerErrorType.NoCharAlreadyExistNm;

                // The rest of the codes are failed inserts and a missing item serial: the client
                // has nothing to fix there, so it gets the common error
                default:
                    return GameServerErrorType.UnknownError;
            }
        }

        /// <summary>
        ///     Reason of a refused deletion for the log: the client sees the same code for all
        ///     of them, so the server side has to keep the difference readable
        /// </summary>
        /// <param name="returnCode">RETURN code of UspDeletePcEx, above zero</param>
        /// <returns>Short description of the reason</returns>
        private static string DescribeDeleteReason(int returnCode)
        {
            switch (returnCode)
            {
                case DeleteGuildMember:
                    return "the character belongs to a guild";

                case DeleteApprenticeship:
                    return "the character is a side of an apprenticeship";

                case DeleteTooMany:
                    return "too many characters were deleted already";

                case DeleteAuctionLot:
                    return "the character has lots on the auction";

                default:
                    return "the procedure refused the deletion";
            }
        }
    }
}

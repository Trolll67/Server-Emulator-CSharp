using Packets.Server.Game.Models.Receive.Character;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class CharacterHandler : ICharacterHandler
    {
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
            // Check if character more 3
            if (client.Pcs.Count() >= 3)
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

            // Стартовые карта и позиция зависят от класса, значения задаются в gamesettings.json
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
            CreatePcResult result = _gameRepository.CreatePc(new CreatePcRequest
            {
                Owner = client.Sessions.AccountId,
                Slot = model.Slot,
                Nm = model.Name,
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

            if (!result.IsSuccess)
            {
                // ReturnCode 2 - eErrNoCharAlreadyExistNm, 1 - the slot is already taken
                GameServerErrorType errorType = result.ReturnCode == 2
                    ? GameServerErrorType.NoCharAlreadyExistNm
                    : GameServerErrorType.NoUserCharSlotBusy;

                _errorFactory.SendServerError(client, PacketType.CreatePcReq, errorType, true);
                return;
            }

            // Build the freshly created character from the loader procedures for the client
            GPc gamePc = _gameRepository.GetPc(result.PcNo, model.Slot);
            client.Pcs.Add(gamePc);

            _characterFactory.SendCompleteCreateCharacters(client, gamePc);
        }

        [HandlerAction(PacketType.DeletePcReq)]
        public void DeleteCharactersHandle(GameSession client, DeletePcReqModel model)
        {
            GPc pcGame = client.Pcs.FirstOrDefault(c => c.Simple.PcNo == model.PcNo);

            if (pcGame == null)
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoCharCannotDel, true);
                return;
            }

            // Delete the character through UspDeletePcEx, the business outcome is the return code
            DeletePcResult result = _gameRepository.DeletePc(client.Sessions.AccountId, (int)model.PcNo);

            if (!result.IsSuccess)
            {
                _errorFactory.SendServerError(client, PacketType.CreatePcReq, GameServerErrorType.NoCharCannotDel, true);
                return;
            }

            // Delete character from client
            client.Pcs.Remove(pcGame);

            _characterFactory.SendCompleteDeleteCharacters(client, pcGame);
        }
    }
}

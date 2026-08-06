using Packets.Server.Game.Models.Receive.Character;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using System.Linq;
using Server.Game.Network;
using Packets.Core.Attributes;
using Packets.Core.Enums;
using Packets.Server.Game.Models.Send;
using Server.Game.Models.Game;
using Server.Game.Services.Database;
using Database.Fnl.Game;

namespace Server.Game.Core.Handlers
{
    [Handler]
    public class CharacterHandler : ICharacterHandler
    {
        // Home position of a freshly created character. In the original this comes from the class
        // template; the starting map is not known here, so 0 keeps the previous behaviour
        private const int StartingMap = 0;
        private const float StartingPosX = 364000.2f;
        private const float StartingPosY = 313483.7f;
        private const float StartingPosZ = 12339.71f;

        private readonly GameRepository _gameRepository;
        private readonly ICharacterFactory _characterFactory;
        private readonly IErrorFactory _errorFactory;

        public CharacterHandler(GameRepository gameRepository, ICharacterFactory characterFactory, IErrorFactory errorFactory)
        {
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
                HomeMap = StartingMap,
                HomeX = StartingPosX,
                HomeY = StartingPosY,
                HomeZ = StartingPosZ
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

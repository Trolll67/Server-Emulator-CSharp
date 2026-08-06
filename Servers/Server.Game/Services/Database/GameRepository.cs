using Database.Fnl.Game;
using Server.Game.Models.Game;
using Server.Game.Services.Mapping;
using System.Collections.Generic;

namespace Server.Game.Services.Database
{
    /// <summary>
    ///     Database service over the original FNLGame stored procedures
    /// </summary>
    public class GameRepository
    {
        private readonly IFnlGameRepository _gameRepository;
        private readonly GameMappingService _gameMappingService;

        public GameRepository(IFnlGameRepository gameRepository, GameMappingService gameMappingService)
        {
            _gameRepository = gameRepository;
            _gameMappingService = gameMappingService;
        }

        #region Character
        /// <summary>
        ///     Get the account characters for the selection screen. UspListPc returns only
        ///     slot and number, so state and equipment are loaded per character for the preview
        /// </summary>
        /// <param name="userNo">Account number, TblPc.mOwner</param>
        /// <returns></returns>
        public List<GPc> GetPcsByAccountId(int userNo)
        {
            var rows = _gameRepository.ListPc(userNo);

            List<GPc> characterGames = new List<GPc>(rows.Count);

            foreach (var row in rows)
            {
                var gPc = BuildPc(row.No, row.Slot);
                if (gPc == null)
                    continue;

                characterGames.Add(gPc);
            }

            return characterGames;
        }

        /// <summary>
        ///     Load a character without marking it online. Used right after UspCreatePc to build the
        ///     domain model of the freshly created character for the client
        /// </summary>
        /// <param name="pcNo">Character number, TblPc.mNo</param>
        /// <param name="slot">Slot the character occupies, TblPc.mSlot</param>
        /// <returns>The assembled character or null when there is no such character</returns>
        public GPc GetPc(int pcNo, byte slot)
        {
            return BuildPc(pcNo, slot);
        }

        /// <summary>
        ///     Assemble a full character from the loader procedures (detail, inventory, equipment)
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <param name="slot">Slot the character occupies, TblPc.mSlot</param>
        /// <returns>The assembled character or null when there is no such character</returns>
        private GPc BuildPc(int pcNo, byte slot)
        {
            var detail = _gameRepository.GetPcDetail(pcNo);
            if (detail == null)
                return null;

            var items = _gameRepository.GetPcItem(pcNo);
            var equips = _gameRepository.GetPcEquip(pcNo);

            var gPc = _gameMappingService.GetCharacterGame(pcNo, slot, detail, items, equips);

            // Keep the map the character was loaded on so the per-tick UspUpdatePos write does not
            // reset TblPcState.mMapNo to 0 (in this slice the player never changes the map)
            gPc.MapNo = detail.MapNo;

            return gPc;
        }

        /// <summary>
        ///     Load the selected character on the way into the world: mark it online (UspLoginPc)
        ///     and assemble the full character from the loader procedures
        /// </summary>
        /// <param name="userNo">Account number, @pUserNo</param>
        /// <param name="pcNo">Chosen character number, @pPcNo</param>
        /// <param name="ip">Client address, @pIp</param>
        /// <returns>The assembled character or null when there is no such character</returns>
        public GPc LoadPc(int userNo, int pcNo, string ip)
        {
            _gameRepository.LoginPc(userNo, pcNo, ip);

            // TODO active abnormals (UspGetListAbnormal) have no domain target yet (GPc has no
            // abnormal list); load and apply them once the buff system is ported

            // Slot is not returned by the loader procedures and the world does not need it here
            return BuildPc(pcNo, 0);
        }

        /// <summary>
        ///     Create a character (UspCreatePc). The business outcome is reported by the return code
        /// </summary>
        public CreatePcResult CreatePc(CreatePcRequest request)
        {
            return _gameRepository.CreatePc(request);
        }

        /// <summary>
        ///     Delete a character (UspDeletePcEx). The business outcome is reported by the return code
        /// </summary>
        public DeletePcResult DeletePc(int owner, int pcNo)
        {
            return _gameRepository.DeletePc(owner, pcNo);
        }

        /// <summary>
        ///     Save the position and volatile state of a character (UspUpdatePos)
        /// </summary>
        public void SavePosition(GPc gPc)
        {
            // UspUpdatePos also writes mMapNo, so the map loaded with the character is sent back
            // unchanged; sending 0 here would wipe the character's map on every autosave
            _gameRepository.UpdatePos(
                (int)gPc.Simple.PcNo,
                gPc.Simple.Hp,
                gPc.Simple.Mp,
                gPc.MapNo,
                gPc.PositionCur.X,
                gPc.PositionCur.Y,
                gPc.PositionCur.Z,
                gPc.Simple.Stomach);
        }
        #endregion

        #region Item

        public void UpdateItem(int id, int count)
        {
            //ItemModel item = _gameContext.Items.FirstOrDefault(i => i.Id == id);

            //if (item == null)
            //{
            //    throw new System.Exception("Item not found");
            //}

            //item.Count = count;

            //_gameContext.SaveChanges();
        }

        #endregion
    }
}

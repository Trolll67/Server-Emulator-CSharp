using Server.Game.Models.Game;
using Server.Game.Services.Mapping;

namespace Server.Game.Core.Systems
{
    public class CharacterSystem
    {
        private readonly EquipSystem _equipSystem;
        private readonly InventarSystem _inventarSystem;
        private readonly GameMappingService _gameMappingService;

        public CharacterSystem(EquipSystem equipSystem, InventarSystem inventarSystem, GameMappingService gameMappingService)
        {
            _equipSystem = equipSystem;
            _inventarSystem = inventarSystem;
            _gameMappingService = gameMappingService;
        }

        // Building a GPc from persisted state now happens in GameRepository over the FNLGame loader
        // procedures (UspGetPcDetail/UspGetPcItem/UspGetPcEquip), so the old Pc -> GPc mapping that
        // used the EF entity lives no more. The class is kept for the systems that inject it.

        /// <summary>
        ///     Recalculate character
        /// </summary>
        /// <param name="pc"></param>
        //public void RecalculateCharacter(GPc pc)
        //{
        //    _gameMappingService.ReMapCharacterGame(pc);

        //    foreach (var equip in pc.Equip)
        //    {
        //        _equipSystem.EquipItem(pc, equip.Item);
        //    }
        //}
    }
}

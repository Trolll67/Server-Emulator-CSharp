using Database.DataModel.Enums;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     One worn item of a character: the item and the slot it is worn in. A record is built
    ///     whole and never changes afterwards - an equip operation publishes a new list of records
    ///     instead of moving the ones already published, so a reader on another thread (visibility,
    ///     a swing, the ai of a monster) keeps reading a snapshot that stays still under it
    /// </summary>
    public class GPcEquip
    {
        public GItem Item { get; init; }
        public ulong SerialNo { get; init; }
        public int IsConfirm { get; init; }
        public ItemStatusEnum Status { get; init; }
        public bool IsEquip { get; init; }
        public bool IsSeal { get; init; }

        /// <summary>
        ///     Slot the item is worn in. The slot belongs to the record and not to the item: two
        ///     rings of one kind are told apart by nothing else, and an item that is only carried
        ///     must not drag a slot of its own around. Every path that builds a record - loading
        ///     and an equip operation alike - names the slot, so a record left without one is a
        ///     mistake and reads as worn nowhere instead of silently landing in the first slot
        /// </summary>
        public ItemEquipTypeEnum Pos
        {
            get => _pos ?? ItemEquipTypeEnum.NotEquipped;
            init => _pos = value;
        }

        private readonly ItemEquipTypeEnum? _pos;
    }
}

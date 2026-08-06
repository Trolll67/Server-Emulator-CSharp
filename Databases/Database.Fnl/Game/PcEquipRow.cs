namespace Database.Fnl.Game
{
    /// <summary>
    ///     One row of the dbo.UspGetPcEquip result set: an item worn in an equipment slot.
    ///     Joins TblPcEquip with TblPcInventory; only items that are not expired and not seized
    ///     are returned. Column order follows the SELECT of the procedure
    /// </summary>
    public class PcEquipRow
    {
        /// <summary>
        ///     Equipment slot, TblPcEquip.mSlot
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        ///     Inventory serial of the worn item, TblPcEquip.mSerialNo, links to the PcItem rows
        /// </summary>
        public long SerialNo { get; set; }

        /// <summary>
        ///     Item template number, TblPcInventory.mItemNo
        /// </summary>
        public int ItemNo { get; set; }

        /// <summary>
        ///     TblPcInventory.mBindingType, how the item is bound to the character
        /// </summary>
        public byte BindingType { get; set; }
    }
}

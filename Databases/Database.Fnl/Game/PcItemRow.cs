namespace Database.Fnl.Game
{
    /// <summary>
    ///     One row of the dbo.UspGetPcItem result set: an item in the character inventory.
    ///     The procedure returns the ordinary items followed (UNION ALL) by the servant items;
    ///     both branches project the same fifteen columns in the same order, read by ordinal.
    ///     The two date columns come back as DATEDIFF(minute, now, endDate), i.e. minutes left
    /// </summary>
    public class PcItemRow
    {
        /// <summary>
        ///     Inventory serial, TblPcInventory.mSerialNo, unique per item instance
        /// </summary>
        public long SerialNo { get; set; }

        /// <summary>
        ///     Item template number, TblPcInventory.mItemNo
        /// </summary>
        public int ItemNo { get; set; }

        /// <summary>
        ///     Minutes left until the item expires, DATEDIFF(minute, now, TblPcInventory.mEndDate)
        /// </summary>
        public int EndDateMinutes { get; set; }

        /// <summary>
        ///     Stack count, TblPcInventory.mCnt
        /// </summary>
        public int Cnt { get; set; }

        /// <summary>
        ///     TblPcInventory.mIsConfirm
        /// </summary>
        public bool IsConfirm { get; set; }

        /// <summary>
        ///     TblPcInventory.mStatus
        /// </summary>
        public byte Status { get; set; }

        /// <summary>
        ///     TblPcInventory.mCntUse
        /// </summary>
        public short CntUse { get; set; }

        /// <summary>
        ///     TblPcInventory.mIsSeizure
        /// </summary>
        public bool IsSeizure { get; set; }

        /// <summary>
        ///     TblPcInventory.mApplyAbnItemNo
        /// </summary>
        public int ApplyAbnItemNo { get; set; }

        /// <summary>
        ///     Minutes left on the applied abnormal item,
        ///     DATEDIFF(minute, now, TblPcInventory.mApplyAbnItemEndDate); 0 when there is none
        /// </summary>
        public int ApplyAbnItemEndDateMinutes { get; set; }

        /// <summary>
        ///     TblPcInventory.mOwner, the slot the item is placed in
        /// </summary>
        public int Owner { get; set; }

        /// <summary>
        ///     TblPcInventory.mPracticalPeriod
        /// </summary>
        public int PracticalPeriod { get; set; }

        /// <summary>
        ///     TblPcInventory.mBindingType
        /// </summary>
        public byte BindingType { get; set; }

        /// <summary>
        ///     TblPcInventory.mRestoreCnt
        /// </summary>
        public byte RestoreCnt { get; set; }

        /// <summary>
        ///     TblPcInventory.mHoleCount
        /// </summary>
        public byte HoleCount { get; set; }
    }
}

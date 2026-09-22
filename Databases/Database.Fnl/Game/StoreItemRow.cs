namespace Database.Fnl.Game
{
    /// <summary>
    ///     One row of the personal warehouse, dbo.UspGetListFromStore. The warehouse belongs to
    ///     the account and not to a character, so every character of the account sees the same
    ///     rows. The columns are the ones the original sends on the wire as CStore, in that order
    /// </summary>
    public class StoreItemRow
    {
        /// <summary>
        ///     TblPcStore.mSerialNo, the serial of the thing. It stays the same while the thing
        ///     travels between the bag and the warehouse
        /// </summary>
        public long SerialNo { get; set; }

        /// <summary>
        ///     TblPcStore.mItemNo, the row of the thing in the reference tables
        /// </summary>
        public int ItemNo { get; set; }

        /// <summary>
        ///     TblPcStore.mIsConfirm, whether the thing is identified
        /// </summary>
        public bool IsConfirm { get; set; }

        /// <summary>
        ///     TblPcStore.mStatus
        /// </summary>
        public byte Status { get; set; }

        /// <summary>
        ///     TblPcStore.mCnt, how many of them lie in the slot
        /// </summary>
        public int Cnt { get; set; }

        /// <summary>
        ///     TblPcStore.mCntUse, the charges left
        /// </summary>
        public short CntUse { get; set; }

        /// <summary>
        ///     TblPcStore.mOwner, the character that owns the thing
        /// </summary>
        public int Owner { get; set; }

        /// <summary>
        ///     TblPcStore.mPracticalPeriod, sent as mTermOfEffectivity
        /// </summary>
        public int PracticalPeriod { get; set; }

        /// <summary>
        ///     TblPcStore.mHoleCount, the sockets of the thing
        /// </summary>
        public byte HoleCount { get; set; }
    }
}

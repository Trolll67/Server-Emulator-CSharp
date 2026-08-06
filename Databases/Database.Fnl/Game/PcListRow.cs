namespace Database.Fnl.Game
{
    /// <summary>
    ///     One row of the dbo.UspListPc result set: a character on the account selection screen.
    ///     The procedure returns only the slot and the character number (TblPc mSlot, mNo);
    ///     everything else the selection screen needs is loaded separately per character
    /// </summary>
    public class PcListRow
    {
        /// <summary>
        ///     Slot the character occupies, TblPc.mSlot
        /// </summary>
        public byte Slot { get; set; }

        /// <summary>
        ///     Character number, TblPc.mNo, the key every other Game procedure is called with
        /// </summary>
        public int No { get; set; }
    }
}

namespace Database.Fnl.Game
{
    /// <summary>
    ///     One row of the dbo.UspGetListAbnormal result set: an abnormal (buff/debuff) still
    ///     active on the character. Read from TblPcAbnormal by ordinal
    /// </summary>
    public class PcAbnormalRow
    {
        /// <summary>
        ///     Abnormal parameter number, TblPcAbnormal.mParmNo
        /// </summary>
        public int ParmNo { get; set; }

        /// <summary>
        ///     Seconds left on the abnormal, TblPcAbnormal.mLeftTime
        /// </summary>
        public int LeftTime { get; set; }

        /// <summary>
        ///     Additional abnormal parameter number, TblPcAbnormal.mAbParmNo
        /// </summary>
        public int AbParmNo { get; set; }

        /// <summary>
        ///     TblPcAbnormal.mRestoreCnt
        /// </summary>
        public byte RestoreCnt { get; set; }
    }
}

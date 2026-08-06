namespace Database.Fnl.Game
{
    /// <summary>
    ///     Output of dbo.UspCreatePc
    /// </summary>
    public class CreatePcResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - success, 1 - the slot is already taken,
        ///     2 - a character with that name already exists, 8 - could not allocate an item serial,
        ///     3/4/5 - an insert into TblPc/TblPcState/TblPcInventory failed
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     Number of the created character, @pPcNo. Meaningful only on success
        /// </summary>
        public int PcNo { get; set; }

        /// <summary>
        ///     Level of the created character, @pLevel (always 1). Meaningful only on success
        /// </summary>
        public short Level { get; set; }

        /// <summary>
        ///     Error name, @pErrNoStr, for example eErrNoCharAlreadyExistNm. Meaningful only
        ///     when <see cref="ReturnCode"/> is not zero: on success the procedure leaves there
        ///     its initial value 'eErrNoSqlInternalError'
        /// </summary>
        public string ErrNo { get; set; }
    }
}

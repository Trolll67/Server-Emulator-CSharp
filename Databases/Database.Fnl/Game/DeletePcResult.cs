namespace Database.Fnl.Game
{
    /// <summary>
    ///     Output of dbo.UspDeletePcEx
    /// </summary>
    public class DeletePcResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - success, 7 - the daily delete limit is exceeded,
        ///     the rest (1..8) - the character does not exist, still belongs to a guild/party/etc.
        ///     or an internal error, see the procedure body
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     Name of the deleted character, RTRIM(@pNm char(12))
        /// </summary>
        public string Nm { get; set; }

        /// <summary>
        ///     Guild the character belonged to, @pGuildNo, 0 when it was in no guild
        /// </summary>
        public int GuildNo { get; set; }

        /// <summary>
        ///     Guild grade the character held, @pGuildGrade, meaningful only when it was in a guild
        /// </summary>
        public byte GuildGrade { get; set; }
    }
}

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Session game model
    /// </summary>
    public class GSession
    {
        public int Id { get; set; }

        /// <summary>
        ///     Account id
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        ///     Server id
        /// </summary>
        public int? ServerId { get; set; }

        /// <summary>
        ///     Is in game
        /// </summary>
        public bool InGame { get; set; }

        /// <summary>
        ///     TblUser.mUseMacro received from UspLoginUser; UspLogoutUser writes it back on logout
        /// </summary>
        public short UseMacro { get; set; }

        /// <summary>
        ///     TblUser.mUserAuth received from UspLoginUser: 0 - the account is not activated,
        ///     1 - an ordinary player, above - staff. The client is told it in 5101
        /// </summary>
        public byte UserAuth { get; set; }

        /// <summary>
        ///     The session key actually written into TblUser.mCertifiedKey by UspLoginUser.
        ///     A later re-authorization of this session must present exactly this value
        /// </summary>
        public int CertifiedKey { get; set; }
    }
}

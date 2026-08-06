namespace Database.Fnl.Game
{
    /// <summary>
    ///     Input of dbo.UspCreatePc. Field order and types follow sys.parameters of the live procedure
    /// </summary>
    public class CreatePcRequest
    {
        /// <summary>
        ///     Account the character belongs to, @pOwner (TblPc.mOwner)
        /// </summary>
        public int Owner { get; set; }

        /// <summary>
        ///     Slot the character is created in, @pSlot
        /// </summary>
        public byte Slot { get; set; }

        /// <summary>
        ///     Character name, @pNm char(12)
        /// </summary>
        public string Nm { get; set; }

        /// <summary>
        ///     Character class, @pClass
        /// </summary>
        public byte Class { get; set; }

        /// <summary>
        ///     Character sex, @pSex
        /// </summary>
        public byte Sex { get; set; }

        /// <summary>
        ///     Head appearance, @pHead
        /// </summary>
        public byte Head { get; set; }

        /// <summary>
        ///     Face appearance, @pFace
        /// </summary>
        public byte Face { get; set; }

        /// <summary>
        ///     Body appearance, @pBody
        /// </summary>
        public byte Body { get; set; }

        /// <summary>
        ///     Home map the character starts on, @pHomeMap
        /// </summary>
        public int HomeMap { get; set; }

        /// <summary>
        ///     Home position, @pHomeX real
        /// </summary>
        public float HomeX { get; set; }

        /// <summary>
        ///     Home position, @pHomeY real
        /// </summary>
        public float HomeY { get; set; }

        /// <summary>
        ///     Home position, @pHomeZ real
        /// </summary>
        public float HomeZ { get; set; }
    }
}

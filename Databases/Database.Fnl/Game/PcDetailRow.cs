namespace Database.Fnl.Game
{
    /// <summary>
    ///     The single row of the dbo.UspGetPcDetail result set: the full state of one character.
    ///     Joins TblPc, TblPcState, TblGuildMember (left) and TblDiscipleMember (left);
    ///     the guild columns are null when the character is in no guild. Column order follows
    ///     the final SELECT of the procedure and is read by ordinal
    /// </summary>
    public class PcDetailRow
    {
        /// <summary>
        ///     Character name, RTRIM(TblPc.mNm)
        /// </summary>
        public string Nm { get; set; }

        /// <summary>
        ///     TblPc.mClass
        /// </summary>
        public byte Class { get; set; }

        /// <summary>
        ///     TblPc.mSex
        /// </summary>
        public byte Sex { get; set; }

        /// <summary>
        ///     TblPc.mHead
        /// </summary>
        public byte Head { get; set; }

        /// <summary>
        ///     TblPc.mFace
        /// </summary>
        public byte Face { get; set; }

        /// <summary>
        ///     TblPc.mBody
        /// </summary>
        public byte Body { get; set; }

        /// <summary>
        ///     TblPcState.mLevel
        /// </summary>
        public short Level { get; set; }

        /// <summary>
        ///     TblPcState.mHpAdd, bonus HP on top of the class base
        /// </summary>
        public int HpAdd { get; set; }

        /// <summary>
        ///     TblPcState.mHp, current HP
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        ///     TblPcState.mMpAdd, bonus MP on top of the class base
        /// </summary>
        public int MpAdd { get; set; }

        /// <summary>
        ///     TblPcState.mMp, current MP
        /// </summary>
        public int Mp { get; set; }

        /// <summary>
        ///     TblPcState.mExp
        /// </summary>
        public long Exp { get; set; }

        /// <summary>
        ///     TblPcState.mStomach
        /// </summary>
        public short Stomach { get; set; }

        /// <summary>
        ///     TblPcState.mMapNo, the map the character logged out on
        /// </summary>
        public int MapNo { get; set; }

        /// <summary>
        ///     TblPcState.mPosX
        /// </summary>
        public float PosX { get; set; }

        /// <summary>
        ///     TblPcState.mPosY
        /// </summary>
        public float PosY { get; set; }

        /// <summary>
        ///     TblPcState.mPosZ
        /// </summary>
        public float PosZ { get; set; }

        /// <summary>
        ///     TblPc.mHomeMapNo, the map the character is respawned on
        /// </summary>
        public int HomeMapNo { get; set; }

        /// <summary>
        ///     TblPc.mHomePosX
        /// </summary>
        public float HomePosX { get; set; }

        /// <summary>
        ///     TblPc.mHomePosY
        /// </summary>
        public float HomePosY { get; set; }

        /// <summary>
        ///     TblPc.mHomePosZ
        /// </summary>
        public float HomePosZ { get; set; }

        /// <summary>
        ///     TblGuildMember.mGuildNo, 0 when the character is in no guild
        /// </summary>
        public int GuildNo { get; set; }

        /// <summary>
        ///     RTRIM(TblGuildMember.mNickNm), null when the character is in no guild
        /// </summary>
        public string NickNm { get; set; }

        /// <summary>
        ///     TblGuildMember.mGuildGrade, meaningful only when the character is in a guild
        /// </summary>
        public byte GuildGrade { get; set; }

        /// <summary>
        ///     TblPcState.mPkCnt
        /// </summary>
        public int PkCnt { get; set; }

        /// <summary>
        ///     TblPcState.mChaotic
        /// </summary>
        public int Chaotic { get; set; }

        /// <summary>
        ///     ISNULL(TblDiscipleMember.mMaster, 0), the master of this disciple or 0
        /// </summary>
        public int DiscipleNo { get; set; }

        /// <summary>
        ///     ISNULL(TblDiscipleMember.mType, 0)
        /// </summary>
        public byte DiscipleType { get; set; }

        /// <summary>
        ///     TblPcState.mIsLetterLimit
        /// </summary>
        public bool IsLetterLimit { get; set; }

        /// <summary>
        ///     TblPcState.mIsPreventItemDrop
        /// </summary>
        public bool IsPreventItemDrop { get; set; }

        /// <summary>
        ///     TblPcState.mFlag
        /// </summary>
        public short Flag { get; set; }
    }
}

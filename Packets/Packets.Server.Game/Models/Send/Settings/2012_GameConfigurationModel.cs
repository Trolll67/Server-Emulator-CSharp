using System.Collections.Generic;
using Packets.Core.Attributes;
using Packets.Core.Enums;

namespace Packets.Server.Game.Models.Send.Settings
{
    /// <summary>
    ///     eCTrContentsAck of the original: one page of the contents of this server. The packet of
    ///     the original holds a fixed hundred entries and the number of the page, and the entries
    ///     themselves are the rows of FNLParm.TblParmSvrOp - the switch of an option and its thirty
    ///     values. The client is told every page, so it learns the options of the server whole
    /// </summary>
    [Model(PacketType.GameConfiguration)]
    public class GameConfigurationModel
    {
        /// <summary>
        ///     How many options travel in one packet: the length of CTrContentsAck.mContents
        /// </summary>
        public const int ContentsPerPage = 100;

        /// <summary>
        ///     How many values one option carries, TblParmSvrOp.mOpValue1..mOpValue30. They are
        ///     float in the table and four bytes each on the wire
        /// </summary>
        public const int ValuesPerContent = 30;

        /// <summary>
        ///     Number of the page, mContentsSeq of the original: page zero carries the options
        ///     1..100, page one the options 101..200
        /// </summary>
        public uint ContentsSeq { get; set; }

        /// <summary>
        ///     Options of this page in the order of their numbers. A slot nobody filled is an
        ///     option this server has no row for, and it goes out as zeroes
        /// </summary>
        public List<GameConfigurationContent> Contents { get; set; } = new List<GameConfigurationContent>();
    }

    /// <summary>
    ///     One entry of CTrContentsAck.mContents: a single row of TblParmSvrOp
    /// </summary>
    public class GameConfigurationContent
    {
        /// <summary>
        ///     TblParmSvrOp.mIsSetup, whether the option is turned on
        /// </summary>
        public bool IsSetup { get; set; }

        /// <summary>
        ///     TblParmSvrOp.mOpValue1..mOpValue30, in that order
        /// </summary>
        public IReadOnlyList<double> Values { get; set; }
    }
}

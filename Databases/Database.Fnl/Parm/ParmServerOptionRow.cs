using System.Collections.Generic;

namespace Database.Fnl.Parm
{
    /// <summary>
    ///     A row of TblParmSvrOp: one server option
    /// </summary>
    public class ParmServerOptionRow
    {
        /// <summary>
        ///     Option number, TblParmSvrOp.mOpNo. See <see cref="ParmServerOption"/>
        /// </summary>
        public int OpNo { get; set; }

        /// <summary>
        ///     Whether the option is turned on, TblParmSvrOp.mIsSetup
        /// </summary>
        public bool IsSetup { get; set; }

        /// <summary>
        ///     Values of the option, TblParmSvrOp.mOpValue1..mOpValue30, in that order.
        ///     They are float in the table, and their meaning depends on the option
        /// </summary>
        public IReadOnlyList<double> Values { get; set; }
    }
}

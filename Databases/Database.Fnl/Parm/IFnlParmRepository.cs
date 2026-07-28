using System.Collections.Generic;

namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Access to the FNLParm database through the original stored procedures
    /// </summary>
    public interface IFnlParmRepository
    {
        /// <summary>
        ///     Reads the settings of a server by its kind and address (dbo.UspGetParmSvr).
        ///     This is how the original server learns its own mSvrNo on startup
        /// </summary>
        /// <param name="type">Server kind, TblParmSvr.mType</param>
        /// <param name="majorIp">Server address, TblParmSvr.mMajorIp</param>
        /// <returns>Server settings or null when there is no valid row for that kind and address</returns>
        ParmServerRow GetParmSvr(ParmServerType type, string majorIp);

        /// <summary>
        ///     Reads the family of the given server (dbo.UspGetFamilyEx): every other valid server
        ///     it is supposed to know about. The procedure decides the contents by the kind of
        ///     <paramref name="svrNo"/> itself, no filtering is done here
        /// </summary>
        /// <param name="svrNo">Own server number, TblParmSvr.mSvrNo</param>
        /// <returns>Family rows in the order the procedure returns them (mDispOrder, mSvrNo)</returns>
        IReadOnlyList<FamilyServerRow> GetFamily(short svrNo);
    }
}

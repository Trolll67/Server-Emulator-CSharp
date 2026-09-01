using System.Collections.Generic;

namespace Database.Fnl.Game
{
    /// <summary>
    ///     Access to the FNLGame database through the original stored procedures:
    ///     the character list of the selection screen, character creation/deletion and the
    ///     load/save of the selected character
    /// </summary>
    public interface IFnlGameRepository
    {
        /// <summary>
        ///     Reads the characters of an account for the selection screen (dbo.UspListPc).
        ///     Returns only the slot and number of each character; the rest is loaded per character
        /// </summary>
        /// <param name="userNo">Account number, TblPc.mOwner</param>
        /// <returns>Characters ordered by slot, an empty list when the account has none</returns>
        IReadOnlyList<PcListRow> ListPc(int userNo);

        /// <summary>
        ///     Creates a character (dbo.UspCreatePc). The business outcome is reported by the
        ///     return code, see <see cref="CreatePcResult"/>
        /// </summary>
        /// <param name="request">Owner, slot, name, appearance and home position of the character</param>
        /// <returns>Number and level of the created character or the failure reason</returns>
        CreatePcResult CreatePc(CreatePcRequest request);

        /// <summary>
        ///     Deletes a character (dbo.UspDeletePcEx). The business outcome is reported by the
        ///     return code, see <see cref="DeletePcResult"/>
        /// </summary>
        /// <param name="owner">Account the character belongs to, @pOwner</param>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <returns>Name and former guild of the deleted character or the failure reason</returns>
        DeletePcResult DeletePc(int owner, int pcNo);

        /// <summary>
        ///     Marks a character online as it enters the world (dbo.UspLoginPc)
        /// </summary>
        /// <param name="userNo">Account number, @pUserNo</param>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <param name="ip">Client address, @pIp char(15)</param>
        void LoginPc(int userNo, int pcNo, string ip);

        /// <summary>
        ///     Marks a character offline as it leaves the world (dbo.UspLogoutPc)
        /// </summary>
        /// <param name="userNo">Account number, @pUserNo</param>
        /// <param name="pcNo">Character number, @pPcNo</param>
        void LogoutPc(int userNo, int pcNo);

        /// <summary>
        ///     Reads the full state of a character (dbo.UspGetPcDetail)
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <returns>The character state or null when there is no such character</returns>
        PcDetailRow GetPcDetail(int pcNo);

        /// <summary>
        ///     Reads the worn equipment of a character (dbo.UspGetPcEquip)
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <returns>Equipment rows, an empty list when the character wears nothing</returns>
        IReadOnlyList<PcEquipRow> GetPcEquip(int pcNo);

        /// <summary>
        ///     Reads the inventory of a character (dbo.UspGetPcItem)
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <returns>Inventory rows, an empty list when the character carries nothing</returns>
        IReadOnlyList<PcItemRow> GetPcItem(int pcNo);

        /// <summary>
        ///     Reads the active abnormals of a character (dbo.UspGetListAbnormal)
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <returns>Abnormal rows, an empty list when the character has none</returns>
        IReadOnlyList<PcAbnormalRow> GetListAbnormal(int pcNo);

        /// <summary>
        ///     Saves the position and volatile state of a character (dbo.UspUpdatePos).
        ///     This is the only per-tick write of the world server
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <param name="hp">Current HP, @pHp</param>
        /// <param name="mp">Current MP, @pMp</param>
        /// <param name="mapNo">Current map, @pMapNo</param>
        /// <param name="x">Position, @pPosX real</param>
        /// <param name="y">Position, @pPosY real</param>
        /// <param name="z">Position, @pPosZ real</param>
        /// <param name="stomach">Stomach, @pStomach</param>
        void UpdatePos(int pcNo, int hp, int mp, int mapNo, float x, float y, float z, short stomach);

        /// <summary>
        ///     Writes one equipment slot of a character (dbo.UspEquip): the procedure puts the serial
        ///     into the column of that slot. Taking an item off is the same call with a zero serial.
        ///     A broken connection surfaces as an exception, like in the other methods here
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <param name="slot">Worn slot, weapon..cloak; the procedure knows no other column</param>
        /// <param name="serialNo">Serial of the item, @pSerial bigint, zero to clear the slot</param>
        /// <returns>True when the slot is written, false on a slot the procedure has no column for
        ///     and on a non zero return code</returns>
        bool Equip(int pcNo, int slot, long serialNo);
    }
}

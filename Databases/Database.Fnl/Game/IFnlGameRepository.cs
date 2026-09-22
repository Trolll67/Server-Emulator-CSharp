using System.Collections.Generic;

namespace Database.Fnl.Game
{
    /// <summary>
    ///     Access to the FNLGame database through the original stored procedures:
    ///     the character list of the selection screen, character creation/deletion, the
    ///     load/save of the selected character and the writes of its equipment and inventory
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
        ///     Puts an item into an equipment slot of a character (dbo.UspSetEquip): the row of that
        ///     slot is rewritten with the serial given. The procedure keeps one row per slot and
        ///     limits the slot number to nothing, so the carried slots are stored as well.
        ///     A broken connection surfaces as an exception, like in the other methods here
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <param name="serialNo">Serial of the item, @pSerial bigint</param>
        /// <param name="slot">Slot the item is worn in, @pSlot</param>
        /// <returns>RETURN code: 0 - the slot is written, 1 - the character carries no item with
        ///     that serial, 2 - the write itself failed</returns>
        int SetEquip(int pcNo, long serialNo, int slot);

        /// <summary>
        ///     Clears an equipment slot of a character (dbo.UspResetEquip): the row of that slot is
        ///     deleted. Taking an item off has its own procedure, it is not a write of a zero serial
        /// </summary>
        /// <param name="pcNo">Character number, @pPcNo</param>
        /// <param name="slot">Slot being emptied, @pSlot</param>
        /// <returns>RETURN code: 0 - the slot is empty now, 1 - the delete failed. A slot that was
        ///     empty already is a success as well</returns>
        int ResetEquip(int pcNo, int slot);

        /// <summary>
        ///     Puts an item into the inventory of a character (dbo.UspPushItem). A zero source serial
        ///     creates a new row, a non zero one moves the row of that serial to the new owner.
        ///     Merging into a stack the character already carries is done by the procedure itself:
        ///     it answers with the serial of the row the item ended up in, either a fresh one or the
        ///     one of the stack that grew
        /// </summary>
        /// <param name="pcNo">New owner, @pPcNo; <see cref="FnlGameRepository.DroppedItemOwner"/>
        ///     for an item that goes to the ground</param>
        /// <param name="serialNo">Serial of the source row, @pSerial, zero to create a new one</param>
        /// <param name="itemNo">Item template number, @pItemNo</param>
        /// <param name="validDay">Lifetime of the item in days, @pValidDay</param>
        /// <param name="cnt">Count being moved, @pCnt</param>
        /// <param name="cntUse">Use counter of the item, @pCntUse</param>
        /// <param name="isConfirm">The item is identified, @pIsConfirm</param>
        /// <param name="status">Item status, @pStatus</param>
        /// <param name="isStack">The item stacks, @pIsStack; the procedure merges only then</param>
        /// <param name="isCharge">The item is a charged one, @pIsCharge</param>
        /// <param name="practicalPeriod">Period the item stays effective, @pPracticalPeriod</param>
        /// <param name="bindingType">How the item binds to the character, @pBindingType</param>
        /// <param name="restoreCnt">Restore counter, @pRestoreCnt</param>
        /// <returns>The row the procedure answers with: error code, the resulting serial and the
        ///     minutes left of the lifetime</returns>
        PushItemRow PushItem(int pcNo, long serialNo, int itemNo, int validDay, int cnt, short cntUse,
            bool isConfirm, byte status, bool isStack, bool isCharge = false, int practicalPeriod = 0,
            byte bindingType = 0, byte restoreCnt = 0);

        /// <summary>
        ///     Takes an item out of the inventory of a character (dbo.UspPopItem). The row either
        ///     goes to the new owner whole, or the count is split off it into a row of its own; the
        ///     serial of the row the taken part ended up in comes back in the output parameter
        /// </summary>
        /// <param name="pcNo">New owner of the taken part, @pPcNo;
        ///     <see cref="FnlGameRepository.DroppedItemOwner"/> for an item that goes to the ground</param>
        /// <param name="serialNo">Serial of the row being taken from, @pSerial</param>
        /// <param name="cnt">Count being taken, @pCnt</param>
        /// <param name="cachingCnt">Cached change of the count, @pCachingCnt: the count being taken
        ///     with a minus sign</param>
        /// <param name="isSpend">The taken part is destroyed instead of going to the new owner,
        ///     @pIsSpend</param>
        /// <param name="isStack">The item stacks, @pIsStack</param>
        /// <returns>RETURN code of the procedure and the serial of the resulting row</returns>
        PopItemResult PopItem(int pcNo, long serialNo, int cnt, int cachingCnt, bool isSpend, bool isStack);

        /// <summary>
        ///     Deletes an inventory row (dbo.UspEraseItem): the item is gone for good, nobody owns
        ///     it afterwards
        /// </summary>
        /// <param name="serialNo">Serial of the row, @pSerial</param>
        /// <returns>RETURN code: 0 - the row is deleted, 3 - there was no such row</returns>
        int EraseItem(long serialNo);
        /// <summary>
        ///     Rows of the personal warehouse of an account, dbo.UspGetListFromStore.
        ///     The warehouse belongs to the account, so every character of it sees the same rows
        /// </summary>
        /// <param name="userNo">Account, TblUser.mUserNo</param>
        IReadOnlyList<StoreItemRow> GetStoreList(int userNo);

        /// <summary>
        ///     Puts a thing of a character into the warehouse, dbo.UspPushItemToStoreEx.
        ///     The thing leaves the bag of the character in the same procedure
        /// </summary>
        /// <param name="serialNo">Serial of the thing that goes in</param>
        /// <param name="count">How many of a stack go in</param>
        /// <param name="userNo">Account the warehouse belongs to</param>
        /// <param name="isStack">Whether the thing joins a stack that is already there</param>
        /// <param name="targetSerialNo">Serial the thing ends up under in the warehouse</param>
        /// <returns>Return code of the procedure, zero when the thing is in</returns>
        int PushItemToStore(long serialNo, int count, int userNo, bool isStack, out long targetSerialNo);

        /// <summary>
        ///     Takes a thing out of the warehouse into the bag of a character,
        ///     dbo.UspPopItemFromStore
        /// </summary>
        /// <param name="serialNo">Serial of the row of the warehouse</param>
        /// <param name="userNo">Account the warehouse belongs to</param>
        /// <param name="count">How many of a stack come out</param>
        /// <param name="itemNo">Row of the thing in the reference tables</param>
        /// <param name="pcNo">Character that takes the thing</param>
        /// <param name="isStack">Whether the thing joins a stack in the bag</param>
        /// <returns>Return code of the procedure, zero when the thing is out</returns>
        int PopItemFromStore(long serialNo, int userNo, int count, int itemNo, int pcNo, bool isStack);

        /// <summary>
        ///     Password of the warehouse of an account, dbo.UspGetStorePassword
        /// </summary>
        /// <returns>The password, null when the warehouse has none</returns>
        string GetStorePassword(int userNo);

        /// <summary>
        ///     Sets or clears the password of the warehouse, dbo.UspSetStorePassword
        /// </summary>
        /// <param name="userNo">Account the warehouse belongs to</param>
        /// <param name="password">New password, ignored when it is cleared</param>
        /// <param name="isSet">True to set the password, false to clear it</param>
        /// <returns>Return code of the procedure</returns>
        int SetStorePassword(int userNo, string password, bool isSet);
    }

    /// <summary>
    ///     The row dbo.UspPushItem answers with. The procedure reports its outcome by a result set
    ///     of three columns instead of a return code
    /// </summary>
    public class PushItemRow
    {
        /// <summary>
        ///     Error code of the procedure: 0 - the item is stored, 11 - the binding of the item
        ///     forbids the move, 12 - the restore counter of the item is above the one the item
        ///     allows, 28/29 - the item is seized, 30/31 - a count above one for an item that does
        ///     not stack, the rest - the write itself failed
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the error code only
        /// </summary>
        public bool IsSuccess => ErrorCode == 0;

        /// <summary>
        ///     Serial of the row the item ended up in: a fresh one, the serial of the source row
        ///     that moved, or the serial of the stack the item was merged into. Meaningful only
        ///     on success
        /// </summary>
        public long SerialNo { get; set; }

        /// <summary>
        ///     Minutes left of the lifetime of that row, DATEDIFF(minute, now, endDate)
        /// </summary>
        public int EndDateMinutes { get; set; }
    }

    /// <summary>
    ///     Output of dbo.UspPopItem
    /// </summary>
    public class PopItemResult
    {
        /// <summary>
        ///     RETURN code of the procedure: 0 - the item is taken, 1 - there is no such row,
        ///     11 - the item is seized, 12 - the binding of the item forbids the move, 13 - a count
        ///     above one for an item that does not stack, the rest - the write itself failed
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        ///     The procedure reports business errors by the return code only
        /// </summary>
        public bool IsSuccess => ReturnCode == 0;

        /// <summary>
        ///     Serial of the row the taken part ended up in, @pSerialNew: the serial of the source
        ///     row when it moved whole, a fresh one when the count was split off. Meaningful only
        ///     on success
        /// </summary>
        public long SerialNoNew { get; set; }

    }
}

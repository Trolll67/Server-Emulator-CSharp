using Packets.Server.Game.Models.Receive.Inventory;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
using Packets.Server.Game.Models.Send;
using Packets.Server.Game.Structures;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Packets.Server.Game.Enums;
using Database.DataModel.Enums;
using Microsoft.Data.SqlClient;
using Packets.Core.Attributes;
using Server.Game.Models.Game;
using Server.Game.Network;
using Packets.Core.Enums;
using Database.Fnl.Game;
using System;

namespace Server.Game.Core.Handlers
{
    /// <summary>
    ///     Equipment of a character that is already in the world: 5128 puts an item on, 5130 takes
    ///     one off. Both requests walk the same three steps and in the same order the original
    ///     walks them - the character checks the request and builds a plan of the change, every
    ///     slot the plan moves is written to the database, and only a stored change is applied to
    ///     the model and told to the clients. A refusal at any of the steps leaves both the model
    ///     and the database exactly as they were.
    ///     A request comes in on the network thread of the session, and the equipment is not the
    ///     only writer of what it moves: the item a change is built over is a row of the bag, and
    ///     the bag is written by the requests of the player and by the loot of a killed monster off
    ///     other threads (InventoryHandler, UnitDropSystem). The three steps are therefore walked
    ///     under the lock of the bag of the character, the call of the database included, so that
    ///     an item is never put on while the very same item is being thrown away. The threads that
    ///     only read the equipment - the swings, the ai of the monsters, the visibility - take
    ///     snapshots by reference and take no lock at all; they meet the change on the next
    ///     snapshot they take
    /// </summary>
    [Handler]
    public class EquipHandler : IEquipHandler
    {
        /// <summary>
        ///     First and last slot everybody around hears about: a slot of this range is worn on
        ///     the body and is drawn on the character, the rest are carried and are the business of
        ///     the owner alone. Every slot of the character is written into the database, this
        ///     range included and the carried slots as well - the live procedures take any slot
        /// </summary>
        private const ItemEquipTypeEnum VisibleSlotFirst = ItemEquipTypeEnum.Weapon;
        private const ItemEquipTypeEnum VisibleSlotLast = ItemEquipTypeEnum.Cloak;

        private readonly ILogger<EquipHandler> _logger;
        private readonly IFnlGameRepository _gameRepository;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IInventoryFactory _inventoryFactory;
        private readonly IEquipFactory _equipFactory;
        private readonly IErrorFactory _errorFactory;

        public EquipHandler(ILogger<EquipHandler> logger, IFnlGameRepository gameRepository, ICharacteristicFactory characteristicFactory, IInventoryFactory inventoryFactory, IEquipFactory equipFactory, IErrorFactory errorFactory)
        {
            _logger = logger;
            _gameRepository = gameRepository;
            _characteristicFactory = characteristicFactory;
            _inventoryFactory = inventoryFactory;
            _equipFactory = equipFactory;
            _errorFactory = errorFactory;
        }

        /// <summary>
        ///     5128: put an item of the inventory on. The slot the request names is not read at
        ///     all - which slot the item goes to is decided by the type of the item, the way the
        ///     original decides it, so a client that names a wrong slot (and one of the recorded
        ///     ones does) changes nothing about the answer
        /// </summary>
        /// <param name="client">Session that asked to put the item on</param>
        /// <param name="wearEquipReqModel">Serial of the item and the slot the client drew it into</param>
        [HandlerAction(PacketType.EquipReq)]
        public void Equip(GameSession client, EquipReqModel wearEquipReqModel)
        {
            // A request of a session that is not in the world: the character is either not chosen
            // yet or already on its way out, so there is nothing to put the item on. The original
            // answers such a request with nothing at all and only writes it into its log
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            GEquipChange change;

            // The plan, the rows of the slots and the change itself are one sequence for one
            // writer at a time - see the note at the class
            lock (client.Pc.InventoryLock)
            {
                change = client.Pc.EquipItem(wearEquipReqModel.SerialNumber);

                if (!change.IsSuccess)
                {
                    Refuse(client, PacketType.EquipReq, change.Error, wearEquipReqModel.SerialNumber);
                    return;
                }

                if (!Store(client, change, PacketType.EquipReq, wearEquipReqModel.SerialNumber))
                {
                    return;
                }

                if (!client.Pc.ApplyEquipChange(change))
                {
                    return;
                }
            }

            // The packets go out with the lock let go: the character already wears the change, and
            // nothing that is sent reads the lists any more
            Report(client, change);
        }

        /// <summary>
        ///     5130: take the item of a slot off. An empty slot and a slot that is not a slot at
        ///     all are refused by the character itself
        /// </summary>
        /// <param name="client">Session that asked to take the item off</param>
        /// <param name="unEquipReqModel">Slot to empty</param>
        [HandlerAction(PacketType.UnEquipReq)]
        public void UnEquip(GameSession client, UnEquipReqModel unEquipReqModel)
        {
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            GEquipChange change;

            // One writer of the equipment and of the bag at a time - see the lock in Equip
            lock (client.Pc.InventoryLock)
            {
                change = client.Pc.UnEquipItem((ItemEquipTypeEnum)unEquipReqModel.Position);

                if (!change.IsSuccess)
                {
                    // A request to take an item off names no item at all: the field of the refusal
                    // that gives the request back goes out at zero, the way the original sends it
                    Refuse(client, PacketType.UnEquipReq, change.Error, 0);
                    return;
                }

                if (!Store(client, change, PacketType.UnEquipReq, 0))
                {
                    return;
                }

                if (!client.Pc.ApplyEquipChange(change))
                {
                    return;
                }
            }

            Report(client, change);
        }

        /// <summary>
        ///     Writes every slot a plan moves into the database, the way the original writes it -
        ///     before the character starts wearing the change and before a single packet goes out.
        ///     A plan that is not stored is dropped whole: the half that was written is taken back,
        ///     the character is not touched, and the player is told the request failed - the
        ///     original answers a failed write of the equipment with the internal error of the
        ///     database, so the player keeps wearing what it wore and may simply ask again
        /// </summary>
        /// <param name="client">Session the plan belongs to</param>
        /// <param name="change">Plan built by the character</param>
        /// <param name="packet">Opcode of the request the plan was built for</param>
        /// <param name="serialNo">Serial of the item of the request, zero when it names none</param>
        /// <returns>True when the change may be applied and reported</returns>
        private bool Store(GameSession client, GEquipChange change, PacketType packet, ulong serialNo)
        {
            if (Write(client, change))
            {
                return true;
            }

            Restore((int)client.Pc.Simple.PcNo, change);

            _errorFactory.SendServerError(client, packet, GameServerErrorType.SqlInternalError, serialNo, false);

            return false;
        }

        /// <summary>
        ///     Writes the rows of the slots a plan moves. The slots that are emptied go first: a
        ///     swap moves the old item out of the very slot the new one is written into, so the
        ///     order of the two writes is the whole difference between a stored swap and a slot
        ///     that holds two items at once
        /// </summary>
        /// <param name="client">Session the plan belongs to</param>
        /// <param name="change">Plan built by the character</param>
        /// <returns>True when every slot of the plan is written</returns>
        private bool Write(GameSession client, GEquipChange change)
        {
            int pcNo = (int)client.Pc.Simple.PcNo;

            try
            {
                foreach (GPcEquip taken in change.UnEquipped)
                {
                    if (!ClearSlot(pcNo, taken.Pos))
                    {
                        return false;
                    }
                }

                return change.Equipped == null || StoreSlot(pcNo, change.Equipped);
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                // FNLGame is down or the procedure failed: the plan is dropped and the character
                // keeps what it wears. That is the honest half of the deal - a change that is not
                // stored would be lost on the next entry anyway, and the player would meanwhile
                // fight with an item the database knows nothing about
                _logger.LogError(e, "Can not store an equipment change of character {PcNo}, the change is dropped", client.Pc.Simple.PcNo);
                return false;
            }
        }

        /// <summary>
        ///     Puts the item of a worn record into the row of its slot (dbo.UspSetEquip). Every
        ///     slot of the character is written, the carried ones - the materials - included: the
        ///     live procedure keeps one row per slot and limits the number of the slot to nothing
        /// </summary>
        /// <param name="pcNo">Character the slot belongs to</param>
        /// <param name="equip">Record being worn, the slot and the serial of the item alike</param>
        /// <returns>True when the slot is stored</returns>
        private bool StoreSlot(int pcNo, GPcEquip equip)
        {
            if (_gameRepository.SetEquip(pcNo, (long)equip.SerialNo, (int)equip.Pos) == 0)
            {
                return true;
            }

            // The procedure refused the write; the caller drops the plan and rolls the stored
            // half of it back
            _logger.LogError("Character {PcNo} could not store slot {Slot}, the procedure refused the write", pcNo, equip.Pos);
            return false;
        }

        /// <summary>
        ///     Clears the row of a slot the character empties (dbo.UspResetEquip). Taking an item
        ///     off has a procedure of its own - it is not a write of a zero serial
        /// </summary>
        /// <param name="pcNo">Character the slot belongs to</param>
        /// <param name="pos">Slot being emptied</param>
        /// <returns>True when the slot is cleared</returns>
        private bool ClearSlot(int pcNo, ItemEquipTypeEnum pos)
        {
            if (_gameRepository.ResetEquip(pcNo, (int)pos) == 0)
            {
                return true;
            }

            _logger.LogError("Character {PcNo} could not clear slot {Slot}, the procedure refused the write", pcNo, pos);
            return false;
        }

        /// <summary>
        ///     Answers a refused request the way the original answers it: 1102 with the opcode of
        ///     the request echoed back, the reason under the name of the original and the identifier
        ///     of the item the request named - a request to take an item off names none and the
        ///     field goes out at zero. The reason is shown as a line of the system chat and not as a
        ///     window of its own, the way the original sends it.
        ///     A reason that has no name of the original is one the original answers nothing at all
        ///     to - a request that names a slot that is no slot: it is written into the log and the
        ///     client hears nothing
        /// </summary>
        /// <param name="client">Session the refusal goes to</param>
        /// <param name="packet">Opcode of the request that was refused</param>
        /// <param name="reason">Reason the character refused the request for</param>
        /// <param name="serialNo">Serial of the item of the request, zero when it names none</param>
        private void Refuse(GameSession client, PacketType packet, ErrorEnum reason, ulong serialNo)
        {
            GameServerErrorType error = ToErrorType(reason);

            if (error == null)
            {
                _logger.LogDebug("Character {PcNo} sent an equipment request that names no slot of the equipment", client.Pc.Simple.PcNo);
                return;
            }

            _logger.LogDebug("Character {PcNo} was refused an equipment change: {Error}", client.Pc.Simple.PcNo, error);

            _errorFactory.SendServerError(client, packet, error, serialNo, false);
        }

        /// <summary>
        ///     Reason of a refusal of the equipment under the name the client knows it by. The
        ///     names of our list are the names of the original with the prefix of its constants
        ///     dropped, so the two lists meet one to one.
        ///     A slot that is no slot of the equipment at all has no name of the original here:
        ///     the original reads such a request as a broken one and answers it with nothing
        /// </summary>
        /// <param name="error">Reason the character refused the request for</param>
        /// <returns>Reason as it goes out in 1102, empty when nothing is answered</returns>
        private static GameServerErrorType ToErrorType(ErrorEnum error)
        {
            switch (error)
            {
                case ErrorEnum.PosInvalid:
                    return null;
                case ErrorEnum.CharAlreadyDie:
                    return GameServerErrorType.CharAlreadyDie;
                case ErrorEnum.ItemCantEquipLimitClass:
                    return GameServerErrorType.ItemCantEquipLimitClass;
                case ErrorEnum.ItemCantEquipLimitLevel:
                    return GameServerErrorType.ItemCantEquipLimitLevel;
                case ErrorEnum.ItemEquipped:
                    return GameServerErrorType.ItemEquipped;
                case ErrorEnum.ItemNotEquipSlot:
                    return GameServerErrorType.ItemNotEquipSlot;
                case ErrorEnum.ItemNotEquip:
                    return GameServerErrorType.ItemNotEquip;
                case ErrorEnum.ItemCantFindBow:
                    return GameServerErrorType.ItemCantFindBow;
                case ErrorEnum.ItemCantEquipSpear:
                    return GameServerErrorType.ItemCantEquipSpear;
                case ErrorEnum.ItemCantEquipShield:
                    return GameServerErrorType.ItemCantEquipShield;
                default:
                    return GameServerErrorType.ItemNotExist;
            }
        }

        /// <summary>
        ///     Best-effort rollback of a half-stored plan: the slots that were already emptied get
        ///     their old serials written back, so the database keeps matching what the character
        ///     still wears. A rollback write that fails itself is only logged - the items are all
        ///     in the inventory either way, only the worn set of the next entry may differ
        /// </summary>
        /// <param name="pcNo">Character the plan belongs to</param>
        /// <param name="change">Plan whose stored half is taken back</param>
        private void Restore(int pcNo, GEquipChange change)
        {
            foreach (GPcEquip taken in change.UnEquipped)
            {
                try
                {
                    if (!StoreSlot(pcNo, taken))
                    {
                        _logger.LogError("Character {PcNo} could not restore slot {Slot} after a failed change", pcNo, taken.Pos);
                    }
                }
                catch (Exception e) when (e is SqlException || e is InvalidOperationException)
                {
                    _logger.LogError(e, "Character {PcNo} could not restore slot {Slot} after a failed change", pcNo, taken.Pos);
                }
            }
        }

        /// <summary>
        ///     Tells the clients about a change that is already stored and applied. The order is
        ///     the one of the original: every emptied slot first (a swap reports the item that
        ///     leaves before the one that arrives), then the number of the item that was put on,
        ///     then the item itself, and the recalculated characteristics after all of it - the
        ///     client redraws the character by the slots and rereads the numbers behind them.
        ///     The list of the neighbours is read once into a reference: the visibility publishes
        ///     it whole, so the packets go to the ones that were around when the change happened
        /// </summary>
        /// <param name="client">Session the change belongs to</param>
        /// <param name="change">Plan that is already applied to the character</param>
        private void Report(GameSession client, GEquipChange change)
        {
            UniqueId uniqueId = client.Pc.UniqueId;
            List<GameSession> visibleCharacterGames = client.Pc.VisibleCharacterGames;

            foreach (GPcEquip taken in change.UnEquipped)
            {
                ItemPositionType position = (ItemPositionType)taken.Pos;

                _equipFactory.SendUnEquip(client, uniqueId, position);

                if (IsSeenAround(taken.Pos))
                {
                    foreach (GameSession visibleCharacterGame in visibleCharacterGames)
                    {
                        _equipFactory.SendUnEquip(visibleCharacterGame, uniqueId, position);
                    }
                }
            }

            if (change.Equipped != null)
            {
                // The number of the item that was put on goes to the owner alone and right before
                // the item itself: the client waits for it to play the change of the equipment
                _inventoryFactory.SendItemUseAck(client, change.Equipped.Item.Id);

                ItemPositionType position = (ItemPositionType)change.Equipped.Pos;

                _equipFactory.SendEquip(client, uniqueId, change.Equipped.Item, position);

                if (IsSeenAround(change.Equipped.Pos))
                {
                    foreach (GameSession visibleCharacterGame in visibleCharacterGames)
                    {
                        // The neighbours are told the real number of the item: the original picks
                        // the number it shows to others out of a field of its own, and we have no
                        // source of a substitute number
                        _equipFactory.SendEquip(visibleCharacterGame, uniqueId, change.Equipped.Item, position);
                    }
                }
            }

            // The speeds are the only part of the recalculation the neighbours need: they walk and
            // swing the character on their own screens by the rates of it
            _characteristicFactory.SendSpeedCharacteristics(client, client);

            foreach (GameSession visibleCharacterGame in visibleCharacterGames)
            {
                _characteristicFactory.SendSpeedCharacteristics(client, visibleCharacterGame);
            }

            _characteristicFactory.SendInformationAbilityCharacteristics(client);

            // The health goes out because the recalculation may have cut it: an item that raised
            // the maximum takes the points above the new maximum away with it when it comes off
            _characteristicFactory.SendHealthPointCharacteristics(client);

            // The weight the character may carry changes only when one of the moved items raises
            // it - the items themselves weigh in the inventory whether they are worn or not, and
            // the recorded changes of the equipment carry no weight packet at all
            if (change.HasWeightBonus)
            {
                _characteristicFactory.SendInfoWeight(client);
            }
        }

        /// <summary>
        ///     Whether a change of a slot is told to everybody around: the slots of the body are
        ///     drawn on the character and are seen by the neighbours, the carried ones are not.
        ///     The original keeps a table of the slots it broadcasts, its contents are not
        ///     restored - the range of the worn slots is taken instead
        /// </summary>
        /// <param name="pos">Slot that changed</param>
        /// <returns>True when the neighbours are told about the slot</returns>
        private static bool IsSeenAround(ItemEquipTypeEnum pos)
        {
            return pos >= VisibleSlotFirst && pos <= VisibleSlotLast;
        }
    }
}

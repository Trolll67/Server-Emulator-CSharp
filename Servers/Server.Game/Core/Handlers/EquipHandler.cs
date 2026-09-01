using Packets.Server.Game.Models.Receive.Inventory;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Core.Handlers.Interfaces;
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
    ///     Everything here happens on the network thread of the session, one request after another:
    ///     the plan is built, stored and applied without letting another request of the same
    ///     session in between. The threads that read the equipment from the side - the swings, the
    ///     ai of the monsters, the visibility - take snapshots by reference and are not touched by
    ///     the operation at all; they meet the change on the next snapshot they take
    /// </summary>
    [Handler]
    public class EquipHandler : IEquipHandler
    {
        /// <summary>
        ///     First and last slot the character keeps in the database: the procedure of the
        ///     equipment has a column for the weapon..cloak slots and for nothing else. The same
        ///     range decides who hears about a change of a slot - a slot of this range is worn on
        ///     the body and is seen by everybody around, the rest are carried and are the business
        ///     of the owner alone
        /// </summary>
        private const ItemEquipTypeEnum StoredSlotFirst = ItemEquipTypeEnum.Weapon;
        private const ItemEquipTypeEnum StoredSlotLast = ItemEquipTypeEnum.Cloak;

        private readonly ILogger<EquipHandler> _logger;
        private readonly IFnlGameRepository _gameRepository;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IInventoryFactory _inventoryFactory;
        private readonly IEquipFactory _equipFactory;

        public EquipHandler(ILogger<EquipHandler> logger, IFnlGameRepository gameRepository, ICharacteristicFactory characteristicFactory, IInventoryFactory inventoryFactory, IEquipFactory equipFactory)
        {
            _logger = logger;
            _gameRepository = gameRepository;
            _characteristicFactory = characteristicFactory;
            _inventoryFactory = inventoryFactory;
            _equipFactory = equipFactory;
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
            // yet or already on its way out, so there is nothing to put the item on
            if (!client.IsInWorld || client.Pc == null)
            {
                return;
            }

            GEquipChange change = client.Pc.EquipItem(wearEquipReqModel.SerialNumber);

            if (!Store(client, change))
            {
                return;
            }

            if (!client.Pc.ApplyEquipChange(change))
            {
                return;
            }

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

            GEquipChange change = client.Pc.UnEquipItem((ItemEquipTypeEnum)unEquipReqModel.Position);

            if (!Store(client, change))
            {
                return;
            }

            if (!client.Pc.ApplyEquipChange(change))
            {
                return;
            }

            Report(client, change);
        }

        /// <summary>
        ///     Writes every slot a plan moves into the database, the way the original writes it -
        ///     before the character starts wearing the change and before a single packet goes out.
        ///     The slots that are emptied go first: a swap moves the old item out of the very
        ///     column the new one is written into, so the order of the two writes is the whole
        ///     difference between a stored swap and a cleared slot.
        ///     A plan that is not stored is dropped whole: the character is not touched, so the
        ///     player keeps wearing what it wore and may simply ask again
        /// </summary>
        /// <param name="client">Session the plan belongs to</param>
        /// <param name="change">Plan built by the character</param>
        /// <returns>True when the change may be applied and reported</returns>
        private bool Store(GameSession client, GEquipChange change)
        {
            if (!change.IsSuccess)
            {
                // TODO: the original answers a refusal with 1102, the reason of the refusal and the
                // opcode of the request. The codes of the errors of the original are hashes built
                // at runtime and we have none of them for the equipment, so a refused request stays
                // silent for now and the reason is left in the log
                _logger.LogDebug("Character {PcNo} was refused an equipment change: {Error}", client.Pc.Simple.PcNo, change.Error);
                return false;
            }

            int pcNo = (int)client.Pc.Simple.PcNo;

            try
            {
                foreach (GPcEquip taken in change.UnEquipped)
                {
                    if (!Store(pcNo, taken.Pos, 0))
                    {
                        Restore(pcNo, change);
                        return false;
                    }
                }

                if (change.Equipped != null && !Store(pcNo, change.Equipped.Pos, (long)change.Equipped.SerialNo))
                {
                    Restore(pcNo, change);
                    return false;
                }
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                // FNLGame is down or the procedure failed: the plan is dropped and the character
                // keeps what it wears. That is the honest half of the deal - a change that is not
                // stored would be lost on the next entry anyway, and the player would meanwhile
                // fight with an item the database knows nothing about
                _logger.LogError(e, "Can not store an equipment change of character {PcNo}, the change is dropped", client.Pc.Simple.PcNo);
                Restore(pcNo, change);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Writes one slot of the equipment: the serial of the item that fills it, or a zero
        ///     for a slot that is emptied. The slots the procedure has no column for - the
        ///     materials - are left out: they are not stored anywhere yet, but the character is
        ///     allowed to move them, so the write is skipped instead of failing the whole change
        /// </summary>
        /// <param name="pcNo">Character the slot belongs to</param>
        /// <param name="pos">Slot being written</param>
        /// <param name="serialNo">Serial of the item, zero to empty the slot</param>
        /// <returns>True when the slot is stored or needs no storing</returns>
        private bool Store(int pcNo, ItemEquipTypeEnum pos, long serialNo)
        {
            // TODO: where the materials of a character are stored is not established - a change of
            // one of those slots lives until the character leaves the world
            if (pos < StoredSlotFirst || pos > StoredSlotLast)
            {
                return true;
            }

            if (_gameRepository.Equip(pcNo, (int)pos, serialNo))
            {
                return true;
            }

            // The procedure refused the write; the caller rolls the stored half back
            _logger.LogError("Character {PcNo} could not store slot {Slot}, the equipment change is dropped", pcNo, pos);
            return false;
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
                    if (!Store(pcNo, taken.Pos, (long)taken.SerialNo))
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
            return pos >= StoredSlotFirst && pos <= StoredSlotLast;
        }
    }
}

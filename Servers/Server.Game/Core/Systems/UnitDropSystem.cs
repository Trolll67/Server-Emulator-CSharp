using Database.DataModel.Enums;
using Database.DataModel.Models;
using Database.Fnl.Game;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Packets.Server.Game.Enums;
using Packets.Server.Game.Structures;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Network;
using Server.Game.Services;
using Server.Game.Services.Database;
using System;
using System.Collections.Generic;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     Loot of a killed monster: what falls out of it is drawn off the references of the parm
    ///     and either laid on the ground around the corpse or put straight into the bag of the
    ///     killer. Called from the swing pass at the death of the monster
    ///     (<c>AttackGameService.KillTarget</c>), between the experience and the packet of the
    ///     death - that is the order the original sends them in and the order the reference
    ///     captures show.
    ///     <para>
    ///     Three rolls decide what falls, exactly as the original rolls them:
    ///     one roll for the whole cycle of the groups of the monster, then a roll of its own for
    ///     every position of a group that came up, and then no roll at all - every item of the
    ///     position falls with the count and the status its row of the reference names.
    ///     </para>
    ///     <para>
    ///     Threads: the whole pass runs on the thread of the swings. An item that goes on the
    ///     ground never touches the database - it lives in memory without a serial number until
    ///     somebody picks it up, and the row is created by the pick-up on the network thread of
    ///     that session. The world of the items is written under the lock of the identification
    ///     service, the visibility pass reads it by snapshot from a thread of its own and draws
    ///     the new item on its next tick. Only the branch that puts an item into the bag of the
    ///     killer writes to the database, and the reference gives it to a couple of groups out of
    ///     hundreds - that branch is the one place the pass touches the bag of a player, so it
    ///     takes the lock of that bag for the whole of its work (<c>GPc.InventoryLock</c>): the
    ///     player writes into the same bag off the network thread of its session
    ///     </para>
    /// </summary>
    public class UnitDropSystem
    {
        /// <summary>
        ///     Width of the square the items are scattered over around the corpse, value of the
        ///     original: every horizontal axis draws its offset within half of it in each
        ///     direction
        /// </summary>
        private const int ScatterRange = 50;

        /// <summary>
        ///     Offset the degenerate case is replaced with, value of the original: an item that
        ///     drew a zero on both axes would lie inside the corpse, so one axis is pushed out by
        ///     this much
        /// </summary>
        private const int ScatterMinOffset = 20;

        /// <summary>
        ///     Scale the chance of a group is counted on: the chance of a group is a whole percent
        /// </summary>
        private const int GroupPercentScale = 100;

        /// <summary>
        ///     Scale the chance of a position is counted on, value of the original: the chance is
        ///     rolled in hundred-thousandths
        /// </summary>
        private const int ItemPercentScale = 100000;

        /// <summary>
        ///     Our reference keeps the chance of a position as a percent with a fraction, the
        ///     original keeps it in hundred-thousandths already: the percent is brought onto the
        ///     scale of the roll by this
        /// </summary>
        private const int ItemPercentToScale = 1000;

        /// <summary>
        ///     How far the count of the money that falls out wanders around the count of the
        ///     reference, in percent, value of the original
        /// </summary>
        private const int MoneySpreadPercent = 10;

        /// <summary>
        ///     Randomness of the loot. The system is asked from the swing pass, and Random is not
        ///     thread safe, so the one generator of the loot is used under a lock - the way the
        ///     session keys are drawn. One generator and not one per roll: a Random built anew on
        ///     every roll is seeded off the clock and gives out the very same number to every roll
        ///     of one tick
        /// </summary>
        private static readonly Random DropRandom = new Random();

        private readonly IdentificationService _identificationService;
        private readonly ParmRepository _parmRepository;
        private readonly IFnlGameRepository _gameRepository;
        private readonly IInventoryFactory _inventoryFactory;
        private readonly ILogger<UnitDropSystem> _logger;

        public UnitDropSystem(IdentificationService identificationService, ParmRepository parmRepository, IFnlGameRepository gameRepository, IInventoryFactory inventoryFactory, ILogger<UnitDropSystem> logger)
        {
            _identificationService = identificationService;
            _parmRepository = parmRepository;
            _gameRepository = gameRepository;
            _inventoryFactory = inventoryFactory;
            _logger = logger;
        }

        /// <summary>
        ///     Everything the monster leaves behind. The first roll is made once for the whole
        ///     cycle of the groups and every group is compared against that one roll - that is what
        ///     the original does, and it is reproduced here on purpose: the groups of one monster
        ///     are not independent of each other, a roll under the chance of the rarest group makes
        ///     every group of the monster come up at once.
        ///     Nothing here is allowed to break the death of the monster: the swing pass carries the
        ///     whole fight of the server, so a hole in the reference or a database that is down ends
        ///     as a line in the log and not as an interrupted death. The guard sits on the single
        ///     group and, inside it, on the single position: one row of the reference that fell over
        ///     costs the items of that row and never the loot the monster carries beside it
        /// </summary>
        /// <param name="client">Session that killed the monster</param>
        /// <param name="monster">Killed monster</param>
        public void DropItems(GameSession client, GMonster monster)
        {
            try
            {
                List<GDropGroup> groups = monster?.ParmMon?.DropGroups;

                if (groups == null || groups.Count == 0)
                {
                    return;
                }

                // One roll of a whole percent for the whole cycle: a group comes up when the roll
                // is under its chance
                int groupRoll = NextRandom(GroupPercentScale);

                foreach (GDropGroup group in groups)
                {
                    if (groupRoll >= group.Percent)
                    {
                        continue;
                    }

                    // Every group is dropped on its own: a group that failed costs its own items
                    // and leaves the rest of the loot of the monster where it is
                    try
                    {
                        DropGroup(client, monster, group);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Can not drop group {DropGroupId} of the loot of monster {ParmNo}", group.DropGroupId, monster?.ParmMon?.ParmNo);
                    }
                }
            }
            catch (Exception e)
            {
                // What is left for this one: the reference of the monster itself and the roll of
                // the cycle
                _logger.LogError(e, "Can not drop the loot of monster {ParmNo}", monster?.ParmMon?.ParmNo);
            }
        }

        /// <summary>
        ///     Positions of a group that came up. Every position is rolled on its own, on the scale
        ///     of hundred-thousandths: the chances inside a group add up to more than a hundred
        ///     percent often enough, so this is a row of independent rolls and not a wheel with one
        ///     winner
        /// </summary>
        /// <param name="client">Session that killed the monster</param>
        /// <param name="monster">Killed monster</param>
        /// <param name="group">Group of the loot that came up</param>
        private void DropGroup(GameSession client, GMonster monster, GDropGroup group)
        {
            foreach (DropGroupItem position in group.Items)
            {
                // A position that points at a row the reference does not hold is skipped: a hole in
                // the parm costs one item and not the whole death
                if (position.DropItem == null)
                {
                    continue;
                }

                if (NextRandom(ItemPercentScale) >= (double)position.Percent * ItemPercentToScale)
                {
                    continue;
                }

                // A position that came up drops everything it names without a roll of its own. The
                // original keeps up to twenty items in one position and our reference keeps one -
                // the row of the loot and the position of a group are one and the same thing here -
                // so the atomic set is a single row.
                // One position that fell over costs the items of that position alone: the ones
                // behind it in the group are rolled and dropped as if nothing happened
                try
                {
                    DropRow(client, monster, group.DropGroupType, position.DropItem);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Can not drop position {DropItemId} of group {DropGroupId} of the loot of monster {ParmNo}", position.DropItemId, group.DropGroupId, monster?.ParmMon?.ParmNo);
                }
            }
        }

        /// <summary>
        ///     One row of the loot: the item is built off the parm, gets the count and the status of
        ///     the row, and goes wherever the type of its group sends it
        /// </summary>
        /// <param name="client">Session that killed the monster</param>
        /// <param name="monster">Killed monster</param>
        /// <param name="groupType">Type of the group the row belongs to</param>
        /// <param name="row">Row of the loot</param>
        private void DropRow(GameSession client, GMonster monster, DropGroupTypeEnum groupType, DropItem row)
        {
            GItem item = BuildItem(row);

            if (item == null)
            {
                _logger.LogDebug("The loot row {DropItemId} names item {ItemId} the parm does not hold", row.Id, row.ItemId);
                return;
            }

            int count = row.Count;

            if (IsMoney(item))
            {
                // The money of the original is an item like any other, only its count wanders and
                // it always lands on the ground - the type of the group does not hold it back. The
                // tax of a besieged castle takes a share of it away before it falls; there are
                // neither castles nor treasuries here, so nothing is taken
                count = SpreadMoneyCount(count);
                groupType = DropGroupTypeEnum.GroupGround;
            }

            if (count <= 0)
            {
                return;
            }

            // An item that does not stack lies one to a row: no row of our reference names more
            // than one of those, but a row that did would otherwise put a stack of them where the
            // bag and the stored procedure both refuse to keep one
            bool isStackable = GPcInventory.IsStackable(item);
            int rows = isStackable ? 1 : count;
            int countPerRow = isStackable ? count : 1;

            for (int i = 0; i < rows; i++)
            {
                // Every item lives on its own from here on: the one built above goes first, the
                // rest are built anew off the parm. The copy constructor of the item is not used
                // for that on purpose - it leaves the fake number and the status behind
                GItem dropped = i == 0 ? item : BuildItem(row);

                if (dropped == null)
                {
                    return;
                }

                dropped.Count = countPerRow;

                Deliver(client, monster, groupType, dropped);
            }
        }

        /// <summary>
        ///     Where a dropped item goes. The bag of the killer takes it only when the group says
        ///     so and the killer is still there to take it: anything else - a killer who left the
        ///     world, a bag that refuses the item, a write that failed - lays the item on the
        ///     ground, where anybody may pick it up
        /// </summary>
        /// <param name="client">Session that killed the monster</param>
        /// <param name="monster">Killed monster the item falls out of</param>
        /// <param name="groupType">Type of the group the item comes from</param>
        /// <param name="item">Item that falls out</param>
        private void Deliver(GameSession client, GMonster monster, DropGroupTypeEnum groupType, GItem item)
        {
            if (groupType == DropGroupTypeEnum.GroupInven && PutIntoBag(client, item))
            {
                return;
            }

            PutOnGround(monster, item);
        }

        /// <summary>
        ///     Item of the loot as it lies in the world: no serial number of its own (the row of
        ///     the database is created by the pick-up, so the death of a monster costs no query
        ///     at all), the count and the status of the row of the reference, and a place of its
        ///     own around the corpse
        /// </summary>
        /// <param name="monster">Monster the item falls out of</param>
        /// <param name="item">Item that goes on the ground</param>
        private void PutOnGround(GMonster monster, GItem item)
        {
            GPublicItem publicItem = new GPublicItem
            {
                Item = item,
                Position = GetGroundPosition(monster.PositionCur),
                // The item is drawn to everybody around by the visibility pass on its next tick:
                // it sends the packet of an item that entered the map to the ones who see it for
                // the first time, so the drop writes no packet of its own
                IsVsibleFirst = true,
                DateCreate = DateTime.Now
            };

            _identificationService.AddItem(publicItem);
        }

        /// <summary>
        ///     Item that goes straight into the bag of the killer. The three steps are the ones of
        ///     the pick-up: the character checks the request and builds the plan, the row is created
        ///     in the database, and only then the plan is applied with the serial number the
        ///     procedure issued. A plan that is not applied leaves the bag exactly as it was
        /// </summary>
        /// <param name="client">Session that killed the monster</param>
        /// <param name="item">Item the bag is offered</param>
        /// <returns>True when the item ended up in the bag; false leaves it to the ground</returns>
        private bool PutIntoBag(GameSession client, GItem item)
        {
            if (client == null || client.Pc == null || !client.IsInWorld)
            {
                return false;
            }

            GInventoryChange change;
            GItem added;

            // The three steps are one sequence for one writer at a time, the query included: this
            // runs on the thread of the swings while the player picks things up and throws them
            // away on the network thread of its session, and a plan built over the bag of before
            // the other operation would publish its list over the applied one and lose it
            // (GPc.InventoryLock)
            lock (client.Pc.InventoryLock)
            {
                change = client.Pc.PickUpItem(item, item.Count);

                if (!change.IsSuccess)
                {
                    _logger.LogDebug("The bag of character {PcNo} does not take item {ItemId} of the loot: {Error}", client.Pc.Simple.PcNo, item.Id, change.Error);
                    return false;
                }

                PushItemRow stored;

                try
                {
                    // A zero source serial tells the procedure to create a row; the merging of the
                    // stacks is its business, and the serial it answers with is the one of the row
                    // the item ended up in
                    stored = _gameRepository.PushItem((int)client.Pc.Simple.PcNo, 0, item.Id, item.TermOfValidity,
                        item.Count, item.UseCount, item.IsConfirm, (byte)item.Status, GPcInventory.IsStackable(item));
                }
                catch (Exception e) when (e is SqlException || e is InvalidOperationException)
                {
                    _logger.LogError(e, "Can not store item {ItemId} of the loot into the bag of character {PcNo}", item.Id, client.Pc.Simple.PcNo);
                    return false;
                }

                if (!stored.IsSuccess)
                {
                    _logger.LogError("The procedure refused item {ItemId} of the loot for character {PcNo} with code {ErrorCode}", item.Id, client.Pc.Simple.PcNo, stored.ErrorCode);
                    return false;
                }

                ulong serialNo = (ulong)stored.SerialNo;

                // The procedure is the arbiter of the merge: the serial it answers with names the
                // row the item ended up in, and the bag is brought over to that row whether the
                // procedure merged the item or took a row of its own for it - see
                // GPc.ApplyInventoryChange
                // Safety net: the overload never refuses right now; if it ever did, the row is
                // already written and the item is not handed back, nor put back on the ground
                if (!client.Pc.ApplyInventoryChange(change, serialNo))
                {
                    _logger.LogError("Character {PcNo} could not apply inventory change for row {SerialNo} of item {ItemId} of the loot, the row is already written", client.Pc.Simple.PcNo, serialNo, item.Id);
                    return false;
                }

                // The row of the bag as it is now, read by the serial of the procedure
                added = client.Pc.FindItem(serialNo) ?? change.Item;
            }

            // The packet goes out with the lock let go - the bag already holds the item.
            // The reason of the original is the one of a pick-up: the item is given to the killer
            // the same way a thing lifted off the ground is
            _inventoryFactory.SendItemAdd(client, added, client.Pc.UniqueId, Reason.Pickup);

            return true;
        }

        /// <summary>
        ///     Item built off the row of the loot. It is asked of the parm every time and never
        ///     copied off another item: the parm hands out a fresh one with the whole mapping on
        ///     it - the fake number an unidentified item is drawn under among the rest.
        ///     The status is the one of the row of the loot, the serial number stays at zero (it is
        ///     assigned only when the item is picked up), and the tick the term runs out at
        ///     carries the raw term of the parm - that is what
        ///     the original sends for an item that has never been through a bag
        /// </summary>
        /// <param name="row">Row of the loot</param>
        /// <returns>Item of the world, null when the parm holds no such item</returns>
        private GItem BuildItem(DropItem row)
        {
            GItem item = _parmRepository.GetGItemById(row.ItemId);

            if (item == null)
            {
                return null;
            }

            item.Status = row.Status;
            item.SerialNumber = 0;
            item.EndTick = (uint)item.TermOfValidity;

            return item;
        }

        /// <summary>
        ///     Place an item lands on. The offsets of both horizontal axes are drawn within half of
        ///     the scatter in each direction and the height is the one of the corpse - the server
        ///     has no ground of its own to take another one from. Two zeroes would leave the item
        ///     inside the corpse, so one axis is pushed out by the smallest offset. The original
        ///     clips the place to the map and asks whether it can be walked to; we have neither
        ///     check, so the item lies where it fell
        /// </summary>
        /// <param name="corpse">Place the monster died at</param>
        private static Vector3 GetGroundPosition(Vector3 corpse)
        {
            Vector3 position = corpse ?? new Vector3();

            int offsetX = ScatterRange / 2 - NextRandom(ScatterRange);
            int offsetZ = ScatterRange / 2 - NextRandom(ScatterRange);

            if (offsetX == 0 && offsetZ == 0)
            {
                offsetX = ScatterMinOffset;
            }

            return new Vector3(position.X + offsetX, position.Y, position.Z + offsetZ);
        }

        /// <summary>
        ///     Whether the item is the money of the world. The original tells it by the number of
        ///     the item of silver; the type of the parm row says the same thing and keeps a number
        ///     of the reference out of the code
        /// </summary>
        /// <param name="item">Item that falls out</param>
        private static bool IsMoney(GItem item)
        {
            return item.Type == ItemTypeEnum.Gold;
        }

        /// <summary>
        ///     Count of the money that falls out: the count of the reference with a spread of ten
        ///     percent around it, the way the original spreads it. The spread is never smaller than
        ///     one, so even the smallest heap wanders. The multipliers of the original - the global
        ///     one, the one of the place of the map and the premium one - are all at one here
        /// </summary>
        /// <param name="count">Count the row of the loot names</param>
        private static int SpreadMoneyCount(int count)
        {
            int spread = count * MoneySpreadPercent / 100;

            if (spread < 1)
            {
                spread = 1;
            }

            return count + NextRandom(2 * spread + 1) - spread;
        }

        /// <summary>
        ///     Next roll of the loot, from zero to the bound. The generator is one for the whole
        ///     system and is not thread safe, so every roll goes through the lock on it
        /// </summary>
        /// <param name="maxValue">Bound of the roll, not included</param>
        private static int NextRandom(int maxValue)
        {
            lock (DropRandom)
            {
                return DropRandom.Next(maxValue);
            }
        }
    }
}

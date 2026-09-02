using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Reason a bag operation refuses to do anything. The names are the ones of the original,
    ///     with the prefix of its error constants dropped, so the code of the refusal a client
    ///     gets is built off the very name the original would have sent.
    ///     The list lives here and not beside the codes of the equipment (ErrorEnum) only because
    ///     that enum carries no name of a bag refusal yet: both belong in one place as soon as the
    ///     codes are put on the names of the original
    /// </summary>
    public enum InventoryErrorEnum
    {
        /// <summary>
        ///     No such item: nothing lies where the request points, or the bag holds no row of
        ///     that serial number
        /// </summary>
        ItemNotExist,

        /// <summary>
        ///     A dead character neither picks anything up nor throws anything away
        /// </summary>
        CharAlreadyDie,

        /// <summary>
        ///     Count of a pick-up that makes no sense: nothing at all, or more than one of an item
        ///     that does not stack
        /// </summary>
        ItemInvalidCnt,

        /// <summary>
        ///     Bag is full: it holds no row to merge the item into and no room for one more
        /// </summary>
        InvFull,

        /// <summary>
        ///     Merged stack would hold more than one row may
        /// </summary>
        ItemTooManyStackCnt,

        /// <summary>
        ///     Character carries too much already to take the item
        /// </summary>
        ItemTooHeavy,

        /// <summary>
        ///     Row of the bag holds fewer items than the request wants to throw away
        /// </summary>
        ItemLack,

        /// <summary>
        ///     Item is worn: it is not thrown away and is not taken off by itself
        /// </summary>
        ItemEquipped
    }

    /// <summary>
    ///     What an operation of the bag is going to do to a character, or the reason it refuses to
    ///     do anything at all.
    ///     The operation is split in two exactly the way the equipment is (see GEquipChange): the
    ///     character checks the request and builds this plan, the caller writes what the plan names
    ///     wherever it has to be written - the row of the item is created, moved or destroyed by a
    ///     stored procedure - and only then the plan is applied to the character
    ///     (<c>GPc.ApplyInventoryChange</c>). Nothing of the character moves while the plan is
    ///     being built, so a caller that fails to store the change drops the plan and leaves the
    ///     bag as it was. That is why the model needs to know nothing about sessions, packets or a
    ///     database.
    ///     A plan is built and applied on one thread and used once: it carries the whole list of
    ///     rows the bag is about to publish, so a second plan built over the same character before
    ///     the first one is applied is stale and must not be applied after it
    /// </summary>
    public class GInventoryChange
    {
        private static readonly IReadOnlyList<GItem> None = new List<GItem>();

        private GInventoryChange(InventoryErrorEnum error)
        {
            Error = error;
            Items = None;
        }

        private GInventoryChange(IReadOnlyList<GItem> items, GItem item, int count, int leftCount)
        {
            IsSuccess = true;
            Items = items;
            Item = item;
            Count = count;
            LeftCount = leftCount;
        }

        /// <summary>
        ///     Whether the operation goes through. A refused one carries the reason in Error and
        ///     changes nothing anywhere
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        ///     Reason of a refusal, meaningless while IsSuccess holds
        /// </summary>
        public InventoryErrorEnum Error { get; }

        /// <summary>
        ///     List of rows the bag publishes when the plan is applied. Built beside the list the
        ///     character carries now, which is left untouched for the readers that are already
        ///     walking it. A merge and a drop of a part of a stack add and take away no row at all,
        ///     and both publish the very list the plan was built over
        /// </summary>
        public IReadOnlyList<GItem> Items { get; }

        /// <summary>
        ///     Row of the bag the operation changes: the one a pick-up merges into or adds, the one
        ///     a drop takes items out of
        /// </summary>
        public GItem Item { get; }

        /// <summary>
        ///     How many items move: into the bag on a pick-up, out of it on a drop
        /// </summary>
        public int Count { get; }

        /// <summary>
        ///     Count the row holds once the plan is applied: the whole stack after a merge, what is
        ///     left of it after a drop, zero when the row leaves the bag
        /// </summary>
        public int LeftCount { get; }

        /// <summary>
        ///     Whether the item went into a row the bag already held. The row keeps its serial
        ///     number - the stored procedure merges the stack of the database on its own and hands
        ///     back the serial number of the row it merged into, and a caller that gets another one
        ///     is looking at a bag that no longer matches the database
        /// </summary>
        public bool IsMerged { get; private set; }

        /// <summary>
        ///     Whether the bag takes a new row in. The row waits for the serial number the database
        ///     issues to it - see the overload of GPc.ApplyInventoryChange that carries it
        /// </summary>
        public bool IsAdded { get; private set; }

        /// <summary>
        ///     Whether the row leaves the bag whole: the drop takes every item of the stack
        /// </summary>
        public bool IsRemoved { get; private set; }

        /// <summary>
        ///     Item a drop is going to put on the ground, built off the row of the bag and holding
        ///     the count that leaves it. It carries the serial number of the row it came off - the
        ///     caller replaces it with the one the stored procedure issues to the item of a
        ///     partially dropped stack
        /// </summary>
        public GItem Ground { get; private set; }

        /// <summary>
        ///     Plan that does nothing but name the reason
        /// </summary>
        /// <param name="error">Reason of the refusal</param>
        public static GInventoryChange Refused(InventoryErrorEnum error)
        {
            return new GInventoryChange(error);
        }

        /// <summary>
        ///     Plan of a pick-up that goes into a row the bag already holds
        /// </summary>
        /// <param name="items">List of rows to publish - the one the bag already carries</param>
        /// <param name="stack">Row the item goes into</param>
        /// <param name="count">How many items go in</param>
        /// <param name="leftCount">Count the row holds after the merge</param>
        public static GInventoryChange Merged(IReadOnlyList<GItem> items, GItem stack, int count, int leftCount)
        {
            GInventoryChange change = new GInventoryChange(items, stack, count, leftCount);

            change.IsMerged = true;

            return change;
        }

        /// <summary>
        ///     Plan of a pick-up that takes a new row into the bag
        /// </summary>
        /// <param name="items">List of rows to publish, the new one included</param>
        /// <param name="added">Row the bag takes in</param>
        /// <param name="count">How many items the row holds</param>
        public static GInventoryChange Added(IReadOnlyList<GItem> items, GItem added, int count)
        {
            GInventoryChange change = new GInventoryChange(items, added, count, count);

            change.IsAdded = true;

            return change;
        }

        /// <summary>
        ///     Plan of a drop: a part of a stack leaves the row it lies in, the whole of it leaves
        ///     the bag
        /// </summary>
        /// <param name="items">List of rows to publish</param>
        /// <param name="item">Row the items are taken out of</param>
        /// <param name="count">How many items leave</param>
        /// <param name="leftCount">Count the row holds afterwards, zero when the row leaves</param>
        /// <param name="ground">Item built for the ground</param>
        public static GInventoryChange Dropped(IReadOnlyList<GItem> items, GItem item, int count, int leftCount,
            GItem ground)
        {
            GInventoryChange change = new GInventoryChange(items, item, count, leftCount);

            change.IsRemoved = leftCount == 0;
            change.Ground = ground;

            return change;
        }
    }
}

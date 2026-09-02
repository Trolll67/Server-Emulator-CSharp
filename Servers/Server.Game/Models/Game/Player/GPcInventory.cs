using Database.DataModel.Enums;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     Bag of a character: the rows it holds, the weight they add up to and the caps the
    ///     original puts on both. The rows are published the way the worn items are - an operation
    ///     of the bag replaces the list whole (GPc.ApplyInventoryChange), so a reader that took the
    ///     reference before the change keeps walking the bag of before it and never a list being
    ///     rebuilt under it.
    ///     The weight, its cap and the state of the load are written by two hands - an operation of
    ///     the bag and the recalc of the characteristics that moves the cap - and both write them
    ///     under one lock, GPc.InventoryLock; the order the character takes its locks in is written
    ///     down beside that lock
    /// </summary>
    public class GPcInventory
    {
        /// <summary>
        ///     Rows the bag holds at most, value of the original: the row that does not fit is
        ///     refused with "bag full", and the read of the bag out of the database stops at the
        ///     same count
        /// </summary>
        public const int MaxSize = 160;

        /// <summary>
        ///     Items one row holds at most, value of the original. The stored procedure that merges
        ///     the stacks keeps to the same cap, so a merge over it is refused here as well
        /// </summary>
        public const int MaxStackCount = 1000000000;

        /// <summary>
        ///     Share of the weight cap a character starts to be heavy at; at the cap itself it is
        ///     overloaded. Values of the original
        /// </summary>
        private const double HeavyRate = 0.7;

        public GPcInventory()
        {
            Items = new List<GItem>();
        }

        /// <summary>
        ///     Rows of the bag. The list is replaced whole and never changed in place - see the
        ///     remark of the class
        /// </summary>
        public List<GItem> Items { get; set; }

        public uint ChaosSilver { get; set; }
        public ulong ChaosSilverSerialNo { get; set; }
        public int Weight { get; set; }
        public int MaxWeight { get; set; }
        public WeightStatusEnum WeightStatus { get; set; }

        /// <summary>
        ///     Set the weight the character may carry. The cap moves with the characteristics and
        ///     with the worn items, and the state of the load is rebuilt behind it: the very same
        ///     weight is normal under one cap and an overload under another
        /// </summary>
        /// <param name="maxWeight">Weight cap of the character</param>
        public void SetMaxWeight(int maxWeight)
        {
            if (MaxWeight != maxWeight)
            {
                MaxWeight = maxWeight;
                CalcWeightStatus();
            }
        }

        /// <summary>
        ///     Sum up the weight of the bag over its rows and rebuild the state of the load behind
        ///     it. Called on every change of the bag: the state depends on the weight just as much
        ///     as it does on the cap, and a bag that got heavier under an unchanged cap has to end
        ///     up in the same state as one that got the cap lowered under it.
        ///     A row holds up to a billion items, and its weight alone overflows a machine word of
        ///     the character, so the sum is counted wide and clipped: an overloaded character stays
        ///     overloaded instead of turning light again on the wrap
        /// </summary>
        public void RecalcWeight()
        {
            // Snapshot of the rows: an operation of the bag publishes another list, and a sum
            // taken over two of them would count neither bag
            List<GItem> items = Items;

            long weight = 0;
            foreach (GItem item in items)
            {
                weight += (long)item.Count * item.Weight;
            }

            Weight = weight > int.MaxValue ? int.MaxValue : (int)weight;
            CalcWeightStatus();
        }

        /// <summary>
        ///     Whether the item stacks. The column of the parm carries a flag and not a cap of its
        ///     own: anything but zero means the items of that number lie in one row, and how many
        ///     of them a row holds is the cap above
        /// </summary>
        /// <param name="item">Item to look at</param>
        public static bool IsStackable(GItem item)
        {
            return item.MaxStack != 0;
        }

        /// <summary>
        ///     Row of a bag an item goes into, none when it goes into a row of its own. The
        ///     conditions are the ones of the original and of the stored procedure that merges the
        ///     stacks of the database - the number of the item, its status, the way it is bound to
        ///     the character, the term it is given, the flag of an identified item, and the item has
        ///     to stack at all. The model must not merge what the procedure keeps apart, or the bag
        ///     and the database tell two different stories.
        ///     The date the term of the row runs out at, which the procedure compares as well, is
        ///     left out of the conditions: it lives in the row of the database and nothing of ours
        ///     keeps it - the rows read into the bag come in with EndTick at zero while a picked up
        ///     item carries a tick of its own, so asking for it here would keep apart what the
        ///     procedure merges (the debt of EndTick and of the read of the bag). The procedure is
        ///     the arbiter of every merge either way: it answers with the serial number of the row
        ///     the item ended up in, and the bag follows that serial number - see
        ///     GPc.ApplyInventoryChange
        /// </summary>
        /// <param name="items">Snapshot of the rows of the bag</param>
        /// <param name="item">Item that is being taken in</param>
        public static GItem FindStack(List<GItem> items, GItem item)
        {
            if (!IsStackable(item))
            {
                return null;
            }

            // The last of the conditions is the one above: a row of the same number comes off the
            // same parm row and carries the same flag
            foreach (GItem row in items)
            {
                if (row.Id == item.Id
                    && row.Status == item.Status
                    && row.ItemBind == item.ItemBind
                    && row.TermOfValidity == item.TermOfValidity
                    && row.IsConfirm == item.IsConfirm)
                {
                    return row;
                }
            }

            return null;
        }

        /// <summary>
        ///     State of the load off the weight and the cap. A cap that is not counted yet - the
        ///     character is still being built - leaves the state normal instead of reading an empty
        ///     bag as an overloaded one
        /// </summary>
        private void CalcWeightStatus()
        {
            if (MaxWeight <= 0)
            {
                WeightStatus = WeightStatusEnum.Normal;
                return;
            }

            if (Weight >= MaxWeight)
                WeightStatus = WeightStatusEnum.Over;
            else if (Weight >= MaxWeight * HeavyRate)
                WeightStatus = WeightStatusEnum.Heavy;
            else
                WeightStatus = WeightStatusEnum.Normal;
        }
    }
}

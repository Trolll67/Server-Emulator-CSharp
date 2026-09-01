using Database.DataModel.Enums;
using System.Collections.Generic;

namespace Server.Game.Models.Game
{
    /// <summary>
    ///     What an equip operation is going to do to a character, or the reason it refuses to do
    ///     anything at all.
    ///     The operation is split in two on purpose: the character checks the request and builds
    ///     this plan, the caller writes the slots the plan names wherever it has to write them, and
    ///     only then the plan is applied to the character (<c>GPc.ApplyEquipChange</c>). Nothing
    ///     of the character moves while the plan is being built, so a caller that fails to store
    ///     the change simply drops the plan and leaves the character as it was. That is why the
    ///     model needs to know nothing about sessions, packets or a database.
    ///     A plan is built and applied on one thread and used once: it carries the whole list of
    ///     worn items the character is about to publish, so a second plan built over the same
    ///     character before the first one is applied is stale and must not be applied after it
    /// </summary>
    public class GEquipChange
    {
        private static readonly IReadOnlyList<GPcEquip> None = new List<GPcEquip>();

        private GEquipChange(ErrorEnum error)
        {
            Error = error;
            UnEquipped = None;
            Worn = None;
        }

        private GEquipChange(List<GPcEquip> worn, IReadOnlyList<GPcEquip> unEquipped, GPcEquip equipped)
        {
            IsSuccess = true;
            Worn = worn;
            UnEquipped = unEquipped;
            Equipped = equipped;
        }

        /// <summary>
        ///     Whether the operation goes through. A refused one carries the reason in Error and
        ///     changes nothing anywhere
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        ///     Reason of a refusal, meaningless while IsSuccess holds
        /// </summary>
        public ErrorEnum Error { get; }

        /// <summary>
        ///     Records that leave the character: the item of an occupied slot pushed out by a new
        ///     one, and the arrows that follow a bow out of the hand - up to two of them, and a
        ///     caller that reports the change to a client has to report every one
        /// </summary>
        public IReadOnlyList<GPcEquip> UnEquipped { get; }

        /// <summary>
        ///     Record the character puts on, empty when the operation only takes items off
        /// </summary>
        public GPcEquip Equipped { get; }

        /// <summary>
        ///     Whether one of the items the operation moves carries a bonus to the weight the
        ///     character may carry: the cap of the character changes only then, and only then is
        ///     there anything to tell a client about it
        /// </summary>
        public bool HasWeightBonus { get; private set; }

        /// <summary>
        ///     List of worn items the character publishes when the plan is applied. Built beside
        ///     the list the character wears now, which is left untouched for the readers that are
        ///     already walking it
        /// </summary>
        public IReadOnlyList<GPcEquip> Worn { get; }

        /// <summary>
        ///     Plan that does nothing but name the reason
        /// </summary>
        /// <param name="error">Reason of the refusal</param>
        public static GEquipChange Refused(ErrorEnum error)
        {
            return new GEquipChange(error);
        }

        /// <summary>
        ///     Plan of a change that passed every check of the character
        /// </summary>
        /// <param name="worn">List of worn items to publish</param>
        /// <param name="unEquipped">Records that leave the character</param>
        /// <param name="equipped">Record the character puts on, empty when nothing is put on</param>
        public static GEquipChange Done(List<GPcEquip> worn, List<GPcEquip> unEquipped, GPcEquip equipped)
        {
            GEquipChange change = new GEquipChange(worn, unEquipped, equipped);

            change.HasWeightBonus = equipped != null && equipped.Item.AddWeight != 0;
            foreach (GPcEquip taken in unEquipped)
            {
                change.HasWeightBonus |= taken.Item.AddWeight != 0;
            }

            return change;
        }
    }
}

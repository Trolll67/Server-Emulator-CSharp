using Database.DataModel.Enums;
using System.Collections.Generic;

namespace Database.DataModel.Models
{
    public class DropGroup
    {
        public DropGroup()
        {
            Items = new List<DropGroupItem>();
        }
        public int DropGroupId { get; set; }

        /// <summary>
        ///     Where the items of the group go when the group rolls: on the ground or straight into
        ///     the bag of the killer. Comes off the header row of the group in the reference
        /// </summary>
        public DropGroupTypeEnum DropGroupType { get; set; }

        /// <summary>
        ///     Chance of the whole group to roll, a whole percent 0..100. The chance belongs to the
        ///     link "monster -> group" and not to the group itself, so the reference read leaves it
        ///     at zero: it is filled on the copy of the group a monster parm keeps
        /// </summary>
        public byte Percent { get; set; }

        public List<DropGroupItem> Items { get; set; }
    }
}

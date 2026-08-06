using System.Collections.Generic;
using Database.DataModel.Enums;
using Database.DataModel.Models;

namespace Database.Fnl.Parm
{
    /// <summary>
    ///     Access to the FNLParm reference-data tables through the original stored procedures.
    ///     Every method reads one procedure and returns ready to use POCO from Database.DataModel.
    ///     Cross-entity relations that a procedure carries as its own result sets are attached to
    ///     the parent POCO (for example <see cref="Item.Skills"/>) as stubs that only hold the linked
    ///     Id; the master rows come from <see cref="GetSkills"/>/<see cref="GetAbnormals"/>/… and the
    ///     final stitching (id -> master row) is done by the Server.Game caching layer (task T6).
    ///     This repository is separate from <see cref="IFnlParmRepository"/>, which serves the Login path
    /// </summary>
    public interface IFnlParmReferenceRepository
    {
        /// <summary>
        ///     Reads every item with its link collections (dbo.UspGetParmItem).
        ///     The procedure returns seven result sets over DT_Item, each joined to a different link
        ///     table (skill, attribute add/resist, slain, protect, abnormal add/resist). The base
        ///     columns are identical in every set, so the item body is read once and the link id of
        ///     each set is appended to the matching collection of the item
        /// </summary>
        IReadOnlyList<Item> GetItems();

        /// <summary>
        ///     Reads every monster with its link collections (dbo.UspGetParmMonsterEx). Same seven
        ///     result set shape as the items: slain, protect, drop, attribute add/resist, abnormal
        ///     add/resist. Roles and spots come from their own procedures
        /// </summary>
        IReadOnlyList<Monster> GetMonsters();

        /// <summary>
        ///     Reads the monster -> role pairs (dbo.UspGetParmMonsterRole). Grouping by monster is
        ///     left to the caller (task T6 fills <see cref="Monster.Roles"/>)
        /// </summary>
        IReadOnlyList<MonsterRoleRow> GetMonsterRoles();

        /// <summary>
        ///     Reads the monster spots (dbo.UspGetParmMonsterSpot)
        /// </summary>
        IReadOnlyList<MonsterSpot> GetMonsterSpots();

        /// <summary>
        ///     Reads the spot groups (dbo.UspGetParmMonsterSpotGroup). A spot links to a group by
        ///     <see cref="MonsterSpot.GroupId"/> == <see cref="MonsterSpotGroup.Id"/>; duplicate group
        ///     rows the join produces per monster are collapsed by id here
        /// </summary>
        IReadOnlyList<MonsterSpotGroup> GetMonsterSpotGroups();

        /// <summary>
        ///     Reads the master abnormal table (dbo.UspGetParmAbnormal)
        /// </summary>
        IReadOnlyList<Abnormal> GetAbnormals();

        /// <summary>
        ///     Reads the master abnormal-add table (dbo.UspGetParmAbnormalAdd)
        /// </summary>
        IReadOnlyList<AbnormalAdd> GetAbnormalAdds();

        /// <summary>
        ///     Reads the master abnormal-resist table (dbo.UspGetParmAbnormalResist)
        /// </summary>
        IReadOnlyList<AbnormalResist> GetAbnormalResists();

        /// <summary>
        ///     Reads every skill with its link collections (dbo.UspGetParmSkill): three result sets
        ///     over DT_Skill joined to attribute, abnormal and slain link tables
        /// </summary>
        IReadOnlyList<Skill> GetSkills();

        /// <summary>
        ///     Reads the drop items (dbo.UspGetParmDrop): the pool a drop group can pick from
        /// </summary>
        IReadOnlyList<DropItem> GetDropItems();

        /// <summary>
        ///     Reads the drop groups (dbo.UspGetParmDropGroup): each group with the drop items and
        ///     their pick chance
        /// </summary>
        IReadOnlyList<DropGroup> GetDropGroups();

        /// <summary>
        ///     Reads the experience table (dbo.UspGetParmExpTable)
        /// </summary>
        IReadOnlyList<ExpRow> GetExps();
    }

    /// <summary>
    ///     One row of dbo.UspGetParmMonsterRole: the role a monster plays. There is no Database.DataModel
    ///     POCO for the link itself (<see cref="Monster.Roles"/> is a flat list of roles), so the pair
    ///     is carried here for the caller to group by <see cref="MonsterId"/>
    /// </summary>
    public class MonsterRoleRow
    {
        public int MonsterId { get; set; }
        public NpcRoleEnum Role { get; set; }
    }

    /// <summary>
    ///     One row of dbo.UspGetParmExpTable. There is no experience POCO in Database.DataModel, so the
    ///     three columns are carried in this small row (mirrors the shipped DT_Exp: level, total exp,
    ///     rest-exp rate)
    /// </summary>
    public class ExpRow
    {
        public short Level { get; set; }
        public long Exp { get; set; }
        public int RestExpRate { get; set; }
    }
}

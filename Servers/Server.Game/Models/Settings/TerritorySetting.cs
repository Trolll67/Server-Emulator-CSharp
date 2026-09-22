using System.Collections.Generic;

namespace Server.Game.Models.Settings
{
    /// <summary>
    ///     One territory of the world, STerritoryInfo of the original. The world is painted into
    ///     territories by the region layer of the maps, and every territory holds the point its
    ///     dead are raised at and the towns that belong to it
    /// </summary>
    public class TerritorySetting
    {
        /// <summary>
        ///     Number of the territory, ETerritory of the original. It is also the value the
        ///     region layer keeps in the low nibble of its cell
        /// </summary>
        public int No { get; set; }

        /// <summary>
        ///     Name of the territory, for the log and for reading this file
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Colour that marks this territory on the region layer, six hex digits RRGGBB.
        ///     The original keeps the same table and looks the colour of the cell up in it
        /// </summary>
        public string Rgb { get; set; }

        /// <summary>
        ///     Where a character that died in this territory is raised, mRespawnPos of the original.
        ///     All zeroes mean the territory has no point of its own - the battlefields are like that
        /// </summary>
        public PositionSetting Respawn { get; set; }

        /// <summary>
        ///     Towns of the territory, mTownPos[5]. The original picks one of the five at random
        ///     when it has to send a character to a town rather than to the respawn point
        /// </summary>
        public List<PositionSetting> Towns { get; set; } = new List<PositionSetting>();
    }

    /// <summary>
    ///     A point of the world, C3D&lt;float&gt; of the original: Y is the height
    /// </summary>
    public class PositionSetting
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        /// <summary>
        ///     Whether the point is a real one. A point that was never filled is all zeroes,
        ///     which is the corner of the world and never a place anybody is raised at
        /// </summary>
        public bool IsSet => X != 0f || Y != 0f || Z != 0f;
    }
}

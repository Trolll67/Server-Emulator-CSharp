using System.Collections.Generic;

namespace Server.Game.Models.Settings
{
    /// <summary>
    ///     Config for setting game server
    /// </summary>
    public class GameSetting
    {
        public short Id { get; set; }

        /// <summary>
        ///     Address the server listens on. Doubles as the key this field server is looked up by
        ///     in FNLParm.TblParmSvr (mMajorIp), which is where the listen port comes from
        /// </summary>
        public string ServerIp { get; set; }

        // Garbage settings
        public int GarbageItems { get; set; }
        public int GarbageUnits { get; set; }

        // Recovery characteristics hp mp
        public int RecoveryCharacteristics { get; set; }

        // Visible settings
        public int VisibleConnections { get; set; }
        public int VisibleItems { get; set; }
        public int VisibleUnits { get; set; }

        // Inventar settings
        public int ItemPickUpDistance { get; set; }

        /// <summary>
        ///     Move settings. How far a character may move with one packet, in units. Kept inside
        ///     [500, 10000) as the original does, any other value means the default of 500
        /// </summary>
        public int MoveMaxDistancePerTick { get; set; }

        public int SavePcsEverySeconds { get; set; }

        /// <summary>
        ///     Периоды фоновых заданий планировщика в миллисекундах, по имени задания. Ключа нет —
        ///     задание работает со своим значением по умолчанию
        /// </summary>
        public Dictionary<string, int> JobIntervals { get; set; } = new Dictionary<string, int>();

        // Стартовые карта и позиция при создании персонажа, по одной записи на класс
        public List<StartPosition> StartPositions { get; set; } = new List<StartPosition>();
    }
}
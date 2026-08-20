using Database.DataModel.Enums;

namespace Server.Game.Models.Settings
{
    /// <summary>
    ///     Стартовая карта и позиция персонажа выбранного класса.
    ///     Используется при создании персонажа (5118) как @pHomeMap/@pHomeX/Y/Z процедуры UspCreatePc
    /// </summary>
    public class StartPosition
    {
        /// <summary>
        ///     Класс персонажа, для которого действует эта точка
        /// </summary>
        public CharacterTypeEnum Class { get; set; }

        /// <summary>
        ///     Номер стартовой карты. 1 — остров Гвинея, 80 — остров Аккра
        /// </summary>
        public int Map { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}

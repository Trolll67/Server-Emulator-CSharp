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

        /// <summary>
        ///     Фича-флаг: шифровать исходящие пакеты. Выключен - кадр уходит как сейчас, с
        ///     crypt-байтом 0x00 и телом открытым текстом; включён - crypt-байт 0x01, а тело
        ///     кадра проходит через тот же поточный шифр, которым разбирается входящий поток.
        ///     Welcome-пакет уходит открытым при любом значении флага, иначе клиенту нечем
        ///     расшифровать кадр, который и приносит ему ключевой блок. Включение требует проверки
        ///     на живом клиенте: не исключено, что для направления сервер -> клиент нужен ключ из
        ///     welcome-блока, а не статический ключ BlowfishCrypt
        /// </summary>
        public bool EncryptOutgoingPackets { get; set; }

        /// <summary>
        ///     Фича-флаг: генерировать ключевой блок welcome-пакета. Выключен - клиенту уходит
        ///     заготовленный статический блок, как и раньше; включён - блок той же длины и той же
        ///     раскладки, но с новой ключевой частью на каждое подключение. Ключ шифра сессии при
        ///     этом не меняется, см. <see cref="Server.Game.Network.GameSession.CipherKey"/>
        /// </summary>
        public bool GenerateSessionKey { get; set; }

        /// <summary>
        ///     How many players this server is able to hold. Goes to the channel in the packet of
        ///     the state, and the channel turns it into the fullness the client draws in the list
        /// </summary>
        public short MaxSessions { get; set; } = 1000;

        /// <summary>
        ///     How often, in seconds, the server calls the channels of its world: a link that is
        ///     down is opened again, an open one gets a ping and the state of this server
        /// </summary>
        public int FamilyKeepAliveSeconds { get; set; } = 30;

        /// <summary>
        ///     Build this server names itself with when it joins the world. The original logs it
        ///     and lets the link live either way
        /// </summary>
        public int FamilyVersion { get; set; }

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
        ///     Сколько процентов накопленного опыта снимает смерть персонажа. Без ключа в конфиге
        ///     остаются прежние 2 процента; значение вне [0, 100] системой опыта тоже сводится
        ///     к 2, см. <see cref="Server.Game.Core.Systems.ExpSystem.DeathExpPenaltyPercent"/>
        /// </summary>
        public int DeathExpPenaltyPercent { get; set; } = 2;

        /// <summary>
        ///     Периоды фоновых заданий планировщика в миллисекундах, по имени задания. Ключа нет —
        ///     задание работает со своим значением по умолчанию
        /// </summary>
        public Dictionary<string, int> JobIntervals { get; set; } = new Dictionary<string, int>();

        // Стартовые карта и позиция при создании персонажа, по одной записи на класс
        public List<StartPosition> StartPositions { get; set; } = new List<StartPosition>();
    }
}
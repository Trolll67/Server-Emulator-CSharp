using Microsoft.Extensions.Options;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Game;
using Server.Game.Models.Settings;
using Server.Game.Network;
using Server.Game.Services.Database;

namespace Server.Game.Core.Systems
{
    /// <summary>
    ///     Опыт персонажа: начисление за убитого монстра, набор уровней и штраф при смерти.
    ///     Порог уровня берётся из таблицы опыта парма (<see cref="ParmRepository.GetExpByLvl"/>),
    ///     та же таблица уходит клиенту при входе в мир
    /// </summary>
    public class ExpSystem
    {
        /// <summary>
        ///     Штраф опыта при смерти в процентах, когда настройка вне допустимого диапазона.
        ///     Прежнее зашитое в код значение
        /// </summary>
        public const int DefaultDeathExpPenaltyPercent = 2;

        /// <summary>
        ///     Верхняя граница штрафа: сто процентов обнуляют опыт текущего уровня целиком
        /// </summary>
        public const int LimitDeathExpPenaltyPercent = 100;

        private readonly ParmRepository _databaseBalanceService;
        private readonly ICharacteristicFactory _characteristicFactory;

        public ExpSystem(ParmRepository databaseBalanceService, ICharacteristicFactory characteristicFactory, IOptions<GameSetting> gameSetting)
        {
            _databaseBalanceService = databaseBalanceService;
            _characteristicFactory = characteristicFactory;

            DeathExpPenaltyPercent = GetDeathExpPenaltyPercent(gameSetting.Value.DeathExpPenaltyPercent);
        }

        /// <summary>
        ///     Сколько процентов накопленного опыта снимает смерть
        /// </summary>
        public int DeathExpPenaltyPercent { get; }

        /// <summary>
        ///     Kill connection and update
        /// </summary>
        /// <param name="unitGameModel"></param>
        /// <param name="gameSession"></param>
        public void KillConnection(GMonster unitGameModel, GameSession gameSession)
        {
            var expModel = _databaseBalanceService.GetExpByLvl(gameSession.Pc.Simple.Level);

            // Процент считается умножением до деления: (Exp / 100) * N обнуляет штраф на любом
            // опыте меньше сотни, а Exp здесь беззнаковый - уйти ниже нуля он не может
            var penalty = gameSession.Pc.Simple.Exp * (ulong)DeathExpPenaltyPercent / 100;

            gameSession.Pc.Simple.Exp -= penalty;

            _characteristicFactory.SendInfoExp(gameSession, expModel);
        }

        /// <summary>
        ///     Kill unit and update
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monster"></param>
        public void KillUnit(GameSession client, GMonster monster)
        {
            client.Pc.Simple.Exp += monster.ParmMon.Exp;

            var expModel = _databaseBalanceService.GetExpByLvl(client.Pc.Simple.Level);
            var maxLevel = _databaseBalanceService.MaxExpLevel;
            var levelsGained = 0;

            // Одно начисление может закрыть несколько порогов сразу: излишек переносится на
            // следующий уровень, порог перечитывается на каждом шаге. Нулевой порог означает,
            // что строки уровня в парме нет - тогда уровень не растёт
            while (expModel.Exp > 0 && client.Pc.Simple.Exp >= expModel.Exp)
            {
                if (client.Pc.Simple.Level >= maxLevel)
                {
                    // Потолок таблицы: расти некуда, опыт замирает на пороге последнего уровня
                    client.Pc.Simple.Exp = expModel.Exp;
                    break;
                }

                client.Pc.Simple.Exp -= expModel.Exp;
                client.Pc.Simple.Level += 1;
                levelsGained++;

                expModel = _databaseBalanceService.GetExpByLvl(client.Pc.Simple.Level);
            }

            if (levelsGained > 0)
            {
                // Пересчёт один на все набранные уровни: CalcAbility выставляет новые максимумы
                // HP/MP (и подрезает по ним текущие), CalcSpeed - скорости от новых характеристик.
                // Обнуление характеристик делает сама CalcAbility, поэтому повторный вызов на той
                // же Ability прибавки экипировки не удваивает
                client.Pc.CalcAbility();
                client.Pc.CalcSpeed();

                client.Pc.Simple.Hp = client.Pc.Ability.MaxHp;
                client.Pc.Simple.Mp = client.Pc.Ability.MaxMp;

                _characteristicFactory.SendInformationAbilityCharacteristics(client);
                _characteristicFactory.SendHealthPointCharacteristics(client);
                _characteristicFactory.SendSpeedCharacteristics(client, client);
                _characteristicFactory.SendInfoWeight(client);

                _characteristicFactory.SendLevelUp(client, client);

                foreach (var visible in client.Pc.VisibleCharacterGames)
                {
                    _characteristicFactory.SendSpeedCharacteristics(client, visible);
                    _characteristicFactory.SendLevelUp(client, visible);
                }
            }

            _characteristicFactory.SendInfoExp(client, expModel);
        }

        /// <summary>
        ///     Процент штрафа из настроек, значение вне [0, 100] заменяется значением по умолчанию
        /// </summary>
        /// <param name="configured"></param>
        private static int GetDeathExpPenaltyPercent(int configured)
        {
            if (configured < 0 || configured > LimitDeathExpPenaltyPercent)
            {
                return DefaultDeathExpPenaltyPercent;
            }

            return configured;
        }
    }
}

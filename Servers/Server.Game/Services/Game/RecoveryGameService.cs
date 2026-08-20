using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Core.Factories.Interfaces;
using Server.Game.Models.Settings;
using Server.Game.Services.Scheduling;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.GameServices
{
    /// <summary>
    ///     Recovery game service
    /// </summary>
    public class RecoveryGameService : IHostedService
    {
        /// <summary>
        ///     How often the recovery passes are repeated, in milliseconds. Overridable through
        ///     "GameSetting:JobIntervals:&lt;job name&gt;". The pass itself is cheap: hp/mp are added
        ///     only once per "GameSetting:RecoveryCharacteristics" of every character
        /// </summary>
        private const int TickIntervalMilliseconds = 100;

        /// <summary>
        ///     Pause between two regeneration ticks of one character used when the configured value is
        ///     out of range, in milliseconds
        /// </summary>
        public const int DefaultRecoveryCharacteristics = 10000;

        /// <summary>
        ///     Lower bound of that pause: a tick may not be more frequent than the pass itself, and a
        ///     missing setting is a zero that would otherwise regenerate hp/mp every 100 ms
        /// </summary>
        public const int MinimumRecoveryCharacteristics = 1000;

        private readonly int _recoveryCharacteristics;
        private readonly ICharacteristicFactory _characteristicFactory;
        private readonly IdentificationService _identificationService;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<RecoveryGameService> _logger;

        public RecoveryGameService(IOptions<GameSetting> gameSetting, ICharacteristicFactory characteristicFactory, IdentificationService identificationService, PeriodicScheduler periodicScheduler, ILogger<RecoveryGameService> logger)
        {
            _recoveryCharacteristics = GetRecoveryCharacteristics(gameSetting.Value.RecoveryCharacteristics);
            _characteristicFactory = characteristicFactory;
            _identificationService = identificationService;
            _periodicScheduler = periodicScheduler;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _periodicScheduler.Schedule(nameof(RecoveryConnections), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), RecoveryConnections);
            _periodicScheduler.Schedule(nameof(RecoveryUnits), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), RecoveryUnits);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Recovery connections
        /// </summary>
        private void RecoveryConnections()
        {
            try
            {
                // Only the characters that are already in the world: a session on the loading screen
                // has nothing to show 5146 on, and its Pc may still be dropped by a failed entry
                var connections = _identificationService.GetAllConnections().Where(c => c.IsInWorld && c.Pc != null);

                foreach (var connection in connections)
                {
                    // A dead character does not regenerate, its hp/mp are restored by the resurrection
                    if (connection.Pc.DeadTime != null)
                    {
                        continue;
                    }

                    int hp = connection.Pc.Simple.Hp;
                    int mp = connection.Pc.Simple.Mp;

                    // The regeneration is much rarer than the pass, LastUpdateHpMp keeps the moment of
                    // the last tick of this character
                    if (connection.Pc.LastUpdateHpMp.AddMilliseconds(_recoveryCharacteristics) < DateTime.Now)
                    {
                        hp += connection.Pc.Ability.HpRegen;
                        mp += connection.Pc.Ability.MpRegen;

                        connection.Pc.LastUpdateHpMp = DateTime.Now;
                    }

                    // The ceiling is checked on every pass, not only on the tick: the maximum itself
                    // can drop (unequipped item, ended buff) between two ticks
                    if (hp > connection.Pc.Ability.MaxHp)
                    {
                        hp = connection.Pc.Ability.MaxHp;
                    }

                    if (mp > connection.Pc.Ability.MaxMp)
                    {
                        mp = connection.Pc.Ability.MaxMp;
                    }

                    // Only a real change is written back and only it sends 5146: with a zero
                    // regeneration or with full hp/mp the pass stays silent, and an unconditional
                    // write would roll back the damage of a hit landed inside the pass
                    bool isUpdate = false;

                    if (hp != connection.Pc.Simple.Hp)
                    {
                        connection.Pc.Simple.Hp = hp;
                        isUpdate = true;
                    }

                    if (mp != connection.Pc.Simple.Mp)
                    {
                        connection.Pc.Simple.Mp = mp;
                        isUpdate = true;
                    }

                    if (isUpdate)
                    {
                        _characteristicFactory.SendHealthPointCharacteristics(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not recovery connections");
            }
        }

        /// <summary>
        ///     Recovery units
        /// </summary>
        private void RecoveryUnits()
        {
            try
            {
                var units = _identificationService.GetAllUnits();

                foreach (var unit in units)
                {
                    // A dead monster does not regenerate, its hp/mp are restored by the respawn
                    if (unit.DeadTime != null)
                    {
                        continue;
                    }

                    // Outside its own tick the monster is not touched at all: every pass that reads
                    // Simple and writes it back is a read-modify-write that can silently roll back a
                    // hit landed in between
                    if (unit.LastUpdateHpMp.AddMilliseconds(_recoveryCharacteristics) >= DateTime.Now)
                    {
                        continue;
                    }

                    unit.LastUpdateHpMp = DateTime.Now;

                    // Current hp/mp of the monster live in Simple, their maximums - in the parm
                    int hp = Math.Min(unit.Simple.Hp + unit.Ability.HpRegen, unit.ParmMon.Hp);
                    int mp = Math.Min(unit.Simple.Mp + unit.Ability.MpRegen, unit.ParmMon.Mp);

                    // Only a real change is written back, and no packet goes to anybody: the client
                    // learns the hp of the monster from 5132 of the hit
                    if (hp != unit.Simple.Hp)
                    {
                        unit.Simple.Hp = hp;
                    }

                    if (mp != unit.Simple.Mp)
                    {
                        unit.Simple.Mp = mp;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not recovery units");
            }
        }

        /// <summary>
        ///     Pause between two regeneration ticks of one character: a value below the minimum is not
        ///     pulled to the border but replaced by the default, the same way MoveSystem reads its
        ///     threshold. An absent setting is a zero and lands on the default as well
        /// </summary>
        /// <param name="configured">Value of GameSetting.RecoveryCharacteristics</param>
        /// <returns></returns>
        private static int GetRecoveryCharacteristics(int configured)
        {
            if (configured < MinimumRecoveryCharacteristics)
            {
                return DefaultRecoveryCharacteristics;
            }

            return configured;
        }
    }
}

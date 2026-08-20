using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Models.Settings;
using Server.Game.Services.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.GameServices
{
    /// <summary>
    ///     Garbage game service
    /// </summary>
    public class GarbageGameService : IHostedService
    {
        /// <summary>
        ///     How often the garbage passes are repeated, in milliseconds. Overridable through
        ///     "GameSetting:JobIntervals:&lt;job name&gt;"
        /// </summary>
        private const int TickIntervalMilliseconds = 100;

        private readonly GameSetting _gameSetting;
        private readonly IdentificationService _identificationService;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<GarbageGameService> _logger;

        public GarbageGameService(IOptions<GameSetting> gameSetting, IdentificationService identificationService, PeriodicScheduler periodicScheduler, ILogger<GarbageGameService> logger)
        {
            _gameSetting = gameSetting.Value;
            _identificationService = identificationService;
            _periodicScheduler = periodicScheduler;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _periodicScheduler.Schedule(nameof(GarbageItems), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), GarbageItems);
            _periodicScheduler.Schedule(nameof(GarbageUnits), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), GarbageUnits);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Garbage items
        /// </summary>
        private void GarbageItems()
        {
            try
            {
                var items = _identificationService.GetAllItems();

                foreach (var item in items)
                {
                    if (item.DateCreate.AddMilliseconds(_gameSetting.GarbageItems) > DateTime.Now)
                    {
                        continue;
                    }

                    _identificationService.RemoveItem(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not garbage items");
            }
        }

        /// <summary>
        ///     Garbage units
        /// </summary>
        private void GarbageUnits()
        {
            try
            {
                var units = _identificationService.GetAllUnits();

                foreach (var unit in units)
                {
                    if (unit.DeadTime == null)
                    {
                        continue;
                    }

                    if (unit.Respawn <= _gameSetting.GarbageUnits + 5000)
                    {
                        if (unit.DeadTime.Value.AddMilliseconds(unit.Respawn - 5000) > DateTime.Now)
                        {
                            continue;
                        }

                        _identificationService.RemoveUnit(unit);
                    }
                    else
                    {
                        if (unit.DeadTime.Value.AddMilliseconds(_gameSetting.GarbageUnits) > DateTime.Now)
                        {
                            continue;
                        }

                        _identificationService.RemoveUnit(unit);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not garbage units");
            }
        }
    }
}

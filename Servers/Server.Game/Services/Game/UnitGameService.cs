using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Game.Core.Systems;
using Server.Game.Models.Game;
using Server.Game.Services.Database;
using Server.Game.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.GameServices
{
    /// <summary>
    ///     Unit game service
    /// </summary>
    public class UnitGameService : IHostedService
    {
        /// <summary>
        ///     How often the respawn pass is repeated, in milliseconds. Overridable through
        ///     "GameSetting:JobIntervals:&lt;job name&gt;"
        /// </summary>
        private const int TickIntervalMilliseconds = 100;

        private readonly UnitSystem _unitSystem;
        private readonly ParmRepository _databaseBalanceService;
        private readonly IdentificationService _identificationService;
        private readonly PeriodicScheduler _periodicScheduler;

        private readonly List<GMonster> _monsters;
        private readonly ILogger<UnitGameService> _logger;

        public UnitGameService(UnitSystem unitSystem, ParmRepository databaseBalanceService, IdentificationService identificationService, PeriodicScheduler periodicScheduler, ILogger<UnitGameService> logger)
        {
            _unitSystem = unitSystem;
            _databaseBalanceService = databaseBalanceService;
            _identificationService = identificationService;
            _periodicScheduler = periodicScheduler;
            _logger = logger;

            // Load units
            _monsters = _unitSystem.GetUnitGames();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Run tasks
            _periodicScheduler.Schedule(nameof(RespawnUnits), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), RespawnUnits);
            // MoveUnits();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Respawn units
        /// </summary>
        private void RespawnUnits()
        {
            try
            {
                foreach (var monster in _monsters)
                {
                    if (monster.DeadTime == null)
                    {
                        continue;
                    }

                    if (monster.DeadTime.Value.AddMilliseconds(monster.Respawn) > DateTime.Now)
                    {
                        continue;
                    }

                    _identificationService.RemoveUnit(monster);

                    monster._SetDefaultInfo(monster.ParmMon);
                    //_unitSystem.ResetUnit(monster);

                    _identificationService.AddUnit(monster);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not respawn units");
            }
        }
    }
}

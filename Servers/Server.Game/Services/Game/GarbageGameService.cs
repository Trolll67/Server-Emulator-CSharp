using Database.Fnl.Game;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Models.Game;
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
        private readonly IFnlGameRepository _gameRepository;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<GarbageGameService> _logger;

        public GarbageGameService(IOptions<GameSetting> gameSetting, IdentificationService identificationService, IFnlGameRepository gameRepository, PeriodicScheduler periodicScheduler, ILogger<GarbageGameService> logger)
        {
            _gameSetting = gameSetting.Value;
            _identificationService = identificationService;
            _gameRepository = gameRepository;
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
        ///     Garbage items. A thing that has lain in the world for its whole life
        ///     (<c>GameSetting.GarbageItems</c>) is gone for good: out of the world and out of the
        ///     database, so that no row is left parked under the owner the things on the ground
        ///     belong to.
        ///     <para>
        ///     The original also cuts the life of the things short while the world holds too many
        ///     of them at once; we do not reproduce that, a thing always lives its whole life
        ///     </para>
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

                    // The thing is taken out of the world the same way a pick-up takes it: the
                    // lookup and the removal are one operation under the lock of the service, so a
                    // player who reaches for the thing at this very moment either gets it whole -
                    // and the pass leaves its row alone - or is told there is nothing lying there
                    if (!_identificationService.TryTakeItem(item.UniqueId, out var rotten))
                    {
                        continue;
                    }

                    // The thing is out of the world and is never going back, so the number it lay
                    // under is handed out again: the row of the database is another matter
                    // altogether and a deletion that fails changes nothing about the thing itself
                    _identificationService.ReleaseItem(rotten);

                    EraseItem(rotten);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not garbage items");
            }
        }

        /// <summary>
        ///     Deletes the row of a thing that has been taken out of the world. A thing thrown away
        ///     by a player carries the serial of a row that waits under the owner of the ground, and
        ///     that row goes away with it; loot nobody has picked up carries no serial at all - its
        ///     row is written by the pick-up, so there is nothing to delete.
        ///     A failed deletion is only written down: the thing has already left the world, and the
        ///     pass must not stop on it
        /// </summary>
        /// <param name="item">Thing that has just been taken out of the world</param>
        private void EraseItem(GPublicItem item)
        {
            if (item.Item == null || item.Item.SerialNumber == 0)
            {
                return;
            }

            try
            {
                int returnCode = _gameRepository.EraseItem((long)item.Item.SerialNumber);

                if (returnCode != 0)
                {
                    _logger.LogError("The procedure refused to delete row {SerialNo} of the item {ItemId} that has rotted away with code {ErrorCode}", item.Item.SerialNumber, item.Item.Id, returnCode);
                }
            }
            catch (Exception e) when (e is SqlException || e is InvalidOperationException)
            {
                _logger.LogError(e, "Can not delete row {SerialNo} of the item {ItemId} that has rotted away", item.Item.SerialNumber, item.Item.Id);
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

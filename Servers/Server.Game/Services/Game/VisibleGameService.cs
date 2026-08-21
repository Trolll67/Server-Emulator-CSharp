using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Server.Game.Enums;
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
    ///     Visible game service
    /// </summary>
    public class VisibleGameService : IHostedService
    {
        /// <summary>
        ///     How often the visibility passes are repeated, in milliseconds. Overridable through
        ///     "GameSetting:JobIntervals:&lt;job name&gt;"
        /// </summary>
        private const int TickIntervalMilliseconds = 100;

        /// <summary>
        ///     How many objects one packet of the appeared ones carries. The original keeps an array
        ///     of exactly this many blocks in the packet, so a longer list has to be cut into several
        /// </summary>
        private const int PublicBlocksPerPacket = 45;

        private readonly GameSetting _gameSetting;
        private readonly IVisibleFactory _visibleFactory;
        private readonly IdentificationService _identificationService;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<VisibleGameService> _logger;

        public VisibleGameService(IOptions<GameSetting> gameSetting, IVisibleFactory visibleFactory, IdentificationService identificationService, PeriodicScheduler periodicScheduler, ILogger<VisibleGameService> logger)
        {
            _gameSetting = gameSetting.Value;
            _visibleFactory = visibleFactory;
            _identificationService = identificationService;
            _periodicScheduler = periodicScheduler;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _periodicScheduler.Schedule(nameof(VisibleConnections), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), VisibleConnections);
            _periodicScheduler.Schedule(nameof(VisibleItems), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), VisibleItems);
            _periodicScheduler.Schedule(nameof(VisibleUnits), TimeSpan.FromMilliseconds(TickIntervalMilliseconds), VisibleUnits);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Visible connections
        /// </summary>
        private void VisibleConnections()
        {
            try
            {
                var connections = _identificationService.GetAllConnections().Where(c => c.Pc != null);

                foreach (var connection in connections)
                {
                    // Get all visible connections
                    var visibleConnections = connections.Where(c => c != connection && c.Pc.PositionCur.Distance(connection.Pc.PositionCur) <= _gameSetting.VisibleConnections).ToList();

                    // Get new or old connections
                    var newVisibleConnections = visibleConnections.Where(c => c.Pc.IsVsibleFirst == true);
                    var lastVisibleConnections = visibleConnections.Where(c => c.Pc.IsVsibleFirst == false);

                    // TODO add is teleport or new ifelse

                    // Send me displayed details
                    if (connection.Pc.IsVsibleFirst)
                    {
                        _visibleFactory.SendDisplayedDetailsCharacter(connection, connection);
                    }

                    foreach (var newVisibleConnection in newVisibleConnections)
                    {
                        _visibleFactory.SendDisplayedDetailsCharacter(newVisibleConnection, connection);
                    }

                    // Get appear connections
                    var appearConnections = lastVisibleConnections.Except(connection.Pc.VisibleCharacterGames);

                    var appearConnectionList = appearConnections.ToList();

                    for (int i = 0; i < GetPacketCount(appearConnectionList.Count); i++)
                    {
                        _visibleFactory.SendDisplayedCharacters(appearConnectionList.Skip(i * PublicBlocksPerPacket).Take(PublicBlocksPerPacket).ToList(), connection);
                    }

                    // Get disappear connections
                    var disappearConnections = connection.Pc.VisibleCharacterGames.Except(visibleConnections);

                    foreach (var disappearConnection in disappearConnections)
                    {
                        _visibleFactory.SendExitMap(connection, disappearConnection.Pc.UniqueId, ExitMapWhy.None);
                    }

                    connection.Pc.VisibleCharacterGames = visibleConnections;
                }

                // Reset is vsible first
                foreach (var connection in connections)
                {
                    if (connection.Pc.IsVsibleFirst)
                    {
                        connection.Pc.IsVsibleFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not update visible connections");
            }
        }

        /// <summary>
        ///     Visible items
        /// </summary>
        private void VisibleItems()
        {
            try
            {
                var connections = _identificationService.GetAllConnections().Where(c => c.Pc != null);
                var items = _identificationService.GetAllItems();

                // Visible items for connection
                foreach (var connection in connections)
                {
                    // Get all visible items
                    var visibleItems = items.Where(i => i.Position.Distance(connection.Pc.PositionCur) <= _gameSetting.VisibleItems).ToList();

                    // Get new or old items
                    var newVisibleItems = visibleItems.Where(i => i.IsVsibleFirst == true);
                    var lastVisibleItems = visibleItems.Where(i => i.IsVsibleFirst == false);

                    foreach (var newVisibleItem in newVisibleItems)
                    {
                        _visibleFactory.SendDisplayedDetailsItem(connection, newVisibleItem);
                    }

                    // Get appear items
                    var appearItems = lastVisibleItems.Except(connection.Pc.VisibleItemGames);

                    var appearItemList = appearItems.ToList();

                    for (int i = 0; i < GetPacketCount(appearItemList.Count); i++)
                    {
                        _visibleFactory.SendDisplayedItems(connection, appearItemList.Skip(i * PublicBlocksPerPacket).Take(PublicBlocksPerPacket).ToList());
                    }

                    // Get disappear items
                    var disappearItems = connection.Pc.VisibleItemGames.Except(visibleItems);

                    foreach (var exitItem in disappearItems)
                    {
                        _visibleFactory.SendExitMap(connection, exitItem.UniqueId, ExitMapWhy.None);
                    }

                    connection.Pc.VisibleItemGames = visibleItems;
                }

                // Visible connections for item
                foreach (var item in items)
                {
                    // Get all visible connections
                    var visibleConnections = connections.Where(i => i.Pc.PositionCur.Distance(item.Position) <= _gameSetting.VisibleUnits).ToList();

                    item.VisibleCharacterGames = visibleConnections;
                }

                // Reset is vsible first
                foreach (var item in items)
                {
                    if (item.IsVsibleFirst)
                    {
                        item.IsVsibleFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not update visible items");
            }
        }

        /// <summary>
        ///     Visible units
        /// </summary>
        private void VisibleUnits()
        {
            try
            {
                var connections = _identificationService.GetAllConnections().Where(c => c.Pc != null);
                var units = _identificationService.GetAllUnits();

                // Visible units for connection
                foreach (var connection in connections)
                {
                    // Get all visible unts
                    var visibleUnits = units.Where(i => i.PositionCur.Distance(connection.Pc.PositionCur) <= _gameSetting.VisibleUnits).ToList();

                    // Get new or old units
                    var newVisibleUnits = visibleUnits.Where(u => u.IsVsibleFirst == true);
                    var lastVisibleUnits = visibleUnits.Where(u => u.IsVsibleFirst == false);

                    foreach (var newVisibleUnit in newVisibleUnits)
                    {
                        _visibleFactory.SendDisplayedDetailsUnit(connection, newVisibleUnit);
                    }

                    // Get appear units
                    var appearUnits = lastVisibleUnits.Except(connection.Pc.VisibleUnitGames);

                    var appearUnitList = appearUnits.ToList();

                    for (int i = 0; i < GetPacketCount(appearUnitList.Count); i++)
                    {
                        _visibleFactory.SendDisplayedUnit(connection, appearUnitList.Skip(i * PublicBlocksPerPacket).Take(PublicBlocksPerPacket).ToList());
                    }

                    // Get disappear units
                    var disappearUnits = connection.Pc.VisibleUnitGames.Except(visibleUnits);

                    foreach (var exitUnit in disappearUnits)
                    {
                        _visibleFactory.SendExitMap(connection, exitUnit.UniqueId, ExitMapWhy.None);
                    }

                    connection.Pc.VisibleUnitGames = visibleUnits;
                }

                // Visible connections for unit
                foreach (var unit in units)
                {
                    // Get all visible connections
                    var visibleConnections = connections.Where(i => i.Pc.PositionCur.Distance(unit.PositionCur) <= _gameSetting.VisibleUnits).ToList();

                    unit.VisibleCharacterGames = visibleConnections;
                }

                // Reset is vsible first
                foreach (var unit in units)
                {
                    if (unit.IsVsibleFirst)
                    {
                        unit.IsVsibleFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not update visible units");
            }
        }

        /// <summary>
        ///     How many packets a list of the given length takes. A remainder of the division used to
        ///     stand here instead, and it both lost every full packet (a round forty five objects gave
        ///     no packet at all) and sent empty ones for a list shorter than one packet
        /// </summary>
        /// <param name="count">How many objects have appeared</param>
        private static int GetPacketCount(int count)
        {
            return (count + PublicBlocksPerPacket - 1) / PublicBlocksPerPacket;
        }

    }
}

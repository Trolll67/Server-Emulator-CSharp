using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Models.Settings;
using Server.Game.Services.Scheduling;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.Database
{
    internal class GameSaveService : IHostedService
    {
        /// <summary>
        ///     Autosave period used when "SavePcsEverySeconds" is missing or not positive: the same
        ///     value the tracked gamesettings.json carries
        /// </summary>
        private const int DefaultSaveIntervalSeconds = 5;

        private readonly GameSetting _gameSetting;
        private readonly IdentificationService _identificationService;
        private readonly GameRepository _gameRepository;
        private readonly PeriodicScheduler _periodicScheduler;
        private readonly ILogger<GameSaveService> _logger;

        public GameSaveService(IOptions<GameSetting> gameSetting, IdentificationService identificationService, GameRepository gameRepository, PeriodicScheduler periodicScheduler, ILogger<GameSaveService> logger)
        {
            _gameSetting = gameSetting.Value;
            _identificationService = identificationService;
            _gameRepository = gameRepository;
            _periodicScheduler = periodicScheduler;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int seconds = _gameSetting.SavePcsEverySeconds > 0
                ? _gameSetting.SavePcsEverySeconds
                : DefaultSaveIntervalSeconds;

            _periodicScheduler.Schedule(nameof(SaveCharacters), TimeSpan.FromSeconds(seconds), SaveCharacters);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     One autosave pass over the characters that are online
        /// </summary>
        private void SaveCharacters()
        {
            try
            {
                var connections = _identificationService.GetAllConnections().Where(c => c.Pc != null);

                foreach (var connection in connections)
                {
                    // UspUpdatePos writes position plus HP/MP/Map/Stomach; the map is taken
                    // from the loaded character so it is not reset on autosave
                    _gameRepository.SavePosition(connection.Pc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can not autosave characters");
            }
        }
    }
}

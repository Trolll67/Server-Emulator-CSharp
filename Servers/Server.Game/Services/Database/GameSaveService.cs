using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Server.Game.Models.Settings;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.Database
{
    internal class GameSaveService : IHostedService
    {
        private readonly GameSetting _gameSetting;
        private readonly IdentificationService _identificationService;
        private readonly GameRepository _gameRepository;

        public GameSaveService(IOptions<GameSetting> gameSetting, IdentificationService identificationService, GameRepository gameRepository)
        {
            _gameSetting = gameSetting.Value;
            _identificationService = identificationService;
            _gameRepository = gameRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartSaving();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Visible units
        /// </summary>
        private void StartSaving()
        {
            Task.Run(() =>
            {
                while (true)
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

                    }

                    Thread.Sleep(_gameSetting.SavePcsEverySeconds * 1000);
                }
            });
        }
    }
}

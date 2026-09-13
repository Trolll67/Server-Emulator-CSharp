using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Login.Core.Factories.Interfaces;
using Server.Login.Models.Settings;
using Server.Login.Services.Family;

namespace Server.Login.Services.Hosted
{
    /// <summary>
    ///     Keeps the links to the servers of this world alive. The original fires both of these
    ///     off its own timer wheel: an empty ping, so that a link nobody uses does not look the
    ///     same as a broken one, and the state of the channel itself, so that the rest of the
    ///     world knows how loaded it is.
    ///
    ///     The keys of the logins that nobody came for are cleaned up on the same rounds: the
    ///     original gives every key a deadline of its own, and one sweep comes to the same thing
    /// </summary>
    public class FamilyHostedService : IHostedService, IDisposable
    {
        private readonly IFamilyFactory _familyFactory;
        private readonly CertificationRegistry _certificationRegistry;
        private readonly ILogger<FamilyHostedService> _logger;
        private readonly LoginSetting _loginSetting;

        private Timer _timer;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        public FamilyHostedService(IFamilyFactory familyFactory, CertificationRegistry certificationRegistry, IOptions<LoginSetting> loginSetting, ILogger<FamilyHostedService> logger)
        {
            _familyFactory = familyFactory;
            _certificationRegistry = certificationRegistry;
            _loginSetting = loginSetting.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, _loginSetting.FamilyKeepAliveSeconds));

            _timer = new Timer(Tick, null, interval, interval);

            _logger.LogInformation("Family links are pinged every {Seconds} seconds", interval.TotalSeconds);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     One round of the timer. A broken link only shows up while sending, so a failure
        ///     here must not take the timer down with it
        /// </summary>
        private void Tick(object state)
        {
            try
            {
                _familyFactory.BroadcastKeepAlive();
                _familyFactory.BroadcastOwnState();
                _certificationRegistry.Expire(TimeSpan.FromSeconds(Math.Max(1, _loginSetting.CertifyKeyLifetimeSeconds)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can not reach the servers of the world");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

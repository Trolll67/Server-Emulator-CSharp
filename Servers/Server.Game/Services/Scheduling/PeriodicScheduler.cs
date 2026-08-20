using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Game.Models.Settings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game.Services.Scheduling
{
    /// <summary>
    ///     Single engine for the periodic work of the game server: garbage collection, visibility,
    ///     respawn and autosave. Every job gets its own dedicated background thread instead of a
    ///     "Task.Run + while(true) + Thread.Sleep" loop that blocks a thread pool thread, an exception
    ///     of one job never touches the others, and the whole set stops as soon as the host stops
    ///     the scheduler
    /// </summary>
    public class PeriodicScheduler : IHostedService
    {
        /// <summary>
        ///     How long the whole stop waits for the jobs that are in the middle of their work
        /// </summary>
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        private readonly List<PeriodicJob> _jobs = new List<PeriodicJob>();
        private readonly object _sync = new object();

        // Cancelled on StopAsync: this is what every job loop waits on instead of sleeping
        private readonly CancellationTokenSource _stopping = new CancellationTokenSource();

        // "GameSetting:JobIntervals" of the configuration: interval overrides in milliseconds by job
        // name. Without an override a job keeps the interval it was registered with, so the defaults
        // live next to the code that owns the job
        private readonly Dictionary<string, int> _jobIntervals;

        private readonly ILogger<PeriodicScheduler> _logger;

        private bool _started;

        public PeriodicScheduler(IOptions<GameSetting> gameSetting, ILogger<PeriodicScheduler> logger)
        {
            // The configuration keys are case insensitive, the bound dictionary is not: the lookup by
            // the job name has to stay as forgiving as the rest of the settings
            _jobIntervals = gameSetting.Value.JobIntervals == null
                ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, int>(gameSetting.Value.JobIntervals, StringComparer.OrdinalIgnoreCase);

            _logger = logger;
        }

        /// <summary>
        ///     Registers a periodic job: the action is run, then the scheduler waits the interval and runs
        ///     it again, until the host stops. May be called before the scheduler itself is started (the job
        ///     waits for the start) and after it (the job starts right away), so the hosted services keep
        ///     registering their work from their own StartAsync
        /// </summary>
        /// <param name="name">Job name, used in the log and as the configuration key of its interval</param>
        /// <param name="interval">Default pause between two runs of the action</param>
        /// <param name="action">Body of the job, it is expected to log its own errors</param>
        public void Schedule(string name, TimeSpan interval, Action action)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A periodic job needs a name", nameof(name));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "A periodic job needs a positive interval");
            }

            var job = new PeriodicJob(name, ResolveInterval(name, interval), action);

            lock (_sync)
            {
                // Registration during or after the stop: there is nothing left to run the job on
                if (_stopping.IsCancellationRequested)
                {
                    return;
                }

                _jobs.Add(job);

                if (_started)
                {
                    StartThread(job);
                }
            }

            _logger.LogInformation("Periodic job \"{Job}\" scheduled every {Interval} ms", job.Name, job.Interval.TotalMilliseconds);
        }

        /// <summary>
        ///     Starts the jobs registered so far. The scheduler is registered before the game services, so
        ///     usually the list is still empty here and the jobs are started by Schedule itself
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // The host gave up on the startup: no reason to spin up the threads
            cancellationToken.ThrowIfCancellationRequested();

            lock (_sync)
            {
                // A second start would give every job a second thread and lose the handle of the first
                if (_started)
                {
                    return Task.CompletedTask;
                }

                _started = true;

                foreach (var job in _jobs)
                {
                    StartThread(job);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Cancels the token every job waits on and gives the running jobs a bounded time to finish
        ///     the current pass
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            List<PeriodicJob> jobs;

            lock (_sync)
            {
                if (!_started)
                {
                    return Task.CompletedTask;
                }

                _started = false;

                jobs = new List<PeriodicJob>(_jobs);
                _jobs.Clear();

                // A job breaks its loop right after the current pass instead of waiting out the
                // interval. Cancelled under the lock: otherwise Schedule could slip in between and
                // report a job that would never run
                _stopping.Cancel();
            }

            DateTime deadline = DateTime.UtcNow.Add(StopTimeout);

            foreach (var job in jobs)
            {
                TimeSpan remaining = deadline - DateTime.UtcNow;

                if (remaining > TimeSpan.Zero && !cancellationToken.IsCancellationRequested && job.Thread.Join(remaining))
                {
                    continue;
                }

                // The threads are background ones, a stuck job does not hold the process, only the log
                _logger.LogWarning("Periodic job \"{Job}\" has not finished before the scheduler stopped", job.Name);
            }

            _logger.LogInformation("Periodic scheduler stopped {Count} job(s)", jobs.Count);

            // The token source is left undisposed on purpose: a job that outlived the timeout still
            // waits on its handle

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Reads the interval override of the job from the configuration; anything but a positive
        ///     number of milliseconds leaves the default interval of the job in place
        /// </summary>
        /// <param name="name"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private TimeSpan ResolveInterval(string name, TimeSpan interval)
        {
            if (!_jobIntervals.TryGetValue(name, out int milliseconds))
            {
                return interval;
            }

            if (milliseconds <= 0)
            {
                _logger.LogWarning("Interval {Value} of the periodic job \"{Job}\" is not a positive number of milliseconds, the default {Interval} ms is used",
                    milliseconds, name, interval.TotalMilliseconds);

                return interval;
            }

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        ///     Gives the job its own thread. A dedicated thread keeps a slow job (a database autosave)
        ///     from delaying the others and keeps the thread pool free for the network
        /// </summary>
        /// <param name="job"></param>
        private void StartThread(PeriodicJob job)
        {
            job.Thread = new Thread(() => Run(job))
            {
                Name = $"Periodic {job.Name}",
                IsBackground = true
            };

            job.Thread.Start();
        }

        /// <summary>
        ///     Loop of a single job: run the body, then wait the interval on the stop token
        /// </summary>
        /// <param name="job"></param>
        private void Run(PeriodicJob job)
        {
            CancellationToken token = _stopping.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    job.Action();
                }
                catch (Exception ex)
                {
                    // The jobs log their own errors, this is the net that keeps one of them from
                    // killing its thread and taking the rest of the schedule with it
                    _logger.LogError(ex, "Periodic job \"{Job}\" failed", job.Name);
                }

                // Interruptible pause: the wait returns as soon as the scheduler is asked to stop
                if (token.WaitHandle.WaitOne(job.Interval))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     One registered job: what to run, how often and on which thread it lives
        /// </summary>
        private class PeriodicJob
        {
            public PeriodicJob(string name, TimeSpan interval, Action action)
            {
                Name = name;
                Interval = interval;
                Action = action;
            }

            public string Name { get; }
            public TimeSpan Interval { get; }
            public Action Action { get; }

            public Thread Thread { get; set; }
        }
    }
}

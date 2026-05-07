using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaidSchedule.Configuration;
using RaidSchedule.Util;
using SDG.Unturned;

namespace RaidSchedule.Services
{
    public class RaidScheduleBackgroundService
    {
        private readonly IRaidScheduleService _schedule;
        private readonly IOptions<RaidScheduleConfig> _config;
        private readonly ILogger<RaidScheduleBackgroundService> _logger;

        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        // State we carry between ticks
        private bool? _previousOpenState;
        private DateTime _currentWindowEndUtc = DateTime.MinValue;
        private readonly HashSet<int> _firedWarningsForCurrentWindow = new();

        public RaidScheduleBackgroundService(
            IRaidScheduleService schedule,
            IOptions<RaidScheduleConfig> config,
            ILogger<RaidScheduleBackgroundService> logger)
        {
            _schedule = schedule;
            _config = config;
            _logger = logger;
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_cts != null) _cts.Cancel();
            if (_loopTask != null)
            {
                try { await _loopTask; }
                catch (OperationCanceledException) { /* expected */ }
            }
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            // Wait a moment after plugin load so the server is fully up before broadcasting anything
            try { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Tick();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in raid schedule background loop");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        private void Tick()
        {
            var cfg = _config.Value;
            var isOpen = _schedule.AreRaidsAllowed();
            var nextTransition = _schedule.GetNextTransitionUtc();

            // Detect window rollover: reset warning tracking when the window-end we're tracking changes.
            var windowEndUtc = isOpen ? nextTransition : DateTime.MinValue;
            if (windowEndUtc != _currentWindowEndUtc)
            {
                _currentWindowEndUtc = windowEndUtc;
                _firedWarningsForCurrentWindow.Clear();
            }

            // State transition: broadcast open/close
            if (_previousOpenState.HasValue && _previousOpenState.Value != isOpen)
            {
                var msg = isOpen ? cfg.Announcements.OnWindowOpen : cfg.Announcements.OnWindowClose;
                Broadcast(msg);
            }
            _previousOpenState = isOpen;

            // Pre-close warnings (only fire while window is open)
            if (isOpen)
            {
                var minutesRemaining = (int)Math.Round((nextTransition - DateTime.UtcNow).TotalMinutes);
                foreach (var warningMinutes in cfg.PreCloseWarnings)
                {
                    if (_firedWarningsForCurrentWindow.Contains(warningMinutes)) continue;

                    if (minutesRemaining <= warningMinutes && minutesRemaining > warningMinutes - 2)
                    {
                        var msg = cfg.Announcements.OnPreCloseWarning
                            .Replace("{minutes}", warningMinutes.ToString());
                        Broadcast(msg);
                        _firedWarningsForCurrentWindow.Add(warningMinutes);
                    }
                }
            }
        }

        private void Broadcast(string message)
        {
            var formatted = ChatHelper.Format(message);
            ChatManager.serverSendMessage(
                text: formatted,
                color: UnityEngine.Color.white,
                fromPlayer: null,
                toPlayer: null,
                mode: EChatMode.GLOBAL,
                iconURL: null,
                useRichTextFormatting: true);
        }
    }
}
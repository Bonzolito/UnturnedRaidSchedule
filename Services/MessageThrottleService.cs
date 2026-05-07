using System;
using System.Collections.Concurrent;

namespace RaidSchedule.Services
{
    public interface IMessageThrottleService
    {
        bool ShouldSend(ulong steamId, TimeSpan minInterval);
    }

    public class MessageThrottleService : IMessageThrottleService
    {
        private readonly ConcurrentDictionary<ulong, DateTime> _lastSent = new();

        public bool ShouldSend(ulong steamId, TimeSpan minInterval)
        {
            var now = DateTime.UtcNow;
            if (_lastSent.TryGetValue(steamId, out var last) && now - last < minInterval)
                return false;

            _lastSent[steamId] = now;
            return true;
        }
    }
}
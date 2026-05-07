using System;
using System.Globalization;
using Microsoft.Extensions.Options;
using RaidSchedule.Configuration;
using TimeZoneConverter;

namespace RaidSchedule.Services
{
    public interface IRaidScheduleService
    {
        bool AreRaidsAllowed();
        DateTime GetNextTransitionUtc();
        bool IsCurrentlyOpen { get; }
        TimeSpan TimeUntilNextTransition { get; }
    }

    public class RaidScheduleService : IRaidScheduleService
    {
        private readonly IOptions<RaidScheduleConfig> _config;

        public RaidScheduleService(IOptions<RaidScheduleConfig> config)
        {
            _config = config;
        }

        public bool IsCurrentlyOpen => AreRaidsAllowed();

        public TimeSpan TimeUntilNextTransition => GetNextTransitionUtc() - DateTime.UtcNow;

        public bool AreRaidsAllowed()
        {
            var (startUtc, endUtc) = GetCurrentWindowUtc();
            var now = DateTime.UtcNow;
            return now >= startUtc && now < endUtc;
        }

        public DateTime GetNextTransitionUtc()
        {
            var (startUtc, endUtc) = GetCurrentWindowUtc();
            var now = DateTime.UtcNow;

            // Currently inside the window -> next transition is window close
            if (now >= startUtc && now < endUtc)
                return endUtc;

            // Currently before this week's window opens -> next transition is open
            if (now < startUtc)
                return startUtc;

            // Window has already closed this week -> next transition is next week's open
            return GetWindowUtcForOffset(7).startUtc;
        }

        /// <summary>
        /// Returns the start and end of the raid window for the current week,
        /// anchored on the configured windowStart day. If the user is past
        /// this week's start, this still returns this week's window
        /// (the next-week logic is handled by GetNextTransitionUtc).
        /// </summary>
        private (DateTime startUtc, DateTime endUtc) GetCurrentWindowUtc()
            => GetWindowUtcForOffset(0);

        private (DateTime startUtc, DateTime endUtc) GetWindowUtcForOffset(int weekOffsetDays)
        {
            var cfg = _config.Value;
            var tz = TZConvert.GetTimeZoneInfo(cfg.Timezone);

            var startDay = ParseDayOfWeek(cfg.Schedule.WindowStart.Day);
            var endDay = ParseDayOfWeek(cfg.Schedule.WindowEnd.Day);
            var startTime = ParseTime(cfg.Schedule.WindowStart.Time);
            var endTime = ParseTime(cfg.Schedule.WindowEnd.Time);

            // Anchor "today" in the configured timezone, not server local time
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var todayLocal = nowLocal.Date.AddDays(weekOffsetDays);

            // Find this week's startDay (going backwards from "today" if necessary)
            var daysFromStart = ((int)todayLocal.DayOfWeek - (int)startDay + 7) % 7;
            var startLocalDate = todayLocal.AddDays(-daysFromStart);
            var startLocal = startLocalDate + startTime;

            // End day is calculated by stepping forward from start day
            var dayDelta = ((int)endDay - (int)startDay + 7) % 7;
            // Special case: if start and end are same day, treat as full-week-minus-epsilon? No —
            // for our use case (Fri 16:00 -> Mon 00:00) this is 3 days. If they're equal we treat
            // it as "ends 7 days later" to avoid zero-length window.
            if (dayDelta == 0 && endTime <= startTime) dayDelta = 7;

            var endLocal = startLocalDate.AddDays(dayDelta) + endTime;

            // Convert to UTC, respecting DST. TimeZoneInfo handles ambiguous/invalid times
            // (the 1-2am gap on DST transition days) by throwing or picking standard time;
            // we use the lenient overload behavior.
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), tz);

            return (startUtc, endUtc);
        }

        private static DayOfWeek ParseDayOfWeek(string day)
        {
            if (Enum.TryParse<DayOfWeek>(day, ignoreCase: true, out var result))
                return result;
            throw new ArgumentException($"Invalid day of week in config: '{day}'");
        }

        private static TimeSpan ParseTime(string time)
        {
            if (TimeSpan.TryParseExact(time, "hh\\:mm", CultureInfo.InvariantCulture, out var result))
                return result;
            throw new ArgumentException($"Invalid time format in config: '{time}' (expected HH:mm)");
        }
    }
}
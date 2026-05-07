using System.Collections.Generic;

namespace RaidSchedule.Configuration
{
    public class RaidScheduleConfig
    {
        public string Timezone { get; set; } = "Europe/London";
        public ScheduleConfig Schedule { get; set; } = new();
        public List<int> PreCloseWarnings { get; set; } = new() { 60, 30, 15 };
        public BlockingConfig Blocking { get; set; } = new();
        public PlayerFeedbackConfig PlayerFeedback { get; set; } = new();
        public AnnouncementsConfig Announcements { get; set; } = new();
    }

    public class ScheduleConfig
    {
        public DayTimeConfig WindowStart { get; set; } = new() { Day = "Friday", Time = "16:00" };
        public DayTimeConfig WindowEnd { get; set; } = new() { Day = "Monday", Time = "00:00" };
    }

    public class DayTimeConfig
    {
        public string Day { get; set; } = "Friday";
        public string Time { get; set; } = "16:00";
    }

    public class BlockingConfig
    {
        public bool Structures { get; set; } = true;
        public bool Barricades { get; set; } = true;
        public bool Vehicles { get; set; } = true;
    }

    public class PlayerFeedbackConfig
    {
        public bool Enabled { get; set; } = true;
        public int ThrottleSeconds { get; set; } = 10;
        public string Message { get; set; } = "&cRaiding is disabled. Next raid window in {timeUntil}";
    }

    public class AnnouncementsConfig
    {
        public string OnWindowOpen { get; set; } = "Raid window open!";
        public string OnWindowClose { get; set; } = "Raid window closed.";
        public string OnPreCloseWarning { get; set; } = "Raid window closes in {minutes} minutes.";
    }
}
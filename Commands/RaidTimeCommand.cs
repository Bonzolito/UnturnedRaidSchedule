using System;
using System.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.API.Commands;
using RaidSchedule.Services;
using RaidSchedule.Util;

namespace RaidSchedule.Commands
{
    [Command("raidtime")]
    [CommandDescription("Shows the current raid status and time until the next state change.")]
    [CommandSyntax("")]
    public class RaidTimeCommand : Command
    {
        private readonly IRaidScheduleService _schedule;

        public RaidTimeCommand(IRaidScheduleService schedule, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _schedule = schedule;
        }

        protected override Task OnExecuteAsync()
        {
            var isOpen = _schedule.IsCurrentlyOpen;
            var timeUntil = ChatHelper.FormatTimeSpan(_schedule.TimeUntilNextTransition);

            string msg = isOpen
                ? $"&a&lRaiding is ENABLED. &7Closes in &e{timeUntil}&7."
                : $"&c&lRaiding is DISABLED. &7Next window in &e{timeUntil}&7.";

            return Context.Actor.PrintMessageAsync(ChatHelper.Format(msg));
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Building.Events;
using RaidSchedule.Configuration;
using RaidSchedule.Services;
using RaidSchedule.Util;

namespace RaidSchedule.EventListeners
{
    public class BarricadeDamageListener : IEventListener<UnturnedBarricadeDamagingEvent>
    {
        private readonly IRaidScheduleService _schedule;
        private readonly IMessageThrottleService _throttle;
        private readonly IOptions<RaidScheduleConfig> _config;

        public BarricadeDamageListener(
            IRaidScheduleService schedule,
            IMessageThrottleService throttle,
            IOptions<RaidScheduleConfig> config)
        {
            _schedule = schedule;
            _throttle = throttle;
            _config = config;
        }

        public Task HandleEventAsync(object? sender, UnturnedBarricadeDamagingEvent @event)
        {
            var cfg = _config.Value;
            if (!cfg.Blocking.Barricades) return Task.CompletedTask;
            if (_schedule.AreRaidsAllowed()) return Task.CompletedTask;

            var instigatorSteamId = @event.InstigatorId.m_SteamID;

            // No player instigator (zombies, environment) - let damage through
            if (instigatorSteamId == 0) return Task.CompletedTask;

            // Allow self-damage (player removing their own barricades)
            if (@event.Buildable.Ownership.OwnerPlayerId == instigatorSteamId.ToString())
                return Task.CompletedTask;

            @event.IsCancelled = true;
            SendFeedback(instigatorSteamId);
            return Task.CompletedTask;
        }

        private void SendFeedback(ulong steamId)
        {
            var cfg = _config.Value;
            if (!cfg.PlayerFeedback.Enabled) return;
            if (!_throttle.ShouldSend(steamId, TimeSpan.FromSeconds(cfg.PlayerFeedback.ThrottleSeconds))) return;

            var timeUntil = ChatHelper.FormatTimeSpan(_schedule.TimeUntilNextTransition);
            var msg = ChatHelper.Format(cfg.PlayerFeedback.Message.Replace("{timeUntil}", timeUntil));

            var player = SDG.Unturned.PlayerTool.getSteamPlayer(steamId);
            if (player == null) return;

            SDG.Unturned.ChatManager.serverSendMessage(
                text: msg,
                color: UnityEngine.Color.white,
                fromPlayer: null,
                toPlayer: player,
                mode: SDG.Unturned.EChatMode.SAY,
                iconURL: null,
                useRichTextFormatting: true);
        }
    }
}
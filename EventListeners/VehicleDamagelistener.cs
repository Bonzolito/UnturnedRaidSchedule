using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Vehicles.Events;
using RaidSchedule.Configuration;
using RaidSchedule.Services;
using RaidSchedule.Util;

namespace RaidSchedule.EventListeners
{
    public class VehicleDamageListener : IEventListener<UnturnedVehicleDamagingEvent>
    {
        private readonly IRaidScheduleService _schedule;
        private readonly IMessageThrottleService _throttle;
        private readonly IOptions<RaidScheduleConfig> _config;

        public VehicleDamageListener(
            IRaidScheduleService schedule,
            IMessageThrottleService throttle,
            IOptions<RaidScheduleConfig> config)
        {
            _schedule = schedule;
            _throttle = throttle;
            _config = config;
        }

        public Task HandleEventAsync(object? sender, UnturnedVehicleDamagingEvent @event)
        {
            var cfg = _config.Value;
            if (!cfg.Blocking.Vehicles) return Task.CompletedTask;
            if (_schedule.AreRaidsAllowed()) return Task.CompletedTask;
            if (@event.Instigator == null) return Task.CompletedTask;

            var vehicle = @event.Vehicle.Vehicle; // SDG.Unturned.InteractableVehicle

            // Occupied vehicles (any seat) are always damageable — active PvP, not raiding.
            if (IsOccupied(vehicle)) return Task.CompletedTask;

            // Unlocked + empty: per scrubs' spec, locked-and-parked is what's protected.
            // We treat unlocked vehicles as "not claimed", so they're always damageable.
            if (!vehicle.isLocked) return Task.CompletedTask;

            var instigatorSteamId = @event.Instigator.Value.m_SteamID;

            // Allow owner to damage their own vehicle
            if (vehicle.lockedOwner.m_SteamID == instigatorSteamId) return Task.CompletedTask;

            @event.IsCancelled = true;
            SendFeedback(instigatorSteamId);
            return Task.CompletedTask;
        }

        private static bool IsOccupied(SDG.Unturned.InteractableVehicle vehicle)
        {
            if (vehicle.passengers == null) return false;
            for (int i = 0; i < vehicle.passengers.Length; i++)
            {
                if (vehicle.passengers[i]?.player != null) return true;
            }
            return false;
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
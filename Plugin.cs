using System;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;
using RaidSchedule.Configuration;
using RaidSchedule.Services;

[assembly: PluginMetadata("RaidSchedule", DisplayName = "Raid Schedule", Author = "Bonzolito", Website = "https://bitport.dev")]

namespace RaidSchedule
{
    public class Plugin : OpenModUnturnedPlugin
    {
        private readonly RaidScheduleBackgroundService _backgroundService;

        public Plugin(
            IServiceProvider serviceProvider,
            RaidScheduleBackgroundService backgroundService)
            : base(serviceProvider)
        {
            _backgroundService = backgroundService;
        }

        protected override async UniTask OnLoadAsync()
        {
            await _backgroundService.StartAsync();
            Logger.LogInformation("RaidSchedule loaded.");
        }

        protected override async UniTask OnUnloadAsync()
        {
            await _backgroundService.StopAsync();
            Logger.LogInformation("RaidSchedule unloaded.");
        }
    }

    public class PluginServices : IServiceConfigurator
    {
        public void ConfigureServices(IOpenModServiceConfigurationContext context, IServiceCollection services)
        {
            services.Configure<RaidScheduleConfig>(context.Configuration);
            services.AddSingleton<IRaidScheduleService, RaidScheduleService>();
            services.AddSingleton<IMessageThrottleService, MessageThrottleService>();
            services.AddSingleton<RaidScheduleBackgroundService>();
        }
    }
}
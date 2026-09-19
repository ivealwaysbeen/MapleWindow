using System.IO;
using MapleWindow.App.Services;
using MapleWindow.Core.Config;
using MapleWindow.Core.ImageCache;
using MapleWindow.Core.Nexon;
using MapleWindow.Core.Notifications;
using MapleWindow.Core.OcidResolution;
using MapleWindow.Core.Phrases;
using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MapleWindow.App;

public static class ServiceCollectionExtensions
{
    public static HostApplicationBuilder ConfigureMapleWindowServices(this HostApplicationBuilder builder)
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MapleWindow", "logs");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logDirectory, "app-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
        builder.Services.AddSerilog();

        builder.Services.AddHttpClient<INexonApiClient, NexonApiClient>();
        builder.Services.AddHttpClient<ICharacterImageCache, CharacterImageCache>();
        builder.Services.AddHttpClient<IUpdateChecker, GithubReleaseUpdateChecker>();
        builder.Services.AddHttpClient<AppUpdateService>();

        builder.Services.AddSingleton<IDataProtector, DpapiProtector>();
        builder.Services.AddSingleton<IConfigStore, ConfigService>();
        builder.Services.AddSingleton<IOcidResolver, OcidResolver>();
        builder.Services.AddSingleton<IPhraseRepository, PhraseRepository>();
        builder.Services.AddSingleton<INotificationPreferenceStore, NotificationPreferenceStore>();
        builder.Services.AddSingleton<IOnceDailyStateStore, OnceDailyStateStore>();

        builder.Services.AddSingleton<SpriteFrameProcessor>();
        builder.Services.AddSingleton<CharacterAppearanceService>();
        builder.Services.AddSingleton<SchedulerPollingService>();
        builder.Services.AddSingleton(sp => new SpeakCycleService(
            () => sp.GetRequiredService<SchedulerPollingService>().CurrentPool,
            sp.GetRequiredService<IPhraseRepository>(),
            sp.GetRequiredService<INotificationPreferenceStore>(),
            sp.GetRequiredService<IOnceDailyStateStore>()));

        builder.Services.AddSingleton<TrayIconManager>();
        builder.Services.AddSingleton<StartupRegistrar>();
        builder.Services.AddSingleton<AppUpdateService>();
        builder.Services.AddSingleton<AppBootstrapper>();

        return builder;
    }
}

using System.Windows.Threading;
using MapleWindow.App.Services;
using MapleWindow.App.ViewModels;
using MapleWindow.App.Views;
using MapleWindow.Core.Config;
using MapleWindow.Core.ImageCache;
using MapleWindow.Core.Nexon;
using MapleWindow.Core.Notifications;
using MapleWindow.Core.OcidResolution;
using MapleWindow.Core.Scheduler;
using Serilog;

namespace MapleWindow.App;

/// <summary>Composition root's orchestrator — owns the startup sequence described in the plan's "데이터 흐름" section.</summary>
public sealed class AppBootstrapper
{
    private readonly IConfigStore _configStore;
    private readonly IOcidResolver _ocidResolver;
    private readonly INexonApiClient _api;
    private readonly ICharacterImageCache _imageCache;
    private readonly SpriteFrameProcessor _spriteFrameProcessor;
    private readonly CharacterAppearanceService _appearanceService;
    private readonly SchedulerPollingService _pollingService;
    private readonly SpeakCycleService _speakCycle;
    private readonly INotificationPreferenceStore _notificationPreferences;
    private readonly TrayIconManager _trayIcon;
    private readonly StartupRegistrar _startupRegistrar;
    private readonly AppUpdateService _updateService;

    private OverlayWindow? _overlayWindow;
    private SettingsWindow? _settingsWindow;

    public AppBootstrapper(
        IConfigStore configStore,
        IOcidResolver ocidResolver,
        INexonApiClient api,
        ICharacterImageCache imageCache,
        SpriteFrameProcessor spriteFrameProcessor,
        CharacterAppearanceService appearanceService,
        SchedulerPollingService pollingService,
        SpeakCycleService speakCycle,
        INotificationPreferenceStore notificationPreferences,
        TrayIconManager trayIcon,
        StartupRegistrar startupRegistrar,
        AppUpdateService updateService)
    {
        _configStore = configStore;
        _ocidResolver = ocidResolver;
        _spriteFrameProcessor = spriteFrameProcessor;
        _api = api;
        _imageCache = imageCache;
        _appearanceService = appearanceService;
        _pollingService = pollingService;
        _speakCycle = speakCycle;
        _notificationPreferences = notificationPreferences;
        _trayIcon = trayIcon;
        _startupRegistrar = startupRegistrar;
        _updateService = updateService;
    }

    public async Task RunAsync()
    {
        _trayIcon.Show();
        await _updateService.CheckAndApplyAsync();

        var uiDispatcher = Dispatcher.CurrentDispatcher;

        var config = _configStore.Load();
        if (config is null)
        {
            config = RunSetup(existingApiKey: null);
            if (config is null)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }
        }
        else
        {
            await RefreshOcidAsync(config);
        }

        _startupRegistrar.EnsureRegistered();

        await EnsureAppearanceWithRetryAsync();

        Log.Information("[DIAG] startup poll for {Name}/{World} ocid={Ocid}", _configStore.Current?.CharacterName, _configStore.Current?.WorldName, _configStore.Current?.Ocid);
        await _pollingService.PollOnceAsync();
        var current = _configStore.Current!;
        _pollingService.Start(TimeSpan.FromSeconds(current.PollIntervalSeconds));
        _speakCycle.Start(TimeSpan.FromSeconds(current.SpeakIntervalSeconds));

        ShowOverlay(uiDispatcher);
        InitializeTray(uiDispatcher);
    }

    private async Task RefreshOcidAsync(AppConfig config)
    {
        try
        {
            var result = await _ocidResolver.ResolveAsync(config.ApiKey, config.CharacterName, config.WorldName);
            if (result.Status == OcidResolutionStatus.Found)
            {
                config.Ocid = result.Ocid!;
                _configStore.Save(config);
            }
            else
            {
                _trayIcon.ShowBalloon("MapleWindow", "캐릭터를 찾을 수 없어요. 캐릭터를 다시 선택해주세요.");
                RunSetup(existingApiKey: config.ApiKey, startAtPicker: true);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ocid 재확인 실패 — 마지막으로 알려진 ocid로 계속합니다.");
            _trayIcon.ShowBalloon("MapleWindow", "캐릭터 정보 갱신에 실패했어요. 이전 정보로 계속합니다.");
        }
    }

    /// <summary>Shows SetupWindow modally. Used both for first-run and for tray "캐릭터 변경" (startAtPicker=true skips the API-key step).</summary>
    private AppConfig? RunSetup(string? existingApiKey, bool startAtPicker = false)
    {
        var viewModel = new SetupViewModel(_api, _configStore);
        if (startAtPicker && existingApiKey is not null)
            viewModel.StartAtPickCharacter(existingApiKey);

        var window = new SetupWindow(viewModel);
        var result = window.ShowDialog();

        return result == true ? _configStore.Current : null;
    }

    private async Task EnsureAppearanceWithRetryAsync()
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await _appearanceService.RefreshAsync();
                if (!string.IsNullOrEmpty(_appearanceService.CharacterImageBaseUrl)) return;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "캐릭터 외형 갱신 실패 (시도 {Attempt}/3)", attempt);
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    private void ShowOverlay(Dispatcher dispatcher)
    {
        var overlayViewModel = new OverlayViewModel(_imageCache, _spriteFrameProcessor, _appearanceService, _speakCycle, dispatcher);
        _overlayWindow = new OverlayWindow(overlayViewModel);
        _overlayWindow.Show();
    }

    private void InitializeTray(Dispatcher uiDispatcher)
    {
        _trayIcon.ChangeCharacterRequested += async (_, _) =>
        {
            var apiKey = _configStore.Current?.ApiKey;
            Log.Information("[DIAG] before RunSetup: current={Name}/{World} ocid={Ocid}", _configStore.Current?.CharacterName, _configStore.Current?.WorldName, _configStore.Current?.Ocid);
            var dialogResult = RunSetup(existingApiKey: apiKey, startAtPicker: true);
            Log.Information("[DIAG] after RunSetup: dialogResult={Name}/{World} ocid={Ocid}", dialogResult?.CharacterName, dialogResult?.WorldName, dialogResult?.Ocid);
            await _appearanceService.RefreshAsync();
            await _pollingService.PollOnceAsync();
            _speakCycle.ResetQueue();
            var pool = _pollingService.CurrentPool;
            Log.Information("[DIAG] after re-poll: daily=[{Daily}] weekly=[{Weekly}] boss=[{Boss}]",
                string.Join(",", pool.DailyContents.Select(i => i.ContentName)),
                string.Join(",", pool.WeeklyContents.Select(i => i.ContentName)),
                string.Join(",", pool.BossContents.Select(b => b.ContentName)));
        };

        _trayIcon.SettingsRequested += (_, _) =>
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(new SettingsViewModel(_notificationPreferences, _pollingService, _configStore, _speakCycle, _appearanceService, uiDispatcher));
            _settingsWindow.Show();
        };

        _trayIcon.ExitRequested += (_, _) => System.Windows.Application.Current.Shutdown();
    }
}

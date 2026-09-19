namespace MapleWindow.App.Services;

/// <summary>
/// Uses System.Windows.Forms.NotifyIcon rather than a WPF-native tray library: H.NotifyIcon.Wpf 2.4.1 (the
/// current version at plan time) has no net9.0-windows target (NuGet falls back to its net462 asset with a
/// NU1701 compatibility warning), which is a real reliability risk for a tray-resident background app.
/// System.Windows.Forms ships directly in the net9.0-windows desktop runtime, so this has none of that risk.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event EventHandler? ChangeCharacterRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? UninstallRequested;

    public TrayIconManager()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("캐릭터 변경", null, (_, _) => ChangeCharacterRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("설정", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("프로그램 제거", null, (_, _) => UninstallRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("종료", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "MapleWindow",
            ContextMenuStrip = menu,
            Visible = false,
        };
    }

    private static Icon LoadTrayIcon()
    {
        var resourceStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/tray-icon.ico"));
        return new Icon(resourceStream!.Stream);
    }

    public void Show() => _notifyIcon.Visible = true;

    public void ShowBalloon(string title, string text) => _notifyIcon.ShowBalloonTip(5000, title, text, ToolTipIcon.Info);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}

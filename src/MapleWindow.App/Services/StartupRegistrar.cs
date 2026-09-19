using Microsoft.Win32;

namespace MapleWindow.App.Services;

/// <summary>Registers/unregisters the app under HKCU Run so it launches on Windows login. Idempotent — only writes when the stored path differs.</summary>
public sealed class StartupRegistrar
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MapleWindow";

    public void EnsureRegistered()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null) return;

        var desired = $"\"{exePath}\"";
        if (key.GetValue(ValueName) as string != desired)
            key.SetValue(ValueName, desired);
    }

    public void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

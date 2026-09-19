using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using MapleWindow.Core.Updates;
using Serilog;

namespace MapleWindow.App.Services;

/// <summary>
/// 앱 시작 시 1회 GitHub 최신 릴리즈를 확인하고, 더 새로운 버전이 있으면 다운로드·적용 후 재시작한다.
/// 실행 중인 exe는 자기 자신을 덮어쓸 수 없으므로, 분리된 .cmd 스크립트가 프로세스 종료를 기다렸다가
/// 파일을 교체하고 재실행한다.
/// </summary>
public sealed class AppUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly IUpdateChecker _updateChecker;
    private readonly TrayIconManager _trayIcon;

    public AppUpdateService(HttpClient httpClient, IUpdateChecker updateChecker, TrayIconManager trayIcon)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        _updateChecker = updateChecker;
        _trayIcon = trayIcon;
    }

    public async Task CheckAndApplyAsync()
    {
        try
        {
            var latest = await _updateChecker.GetLatestReleaseAsync();
            if (latest is null) return;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion is null || latest.Version <= currentVersion) return;

            await DownloadAndApplyAsync(latest);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "자동 업데이트 확인/적용 실패 — 정상 부팅을 계속합니다.");
        }
    }

    private async Task DownloadAndApplyAsync(GithubReleaseInfo latest)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;
        var installDir = Path.GetDirectoryName(exePath)!;

        var workDir = Path.Combine(Path.GetTempPath(), "MapleWindowUpdate", latest.Version.ToString());
        Directory.CreateDirectory(workDir);
        var zipPath = Path.Combine(workDir, "download.zip");
        var extractedDir = Path.Combine(workDir, "extracted");

        await using (var fileStream = File.Create(zipPath))
        await using (var httpStream = await _httpClient.GetStreamAsync(latest.ZipAssetUrl))
        {
            await httpStream.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, extractedDir, overwriteFiles: true);

        // Release zip wraps published files in a top-level "MapleWindow" folder (see release.yml) so a flat
        // extraction can't be mistaken for an arbitrary user folder by the uninstall script.
        var copySource = Path.Combine(extractedDir, "MapleWindow");

        var scriptPath = Path.Combine(workDir, "apply.cmd");
        File.WriteAllText(scriptPath, BuildApplyScript(), Encoding.ASCII);

        var pid = Environment.ProcessId;

        _trayIcon.ShowBalloon("MapleWindow", "업데이트를 적용하고 재시작합니다...");

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{scriptPath}\" {pid} \"{copySource}\" \"{installDir}\" \"{exePath}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        });

        Log.CloseAndFlush();
        _trayIcon.Dispose();
        Environment.Exit(0);
    }

    private static string BuildApplyScript() =>
        "@echo off\r\n" +
        ":wait\r\n" +
        "tasklist /fi \"PID eq %1\" 2>nul | find \"%1\" >nul\r\n" +
        "if not errorlevel 1 (\r\n" +
        "  ping -n 2 127.0.0.1 >nul\r\n" +
        "  goto wait\r\n" +
        ")\r\n" +
        "robocopy \"%~2\" \"%~3\" /E /IS /IT /NFL /NDL\r\n" +
        "start \"\" /d \"%~3\" \"%~4\"\r\n" +
        "del \"%~f0\"\r\n";
}

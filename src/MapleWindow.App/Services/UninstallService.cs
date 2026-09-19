using System.Diagnostics;
using System.IO;
using System.Text;
using Serilog;

namespace MapleWindow.App.Services;

/// <summary>
/// 트레이 "프로그램 제거" 확인 후 호출된다. 사용자 데이터와 시작 프로그램 등록을 즉시 제거하고,
/// 실행 중인 exe가 자기 폴더를 지울 수 없으므로 분리된 .cmd 스크립트가 프로세스 종료를 기다렸다가
/// 설치 폴더를 삭제한다.
/// </summary>
public sealed class UninstallService
{
    private readonly StartupRegistrar _startupRegistrar;

    public UninstallService(StartupRegistrar startupRegistrar)
    {
        _startupRegistrar = startupRegistrar;
    }

    public void Uninstall()
    {
        Log.CloseAndFlush();

        DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MapleWindow"));
        DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapleWindow"));

        _startupRegistrar.Unregister();

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Environment.Exit(0);
            return;
        }
        var installDir = Path.GetDirectoryName(exePath)!;

        var scriptDir = Path.Combine(Path.GetTempPath(), "MapleWindowUninstall");
        Directory.CreateDirectory(scriptDir);
        var scriptPath = Path.Combine(scriptDir, "uninstall.cmd");
        File.WriteAllText(scriptPath, BuildUninstallScript(), Encoding.ASCII);

        var pid = Environment.ProcessId;
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{scriptPath}\" {pid} \"{installDir}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        });

        Environment.Exit(0);
    }

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }

    private static string BuildUninstallScript() =>
        "@echo off\r\n" +
        ":wait\r\n" +
        "tasklist /fi \"PID eq %1\" 2>nul | find \"%1\" >nul\r\n" +
        "if not errorlevel 1 (\r\n" +
        "  timeout /t 1 /nobreak >nul\r\n" +
        "  goto wait\r\n" +
        ")\r\n" +
        "rmdir /s /q \"%2\"\r\n" +
        "del \"%~f0\"\r\n";
}

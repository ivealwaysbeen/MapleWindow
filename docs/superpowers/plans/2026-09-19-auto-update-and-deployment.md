# 자동 업데이트 및 배포 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** MapleWindow가 재시작할 때마다 GitHub Releases에서 자동으로 최신 버전을 확인·적용하고, 태그 push만으로 릴리즈 zip이 자동 생성되며, 신규 사용자가 README만 보고 설치·삭제까지 할 수 있게 만든다.

**Architecture:** Core 계층에 GitHub Releases API 클라이언트(`IUpdateChecker`)를 추가해 버전/다운로드 URL만 조회하게 하고, App 계층의 `AppUpdateService`가 실제 다운로드·압축해제·재시작(자기 자신을 못 지우므로 분리된 `.cmd` 스크립트 경유)을 담당한다. 제거 기능도 동일한 "종료 대기 후 파일 작업" 패턴을 별도의 짧은 스크립트로 구현한다. 배포는 태그 push → GitHub Actions가 `dotnet publish` 후 zip을 Release에 첨부하는 방식으로 자동화한다.

**Tech Stack:** .NET 9 (net9.0-windows, WPF), `Microsoft.Extensions.Http`(기존 참조), `System.Text.Json`, `System.IO.Compression`(BCL), `Microsoft.Win32.Registry`(기존 참조), xunit(기존 테스트 인프라), GitHub Actions.

**Spec:** [docs/superpowers/specs/2026-09-19-auto-update-and-deployment-design.md](../specs/2026-09-19-auto-update-and-deployment-design.md)

## Global Constraints

- 대상 저장소: `ivealwaysbeen/MapleWindow` (GitHub, public)
- 릴리즈 zip 에셋 이름은 항상 정확히 `MapleWindow-win-x64.zip` (업데이트 체커와 GitHub Actions 워크플로가 반드시 일치해야 함)
- 버전 태그 형식: `v` + `System.Version` 파싱 가능한 문자열 (예: `v1.0.0`)
- GitHub Releases API 조회 타임아웃: 5초, 실패 시 예외를 던지지 않고 정상 부팅을 계속 진행 (사용자에게 실패를 알리지 않음)
- 새 NuGet 패키지 의존성 추가 금지 — 필요한 기능(HttpClient, 압축, 레지스트리, 프로세스 실행)은 모두 기존 참조로 충분함
- GitHub Actions 워크플로는 저장소 기본 제공 `secrets.GITHUB_TOKEN`만 사용, 추가 시크릿 등록 금지
- 확인 없는 파괴적 동작 금지: "프로그램 제거"는 반드시 확인 팝업([취소]/[제거], 기본 포커스 [취소])을 거친 뒤에만 실제 삭제 진행
- `git push`(원격 최초 push, 태그 push)는 이 문서의 자동화된 태스크에 포함하지 않는다 — 문서 맨 끝 "배포 체크리스트"에서 사용자 확인 후 별도로 실행한다

---

## Task 1: 저장소 정리 (커밋 없이 코드 변경만)

이번 작업은 전체 저장소가 아직 한 번도 커밋되지 않은 상태에서 진행된다. 이 태스크는 초기 커밋을 미리 깨끗하게 만들기 위한 정리 작업이며, 실제 첫 커밋은 이후 태스크들이 끝난 뒤 한 번에 만든다 (플랜 맨 끝 "배포 체크리스트" 참고). 따라서 이 태스크에는 별도의 git commit 스텝이 없다.

**Files:**
- Modify: `.gitignore`
- Delete: `_run.log`

**Interfaces:** 없음 (파일 정리만 수행)

- [ ] **Step 1: `.gitignore`에 로그 파일 패턴 추가**

`.gitignore` 전체를 다음 내용으로 교체:

```
bin/
obj/
.vs/
*.user
*.suo
*.log
_run.log
```

- [ ] **Step 2: 저장소 루트의 빈 로그 파일 삭제**

```bash
rm /c/MapleWindow/_run.log
```

- [ ] **Step 3: 확인**

```bash
cd /c/MapleWindow && git status
```

Expected: `_run.log`가 더 이상 목록에 없고, `.gitignore`만 수정된 상태로 보임 (아직 커밋 없음, 정상).

---

## Task 2: `GithubReleaseUpdateChecker` (Core) — 최신 릴리즈 조회

**Files:**
- Create: `src/MapleWindow.Core/Updates/GithubReleaseInfo.cs`
- Create: `src/MapleWindow.Core/Updates/IUpdateChecker.cs`
- Create: `src/MapleWindow.Core/Updates/GithubReleaseUpdateChecker.cs`
- Modify: `tests/MapleWindow.Core.Tests/TestDoubles/FakeHttpMessageHandler.cs`
- Test: `tests/MapleWindow.Core.Tests/Updates/GithubReleaseUpdateCheckerTests.cs`

**Interfaces:**
- Produces: `MapleWindow.Core.Updates.GithubReleaseInfo` (record, properties `Version` : `System.Version`, `ZipAssetUrl` : `string`); `MapleWindow.Core.Updates.IUpdateChecker` with `Task<GithubReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)`; `MapleWindow.Core.Updates.GithubReleaseUpdateChecker` (public constructor `(HttpClient httpClient)`) implementing `IUpdateChecker` — Task 3의 `AppUpdateService`가 이 인터페이스를 소비한다.

- [ ] **Step 1: 실패하는 테스트 작성 — 먼저 `FakeHttpMessageHandler`에 상태 코드 커스터마이징 추가**

`tests/MapleWindow.Core.Tests/TestDoubles/FakeHttpMessageHandler.cs`를 다음으로 교체 (기존 동작은 그대로 유지하면서 `StatusCode` 속성만 추가):

```csharp
using System.Net;

namespace MapleWindow.Core.Tests.TestDoubles;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<string> RequestedUrls { get; } = [];
    public byte[] ResponseBytes { get; set; } = [1, 2, 3, 4];
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(request.RequestUri!.ToString());
        var response = new HttpResponseMessage(StatusCode) { Content = new ByteArrayContent(ResponseBytes) };
        return Task.FromResult(response);
    }
}
```

- [ ] **Step 2: 테스트 파일 작성**

`tests/MapleWindow.Core.Tests/Updates/GithubReleaseUpdateCheckerTests.cs`:

```csharp
using System.Net;
using System.Text;
using MapleWindow.Core.Tests.TestDoubles;
using MapleWindow.Core.Updates;

namespace MapleWindow.Core.Tests.Updates;

public class GithubReleaseUpdateCheckerTests
{
    private static GithubReleaseUpdateChecker CreateChecker(FakeHttpMessageHandler handler)
        => new(new HttpClient(handler));

    [Fact]
    public async Task GetLatestReleaseAsync_ValidReleaseWithMatchingAsset_ReturnsVersionAndUrl()
    {
        var handler = new FakeHttpMessageHandler
        {
            ResponseBytes = Encoding.UTF8.GetBytes("""
                {
                  "tag_name": "v1.2.3",
                  "assets": [
                    { "name": "MapleWindow-win-x64.zip", "browser_download_url": "https://github.com/ivealwaysbeen/MapleWindow/releases/download/v1.2.3/MapleWindow-win-x64.zip" }
                  ]
                }
                """),
        };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.NotNull(result);
        Assert.Equal(new Version(1, 2, 3), result!.Version);
        Assert.Equal("https://github.com/ivealwaysbeen/MapleWindow/releases/download/v1.2.3/MapleWindow-win-x64.zip", result.ZipAssetUrl);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_RequestsCorrectEndpoint()
    {
        var handler = new FakeHttpMessageHandler
        {
            ResponseBytes = Encoding.UTF8.GetBytes("""{ "tag_name": "v1.0.0", "assets": [] }"""),
        };
        var checker = CreateChecker(handler);

        await checker.GetLatestReleaseAsync();

        Assert.Equal(["https://api.github.com/repos/ivealwaysbeen/MapleWindow/releases/latest"], handler.RequestedUrls);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_NoMatchingAsset_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler
        {
            ResponseBytes = Encoding.UTF8.GetBytes("""
                { "tag_name": "v1.2.3", "assets": [ { "name": "other-file.zip", "browser_download_url": "https://example.com/other-file.zip" } ] }
                """),
        };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_MalformedJson_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler { ResponseBytes = Encoding.UTF8.GetBytes("not json") };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_HttpErrorStatus_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler { StatusCode = HttpStatusCode.InternalServerError };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.Null(result);
    }
}
```

- [ ] **Step 3: 테스트 실행 후 실패 확인**

Run: `dotnet test tests/MapleWindow.Core.Tests --filter GithubReleaseUpdateCheckerTests`
Expected: FAIL (컴파일 오류 — `MapleWindow.Core.Updates` 네임스페이스가 아직 없음)

- [ ] **Step 4: `GithubReleaseInfo` 작성**

`src/MapleWindow.Core/Updates/GithubReleaseInfo.cs`:

```csharp
namespace MapleWindow.Core.Updates;

public sealed record GithubReleaseInfo(Version Version, string ZipAssetUrl);
```

- [ ] **Step 5: `IUpdateChecker` 작성**

`src/MapleWindow.Core/Updates/IUpdateChecker.cs`:

```csharp
namespace MapleWindow.Core.Updates;

public interface IUpdateChecker
{
    Task<GithubReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default);
}
```

- [ ] **Step 6: `GithubReleaseUpdateChecker` 구현**

`src/MapleWindow.Core/Updates/GithubReleaseUpdateChecker.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapleWindow.Core.Updates;

public sealed class GithubReleaseUpdateChecker : IUpdateChecker
{
    private const string RepoOwner = "ivealwaysbeen";
    private const string RepoName = "MapleWindow";
    private const string ZipAssetName = "MapleWindow-win-x64.zip";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;

    public GithubReleaseUpdateChecker(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.github.com");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MapleWindow-UpdateChecker");
    }

    public async Task<GithubReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/repos/{RepoOwner}/{RepoName}/releases/latest", ct);
            if (!response.IsSuccessStatusCode) return null;

            await using var body = await response.Content.ReadAsStreamAsync(ct);
            var release = await JsonSerializer.DeserializeAsync<GithubReleaseApiResponse>(body, JsonOptions, ct);
            if (release?.TagName is null) return null;

            var asset = release.Assets?.FirstOrDefault(a => a.Name == ZipAssetName);
            if (asset?.BrowserDownloadUrl is null) return null;

            var versionText = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionText, out var version)) return null;

            return new GithubReleaseInfo(version, asset.BrowserDownloadUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    private sealed record GithubReleaseApiResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        public List<GithubReleaseAssetApiResponse>? Assets { get; init; }
    }

    private sealed record GithubReleaseAssetApiResponse
    {
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; init; }
    }
}
```

- [ ] **Step 7: 테스트 실행 후 통과 확인**

Run: `dotnet test tests/MapleWindow.Core.Tests --filter GithubReleaseUpdateCheckerTests`
Expected: PASS (5개 테스트 모두)

- [ ] **Step 8: 전체 Core 테스트 회귀 확인**

Run: `dotnet test tests/MapleWindow.Core.Tests`
Expected: 기존 테스트 전부 PASS (특히 `CharacterImageCacheTests` — `FakeHttpMessageHandler` 수정의 영향 없음을 확인)

- [ ] **Step 9: 커밋**

```bash
cd /c/MapleWindow
git add src/MapleWindow.Core/Updates tests/MapleWindow.Core.Tests/Updates tests/MapleWindow.Core.Tests/TestDoubles/FakeHttpMessageHandler.cs
git commit -m "feat: add GitHub release update checker"
```

---

## Task 3: `AppUpdateService` — 다운로드·적용·재시작

**Files:**
- Modify: `src/MapleWindow.App/MapleWindow.App.csproj`
- Create: `src/MapleWindow.App/Services/AppUpdateService.cs`
- Modify: `src/MapleWindow.App/ServiceCollectionExtensions.cs`
- Modify: `src/MapleWindow.App/AppBootstrapper.cs`

**Interfaces:**
- Consumes: `MapleWindow.Core.Updates.IUpdateChecker.GetLatestReleaseAsync(CancellationToken ct = default) : Task<GithubReleaseInfo?>` (Task 2), `TrayIconManager.ShowBalloon(string title, string text)` (기존)
- Produces: `MapleWindow.App.Services.AppUpdateService` (public constructor `(HttpClient httpClient, IUpdateChecker updateChecker, TrayIconManager trayIcon)`), 공개 메서드 `Task CheckAndApplyAsync()` — Task 5의 `AppBootstrapper`가 소비.

이 태스크의 핵심 로직(실제 다운로드/프로세스 종료/재시작)은 실행 중인 프로세스와 실제 GitHub 릴리즈 존재 여부에 의존하므로 자동 테스트 대상에서 제외한다 (스펙의 "테스트 방침" 참고, 기존 코드베이스에도 `StartupRegistrar` 등 OS 연동 코드는 단위 테스트가 없음). 대신 빌드 성공 + "업데이트 없음" 경로의 수동 실행 확인으로 검증한다.

- [ ] **Step 1: `MapleWindow.App.csproj`에 버전 추가**

`src/MapleWindow.App/MapleWindow.App.csproj`의 `<PropertyGroup>` 블록(기존 `<OutputType>WinExe</OutputType>` 바로 아래)에 추가:

```xml
    <Version>1.0.0</Version>
```

- [ ] **Step 2: `AppUpdateService` 작성**

`src/MapleWindow.App/Services/AppUpdateService.cs`:

```csharp
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
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

        _trayIcon.ShowBalloon("MapleWindow", "업데이트를 적용하고 재시작합니다...");

        await using (var fileStream = File.Create(zipPath))
        await using (var httpStream = await _httpClient.GetStreamAsync(latest.ZipAssetUrl))
        {
            await httpStream.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, extractedDir, overwriteFiles: true);

        var scriptPath = Path.Combine(workDir, "apply.cmd");
        File.WriteAllText(scriptPath, BuildApplyScript(), Encoding.ASCII);

        var pid = Environment.ProcessId;
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{scriptPath}\" {pid} \"{extractedDir}\" \"{installDir}\" \"{exePath}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        });

        Log.CloseAndFlush();
        Environment.Exit(0);
    }

    private static string BuildApplyScript() =>
        "@echo off\r\n" +
        ":wait\r\n" +
        "tasklist /fi \"PID eq %1\" 2>nul | find \"%1\" >nul\r\n" +
        "if not errorlevel 1 (\r\n" +
        "  timeout /t 1 /nobreak >nul\r\n" +
        "  goto wait\r\n" +
        ")\r\n" +
        "robocopy \"%2\" \"%3\" /E /IS /IT /NFL /NDL\r\n" +
        "start \"\" \"%4\"\r\n" +
        "del \"%~f0\"\r\n";
}
```

- [ ] **Step 3: DI 등록**

`src/MapleWindow.App/ServiceCollectionExtensions.cs`의 기존 `builder.Services.AddHttpClient<ICharacterImageCache, CharacterImageCache>();` 바로 아래에 추가:

```csharp
        builder.Services.AddHttpClient<IUpdateChecker, GithubReleaseUpdateChecker>();
        builder.Services.AddHttpClient<AppUpdateService>();
```

그리고 파일 상단 `using` 목록에 `using MapleWindow.Core.Updates;` 추가.

`builder.Services.AddSingleton<AppBootstrapper>();` 바로 위에 추가:

```csharp
        builder.Services.AddSingleton<AppUpdateService>();
```

- [ ] **Step 4: `AppBootstrapper`에 업데이트 체크 연결**

`src/MapleWindow.App/AppBootstrapper.cs` 상단 필드 목록(현재 `_startupRegistrar` 아래)에 추가:

```csharp
    private readonly AppUpdateService _updateService;
```

생성자 파라미터 목록 끝(`StartupRegistrar startupRegistrar` 바로 뒤)에 `, AppUpdateService updateService` 추가하고, 생성자 본문에 `_startupRegistrar = startupRegistrar;` 바로 아래 줄로 `_updateService = updateService;` 추가.

`RunAsync()` 메서드의 첫 줄(`var uiDispatcher = Dispatcher.CurrentDispatcher;` 바로 앞)에 추가:

```csharp
        await _updateService.CheckAndApplyAsync();
```

(업데이트가 실제로 적용되는 경우 `AppUpdateService.DownloadAndApplyAsync`가 `Environment.Exit(0)`을 호출하므로 이 지점 이후 코드는 실행되지 않는다. `AppUpdateService`는 `TrayIconManager`에 의존하므로, 생성자 주입 시점에 `TrayIconManager`가 아직 `Show()`되지 않았어도 `ShowBalloon`은 문제없이 동작한다 — `NotifyIcon.Visible=false`인 상태에서 `ShowBalloonTip`을 호출해도 예외는 나지 않지만 풍선이 보이지 않을 수 있으므로, `_trayIcon.Show()`를 이 업데이트 체크보다 먼저 호출하도록 순서를 아래처럼 조정한다.)

`RunAsync()`를 다음과 같이 재구성 (전체 메서드 교체):

```csharp
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

        await _pollingService.PollOnceAsync();
        var current = _configStore.Current!;
        _pollingService.Start(TimeSpan.FromSeconds(current.PollIntervalSeconds));
        _speakCycle.Start(TimeSpan.FromSeconds(current.SpeakIntervalSeconds));

        ShowOverlay(uiDispatcher);
        InitializeTray();
    }
```

`InitializeTray()`에서는 더 이상 `_trayIcon.Show()`를 호출하지 않도록 마지막 줄(`_trayIcon.Show();`)을 삭제한다 (트레이 이벤트 구독만 남김).

- [ ] **Step 5: 빌드 확인**

Run: `dotnet build MapleWindow.sln`
Expected: 빌드 성공 (경고 없이, 또는 기존과 동일한 경고만)

- [ ] **Step 6: 수동 실행 확인 (업데이트 없음 경로)**

Run: `dotnet run --project src/MapleWindow.App`

Expected: 아직 원격 저장소에 릴리즈가 없으므로 `GetLatestReleaseAsync`가 404(`IsSuccessStatusCode=false`)로 `null`을 반환 → `CheckAndApplyAsync`가 조용히 리턴 → 기존 설정 화면/오버레이가 정상적으로 뜸 (앱이 멈추거나 크래시하지 않음을 확인). 확인 후 트레이에서 "종료"로 앱 종료.

- [ ] **Step 7: 커밋**

```bash
cd /c/MapleWindow
git add src/MapleWindow.App/Services/AppUpdateService.cs src/MapleWindow.App/ServiceCollectionExtensions.cs src/MapleWindow.App/AppBootstrapper.cs src/MapleWindow.App/MapleWindow.App.csproj
git commit -m "feat: check and apply GitHub release updates on startup"
```

---

## Task 4: 시작 프로그램 등록 해제 (`StartupRegistrar.Unregister`)

**Files:**
- Modify: `src/MapleWindow.App/Services/StartupRegistrar.cs`

**Interfaces:**
- Produces: `StartupRegistrar.Unregister() : void` — Task 6의 `UninstallService`가 소비.

기존 `EnsureRegistered()`와 동일하게 레지스트리에 직접 접근하는 코드라 자동 테스트가 없다 (기존 코드베이스 관례를 따름). 수동으로 레지스트리를 확인한다.

- [ ] **Step 1: `Unregister()` 메서드 추가**

`src/MapleWindow.App/Services/StartupRegistrar.cs`의 `EnsureRegistered()` 메서드 바로 아래에 추가:

```csharp

    public void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
```

- [ ] **Step 2: 빌드 확인**

Run: `dotnet build src/MapleWindow.App/MapleWindow.App.csproj`
Expected: 빌드 성공

- [ ] **Step 3: 커밋**

```bash
cd /c/MapleWindow
git add src/MapleWindow.App/Services/StartupRegistrar.cs
git commit -m "feat: add StartupRegistrar.Unregister for uninstall flow"
```

---

## Task 5: `ConfirmUninstallWindow` — 제거 확인 팝업

**Files:**
- Create: `src/MapleWindow.App/Views/ConfirmUninstallWindow.xaml`
- Create: `src/MapleWindow.App/Views/ConfirmUninstallWindow.xaml.cs`

**Interfaces:**
- Produces: `MapleWindow.App.Views.ConfirmUninstallWindow` — `ShowDialog()`가 `true`(제거) 또는 `false`/`null`(취소)을 반환. Task 7의 `AppBootstrapper`가 소비.

UI 창이라 자동 테스트 대상이 아니다 (기존 `SetupWindow`/`SettingsWindow`도 테스트 없음). 수동 실행으로 확인한다 (Task 7에서 트레이 메뉴 연결 후 함께 확인).

- [ ] **Step 1: XAML 작성**

`src/MapleWindow.App/Views/ConfirmUninstallWindow.xaml`:

```xml
<Window x:Class="MapleWindow.App.Views.ConfirmUninstallWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="MapleWindow 제거" Height="180" Width="360"
        WindowStartupLocation="CenterScreen" ResizeMode="NoResize">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" TextWrapping="Wrap" FontSize="14"
                   Text="정말 MapleWindow를 삭제하시겠습니까?&#x0a;설정과 캐릭터 데이터가 모두 삭제되며 되돌릴 수 없습니다." />

        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,16,0,0">
            <Button Content="취소" Width="80" Margin="0,0,8,0" IsCancel="True" IsDefault="True" Click="OnCancelClick" />
            <Button Content="제거" Width="80" Click="OnUninstallClick" />
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 2: 코드 비하인드 작성**

`src/MapleWindow.App/Views/ConfirmUninstallWindow.xaml.cs`:

```csharp
using System.Windows;

namespace MapleWindow.App.Views;

public partial class ConfirmUninstallWindow : Window
{
    public ConfirmUninstallWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnUninstallClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
```

- [ ] **Step 3: 빌드 확인**

Run: `dotnet build src/MapleWindow.App/MapleWindow.App.csproj`
Expected: 빌드 성공

- [ ] **Step 4: 커밋**

```bash
cd /c/MapleWindow
git add src/MapleWindow.App/Views/ConfirmUninstallWindow.xaml src/MapleWindow.App/Views/ConfirmUninstallWindow.xaml.cs
git commit -m "feat: add uninstall confirmation dialog"
```

---

## Task 6: `UninstallService` — 실제 제거 로직

**Files:**
- Create: `src/MapleWindow.App/Services/UninstallService.cs`

**Interfaces:**
- Consumes: `StartupRegistrar.Unregister()` (Task 4)
- Produces: `MapleWindow.App.Services.UninstallService` (public constructor `(StartupRegistrar startupRegistrar)`), 공개 메서드 `void Uninstall()` — Task 7의 `AppBootstrapper`가 소비.

`AppUpdateService`와 동일한 이유로 자동 테스트 대상에서 제외 (실제 프로세스 종료·파일 삭제가 관여).

- [ ] **Step 1: `UninstallService` 작성**

`src/MapleWindow.App/Services/UninstallService.cs`:

```csharp
using System.Diagnostics;
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
```

- [ ] **Step 2: 빌드 확인**

Run: `dotnet build src/MapleWindow.App/MapleWindow.App.csproj`
Expected: 빌드 성공

- [ ] **Step 3: 커밋**

```bash
cd /c/MapleWindow
git add src/MapleWindow.App/Services/UninstallService.cs
git commit -m "feat: add UninstallService for tray-triggered removal"
```

---

## Task 7: 트레이 메뉴 연결 — "프로그램 제거"

**Files:**
- Modify: `src/MapleWindow.App/Services/TrayIconManager.cs`
- Modify: `src/MapleWindow.App/ServiceCollectionExtensions.cs`
- Modify: `src/MapleWindow.App/AppBootstrapper.cs`

**Interfaces:**
- Consumes: `ConfirmUninstallWindow` (Task 5), `UninstallService.Uninstall()` (Task 6)
- Produces: `TrayIconManager.UninstallRequested : EventHandler?`

- [ ] **Step 1: `TrayIconManager`에 메뉴 항목과 이벤트 추가**

`src/MapleWindow.App/Services/TrayIconManager.cs`의 이벤트 목록(`public event EventHandler? ExitRequested;` 바로 아래)에 추가:

```csharp
    public event EventHandler? UninstallRequested;
```

메뉴 구성 블록을 다음으로 교체:

```csharp
        var menu = new ContextMenuStrip();
        menu.Items.Add("캐릭터 변경", null, (_, _) => ChangeCharacterRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("설정", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("프로그램 제거", null, (_, _) => UninstallRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("종료", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
```

- [ ] **Step 2: DI 등록**

`src/MapleWindow.App/ServiceCollectionExtensions.cs`의 `builder.Services.AddSingleton<StartupRegistrar>();` 바로 아래에 추가:

```csharp
        builder.Services.AddSingleton<UninstallService>();
```

- [ ] **Step 3: `AppBootstrapper`에 연결**

필드 목록에 추가 (`_startupRegistrar` 아래):

```csharp
    private readonly UninstallService _uninstallService;
```

생성자 파라미터에 `, UninstallService uninstallService` 추가, 본문에 `_uninstallService = uninstallService;` 추가.

`InitializeTray()` 메서드의 `_trayIcon.ExitRequested += (_, _) => System.Windows.Application.Current.Shutdown();` 바로 아래에 추가:

```csharp
        _trayIcon.UninstallRequested += (_, _) =>
        {
            var confirm = new ConfirmUninstallWindow();
            if (confirm.ShowDialog() == true)
                _uninstallService.Uninstall();
        };
```

파일 상단 `using` 목록에 `using MapleWindow.App.Views;`가 이미 있는지 확인하고 없으면 추가한다 (기존에 `SetupWindow`, `SettingsWindow`를 `new`로 생성하고 있어 이미 있을 가능성이 높음 — 이미 있다면 이 스텝은 건너뛴다).

- [ ] **Step 4: 빌드 확인**

Run: `dotnet build MapleWindow.sln`
Expected: 빌드 성공

- [ ] **Step 5: 수동 실행 확인**

Run: `dotnet run --project src/MapleWindow.App`

확인 순서:
1. 트레이 아이콘 우클릭 → "프로그램 제거" 클릭 → 확인 팝업이 뜨는지, 기본 포커스가 [취소]에 있는지 확인
2. [취소] 클릭 → 팝업만 닫히고 앱은 계속 실행되는지 확인
3. (선택, 파괴적 동작이므로 테스트용 복사본에서만) 다시 "프로그램 제거" → [제거] 클릭 → 앱이 종료되고 `%AppData%\MapleWindow`, `%LocalAppData%\MapleWindow` 폴더와 레지스트리 `HKCU\...\Run\MapleWindow` 값이 삭제되었는지, 설치 폴더 자체도 잠시 후 삭제되는지 확인

- [ ] **Step 6: 커밋**

```bash
cd /c/MapleWindow
git add src/MapleWindow.App/Services/TrayIconManager.cs src/MapleWindow.App/ServiceCollectionExtensions.cs src/MapleWindow.App/AppBootstrapper.cs
git commit -m "feat: wire up tray uninstall menu item"
```

---

## Task 8: GitHub Actions 릴리즈 워크플로

**Files:**
- Create: `.github/workflows/release.yml`

**Interfaces:** 없음 (CI 설정 파일). 산출물 `MapleWindow-win-x64.zip`의 이름은 Task 2의 `ZipAssetName` 상수와 정확히 일치해야 한다.

이 태스크는 실제 GitHub 저장소에 push된 뒤에만 실행/검증 가능하므로, 로컬에서는 YAML 문법과 내용만 확인하고 실제 실행 검증은 문서 맨 끝 "배포 체크리스트"에서 진행한다.

- [ ] **Step 1: 워크플로 작성**

`.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - "v*.*.*"

permissions:
  contents: write

jobs:
  build-and-release:
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "9.0.x"

      - name: Publish
        run: dotnet publish src/MapleWindow.App/MapleWindow.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish

      - name: Zip artifact
        shell: pwsh
        run: Compress-Archive -Path publish/* -DestinationPath MapleWindow-win-x64.zip

      - name: Create release
        uses: softprops/action-gh-release@v2
        with:
          files: MapleWindow-win-x64.zip
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

- [ ] **Step 2: YAML 문법 확인 (로컬)**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/release.yml'))" ` (또는 `Get-Content .github/workflows/release.yml | ConvertFrom-Yaml`이 가능하면 그것을 사용; 둘 다 없으면 GitHub 웹 UI가 push 후 문법 오류를 알려주므로 이 스텝은 생략 가능)
Expected: 오류 없이 파싱됨

- [ ] **Step 3: 커밋**

```bash
cd /c/MapleWindow
git add .github/workflows/release.yml
git commit -m "ci: add GitHub Actions release workflow"
```

---

## Task 9: README.md

**Files:**
- Create: `README.md`

**Interfaces:** 없음 (문서).

- [ ] **Step 1: README 작성**

`README.md`:

```markdown
# MapleWindow

메이플스토리 캐릭터를 데스크톱에 오버레이로 띄우고, 일일/주간/월간 컨텐츠 현황을 알려주는 Windows 트레이 상주 프로그램입니다.

## 시작하기

### 1. Nexon Open API 키 발급

1. [Nexon Open API](https://openapi.nexon.com)에 접속해 계정을 등록합니다.
2. 애플리케이션을 등록하고 메이플스토리 API 사용 신청을 합니다. (승인까지 다소 시간이 걸릴 수 있습니다.)
3. 발급된 API 키를 복사해둡니다.

> ⚠️ **API 키는 개인 인증 정보입니다.** 스크린샷, 이슈, 커밋 등 어디에도 붙여넣거나 공유하지 마세요.

### 2. 설치 및 실행

1. [Releases](https://github.com/ivealwaysbeen/MapleWindow/releases) 페이지에서 최신 `MapleWindow-win-x64.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 해제합니다.
3. `MapleWindow.exe`를 실행합니다. (별도 설치 프로그램이나 .NET 런타임 설치가 필요 없습니다.)
4. 최초 실행 시 화면 안내에 따라 API 키를 입력하고, 표시할 캐릭터를 선택합니다.
5. Windows가 "알 수 없는 게시자입니다"라는 SmartScreen 경고를 띄우면 "추가 정보 → 실행"을 눌러 진행합니다.

최초 실행 이후에는 컴퓨터를 켤 때마다 자동으로 실행됩니다.

## 기능

- 캐릭터 오버레이 표시 (데스크톱 위에 캐릭터가 떠 있는 형태)
- 일일/주간/월간 컨텐츠 완료 여부 확인 및 말풍선 알림
- 트레이 아이콘을 통한 캐릭터 변경 및 세부 설정
- **자동 업데이트**: 앱을 재시작할 때마다 최신 버전을 자동으로 확인하고 적용합니다. 별도 조작이 필요 없습니다.

## 제거하기

트레이 아이콘 우클릭 → **프로그램 제거** → 확인 팝업에서 [제거]를 누르면, 설정·캐릭터 데이터·시작 프로그램 등록·설치 폴더가 모두 삭제됩니다.

## 보안

- 입력한 API 키는 외부로 전송되지 않고, 이 컴퓨터에만 암호화되어 저장됩니다 (`%AppData%\MapleWindow`).
- 자동 업데이트는 이 저장소의 공개 GitHub Release에서만 파일을 받아오며, 사용자의 API 키는 업데이트 과정에 전혀 관여하지 않습니다.
```

- [ ] **Step 2: 커밋**

```bash
cd /c/MapleWindow
git add README.md
git commit -m "docs: add README with setup, features, and security notes"
```

---

## Self-Review 결과

- **스펙 커버리지:** §1(자동 업데이트) → Task 2/3, §2(GitHub Actions) → Task 8, §3(프로그램 제거) → Task 4/5/6/7, §4(README) → Task 9, §5(보안 점검) → Task 1 + Task 9의 보안 문구 + Task 8의 시크릿 미사용. 모두 대응하는 태스크가 있음을 확인.
- **플레이스홀더 스캔:** 없음 — 모든 스텝에 실제 코드/명령어 포함.
- **타입 일관성:** `IUpdateChecker.GetLatestReleaseAsync` 시그니처가 Task 2 정의와 Task 3 소비 지점에서 동일, `GithubReleaseInfo.Version`/`ZipAssetUrl` 필드명이 Task 2/3에서 일관, `StartupRegistrar.Unregister()` 시그니처가 Task 4 정의와 Task 6 소비 지점에서 동일, `UninstallService.Uninstall()`이 Task 6 정의와 Task 7 소비 지점에서 동일.

---

## 배포 체크리스트 (모든 태스크 완료 후, 사용자 확인 하에 진행)

이 섹션은 자동화된 태스크가 아니라 사용자가 직접 확인하며 진행하는 절차입니다. `git push`와 GitHub 저장소 생성은 되돌리기 어렵고 외부(공개 저장소)에 영향을 미치므로, 실행 전 반드시 확인받습니다.

1. GitHub에 `ivealwaysbeen/MapleWindow` 저장소가 아직 없다면 새로 생성 (public)
2. 로컬에서 원격 연결: `git remote add origin https://github.com/ivealwaysbeen/MapleWindow.git`
3. 지금까지의 모든 로컬 커밋을 push: `git push -u origin master` (또는 `main`으로 브랜치명을 맞춘 뒤 push)
4. 최초 릴리즈 태그 생성 및 push: `git tag v1.0.0 && git push origin v1.0.0`
5. GitHub 저장소의 Actions 탭에서 `Release` 워크플로가 성공적으로 실행되는지 확인
6. Releases 탭에 `v1.0.0`과 `MapleWindow-win-x64.zip`이 올라왔는지 확인
7. 로컬에서 `dotnet run --project src/MapleWindow.App`으로 앱을 실행해 (버전을 임시로 `0.9.0`으로 낮춰서) 자동 업데이트가 실제로 동작하는지 최종 확인 후, `<Version>`을 다시 `1.0.0`으로 되돌림

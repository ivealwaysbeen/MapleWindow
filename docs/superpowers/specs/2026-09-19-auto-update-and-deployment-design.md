# 자동 업데이트 및 배포 설계

**날짜:** 2026-09-19
**대상 저장소:** `ivealwaysbeen/MapleWindow` (GitHub, public)

## 배경 및 목표

MapleWindow(.NET 9 WPF 트레이 앱)를 설치 프로그램 없이 zip/폴더 형태로 배포하고,
사용자가 별도 조작 없이 앱을 재시작할 때마다 최신 버전으로 자동 업데이트되도록 한다.
동시에 저장소를 공개(git push)하는 데 따른 보안(키 노출) 문제를 미리 차단하고,
저장소 첫 페이지(README)에서 신규 사용자가 API 키 발급부터 실행까지 따라할 수 있게 한다.
마지막으로, 설치 프로그램이 없는 만큼 사용자가 스스로 제거할 수 있는 수단(트레이 메뉴)을 제공한다.

## 범위

포함:
- GitHub Releases 기반 자동 업데이트 체크/적용 (Core + App)
- 태그 push 시 릴리즈 zip을 자동 생성하는 GitHub Actions 워크플로
- README.md (API 키 발급 안내, 사용법, 기능 요약, 보안 경고)
- `.gitignore` 보완 및 시크릿 노출 점검
- 트레이 메뉴 "프로그램 제거" 기능 (확인 팝업 포함)

제외 (YAGNI — 필요해지면 별도로 다룸):
- 코드 서명 인증서 / SmartScreen 경고 제거
- 베타/스테이블 등 업데이트 채널 구분, 롤백, 델타 패치
- 체크섬/서명 검증 (HTTPS로 GitHub에서 직접 받는 것으로 충분하다고 판단)
- 서드파티 자동업데이트 라이브러리 (Onova 등) — 직접 구현

## 1. 자동 업데이트

### 1.1 버전 관리

- `src/MapleWindow.App/MapleWindow.App.csproj`에 `<Version>1.0.0</Version>` 추가.
- 릴리즈마다 이 값을 수동으로 올리고, 동일한 값으로 git 태그(`v1.0.0`)를 생성한다.
- 현재 버전은 `Assembly.GetExecutingAssembly().GetName().Version`으로 읽는다.
- GitHub 릴리즈의 `tag_name`(`v1.2.3`)에서 앞의 `v`를 제거해 `System.Version`으로 파싱, 비교한다.

### 1.2 신규 컴포넌트 — `MapleWindow.Core/Updates/`

- `GithubReleaseInfo` (record): `Version`(파싱된 최신 버전), `ZipAssetUrl`(다운로드 URL)
- `IUpdateChecker` / `GithubReleaseUpdateChecker`:
  - `GET https://api.github.com/repos/ivealwaysbeen/MapleWindow/releases/latest`
  - 요청에 `User-Agent` 헤더 필수(GitHub API 요구사항), 타임아웃 5초
  - 응답 JSON에서 `tag_name`과, `assets[]` 중 이름이 정확히 `MapleWindow-win-x64.zip`인 자산의 `browser_download_url`을 찾는다
  - 자산을 찾지 못하거나 HTTP 오류/타임아웃/JSON 파싱 실패 시 `null` 반환 (예외를 던지지 않음 — 호출부가 "업데이트 없음"과 동일하게 처리)

### 1.3 신규 컴포넌트 — `MapleWindow.App/Services/AppUpdateService.cs`

`CheckAndApplyAsync()` 메서드 하나로 구성:

1. `IUpdateChecker`로 최신 릴리즈 조회. 실패/버전 동일/버전 낮음 → 즉시 리턴(정상 부팅 계속).
2. 새 버전이 있으면:
   - `_trayIcon.ShowBalloon("MapleWindow", "업데이트를 적용하고 재시작합니다...")`
   - zip을 `%TEMP%\MapleWindowUpdate\<version>\download.zip`로 다운로드
   - `System.IO.Compression.ZipFile.ExtractToDirectory`로 같은 폴더 내 `extracted/`에 압축 해제
   - 재실행 스크립트(`%TEMP%\MapleWindowUpdate\<version>\apply.cmd`) 생성 (내용은 1.4 참조)
   - `Process.Start`로 `cmd.exe /c apply.cmd`를 창 없이(`CreateNoWindow=true`, `UseShellExecute=false`) 분리 실행
   - `Log.CloseAndFlush()` 호출 후 `Environment.Exit(0)`
3. 이 메서드가 프로세스를 종료시키므로, 성공적으로 업데이트를 적용한 경우 `AppBootstrapper.RunAsync()`의 이후 로직(설정 로드 등)은 실행되지 않는다.

호출 위치: `AppBootstrapper.RunAsync()` 맨 첫 줄, `_configStore.Load()`보다 먼저.

### 1.4 재실행 스크립트 (`apply.cmd`)

인자: `<PID> <추출된폴더> <설치폴더> <exe경로>`

```
@echo off
:wait
tasklist /fi "PID eq %1" 2>nul | find "%1" >nul
if not errorlevel 1 (
  ping -n 2 127.0.0.1 >nul
  goto wait
)
robocopy "%~2" "%~3" /E /IS /IT /NFL /NDL
start "" /d "%~3" "%~4"
del "%~f0"
```

- `%~2`/`%~3`/`%~4`처럼 물결(`~`) 수정자를 반드시 사용한다 — 인자가 이미 따옴표로 감싸여 전달되므로 `"%2"`처럼 그대로 다시 따옴표를 씌우면 경로에 공백이 있을 때(`Program Files`, 공백 포함 사용자명, 한글 폴더명 등) 따옴표가 이중으로 겹쳐 `robocopy`/`start`가 인자를 잘못 파싱한다. `%1`(PID)은 따옴표 없이 그대로 두고 비교하므로 예외.
- `timeout` 명령은 콘솔이 없는(`CreateNoWindow=true`) 프로세스에서 "Input redirection is not supported" 오류로 실패할 수 있어, 콘솔 의존이 없는 `ping -n 2 127.0.0.1`로 대기한다.
- `robocopy /E`로 하위 폴더까지 전체 덮어쓰기 (기존 파일 유지하며 갱신)
- 재실행 후 스크립트 자기 자신을 삭제해 임시 파일이 남지 않게 함
- 이 스크립트는 업데이트 전용이며, 제거 기능(§3)의 스크립트와는 별도로 둔다 (공통 추상화로 묶지 않음 — 두 스크립트 모두 짧고 목적이 다름). 단, 종료 대기 부분(6줄)은 두 스크립트에서 동일하므로 한쪽을 고칠 때 반드시 다른 쪽도 함께 검토한다.
- 압축 해제된 `extractedDir`에는 zip이 담고 있는 최상위 `MapleWindow/` 폴더가 그대로 포함되므로(§2 참고), robocopy의 실제 원본 경로는 `extractedDir`가 아니라 `Path.Combine(extractedDir, "MapleWindow")`여야 한다.

### 1.5 실패 처리

- 네트워크 오류, GitHub API 오류, 다운로드 실패, 압축 해제 실패 등 어떤 단계에서든 예외가 발생하면 `Log.Warning`으로 기록하고 **정상 부팅을 계속 진행**한다 (사용자에게 업데이트 실패를 알리지 않음 — 다음 재시작 때 다시 시도되므로 침묵 처리로 충분).
- 업데이트 체크 자체(1.3의 1번 단계, GET 요청)에 걸리는 시간은 최대 5초(타임아웃)로 제한되어 있어 기존 부팅 흐름을 크게 지연시키지 않는다.

## 2. GitHub Actions 릴리즈 워크플로

파일: `.github/workflows/release.yml`

- 트리거: `v*.*.*` 형태의 태그 push
- 단계:
  1. `actions/checkout`
  2. `actions/setup-dotnet` (9.0.x)
  3. `dotnet publish src/MapleWindow.App/MapleWindow.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish`
  4. `publish/` 폴더의 이름을 `MapleWindow/`로 바꾼 뒤 그 폴더를 통째로 `MapleWindow-win-x64.zip`으로 압축한다 (즉, zip 최상위에 `MapleWindow/` 폴더가 그대로 들어가야 함 — `publish/*`처럼 내용물만 평탄하게 압축하면, 사용자가 이미 파일이 있는 공용 폴더에 압축을 풀었을 때 "프로그램 제거"가 그 폴더 전체를 삭제해버리는 사고로 이어진다). 에셋 이름은 §1.2의 `GithubReleaseUpdateChecker`가 찾는 이름과 정확히 일치해야 함
  5. `softprops/action-gh-release@v2`로 현재 태그에 대한 GitHub Release 생성 + zip 첨부, 기본 제공 `secrets.GITHUB_TOKEN` 사용
- 별도 시크릿 등록 불필요 (Nexon API 키는 사용자별 런타임 값이라 빌드에 관여하지 않음)
- 사용법: 로컬에서 `git tag v1.0.0 && git push origin v1.0.0` 실행하면 릴리즈가 자동 생성됨

## 3. 프로그램 제거 기능

### 3.1 UI

- `TrayIconManager`에 컨텍스트 메뉴 항목 "프로그램 제거" 추가 (종료 항목 근처), `UninstallRequested` 이벤트 노출
- 클릭 시 신규 확인창(`ConfirmUninstallWindow`, 기존 SetupWindow/SettingsWindow와 동일한 스타일의 작은 WPF 창) 표시
  - 버튼: **[취소]** / **[제거]**
  - 기본 포커스는 [취소] (Enter 오조작 방지), Esc로도 취소
  - [취소] → 창만 닫고 아무 동작 없음
  - 실제로 삭제될 설치 폴더 경로(`Path.GetDirectoryName(Environment.ProcessPath)`)를 안내 문구에 그대로 표시한다 — zip을 최상위 `MapleWindow/` 폴더 없이 압축했을 경우(§2) 사용자가 공용 폴더에 압축을 풀면 그 폴더 전체가 삭제될 수 있으므로, 사용자가 삭제 전 확인할 수 있게 한다.

### 3.2 [제거] 클릭 시 동작 (`UninstallService`, `MapleWindow.App/Services/`)

1. `Log.CloseAndFlush()` — 로그 파일 핸들 해제
2. 사용자 데이터 삭제: `%AppData%\MapleWindow`(설정, 암호화된 API 키), `%LocalAppData%\MapleWindow`(로그, 이미지 캐시) — `Directory.Delete(path, recursive: true)`, 폴더 없으면 무시
3. `StartupRegistrar.Unregister()` (신규 메서드) — `HKCU\...\Run`의 `MapleWindow` 값 삭제
4. 제거 스크립트(`%TEMP%\MapleWindowUninstall\uninstall.cmd`) 생성 — 인자: `<PID> <설치폴더>`

```
@echo off
:wait
tasklist /fi "PID eq %1" 2>nul | find "%1" >nul
if not errorlevel 1 (
  ping -n 2 127.0.0.1 >nul
  goto wait
)
rmdir /s /q "%~2"
del "%~f0"
```

5. `cmd.exe /c uninstall.cmd`를 창 없이 분리 실행
6. `Environment.Exit(0)`

## 4. README.md

저장소 루트에 작성, 구성:

1. 한 줄 소개
2. **시작하기**
   - [Nexon Open API](https://openapi.nexon.com)에서 계정 등록 → 애플리케이션 등록 → API 키 발급 (승인 대기 시간 있을 수 있음) 절차 안내
   - Releases 페이지에서 `MapleWindow-win-x64.zip` 다운로드 → 압축 해제 → `MapleWindow.exe` 실행
   - 첫 실행 시 발급받은 키 입력 → 캐릭터 선택
   - SmartScreen 경고가 뜰 경우 "추가 정보 → 실행"으로 진행한다는 안내
3. **기능 요약**: 캐릭터 오버레이, 주기적 상태 확인 및 일일/주간/월간 컨텐츠 알림, 말풍선 문구, 트레이 설정, 자동 업데이트, 프로그램 제거
4. **자동 업데이트 안내**: 재시작 시 자동으로 최신 버전을 확인·적용하며 별도 조작이 필요 없다는 설명
5. **제거 방법**: 트레이 메뉴 → 프로그램 제거
6. **보안 안내**: API 키는 로컬에 암호화 저장되며 외부로 전송되지 않는다는 점, 키를 캡처/공유/이슈에 붙여넣지 말라는 경고

## 5. 보안 점검 (이번 작업 범위)

- `.gitignore`에 `_run.log`, `*.log` 추가
- 현재 저장소 코드 전체를 검색해 하드코딩된 API 키/시크릿이 없음을 확인함 (완료 — 없음)
- Nexon API 키는 사용자가 런타임에 직접 입력해 로컬에만 암호화 저장되므로, 저장소나 CI 어디에도 개인 키가 들어갈 경로가 없음
- GitHub Actions 워크플로는 기본 제공 `GITHUB_TOKEN`만 사용, 추가 시크릿 등록 없음
- README에 API 키 노출 금지 경고 문구 포함

## 데이터 흐름 요약

```
앱 시작
  └─ AppUpdateService.CheckAndApplyAsync()
       ├─ 업데이트 없음/체크 실패 → 기존 부팅 흐름 계속 (설정 로드 → ... → 트레이 표시)
       └─ 업데이트 있음 → 다운로드/압축해제 → apply.cmd 분리 실행 → 프로세스 종료
                                                    (apply.cmd: 종료 대기 → robocopy 교체 → 재실행)

트레이 메뉴 "프로그램 제거" 클릭
  └─ 확인 팝업 [취소]/[제거]
       ├─ 취소 → 아무 동작 없음
       └─ 제거 → 로그 종료 → 사용자 데이터 삭제 → 시작 등록 해제
                → uninstall.cmd 분리 실행 → 프로세스 종료
                     (uninstall.cmd: 종료 대기 → 설치 폴더 삭제)
```

## 테스트 방침

- `GithubReleaseUpdateChecker`: HTTP 응답을 모킹해 버전 비교 로직(상위/동일/하위 버전, 자산 이름 불일치, JSON 파싱 실패, HTTP 오류) 단위 테스트
- `AppUpdateService`, `UninstallService`의 파일시스템/프로세스 종료 관련 로직은 자동 테스트 대상에서 제외 (수동 검증): 실제 GitHub 릴리즈를 만들어 낮은 버전의 빌드로 실행 → 자동 업데이트 동작 확인, 그리고 실제 "프로그램 제거" 클릭 후 레지스트리/폴더/데이터가 모두 삭제되는지 수동 확인
- GitHub Actions 워크플로는 테스트 태그(예: `v0.0.1-test`)로 실제 실행해 릴리즈 산출물 확인

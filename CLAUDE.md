# CLAUDE.md

## Running/testing the app locally — do not disturb the installed release

MapleWindow is a Windows tray/overlay app that a user normally has **installed and
running for real** (auto-starting at login). When you build and run a dev/debug
build on this machine to verify a change, two behaviors can silently affect that
installed release:

1. **Startup registration gets hijacked.** `AppBootstrapper.RunAsync()` calls
   `StartupRegistrar.EnsureRegistered()` unconditionally on *every* launch
   (`src/MapleWindow.App/AppBootstrapper.cs`). `StartupRegistrar`
   (`src/MapleWindow.App/Services/StartupRegistrar.cs`) writes
   `Environment.ProcessPath` into
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\MapleWindow` — a single
   value, so running a dev build overwrites the path that currently points at
   the installed release. After that, Windows will try to launch your temporary
   dev build at next login instead of the real app (and fail once you delete the
   build output).
   - **Before running a local build for testing**, read and save the current
     value of that registry key.
   - **After testing**, restore it to the installed release's exe path.

2. **Auto-update can overwrite the install directory.** `AppUpdateService`
   checks GitHub for a newer release on every launch and, if the *running*
   assembly's version is lower than the latest release, downloads it and
   `robocopy /MIR`s it over `Environment.ProcessPath`'s directory, then relaunches
   (`src/MapleWindow.App/Services/AppUpdateService.cs`). This is only safe to run
   locally when the dev build's `<Version>` in
   `src/MapleWindow.App/MapleWindow.App.csproj` is already higher than the
   latest published GitHub release — check this before running, don't assume
   it from a past session.

Both behaviors key off `Environment.ProcessPath` (the exe you actually launch),
not off any dev/debug flag — there's no environment-based guard in the code, so
this has to be handled procedurally every time, not fixed once.

Also note: `%APPDATA%\MapleWindow\config.json` (API key, character, world) is
shared with the installed release — a local dev build will load the user's real
configured character on launch, which is convenient for visual verification but
means it also talks to the real Nexon API under the user's real key.

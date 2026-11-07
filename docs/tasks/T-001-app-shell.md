# T-001: App shell

- **Phase:** 1 — Read-only scan
- **Branch:** `t-001-app-shell`
- **Depends on:** T-000 (merged)

## Goal

Turn the placeholder window into the real application shell: dependency injection, logging,
portable settings storage, light Fluent look, navigation between sections, single instance.
After this task every later feature only adds a page and services — no more plumbing.

## Context

- Read `CLAUDE.md`, `docs/ARCHITECTURE.md` and the decision table in `docs/LEAD_HANDOVER.md`.
- Relevant decisions: D-003 (`asInvoker`), D-006 (English only), D-007 (Windows 10 22H2+, x64),
  D-009 (CommunityToolkit.Mvvm), D-012 (settings/journals next to the exe with fallback),
  D-016 (Fluent, light only), D-017 (DI + logging), D-018 (left navigation), D-019 (single instance).
- Current code: `src/LeftoverScout.App` has `App.xaml` with `StartupUri` and an empty `MainWindow`.

## Scope

### 1. Packages (central versions in `Directory.Packages.props`)
- `CommunityToolkit.Mvvm` — latest stable 8.x.
- `Microsoft.Extensions.DependencyInjection` — latest stable 10.x.
- `Microsoft.Extensions.Logging` — latest stable 10.x (Core may reference only
  `Microsoft.Extensions.Logging.Abstractions`).
- Check each license on nuget.org (must be MIT) and add them to ARCHITECTURE.md → "Third-party packages".
- No other packages. In particular **do not** add the NuGet package named
  `PresentationFramework.Fluent` (a third-party port for .NET Framework) — the Fluent theme is
  built into WPF on .NET 10.

### 2. Composition root and startup
- Remove `StartupUri`. In `App.OnStartup` build the `ServiceProvider`, resolve and show `MainWindow`.
  Dispose the provider on exit.
- Register views/view models/services in one place (e.g. `App/Composition/ServiceRegistration.cs`).
- Global error handling: `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`,
  `TaskScheduler.UnobservedTaskException` → log the exception and show a short, friendly English
  message box (no stack trace in the dialog; it is in the log).
- Set `<Version>0.1.0</Version>` in `Directory.Build.props`.

### 3. Light Fluent theme (D-016)
- Merge the built-in dictionary in `App.xaml`:
  `pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.Light.xaml`
- **Do not** use `Application.ThemeMode` / `Window.ThemeMode`: they are marked
  `[Experimental("WPF0001")]` and warnings are errors in this repo.
- Verify the dictionary path in the WPF docs or the dotnet/wpf source before using it and
  mention the source in the PR.

### 4. Application manifest
- Add `app.manifest`: `requestedExecutionLevel level="asInvoker"`, `supportedOS` entry for
  Windows 10/11, per-monitor DPI awareness as recommended for WPF on .NET (check the docs).

### 5. Single instance (D-019)
- Only one instance per user session. Use a named `Mutex` in the `Local\` namespace whose name
  contains `LeftoverScout`.
- A second start must bring the existing window to the foreground (restore it if minimized) and
  then exit quietly. Use a named `EventWaitHandle` (or a similarly simple mechanism) to signal
  the first instance; the first instance activates its window on the UI thread.
- Put the mechanism in a small, testable class (e.g. `LeftoverScout.Windows/SingleInstance/…`).

### 6. Portable data folder (D-012)
- Core: an `AppDataLocationResolver` (name is up to you) that decides the data folder:
  1. `<exe folder>\data` if it exists or can be created **and** is writable;
  2. otherwise `%LOCALAPPDATA%\LeftoverScout`, and the result says that the fallback is used.
  The writability check goes behind an interface so the logic is unit-testable.
- Windows implementation of the writability check: create and remove a uniquely named probe file
  inside the candidate `data` folder. This is the app's own file, not user data, so removing it
  is allowed; do not touch anything else.
- When the fallback is used, the main window shows a non-blocking info bar:
  "Settings are stored in <path> because the program folder is not writable."
- About page shows the data folder path and an "Open data folder" button (Explorer).

### 7. Settings
- `AppSettings` (record) with `SchemaVersion` and the main window size/position/state.
- `ISettingsStore` in Core, JSON implementation with `System.Text.Json`:
  - file `settings.json` in the data folder;
  - missing or corrupt file → defaults + a logged warning (never crash);
  - save atomically (write a temp file in the same folder, then replace).
- Restore the window bounds on start (only if they are visible on a current screen; otherwise
  center on the primary screen). Save on close.

### 8. Logging (D-017)
- `Microsoft.Extensions.Logging` with a small own file logger provider:
  - file `logs\leftoverscout-yyyyMMdd.log` in the data folder, UTF-8, one line per entry
    (timestamp, level, category, message, exception);
  - keep the 7 newest log files; delete older **log files of this app only** (matching the
    file name pattern) at startup;
  - logging must never throw into the app (swallow I/O errors).
- Log start/exit, version, OS version, data folder, and every unhandled exception.
- Do not log anything that is not needed for diagnostics.

### 9. Main window with left navigation (D-018)
- Left: navigation list with **Scan, Results, Cleanup, History, About**. Right: content area
  (`ContentControl` bound to the current page view model, views chosen by `DataTemplate`).
  Bottom: status bar with a short status text.
- Scan, Results, Cleanup, History: placeholder pages ("Available in Phase N" with one sentence
  about what the page will do).
- About: app name, version (informational version), "MIT License", repository link
  `https://github.com/gromichek/leftover-scout`, data folder path + "Open data folder".
- MVVM with CommunityToolkit.Mvvm (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
  No logic in code-behind.
- Window title: `LeftoverScout`. Minimum size so the layout never breaks (e.g. 900×600).

### 10. Tests
- Data folder resolution: writable exe folder, not writable → fallback, folder creation failure.
- Settings store (temp folders): round trip, missing file, corrupt file, atomic save leaves no temp file.
- Log retention: keeps the 7 newest matching files, ignores files with other names.
- Navigation view model: initial page, switching pages.
- Window bounds validation (off-screen bounds → centered) if implemented as pure logic.

### 11. Documentation
- `docs/user-guide/`: add "Getting started" (how to start, single instance, where settings and
  logs are stored, what the info bar means). Link it from `index.md`.
- `docs/ARCHITECTURE.md`: packages table; update "Settings and journals" and describe the
  composition root, logging and single instance briefly (no longer "planned").

## Out of scope

- Any scanning, evidence or deletion code.
- Settings UI, dark theme, theme switching, localization.
- Publishing as a single-file exe (T-015).

## Acceptance criteria

- [ ] App starts with the light Fluent look and the left navigation; all five pages open.
- [ ] Second start brings the first window to the front; no second window appears.
- [ ] `data\settings.json` and `data\logs\…log` appear next to the exe after the first run.
- [ ] Window size/position is restored after restart.
- [ ] Corrupt `settings.json` does not crash the app (defaults + warning in the log).
- [ ] No `ThemeMode` usage, no suppressed warnings without a comment.
- [ ] Build and tests pass (`dotnet build` / `dotnet test`, Release); CI green.
- [ ] New logic covered by tests; tests use temp folders only.
- [ ] User guide and ARCHITECTURE.md updated; new packages listed with license.

## Manual test steps for the owner

1. `git pull`, then `dotnet run --project src/LeftoverScout.App` (or F5 in Visual Studio).
2. The window is light and modern; click Scan, Results, Cleanup, History, About — each opens its page.
3. About shows version 0.1.0 and the data folder; "Open data folder" opens Explorer there.
4. In that folder: `settings.json` and `logs\leftoverscout-<date>.log` exist.
5. Start the app a second time (while the first is open, minimize it first) — the first window
   comes back to the front, no second window.
6. Resize/move the window, close, start again — size and position are kept.
7. Close the app, write `garbage` into `settings.json`, start again — the app starts normally.

## Notes for the executor

- WPF and Win32 behaviors (foreground activation rules, DPI manifest settings, Fluent dictionary
  path) must be checked in Microsoft docs or dotnet/wpf source — cite them in the PR.
- `SetForegroundWindow` restrictions: the first instance activating itself after a signal may be
  blocked by Windows foreground rules; `Activate()` + restoring `WindowState` is usually enough
  for WPF — verify and describe what you rely on.
- Keep the shell small and boring; later tasks add the real pages.
- If something in this task conflicts with CLAUDE.md or ARCHITECTURE.md, ask in "Open questions".

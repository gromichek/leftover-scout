# Architecture

Status: updated in T-001. Sections marked *(planned)* describe agreed design that has
no code yet. Architectural changes need the owner's decision (see `docs/LEAD_HANDOVER.md`).

## Goal

Help the user find data left behind in `%USERPROFILE%` by programs that are no longer
installed (or caches that are safe to remove), explain **why** each item is suspected, and
remove the chosen items safely.

## Platform

| Item | Choice | Decision |
|---|---|---|
| Runtime | .NET 10 (LTS, supported until Nov 2028) | D-001 |
| UI | WPF, built-in Fluent theme (light only), left navigation, single instance, MVVM with CommunityToolkit.Mvvm | D-009, D-011, D-016, D-018, D-019 |
| Infrastructure | Microsoft.Extensions.DependencyInjection + Logging, own file logger | D-017 |
| OS | Windows 10 22H2 (build 19045) and later, Windows 11; x64 | D-007 |
| Privileges | Runs as the current user (`asInvoker`), no elevation in Phase 1 | D-003 |
| Distribution | Portable self-contained single-file exe | D-007 |
| Network | None. Fully offline, no telemetry | D-005 |
| Language | English only (UI, help, repository) | D-006 |

## Solution layout

```
LeftoverScout.slnx
src/
  LeftoverScout.Core/       net10.0          Domain models, rules engine, classification, interfaces.
                                             No Windows-only APIs → fully unit-testable.
  LeftoverScout.Windows/    net10.0-windows  Implementations of Core interfaces: file system scan,
                                             registry/Appx evidence, Recycle Bin deletion.
  LeftoverScout.App/        net10.0-windows  WPF views and view models; composition root (DI).
tests/
  LeftoverScout.Core.Tests/
  LeftoverScout.Windows.Tests/
  LeftoverScout.App.Tests/      View model tests (added in T-001).
rules/                      (planned) JSON knowledge base shipped with the app.
docs/                       Project documents, tasks, user guide.
```

Dependency direction: `App → Windows → Core`, `App → Core`. Core depends on nothing.

## Key concepts

### ScanTarget
Which profile is scanned: profile root path + whether it is the current user. All scanning
and evidence code takes a `ScanTarget` and never reads the current user's paths directly.
Phase 1 only creates `ScanTarget.ForCurrentUser()`. Administrator mode (Phase 5) will add
other profiles without rewriting the scanner (D-003).

### Scan
A read-only walk over the target profile that builds a **folder tree** with aggregated values.
Individual files are not kept, except **large files** (>= 100 MB, `ScanOptions.LargeFileThresholdBytes`) (D-021).

Pieces:
- `IDirectoryReader` (Core) lists one folder: name, is-directory, logical length, last write (UTC), raw
  attributes, reparse tag. Errors come back as data (`DirectoryListing.ErrorReason`). The Windows
  implementation `Win32DirectoryReader` uses `FindFirstFileExW` (`FindExInfoBasic`, large fetch) and the
  `\\?\` prefix for long paths. It only enumerates; it never opens files.
- `EntryClassifier` (Core, pure) holds **all** link and cloud rules (below).
- `ProfileScanner` (Core) walks the tree with an explicit stack (no recursion), checks the cancellation
  token before every folder (a cancelled scan returns `ScanCompletion.Cancelled` without totals), throttles
  progress reports (default 100 ms) and aggregates totals bottom-up. Children are sorted by name
  (ordinal, ignore case), so results are deterministic. The walk is single-threaded.
- `PersonalFolderMatcher` (Core) marks the personal folders (D-020). `PersonalFolderProvider` (Windows)
  supplies them: `SHGetKnownFolderPath` for Documents, Desktop, Pictures, Videos, Music, Downloads plus the
  environment variables `OneDrive`, `OneDriveConsumer`, `OneDriveCommercial`. Folders outside the profile
  are ignored.
- `ScanService` (Core, singleton, `IScanService`) runs the scanner, logs start and end (totals and
  duration, never paths) and keeps `LastResult` (last *completed* scan) for the Results page.
- `ScanPageViewModel` (App) binds the Scan page; `ScanSummary` (Core) computes the numbers it shows.

Rules (task T-002 section 2; constants from Microsoft Learn, "File Attribute Constants" and
"Reparse Point Tags"):
1. Name-surrogate reparse point (tag & `0x20000000`, e.g. junction, symlink): not followed, not counted;
   recorded as skipped link.
2. Cloud placeholder tags (`(tag & 0xFFFF0FFF) == 0x9000001A`) and other non-surrogate tags: treated as a
   normal file or folder, using metadata only.
3. File with `RECALL_ON_DATA_ACCESS` (`0x00400000`) or `OFFLINE` (`0x1000`): cloud-only. Its length goes
   to the cloud-only total, not to the local size.
4. Folder with `RECALL_ON_OPEN` (`0x00040000`): not enumerated (that would make OneDrive download the
   listing); recorded as cloud folder not scanned.
5. Other non-surrogate reparse files (for example `APPEXECLINK`): counted with their reported length,
   never followed.

Known limitations: size is the **logical** size (D-022), no size on disk; **hard links are counted once
per link**, so files with several links are over-counted; folders that cannot be read are recorded and
skipped, so totals may be slightly low.

The monitor-aware window placement check uses `MonitorWorkAreas` (Windows, `EnumDisplayMonitors`) and the
pure `WindowBoundsValidator` (Core).

### Evidence (planned)
Facts collected read-only from the system that help decide whether an item is still used:
- installed programs: `Uninstall` keys in HKCU and HKLM (both 32- and 64-bit views);
- installed Appx/MSIX packages of the current user;
- Start menu shortcuts and other hints.
The registry is **only read** — LeftoverScout never cleans the registry (D-002).

### Rules (planned)
An offline JSON knowledge base in `rules/` (D-005), validated at load time:
- *ownership rules*: "folder pattern X belongs to program Y (publisher, uninstall key hints)";
- *known caches*: locations that are safe to clear;
- *protected locations*: never offered for deletion.
Rules are data, so the community can extend them via PRs.

### Classification (planned)
Combines items + evidence + rules into **findings**. Each finding has:
- a category (e.g. orphaned program data, cache, temp, unknown);
- a confidence level (High / Medium / Low / Unknown);
- a human-readable explanation of the evidence ("why we think so").

### Cleanup (planned)
A single cleanup service in `LeftoverScout.Windows` is the **only** code allowed to delete:
1. re-checks every selected item against the protected list right before deletion;
2. deletes to the **Recycle Bin** by default (Windows Shell API);
3. permanent deletion only via an explicit option with double confirmation (D-004);
4. reports per-item results (deleted / skipped / failed and why);
5. writes a session journal (what, where from, size, result).

### Settings and journals
Stored in a `data\` folder next to the exe (portable — deleting the app folder leaves no
traces). If that folder is not writable, fall back to `%LOCALAPPDATA%\LeftoverScout` and tell
the user with an info bar (D-012).
- `AppDataLocationResolver` (Core) picks the folder; the writability check is behind
  `IDataFolderProbe`, implemented in `LeftoverScout.Windows` by creating and removing a uniquely
  named probe file.
- `settings.json`: `AppSettings` record, read/written by `JsonSettingsStore` (System.Text.Json).
  A missing or corrupt file gives defaults and a logged warning; saves go to a temp file in the
  same folder and replace the target.
- Journals (Phase 3) will live in the same folder.

### Composition root, logging, single instance
- `App.OnStartup` (in `LeftoverScout.App`) is the composition root: single-instance check,
  data folder resolution, log retention, then `ServiceRegistration` builds the
  `Microsoft.Extensions.DependencyInjection` provider and the main window is resolved from it
  (D-017). Views, view models and services are registered only in
  `Composition/ServiceRegistration.cs`.
- Logging: `Microsoft.Extensions.Logging` with `FileLoggerProvider` (Core) writing
  `data\logs\leftoverscout-yyyyMMdd.log`, one line per entry, never throwing. At startup
  `LogRetention` keeps the 7 newest log files of the app and ignores other files.
- Global error handling logs unhandled exceptions (UI thread, app domain, unobserved tasks) and
  shows a short message without a stack trace.
- Single instance (D-019): `SingleInstanceGuard` (Windows) owns a `Local\` named mutex; a second
  start signals a named event and exits; the first instance restores and activates its window.
- Navigation: `MainViewModel` holds the `PageViewModel` list; views are chosen by `DataTemplate`.
  The main window's bounds are restored by `WindowPlacementService`, validated by the pure
  `WindowBoundsValidator` (Core).

## Safety model (summary)

See `CLAUDE.md` → "Safety rules". In short: scanning is read-only, one service deletes,
Recycle Bin by default, protected list wins, no reparse points, no cloud hydration, tests never
touch a real profile.

## Third-party packages

| Package | License | Used in | Why |
|---|---|---|---|
| xunit.v3 | Apache-2.0 | tests | Test framework (D-010) |
| xunit.runner.visualstudio | Apache-2.0 | tests | Test adapter for `dotnet test` / Visual Studio |
| Microsoft.NET.Test.Sdk | MIT | tests | Test host |
| CommunityToolkit.Mvvm | MIT | App | MVVM helpers: `ObservableObject`, source-generated properties and commands (D-009) |
| Microsoft.Extensions.DependencyInjection | MIT | App | Dependency injection container (D-017) |
| Microsoft.Extensions.Logging | MIT | App | Logging pipeline (D-017) |
| Microsoft.Extensions.Logging.Abstractions | MIT | Core | `ILogger` abstraction, so Core can log without a Windows dependency |

## Build and CI

- SDK pinned in `global.json` (10.0.100, roll forward to latest feature band).
- Shared settings in `Directory.Build.props` (nullable, warnings as errors, code style in build).
- Package versions in `Directory.Packages.props` (central package management).
- GitHub Actions `.github/workflows/ci.yml`: `windows-latest`, restore → build (Release) → test.
  Job name `build-and-test` is the required status check for `main`.

# Architecture

Status: initial version (T-000). Sections marked *(planned)* describe agreed design that has
no code yet. Architectural changes need the owner's decision (see `docs/LEAD_HANDOVER.md`).

## Goal

Help the user find data left behind in `%USERPROFILE%` by programs that are no longer
installed (or caches that are safe to remove), explain **why** each item is suspected, and
remove the chosen items safely.

## Platform

| Item | Choice | Decision |
|---|---|---|
| Runtime | .NET 10 (LTS, supported until Nov 2028) | D-001 |
| UI | WPF, built-in Fluent theme, MVVM with CommunityToolkit.Mvvm | D-009, D-011 |
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

### Scan (planned)
A read-only walk over the target profile producing **items** (folders/files) with size,
dates and attributes.
- Never follows reparse points (junctions, symlinks).
- Never hydrates cloud placeholder files (OneDrive): sizes and dates come from metadata only.
- Tolerates access-denied and locked entries (recorded, not fatal).
- Async, cancellable, reports progress.

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

### Settings and journals (planned)
Stored in a `data\` folder next to the exe (portable — deleting the app folder leaves no
traces). If that folder is not writable, fall back to `%LOCALAPPDATA%\LeftoverScout` and tell
the user (D-012).

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

## Build and CI

- SDK pinned in `global.json` (10.0.100, roll forward to latest feature band).
- Shared settings in `Directory.Build.props` (nullable, warnings as errors, code style in build).
- Package versions in `Directory.Packages.props` (central package management).
- GitHub Actions `.github/workflows/ci.yml`: `windows-latest`, restore → build (Release) → test.
  Job name `build-and-test` is the required status check for `main`.

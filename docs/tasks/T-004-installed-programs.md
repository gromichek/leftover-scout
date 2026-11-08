# T-004: Evidence — installed programs and Programs page

- **Phase:** 1 — Read-only scan
- **Branch:** `t-004-installed-programs` (or the branch your session assigned)
- **Depends on:** T-003 (merged)

## Goal

Collect, read-only, what is installed on the computer for the current user, so that later
classification (T-006) can tell whether a profile folder still belongs to an installed program.
Show the collected list on a new **Programs** page so the owner can verify it.

## Context

- Read `CLAUDE.md` (safety, help rules), `docs/ARCHITECTURE.md` ("Evidence (planned)"),
  decisions D-002 (registry read-only, never cleaned), D-003 (current user, no admin),
  D-035 … D-039 (this task) in `docs/LEAD_HANDOVER.md`.
- `ScanService.RunAsync` runs the profile scan; `IDirectoryReader` lists folders safely
  (no reparse points, no cloud hydration) — reuse it for folder-based sources.

## Owner decisions for this task

- **D-035** Four evidence sources:
  1. **Uninstall registry**: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` in both
     the 64-bit and 32-bit registry views, and the same key under `HKCU` (both views);
  2. **Store / MSIX packages** of the current user;
  3. **Program folders**: top-level folder names in `%ProgramFiles%`, `%ProgramFiles(x86)%`,
     `%LOCALAPPDATA%\Programs` (plus the names of their direct subfolders, kept for matching, not shown);
  4. **Start menu**: names of shortcuts (`.lnk`, `.url`, without extension) and folders under the
     user and all-users `Start Menu\Programs`. **Shortcut files are never opened.**
- **D-036** Store packages via the official WinRT API `Windows.Management.Deployment.PackageManager`
  (`FindPackagesForUser(string.Empty)` = current user, no admin). `LeftoverScout.Windows`,
  `LeftoverScout.App` and the two Windows test projects target **`net10.0-windows10.0.19041.0`**.
- **D-037** New **Programs** page between Results and Cleanup.
- **D-038** Evidence is collected automatically **as part of every scan** (it is fast); a failing
  source never fails the scan.
- **D-039** After a **cancelled** scan, Results (and Programs) keep showing the previous completed result.

## Scope

### 1. Target framework (D-036)
- Change the four projects to `net10.0-windows10.0.19041.0`. Check that `SupportedOSPlatformVersion`
  stays compatible with the minimum build 19045 and that no new analyzer warnings appear.
- No extra NuGet package is needed (the SDK adds the Windows SDK projection). Record in
  ARCHITECTURE.md that this adds the Windows SDK projection assembly to the output (note its size).

### 2. Core model and collector
- `InstalledProgram` (record): `Name`, `Publisher?`, `Version?`, `InstallLocation?`,
  `InstallDate?` (date only), `Source` (enum: `Registry`, `Store`, `ProgramFolder`, `StartMenu`),
  `SourceDetail` (registry key path incl. hive and view / package family name / folder path /
  shortcut path relative to the Start menu root), flags `IsSystemComponent`, `IsFramework`,
  and for program folders `SubfolderNames`.
- `IInstalledProgramSource` (one per source) and an `EvidenceCollector` in Core that runs all
  sources (in parallel is fine), honours cancellation, catches failures per source and returns an
  `EvidenceSnapshot`: programs, per-source counts, per-source errors (source + message, no paths
  with user names in logs), collected-at time.
- Registry parsing rules (pure, tested with a fake registry reader interface):
  - skip subkeys without a non-empty `DisplayName`;
  - skip updates/patches: `ParentKeyName` set, or `ReleaseType` in {`Update`, `Hotfix`,
    `Security Update`};
  - `SystemComponent = 1` → `IsSystemComponent`;
  - `InstallDate` in `yyyyMMdd` → date; anything else → null;
  - `InstallLocation` empty → try the folder of `DisplayIcon` (strip `,index` and quotes);
  - the same program found in both views (same key name and DisplayName) is listed once.
- Never write to the registry. Open keys read-only (`RegistryKey.OpenBaseKey(..., view)` +
  `OpenSubKey(name, writable: false)`).

### 3. Windows sources
- Registry: `Microsoft.Win32.Registry` behind a small reader interface (for tests).
- Store: `PackageManager.FindPackagesForUser(string.Empty)`; per package take `Id.Name`,
  `Id.FamilyName`, `DisplayName`, `PublisherDisplayName`, `Id.Version`, `InstalledLocation.Path`,
  `IsFramework`, `SignatureKind == System` → `IsSystemComponent`. Some properties throw for some
  packages — catch per property/package and keep the rest. Cite the API docs in the PR.
- Program folders and Start menu: use `IDirectoryReader` (no reparse points, nothing opened).
  Start menu: recurse folders, collect shortcut names; folders whose name equals a shortcut
  name are not listed twice.

### 4. Integration with the scan (D-038, D-039)
- `ScanService.RunAsync` runs the evidence collector together with the profile scan (same
  cancellation token) and stores the `EvidenceSnapshot` with the completed result
  (e.g. `ScanResult.Evidence`). Progress shows "Collecting installed programs…" while it runs
  if it takes noticeable time.
- A cancelled scan keeps the previous completed result **and** its evidence (D-039 — this is
  the current Results behavior; keep it and keep the help accurate).
- Log: per-source counts and duration; errors per source without paths.

### 5. Programs page (D-037)
- Navigation item **Programs** between Results and Cleanup; help topic `programs`; **?** / **F1** work.
- No result yet: message + **Go to Scan** (same pattern as Results).
- With a result:
  - header "Collected <date/time> · <n> programs";
  - **Search** box (filters Name and Publisher, case-insensitive, as you type);
  - **Source** filter: All / Registry / Store / Program folder / Start menu;
  - checkbox **Show system components and frameworks** (default off);
  - `DataGrid`: **Name, Publisher, Version, Source, Install location, Installed** (date);
    sortable by value; default sort by Name;
  - **Open install location** button + context menu (only when the folder exists;
    use `IShellLauncher.OpenFolder`) and **Copy name**;
  - status under the table: "<shown> of <total> programs".
- Per-source errors: one short line under the header (e.g. "Store apps could not be read.").

### 6. Collected finding (PROGRESS #5)
- Results: the default sort by Size shows the sort arrow on the **Size** header from the start
  (set `SortDirection` / `SortDescriptions` so the grid and the arrow agree).

### 7. Help (mandatory, EN + RU — D-026)
- New topic `programs.md` (EN + RU): what the page shows, each source in plain words, filters,
  search, buttons, why a program may appear in several sources, tips; that nothing is changed
  and the registry is only read.
- `index.md` (EN + RU): add Programs to the topic list and to the order of work
  (Scan → Results / Programs → …). `scan.md` (EN + RU): the scan also collects installed programs.
  `results.md` (EN + RU): sort arrow behavior if it is mentioned.

### 8. Tests
- Registry parsing rules (every rule above) with a fake reader.
- Store mapping with a fake package source, including a package whose properties throw.
- Program folders and Start menu with `FakeDirectoryReader` (nested Start menu folders, `.lnk`
  and `.url`, duplicate folder/shortcut names, reparse points ignored).
- Collector: parallel sources, one failing source, cancellation.
- Scan service: evidence stored with a completed scan; cancelled scan keeps previous result + evidence.
- Programs view model: search, source filter, system/framework toggle, counts, empty states.
- Real read-only smoke tests on CI: the HKLM Uninstall key returns at least one program;
  `PackageManager` call returns without throwing (do not assert a count).
- Results: default sort arrow state.

## Out of scope

- Matching programs to profile folders, classification, rules (T-005 … T-007).
- Running processes, services, scheduled tasks; reading shortcut targets; other users.

## Acceptance criteria

- [ ] Projects target `net10.0-windows10.0.19041.0`; build has no warnings.
- [ ] Every scan also collects programs from all four sources; a failing source does not fail the scan.
- [ ] Nothing is written anywhere (registry opened read-only; shortcuts never opened).
- [ ] Programs page with search, source filter, system/framework toggle, sorting, Open install location, Copy name.
- [ ] Cancelled scan keeps the previous result and evidence on Results and Programs.
- [ ] Results shows the Size sort arrow from the start.
- [ ] Help `programs`, `index`, `scan` (and `results` if needed) updated in EN + RU and match the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. Start the app → **Programs**: message + **Go to Scan**.
2. Run a scan → **Programs**: a list with your programs; the count looks plausible.
3. Type part of a program name you know in **Search** → it appears. Clear the search.
4. **Source** = Store → Store apps (e.g. Calculator, Photos may be hidden until you tick
   **Show system components and frameworks**).
5. **Source** = Program folder → folder names from `Program Files`.
6. Select a program with an install location → **Open install location** opens Explorer.
7. Compare a few entries with Windows Settings → Apps → Installed apps.
8. Start a new scan and cancel it → Programs and Results still show the previous result.
9. **Results**: the Size column shows the sort arrow right away.
10. Press **?** on Programs — read the help in both languages.

## Notes for the executor

- Read: `RegistryKey.OpenBaseKey` / `RegistryView` docs; the Uninstall registry values
  (`DisplayName`, `SystemComponent`, `ParentKeyName`, `ReleaseType`, `InstallLocation`,
  `DisplayIcon`, `InstallDate`); `PackageManager.FindPackagesForUser` and `Package` properties;
  .NET docs on Windows-specific TFMs (`net10.0-windows10.0.19041.0`, `SupportedOSPlatformVersion`).
  Cite them in the PR.
- Remember the WPF `System.IO` implicit-using pitfall and that every `IScanService` test fake must
  implement new members (CLAUDE.md).
- Keep logs free of personal paths and program lists — counts only.

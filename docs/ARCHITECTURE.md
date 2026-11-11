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
| Language | English only (UI, repository); the user help is also in Russian | D-006, D-024, D-029 |

## Solution layout

```
LeftoverScout.slnx
src/
  LeftoverScout.Core/       net10.0          Domain models, rules engine, classification, interfaces.
                                             No Windows-only APIs → fully unit-testable.
  LeftoverScout.Windows/    net10.0-windows10.0.19041.0  Implementations of Core interfaces: file system scan,
                                             registry/Appx evidence, Recycle Bin deletion.
  LeftoverScout.App/        net10.0-windows10.0.19041.0  WPF views and view models; composition root (DI).
tests/
  LeftoverScout.Core.Tests/
  LeftoverScout.Windows.Tests/
  LeftoverScout.App.Tests/      View model tests (added in T-001).
rules/                      JSON knowledge base, embedded into Core (T-005). See docs/RULES.md.
docs/                       Project documents, tasks, user guide.
```

The Windows, App and both Windows test projects target `net10.0-windows10.0.19041.0` (D-036). The version
suffix makes the SDK add the Windows SDK projection (`Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll`),
which is needed for the WinRT `PackageManager`. It adds those assemblies to the output (their size was not measured
in the environment that wrote T-004; check the published exe size on Windows). The default
`SupportedOSPlatformVersion` becomes 10.0.19041.0, which is below the minimum build 19045 (D-007), so nothing
changes for supported systems. Core stays `net10.0`.

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
- `PersonalFolderMatcher` (Core) marks the personal folders (D-020, D-049). `PersonalFolderProvider` (Windows)
  supplies them: `SHGetKnownFolderPath` for Documents, Desktop, Pictures, Videos, Music, Downloads plus the
  environment variables `OneDrive`, `OneDriveConsumer`, `OneDriveCommercial`. Folders outside the profile
  are ignored (D-048). In addition, a direct child of the profile root is personal **by name** (case-insensitive):
  `Documents`, `Desktop`, `Downloads`, `Pictures`, `Videos`, `Music`, `Favorites`, `Contacts`, `Links`,
  `Saved Games`, `Searches`, `3D Objects` or a name starting with `OneDrive`. This keeps `{Profile}\Documents`
  protected when Windows redirects the real Documents folder to another drive.
- `ScanService` (Core, singleton, `IScanService`) runs the scanner, logs start and end (totals and
  duration, never paths) and keeps `LastResult` (last *completed* scan) for the Results page.
- `ScanPageViewModel` (App) binds the Scan page; `ScanSummary` (Core) computes the numbers it shows.
- `IScanService.LastResultChanged` is raised after every scan that ends (possibly on a background thread);
  a completed result carries `CompletedAt`. A cancelled scan keeps the previous `LastResult`.

### Results page (T-003)
Read-only browsing of the last completed scan (D-031 … D-034). Logic without WPF types lives in
`LeftoverScout.Core.Results`: `FolderRowBuilder` (rows of one folder, percent of parent, the
"(files in this folder)" row, openability), `FolderNavigator` (current folder, breadcrumb, Up),
`FolderTagText` (Notes tags and tooltips), `LargeFileRowBuilder` (relative paths). `ResultsPageViewModel`
(App) holds the state, refreshes on `LastResultChanged` through the UI synchronization context, and exposes
the commands; `FolderRowViewModel` / `LargeFileRowViewModel` expose raw values for `DataGrid` sorting
(`SortMemberPath`) next to formatted texts. `ResultsView` only binds and forwards Enter, Ctrl+C and
double-click to commands. Actions go through `IShellLauncher` (`OpenFolder`, `SelectFile` →
`explorer.exe /select,"<path>"`) and `IClipboardService` (`WpfClipboardService`, `Clipboard.SetDataObject`
with retries). None of them writes or deletes anything.

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

The monitor-aware window placement check uses `MonitorWorkAreas` (Windows, `UI` folder, `EnumDisplayMonitors`) and the
pure `WindowBoundsValidator` (Core).

### Evidence (T-004)
Facts collected read-only from the system that help decide whether an item is still used. Code lives in
`LeftoverScout.Core.Evidence`; only the thin Windows adapters are in `LeftoverScout.Windows.Evidence`.
The registry is **only read** — LeftoverScout never cleans the registry (D-002).

- `InstalledProgram` (record: name, publisher, version, install location, install date, `Source`, `SourceDetail`,
  `IsSystemComponent`, `IsFramework`, `SubfolderNames`) and the `IInstalledProgramSource` interface, one
  implementation per source (D-035):
  1. `RegistryProgramSource` reads the `Uninstall` keys of HKLM and HKCU in the 64-bit and 32-bit view through
     `IUninstallRegistry` (Windows: `WindowsUninstallRegistry`, keys opened with `writable: false`).
     `UninstallEntryParser` (pure) applies the rules: skip entries without `DisplayName`; skip updates
     (`ParentKeyName` set, `ReleaseType` Update / Hotfix / Security Update); `SystemComponent = 1` marks a system
     component; `InstallDate` only as `yyyyMMdd`; empty `InstallLocation` falls back to the folder of `DisplayIcon`;
     the same key name and display name in both views of one hive is listed once.
  2. `StoreProgramSource` maps `IStorePackage` objects from `IStorePackageProvider` (Windows:
     `WinRtStorePackageProvider`, `PackageManager.FindPackagesForUserWithPackageTypes(string.Empty, Main | Framework | Resource | Bundle | Optional)`
     = current user, no admin, D-036; results de-duplicated by package full name in `StorePackageMerger`).
     Each property is read separately; one that throws is left empty. **Docs check (T-006b):** the Microsoft Learn pages for
     `PackageManager.FindPackagesForUser`, `FindPackagesForUserWithPackageTypes` and `PackageTypes` could not be opened in the
     environment that wrote T-006b (network blocked). The `PackageTypes` members used here are from the API as known, not
     verified against the docs, and **it is not known** whether packages with an external location (sparse packages) are returned.
     That is why the classifier does not rely on Store evidence alone: see the vendor cross-check below.
     **Lead docs check (2026-11-09):** `FindPackagesForUserWithPackageTypes` returns the packages of the given types
     installed for the user (Microsoft Learn, "PackageManager.FindPackagesForUserWithPackageTypes"). Packages with an
     external location ("sparse" identity packages that desktop programs use e.g. for File Explorer context menus) are
     registered **per user by the program's own installer** (`Add-AppxPackage -ExternalLocation`), and the program — not
     Windows — is responsible for registering and removing them (Microsoft Learn, "Grant package identity by packaging
     with external location"; Windows Developer Blog, "Identity, Registration and Activation of Non-packaged Win32 Apps",
     2019). So a folder in `Packages` can exist for an installed desktop program while no package is registered for the
     user — exactly finding #10. The vendor cross-check (D-050) covers this case.
  3. `ProgramFolderSource` lists the top-level folders of Program Files, Program Files (x86) and
     `%LOCALAPPDATA%\Programs` (plus names of the direct subfolders for later matching).
  4. `StartMenuSource` lists shortcut names (`.lnk`, `.url`, without extension) and folders under the user and
     all-users `Start Menu\Programs`. Shortcut files are never opened.
  The two folder sources use `IDirectoryReader` and `EntryClassifier`, so reparse points and cloud folders are not
  entered. The folders come from `IEvidenceLocations` (Windows: `WindowsEvidenceLocations`).
- `EvidenceCollector` runs all sources in parallel, catches a failure per source (only the exception type is
  logged; the user sees a short message such as "Store apps could not be read."), honours cancellation and returns an
  `EvidenceSnapshot` (programs, per-source counts, errors, collected-at time, duration).
- `ScanService.RunAsync` starts the collector together with the profile scan (same token, D-038) and stores the
  snapshot in `ScanResult.Evidence` of a completed scan. A cancelled scan stores nothing, so `LastResult` and its
  evidence stay as they were (D-039).
- Programs page (D-037): `ProgramsPageViewModel` reads `LastResult.Evidence`; filtering and sorting of entries is the
  pure `ProgramFilter`. The only actions are "Open install location" (`IShellLauncher.OpenFolder`, enabled when
  `IFolderExistence` says the folder exists) and "Copy name". The view applies the default sort and its header arrow
  with `DataGridSorting` (also used for the Size column of Results).

### Rules (T-005)
An offline JSON knowledge base in `rules/` (D-005, D-040 … D-043), embedded into `LeftoverScout.Core` with the logical
names `Rules/<file>.json` (not copied). Format and contributor guide: `docs/RULES.md`. Code lives in
`LeftoverScout.Core.Rules`:
- three kinds: *ownership* (folder → program with match hints), *cache* (category cache / temp / logs / crashDumps)
  and *protected* (never offered for deletion);
- `PathPattern`: one token (`{Profile}`, `{RoamingAppData}`, `{LocalAppData}`, `{LocalLow}`) followed by literal names or
  `*` (exactly one folder name); no regular expressions. Tokens are resolved against the profile root of the scan, never
  against `Environment` paths (D-003);
- `RuleLoader` parses the files with `System.Text.Json` (`UnmappedMemberHandling = Disallow`), validates them and returns
  a `RuleLoadResult` (valid `RuleSet` plus `RuleValidationError`s). An invalid rule is skipped, never fatal;
  `EmbeddedRuleLoader` logs the skipped rule ids. A `cache` or `ownership` rule that can lie inside a `subtree`-protected
  path is rejected: protection is absolute;
- `RuleMatcher` indexes the patterns in a trie and walks the scan tree once, producing a `RuleMatchMap` (per folder a
  `FolderRuleMatch`). Protected wins, then direct before inherited matches, then more literal segments. Personal folders
  (`FolderNode.IsPersonal`) are protected with subtree scope by a built-in rule (D-020);
- `ScanService` runs the matcher once after each completed scan and stores the map in `ScanResult.RuleMatches`;
  the Results page only reads it (no re-matching while navigating). A matcher failure is logged and leaves the map empty.
- Results (D-043): `FolderRow.RuleMatch` feeds `FolderTagText.RuleLabel/Describe/Explain` (`Protected`, `Cache: <program>`,
  `Temp`, `Logs`, `Crash dumps`, `Owner: <program>` and a tooltip with the rule descriptions).

### Leftover detection (T-006)
Offline, explainable and pure (no file system, no registry; injected `TimeProvider`). Code lives in
`LeftoverScout.Core.Classification` (decisions D-044 … D-047). Safety bias: when in doubt the answer is "not a leftover".
- `NameNormalizer` lower-cases a name, splits it into tokens (non-alphanumerics, camel case, letter-digit
  boundaries), drops version tokens (`2022`, `v2`, `x64`, `x86`) and company suffixes (`inc`, `ltd`, `llc`, `sro`, ...).
  Dotted abbreviations (`s.r.o.`) are joined first.
- `EvidenceIndex` is built once per scan from the `EvidenceSnapshot` (program names, publishers, registry key names,
  Store identity and family names, program folder names with their subfolders, Start menu names). A folder name matches
  when the joined forms are equal, when all significant tokens (at least 3 characters, not generic) of the folder are
  tokens of one entry, or when all significant tokens of an entry are tokens of the folder name. **No fuzzy
  similarity.** `GenericNames` is the stop-list (`temp`, `cache`, `data`, `microsoft`, ...): a generic folder name
  decides nothing.
- `LeftoverCandidateFinder` picks the folders (D-044): first level of `AppData\Roaming`, `Local`, `LocalLow`; the
  subfolders of a *vendor* folder (a first-level folder that matches only a publisher name; never `Microsoft`, whose
  children stay Unknown); dot-folders in the profile root; every folder in `AppData\Local\Packages`. Personal,
  protected (any scope), cache/temp/logs/crash-dump, link, unread cloud and inaccessible folders are not candidates.
- `LeftoverClassifier` returns a `LeftoverMap` (folder → `LeftoverVerdict`): `InUse` (an evidence match or an
  ownership rule whose hints match always wins), `Leftover` with `High` / `Medium` / `Low`, or `Unknown`. A folder is
  "not changed" when its newest file is exactly 6 months old or older. Store package folders: High when the family is
  not installed and unchanged for 6 months, otherwise Medium. Before that, a not-found package whose vendor part (package name before the last `_`, then before the first `.`)
  matches as a whole token the name, publisher or registry key name of an installed program (registry and Store sources only,
  never Start menu or folder names; generic or shorter than 3 characters: skipped) is **In use** (D-050), and a not-found
  package whose vendor part starts with `microsoft` is Medium at most (D-051). Ownership rule with `requiresInstallation: true` and no
  evidence: High; with `false`: Unknown. Other candidates without evidence: Medium (unchanged for 6 months or empty)
  or Low. Two safety limits beyond the task text: no Leftover verdict at all when any evidence source failed, and a
  dot-folder without an ownership rule is rated Low at most (`.ssh`, `.docker`, ... are tool settings).
- `ScanService` runs the classifier once after rule matching and stores the map in `ScanResult.Leftovers`. Results
  (D-047): `FolderRow.Leftover` feeds `FolderTagText` (`Leftover: High|Medium|Low`, `In use: <program>`; the tooltip
  holds the explanation; Unknown shows nothing). The findings page (T-008) is described below.

### Findings page (T-008)
The Findings page (decisions D-052 … D-055) combines the scan tree, the rule matches and the leftover verdicts into
**findings**. Logic without WPF types lives in `LeftoverScout.Core.Findings`:
- `Finding` (record): folder node, path relative to the profile root, `FindingKind` (`Leftover`, `Cache`, `Temp`, `Logs`,
  `CrashDumps`), confidence (leftovers only), size, files, last modified (read from the node), program name, explanation
  lines, matched rule id and description.
- `FindingBuilder.Build(ScanResult)`: every `LeftoverMap` entry with verdict `Leftover`, plus the **top-most** folder whose
  primary rule match is a cache rule (a cache folder inside another listed cache folder, or inside a folder that is listed
  as a leftover *and* is a cache, is not listed separately). The builder re-checks safety itself and does not rely on the
  classifier: personal folders, folders with a protected primary match, folders below a personal or **subtree**-protected
  ancestor, skipped links, unread cloud folders and folders without size and files are never listed. Folder-scope protection
  of an ancestor (for example `AppData\Local` itself) does not block its children, otherwise nothing under `AppData` could be
  listed. The program name comes from the ownership or cache rule (the leftover verdict does not carry one).
- `FindingFilter` (record: High / Medium / Low / Cache / Temp / Logs / Crash dumps flags and a search text; `Default` hides
  Low) and `Apply(...)`, which returns a `FindingList`: the visible `FindingEntry` items and the total size. An item that lies
  inside another **visible** item gets `InsideOf` set, stays in the list and is left out of the total (no double counting).
- `FindingsPageViewModel` (App) builds the findings once per completed scan (on `LastResultChanged`, like the other pages),
  keeps the filter state while the app runs, sorts the rows by size and exposes the three commands. `FindingRowViewModel` exposes
  raw values for `DataGrid` sorting. "Show in Results" raises `ShowInResultsRequested`; `MainViewModel` then calls
  `ResultsPageViewModel.ShowFolder(node)` (`FolderNavigator.GoTo` + row selection) and switches the page.
  Actions use `IShellLauncher.OpenFolder` and `IClipboardService`; none of them writes or deletes anything.

Selection for cleanup will be added to the Findings page later (*planned*); the Cleanup page will then show confirmation and progress.

### Safety guard (T-009)
`CleanupGuard` (`LeftoverScout.Core.Cleanup`) is the one gate every deletion must pass. It deletes, moves and opens
nothing; T-010 (cleanup service) and T-011 (Cleanup page) call it. Input is a `CleanupRequest` (a `ScanTarget` and the
selected `CleanupItem`s: path, kind, confidence, size and newest write time **as the scan saw them**, used for warnings
only). Output is a `CleanupGuardReport`: one `GuardResult` per item (`IsAllowed`, `BlockReason`s, `GuardWarning`s) plus
the reduction info. The guard re-checks everything fresh and never trusts the scan result.

Layers of protection (an item must pass all of them):
1. **Findings builder / rules** (scan time): protected rules and personal folders are never offered. This is the first line.
2. **Request reduction** (`RequestReducer`, pure): duplicates (normalized, case-insensitive) are removed and an item
   inside another selected item is dropped; the report lists what was merged.
3. **Pure path checks** (`CleanupPathChecker`, `ParsedPath`; table-driven tests): block a path that is not a plain fully
   qualified path (`..`, `.`, wildcards, streams, trailing dots or spaces; `/` is read as `\`), a device or `\\?\` path,
   a path that is not **strictly inside** the profile root (whole segments, so `Sam2` is not inside `Sam`), the profile
   root and the fixed roots (`AppData`, `Roaming`, `Local`, `LocalLow`), the app's own program and data folders (and
   anything inside or containing them), personal folders (known-folder paths, standard names and `OneDrive*` directly in the
   profile root; the path itself, any **ancestor**, or a folder that **contains** a known personal folder), and anything
   protected by a rule (`RuleMatcher.MatchPath`: a `subtree` rule of the path or any ancestor, a `folder` rule of the path
   itself, or a folder that contains a protected rule path). Independent of the Findings builder and of the scan tree.
4. **Hard-coded deny list** (`ProtectedDenyList`, in code, not JSON): the fixed roots and the Windows security folders
   of `rules/protected.json` (Microsoft, Packages, Programs, Protect, Credentials, SystemCertificates, Crypto, Vault,
   Start Menu, WindowsApps), so a missing or broken rule file can never unprotect them.
5. **Live file system checks** (`IPathProbe`, Windows implementation `WindowsPathProbe`; only attribute queries with
   `FindFirstFileExW` on the item itself and `GetLongPathNameW`, `\\?\` prefix for long paths, nothing is opened or
   followed). Run only for items that passed 1-4. Block when: the path does not exist or is not a folder; the long form
   (8.3 short names expanded) fails the path checks again (`APPDAT~1` cannot hide `AppData`); the item or **any parent
   between the profile root and the item** is a name-surrogate reparse point (junction, symlink); the item or a parent is
   a cloud folder that is not downloaded (`RECALL_ON_OPEN`); or an attribute query fails ("could not be checked").

What only **warns** (never allows a blocked item): the folder has newer direct content than the scan saw
(`IDirectoryReader` listing, names and times only); confidence Low; confidence Medium. When unsure, the guard blocks.
Logging writes counts per block reason, never paths.

### Cleanup service (T-010)
No UI starts it yet (T-011 adds the Cleanup page). Code lives in `LeftoverScout.Core.Cleanup` (logic) and
`LeftoverScout.Windows.Cleanup` (the only code that deletes). Decisions D-062 … D-064.

- `ICleanupService.RunAsync(CleanupRequest, CleanupMode, IProgress<CleanupProgress>?, CancellationToken)` returns a
  `CleanupSessionResult` (times, mode, one `CleanupItemResult` per item: `Removed`, `PartlyRemoved`,
  `SkippedTooLargeForRecycleBin`, `SkippedRecycleBinOff`, `NotRecycled`, `Blocked`, `NotFound`, `Failed`, `NotStarted`).
  `CleanupService` (Core) runs on a thread-pool thread, reduces the request (`RequestReducer`), and for **every item**
  calls `ICleanupGuard.Check` again with that single item right before touching it. Cancellation is checked between items
  only; the current item completes (D-059). Logging: counts per outcome and bytes, never paths.
- `IEntryRemover` removes one file or folder. `ShellEntryRemover` (Windows) uses `IFileOperation` on a short-lived STA
  thread per call, with `FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR`; Recycle Bin mode adds
  `FOFX_RECYCLEONDELETE | FOF_ALLOWUNDO | FOF_WANTNUKEWARNING`. `FOFX_NOSKIPJUNCTIONS` is never set. `IFileOperation` is
  called through its documented vtable (function pointers; no RCW, so nothing leaks, every COM object is released in a
  `finally`); the progress sink is a source-generated COM class (`[GeneratedComClass]`, `FileOperationSink`).
- **Three layers against silent permanent deletion in Recycle Bin mode (D-062):**
  1. *Pre-check* (`RecycleBinPolicy`, pure; `WindowsRecycleBinSettingsReader`, read-only registry): `NukeOnDelete` and
     `MaxCapacity` of the item's volume under `HKCU\...\Explorer\BitBucket\Volume\{volume GUID}` (the GUID from
     `GetVolumeNameForVolumeMountPointW`), and the policy `Policies\Explorer\NoRecycleFiles` (HKCU, HKLM). Bin off →
     `SkippedRecycleBinOff`; scan size above the maximum → `SkippedTooLargeForRecycleBin`. Unreadable settings allow the
     item (layers 2 and 3 still protect).
  2. *Veto* (`ShellOperationRules.ShouldVetoDelete`): `PreDeleteItem` returns a failure HRESULT when
     `TSF_DELETE_RECYCLE_IF_POSSIBLE` (0x80) is missing from the flags → nothing is deleted, `NotRecycled`.
  3. *Verify* (`ShellOperationRules.IsUnexpectedPermanentDelete`): `PostDeleteItem` with success but no
     `psiNewlyCreated` in Recycle Bin mode sets `DeletedPermanentlyUnexpectedly` on the item and the session.
  The shell reports success only after the guard-independent check that the folder is really gone (`IPathProbe`).
- **Locked files (D-063)** — `FolderRemover` (Core): first the whole folder; if that fails (not a veto), the children one by
  one, depth-first (a subfolder is tried as a whole first). Only entries that cannot be removed (and their parents) remain:
  `PartlyRemoved` with the count of entries left; nothing removed at all → `Failed`. A veto of the whole folder is final
  (`NotRecycled`): the children would be vetoed too. Bytes removed for a partly removed folder = scan size minus what
  `FolderSizer` measures afterwards.
- **Junctions and links:** the fallback never touches a name-surrogate reparse point (junction, symlink) or an online-only
  cloud folder: they are left in place and counted as left. The whole-folder delete is done by the shell without
  `FOFX_NOSKIPJUNCTIONS`; the flag's documented meaning is "do not skip junctions" (Microsoft Learn,
  "IFileOperation::SetOperationFlags"), so by default junctions are not traversed. The integration test
  `Run_JunctionInsideFolder_TargetSurvives` (both modes) proves that a junction target and its files survive.
- **Running programs (D-064):** `IRunningProgramDetector.Find(CleanupRequest)` returns, per item, the names of running
  processes whose image path lies inside the item or whose normalized name equals the normalized program name
  (`CleanupItem.ProgramName`, else the folder name; generic names never match by name). `WindowsProcessLister` uses
  `Process.GetProcesses()` and `QueryFullProcessImageNameW` with `PROCESS_QUERY_LIMITED_INFORMATION`; failures are ignored.
  It is a warning before the start, never a block; no process is touched.

Still to come: the Cleanup page (T-011) and the session journal (T-012).

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
- Journals (Phase 3, T-012) will live in the same folder.

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

## Help system (D-024 … D-030)

The user help is Markdown, one topic per file, and the **same files** are read on GitHub and inside the app.

- **Files:** `docs/user-guide/en/*.md` and `docs/user-guide/ru/*.md`, identical file names. Russian text is
  allowed only here. The UI stays English; Russian help names pages and buttons in English, in bold (D-029).
- **Topics:** `HelpTopics.All` (Core) lists the topic ids and file names: `overview` (`index.md`),
  `getting-started`, `scan`, `results`, `programs`, `findings`, `cleanup`, `history`, `about`, `help`.
- **Embedding:** `LeftoverScout.App.csproj` links every `docs/user-guide/**/*.md` as an embedded resource with the
  logical name `Help/<language>/<file>` (not copied). `EmbeddedHelpSource` (App, `IHelpSource`) reads them and
  normalizes the backslashes that `%(RecursiveDir)` produces.
- **Catalog:** `HelpCatalog` (Core) returns the Markdown for (topic id, language); a missing Russian topic falls back
  to English and logs a warning. `HelpLanguages.Resolve` picks the language: saved `AppSettings.HelpLanguage`
  (`en` / `ru`) wins, otherwise the Windows UI language (`ru` gives Russian, anything else English) (D-028).
  `HelpLink.Resolve` classifies links: a relative `*.md` topic file opens inside Help, `https://` opens the browser
  through `IShellLauncher`, everything else is ignored.
- **Renderer:** Markdig parses the text; `HelpDocumentBuilder` (App) converts the AST into a WPF `FlowDocument`.
  Unknown elements become plain text; the builder never throws. Colors come from the Fluent light theme.
- **Page:** `HelpPageViewModel` holds the topic list, the selected topic, the EN / RU switch and the current Markdown;
  `HelpView` only builds the document and forwards link clicks to the view model.
- **Context help (D-030):** every `PageViewModel` has a `HelpTopicId`. `MainWindow` has a shared header with a **?**
  button and an **F1** key binding, both bound to `MainViewModel.OpenHelpCommand`, which opens Help at the topic of the
  current page (on the Help page: the `help` topic).

Allowed Markdown subset: headings `#` to `###`, paragraphs, **bold**, *italic*, `inline code`, bullet and numbered
lists (one nesting level), pipe tables, block quotes, links. No images, no raw HTML.

**Adding a topic for a new page:**
1. Add a `HelpTopics` constant and entry (id, file name, title).
2. Add the Markdown file in `docs/user-guide/en/` and `docs/user-guide/ru/` with the same name (tests check this).
3. Pass the topic id to the page's `PageViewModel` constructor.
4. Link the new topic from the overview and, where useful, from related topics.

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
| Markdig | BSD-2-Clause | App | Markdown parser for the in-app help (D-027) |
| Microsoft.Extensions.Logging.Abstractions | MIT | Core | `ILogger` abstraction, so Core can log without a Windows dependency |

No NuGet package was added in T-004. The Windows SDK projection that comes with the `net10.0-windows10.0.19041.0`
target framework is part of the .NET SDK (see "Solution layout").

## Build and CI

- SDK pinned in `global.json` (10.0.100, roll forward to latest feature band).
- Shared settings in `Directory.Build.props` (nullable, warnings as errors, code style in build).
- Package versions in `Directory.Packages.props` (central package management).
- GitHub Actions `.github/workflows/ci.yml`: `windows-latest`, restore → build (Release) → test.
  Job name `build-and-test` is the required status check for `main`.

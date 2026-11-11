# T-010: Cleanup service (Recycle Bin / permanent, locked files, running programs)

- **Phase:** 3 — Safe cleanup
- **Branch:** `t-010-cleanup-service` (or the branch your session assigned)
- **Depends on:** T-009 (merged)

## Goal

The single service that removes folders the user selected — to the **Recycle Bin** by default, or
permanently when explicitly asked — and reports exactly what happened to each item. It must never
silently delete permanently in Recycle Bin mode. Still **no UI**: T-011 adds the Cleanup page.

## Context

- Read `CLAUDE.md` ("Safety rules"), `docs/ARCHITECTURE.md` (cleanup guard), decisions D-004,
  D-057 … D-061 and D-062 … D-064 (this task) in `docs/LEAD_HANDOVER.md`.
- `CleanupGuard.Check(CleanupRequest)` (T-009) must be called again **right before each item**.

## Why this is hard (read first)

Windows can permanently delete an item that was asked to go to the Recycle Bin, **without error and
without a dialog** in silent mode: when the item is larger than the bin, when the bin is switched off
for the drive ("Don't move files to the Recycle Bin"), or when the drive has no bin. The flag
`FOF_WANTNUKEWARNING` only shows a dialog. We need real protection.

## Lead decisions for this task (refine owner decisions D-057, D-058)

- **D-062** Recycle Bin mode has **three** layers against silent permanent deletion:
  1. **Pre-check** per item: read the Recycle Bin settings of the item's volume for the current user
     (`HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\{volume GUID}`:
     `NukeOnDelete`, `MaxCapacity` in MB; plus the policies "Do not move deleted files to the Recycle
     Bin" and "Maximum allowed Recycle Bin size" if set). Bin off → skip "Recycle Bin is turned off
     for this drive"; item larger than the maximum → skip "too large for the Recycle Bin". Read-only.
  2. **Veto during the operation**: implement `IFileOperationProgressSink`; in `PreDeleteItem`, if the
     flags do **not** contain `TSF_DELETE_RECYCLE_IF_POSSIBLE`, return a failure HRESULT so the item is
     **not** deleted, and record it as "could not be moved to the Recycle Bin".
  3. **Verify afterwards**: in `PostDeleteItem`, a recycled item has a new shell item in the bin
     (`psiNewlyCreated` not null). If it is null in Recycle Bin mode, record "deleted permanently by
     Windows" as an error in the result (this should never happen with layers 1–2; tests and logs make
     it visible if it does).
  Verify these APIs (`IFileOperation::SetOperationFlags`, `IFileOperationProgressSink::PreDeleteItem`
  / `PostDeleteItem`, `TRANSFER_SOURCE_FLAGS`) in Microsoft Learn; if your session cannot open it,
  name exactly what you relied on and the lead will verify.
- **D-063** Locked files (D-058) in Recycle Bin mode: first try to recycle the whole folder. If that
  fails because something is in use, fall back to recycling its children one by one (depth-first),
  so everything that is not in use goes to the bin and only locked files (and their parent folders)
  remain. Result: "partly removed: N items in use". Permanent mode does the same per entry.
- **D-064** "Program seems to be running" (D-058) is a **warning before start**, never a block:
  a running process whose image path lies inside the item, or whose normalized process name matches
  the item's program (rule program hints / program name in the finding). Names only; no process is
  touched.

## Scope

### 1. Core contracts
- `CleanupMode`: `RecycleBin`, `Permanent`.
- `ICleanupService.RunAsync(CleanupRequest, CleanupMode, IProgress<CleanupProgress>?, CancellationToken)`
  → `CleanupSessionResult`: start/end time, mode, per item `CleanupItemResult` with outcome
  `Removed`, `PartlyRemoved` (count in use), `SkippedTooLargeForRecycleBin`, `SkippedRecycleBinOff`,
  `NotRecycled` (veto of layer 2), `Blocked` (guard reasons), `NotFound`, `Failed` (reason),
  `NotStarted` (cancelled before it began); bytes removed; plus `DeletedPermanentlyUnexpectedly` flag
  for layer 3.
- `CleanupProgress`: current item index/count, current item path (relative), bytes done.
- `IRunningProgramDetector.Find(CleanupRequest)` → per item a list of process names (D-064).
- Cancellation: checked **between items** only — the current item always completes (D-059).

### 2. Windows implementation
- `IFileOperation` (source-generated COM interop, `[GeneratedComInterface]` / `ComWrappers`) on a
  **dedicated STA thread** (not the UI thread).
- Flags: `FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR`; Recycle Bin mode adds
  `FOFX_RECYCLEONDELETE | FOF_ALLOWUNDO` (or `FOFX_ADDUNDORECORD`) **and** `FOF_WANTNUKEWARNING` as a
  last resort. Never `FOFX_NOSKIPJUNCTIONS`.
- Before each item: `CleanupGuard` check (fresh). Blocked → result `Blocked`, nothing touched.
- Junctions/symlinks **inside** a folder must be removed as links — the target must stay intact.
  Check the docs for how `IFileOperation` treats junctions without `FOFX_NOSKIPJUNCTIONS`; prove it
  with a test (below). If the default would leave junctions in place, that is fine (the item becomes
  `PartlyRemoved`); following a junction is never acceptable.
- Recycle Bin settings reader (D-062 layer 1): volume GUID via `GetVolumeNameForVolumeMountPointW`
  for the item's volume root; registry read-only.
- Running program detector: `Process.GetProcesses()` names; image path via
  `QueryFullProcessImageNameW` with `PROCESS_QUERY_LIMITED_INFORMATION`, failures ignored.
- Logging: counts per outcome and bytes; **no paths**.

### 3. Tests
- Pure: outcome aggregation, cancellation between items, guard re-check per item (a fake guard that
  blocks the second item), layer-1 decision logic (bin off, too large, fits, policy values), layer-2/3
  decision logic as pure functions (flags → veto; newly created item null → unexpected).
- Windows integration tests **in temp folders only**:
  - permanent mode removes a nested folder;
  - a **junction inside** the folder pointing to a second temp folder: after cleanup the second
    folder and its files **still exist** (critical);
  - a file kept open with `FileShare.None` → `PartlyRemoved`, other files removed (both modes);
  - Recycle Bin mode on a small temp folder → `Removed`, folder gone, layer 3 reports it was recycled
    (it lands in the CI runner's bin — acceptable);
  - guard blocks an item outside the given profile root (use a fake profile root under temp).
- No test touches the real profile, and no test deletes anything outside its own temp folders.

### 4. Documentation
- ARCHITECTURE.md: cleanup service, the three layers, locked-file fallback, junction behavior (with
  doc links), running-program warning.
- Help: **no change needed** (nothing user-visible yet) — say so in the PR.

## Out of scope

- Any UI (T-011), journal files and History (T-012), emptying the Recycle Bin.

## Acceptance criteria

- [ ] Recycle Bin mode can never silently delete permanently: layers 1–3 implemented and tested.
- [ ] The guard is re-checked before every item; blocked items are untouched.
- [ ] Junction targets survive cleanup (test).
- [ ] Locked files lead to `PartlyRemoved`, the rest is removed (test, both modes).
- [ ] Cancel stops between items; results are complete and honest.
- [ ] No UI path can start a cleanup yet.
- [ ] Build and tests pass (Release); CI green; ARCHITECTURE updated; "Help: no change needed".

## Manual test steps for the owner

None — nothing visible changes. The lead reviews the code and the integration tests.

## Notes for the executor

- When in doubt, do **not** delete: skip the item with an honest reason.
- Recurring mistakes to avoid: xUnit1031 (no blocking waits in test methods), xUnit1051
  (`TestContext.Current.CancellationToken`), missing `using LeftoverScout.Core.Tests.Scanning;`,
  fixed-list tests not updated, test folders named like personal folders are personal, WPF `System.IO`.
- COM interop must not leak: release every COM object; the STA thread ends after the operation.

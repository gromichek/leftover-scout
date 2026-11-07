# T-002: Scan engine and Scan page

- **Phase:** 1 — Read-only scan
- **Branch:** `t-002-scan-engine` (or the branch your session assigned)
- **Depends on:** T-001 (merged)

## Goal

Measure what takes space in the current user's profile — read-only, fast, safe for OneDrive —
and let the user start/cancel a scan and see the totals on the Scan page. The folder tree view
comes in T-003; this task produces the data it will show.

## Context

- Read `CLAUDE.md` (especially "Safety rules"), `docs/ARCHITECTURE.md`, decisions in
  `docs/LEAD_HANDOVER.md`: D-002, D-003 (`ScanTarget`), D-020 … D-023 (this task).
- Existing: `ScanTarget` (Core), app shell with a placeholder Scan page (T-001).

## Owner decisions for this task

- **D-020** Scan the **whole profile**. Personal folders are measured and **marked personal**
  (later they become protected): Documents, Desktop, Pictures, Videos, Music, Downloads, OneDrive.
- **D-021** Result = **folder tree** with aggregated values; individual files are not kept,
  except a list of **large files** (≥ 100 MB, threshold configurable in code).
- **D-022** Size = **logical file size**. Cloud-only (online-only) files are counted
  **separately** and are not part of the local size.
- **D-023** This task includes a **simple Scan page** (start, cancel, progress, summary).

## Scope

### 1. File system access (Core interface, Windows implementation)
- Core: an interface (e.g. `IDirectoryReader`) that lists the entries of one folder:
  name, is-directory, logical length, last write time (UTC), raw Win32 attributes (`uint`),
  reparse tag (`uint`, 0 if none). Errors are returned as data (path + reason), not thrown.
- Windows: implement it with `FindFirstFileExW` / `FindNextFileW`
  (`FindExInfoBasic`, `FIND_FIRST_EX_LARGE_FETCH`). The reparse tag is in
  `WIN32_FIND_DATA.dwReserved0` when `FILE_ATTRIBUTE_REPARSE_POINT` is set
  (see "Reparse Point Tags", Microsoft Learn). Use `LibraryImport`, `SafeHandle` for the find
  handle, and the `\\?\` prefix so that paths longer than 260 characters work.
- The reader only enumerates. It never opens files, never reads file data, never changes
  anything (enumeration does not update access times).

### 2. Reparse points and cloud files — exact rules
Put these rules in **one pure Core class** (e.g. `EntryClassifier`) with unit tests for every case:
1. **Name-surrogate reparse point** (tag has the name-surrogate bit `0x20000000`, e.g.
   junction `IO_REPARSE_TAG_MOUNT_POINT`, symlink `IO_REPARSE_TAG_SYMLINK`):
   **do not descend, do not count**; record it as a skipped link. This covers the legacy
   profile junctions such as `Application Data`, `My Documents`, `Local Settings`.
2. **Cloud placeholder** (tag in the `IO_REPARSE_TAG_CLOUD*` family, i.e. `(tag & 0xFFFF0FFF) == 0x9000001A`,
   or other non-surrogate tags): treat as a normal file/folder **using metadata only**.
3. A **file** with `FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS` (`0x00400000`) or
   `FILE_ATTRIBUTE_OFFLINE` (`0x00001000`) is **cloud-only**: add its length to the cloud-only
   total, not to the local size.
4. A **folder** with `FILE_ATTRIBUTE_RECALL_ON_OPEN` (`0x00040000`) is **not populated locally**:
   **do not enumerate it** (that would make OneDrive download the listing); record it as
   cloud folder not scanned.
5. Other reparse files that are not name surrogates (e.g. `IO_REPARSE_TAG_APPEXECLINK` app
   aliases in `WindowsApps`) are counted as files with their reported length, never followed.
Verify the constants against Microsoft Learn ("File Attribute Constants", "Reparse Point Tags")
and cite them in the PR. If anything here contradicts the docs, stop and ask in "Open questions".

### 3. Scanner (Core)
- `ProfileScanner` (name up to you): `Task<ScanResult> ScanAsync(ScanTarget, ScanOptions,
  IProgress<ScanProgress>?, CancellationToken)`.
- Builds a **folder tree**. Each folder node: name, full path (or computable), local size,
  cloud-only size, file count, subfolder count (totals include the whole subtree),
  newest last-write time in the subtree, flags: personal, skipped link, cloud folder not
  scanned, access denied / error. Children sorted by name (ordinal, case-insensitive) so results
  are deterministic.
- `ScanResult`: root node, large files list (path, size, last write), errors list
  (path, reason), skipped links count, duration, completion state.
- **Personal folders** (D-020): a Core class receives the list of personal folder paths and
  marks matching nodes (inside the profile only). Windows side resolves them via
  `SHGetKnownFolderPath` (Documents, Desktop, Pictures, Videos, Music, Downloads) plus the
  OneDrive roots from the environment variables `OneDrive`, `OneDriveConsumer`,
  `OneDriveCommercial`. Known folders redirected outside the profile are simply not in the scan.
- **Progress**: throttled (at most ~10 reports per second): current folder, folders, files,
  local bytes so far.
- **Cancellation**: stops quickly (check the token per folder); the result is "cancelled" and
  the UI shows that no totals are available.
- Access denied / path errors: never fatal; record and continue.
- Hard links are counted once per link (note this limitation in ARCHITECTURE.md).
- Correctness first. Parallelism is optional; if you add it, cap the degree of parallelism
  and keep results deterministic.
- Memory: do not store per-file objects except large files.

### 4. Scan page (D-023)
- Replace the Scan placeholder with a real page (MVVM):
  - **Start scan** and **Cancel** buttons (only one enabled at a time);
  - while running: current folder (shortened), folders/files found, size so far, elapsed time;
  - after completion: local size, cloud-only size, personal folders size, number of folders and
    files, skipped links, cloud folders not scanned, inaccessible locations, duration;
  - a short list of the **10 largest top-level folders** of the profile with sizes;
  - after cancel: "Scan cancelled."
- Sizes formatted like Windows Explorer (1 KB = 1024 bytes, one decimal: `1.5 GB`) by a tested
  Core helper.
- The scan runs off the UI thread; the window stays responsive. The last result stays in memory
  (a singleton service) so that T-003 can show it on the Results page.
- Status bar: "Scanning…", "Scan completed in 42 s", "Scan cancelled".
- Log start, end (totals and duration) and the number of errors — no list of paths in the log.

### 5. Collected findings from earlier reviews (PROGRESS #1, #2)
1. Placeholder pages and `docs/user-guide/getting-started.md` say Cleanup/History come in
   "Phase 2"; correct is **Phase 3**.
2. `WindowPlacementService`: check visibility against the **work areas of the actual monitors**
   (not one virtual-screen rectangle), so a window is never restored into a gap between monitors
   of different sizes. Keep the logic in the tested Core validator.

### 6. Tests
- `EntryClassifier`: every rule in section 2 (synthetic attributes/tags).
- Scanner with a **fake `IDirectoryReader`** (in-memory tree): aggregation, cloud-only
  accounting, skipped links, not-populated cloud folder, access denied, personal marking,
  large files threshold, deterministic order, cancellation, progress throttling.
- Windows reader against a **temp folder** created by the test (files, nested folders,
  a long path > 260 chars). A junction/symlink test only if it can be created without admin
  rights (directory junctions can); skip with a clear reason otherwise.
- Size formatter; window visibility validator with several monitors.

### 7. Documentation
- User guide: new page "Scanning your profile" (what is measured, what personal / cloud-only /
  skipped links / inaccessible mean, that nothing is changed); fix the phase numbers.
- ARCHITECTURE.md: replace "Scan (planned)" with the actual design, including the reparse/cloud
  rules and the hard-link limitation.

## Out of scope

- Folder tree / Results page (T-003), evidence (T-004), any classification or deletion.
- Size on disk / compression (D-022), scanning outside the profile, admin mode.

## Acceptance criteria

- [ ] Scan of a real profile completes, the window stays responsive, Cancel works within ~1 s.
- [ ] No file in the profile is opened for reading, written or changed by the scan.
- [ ] Junctions/symlinks are never followed; legacy profile junctions show as skipped links.
- [ ] OneDrive online-only files are not downloaded; their size appears as cloud-only.
- [ ] Every rule in section 2 has a unit test; scanner logic tested with a fake reader.
- [ ] Findings #1 and #2 fixed.
- [ ] Build and tests pass (Release); CI green; user guide and ARCHITECTURE updated.

## Manual test steps for the owner

1. Start the app → Scan → **Start scan**. The window can be moved and resized while it runs.
2. When it finishes, compare the local size roughly with Explorer → `C:\Users\<you>` →
   Properties (expect differences: Explorer counts cloud files and cannot read some folders).
3. Check that "skipped links" is not zero (Windows creates legacy junctions in every profile).
4. If you use OneDrive with online-only files: their size appears as cloud-only, and the files
   still show the cloud icon in Explorer afterwards (nothing was downloaded).
5. Start a scan again and press **Cancel** after a few seconds → "Scan cancelled." quickly.
6. The 10 largest top-level folders look plausible (AppData is usually among them).
7. Cleanup/History placeholders now say Phase 3.

## Notes for the executor

- Read Microsoft Learn: `FindFirstFileExW`, `WIN32_FIND_DATAW` (`dwReserved0`), "Reparse Point
  Tags", "File Attribute Constants", "Naming Files, Paths, and Namespaces" (long paths),
  `SHGetKnownFolderPath`, `MonitorFromRect`/`EnumDisplayMonitors` (or `System.Windows.Forms.Screen`
  is **not** allowed — do not add WinForms).
- Interop goes into `LeftoverScout.Windows`; Core stays free of Windows APIs.
- Keep the scanner free of UI types; the page only binds to view model properties.
- You cannot run the app in CI; make the logic testable and describe in the PR what you could
  not verify.

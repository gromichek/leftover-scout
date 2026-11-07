# T-003: Results page (folder browser and large files)

- **Phase:** 1 — Read-only scan
- **Branch:** `t-003-results-page` (or the branch your session assigned)
- **Depends on:** T-002, T-018 (merged)

## Goal

After a scan the user can browse where the space goes: open any folder of the profile, sort its
subfolders by size, see what is personal, a skipped link, cloud-only or unreadable, list the large
files, and open any item in Explorer. Nothing is changed or deleted.

## Context

- Read `CLAUDE.md` (safety rules, help rules), `docs/ARCHITECTURE.md`, decisions in
  `docs/LEAD_HANDOVER.md`, especially D-020 … D-022 and D-031 … D-034 (this task).
- Data: `IScanService.LastResult` (`ScanResult`: `Root` folder tree, `LargeFiles`, errors).
  `FolderNode` has name, full path, parent, children, local size, cloud-only size, file count,
  subfolder count, newest write time, flags (personal, skipped link, cloud not scanned,
  access denied, error). Formatters exist in `Core/Formatting`.
- The Results page today is a placeholder; its help topic `results` says "planned".

## Owner decisions for this task (provisional — the owner re-evaluates after trying it)

- **D-031** Folder view = **contents of one folder at a time** with a **breadcrumb** path:
  a sortable table of the current folder's subfolders; double-click / Enter opens a folder;
  the breadcrumb and **Up** go back.
- **D-032** Columns: **Name, Size, % of parent (bar + number), Files, Last modified, Notes**.
  Notes shows short tags: **Personal**, **Link**, **Cloud**, **Inaccessible**. No treemap.
- **D-033** A second tab **Large files** (≥ 100 MB from the scan): path, size, last modified, sortable.
- **D-034** Row actions: **Open in Explorer** (a file opens Explorer with the file selected) and
  **Copy path**, as buttons above the table and in a context menu.

## Scope

### 1. Scan result notification
- `IScanService` raises an event (e.g. `LastResultChanged`) when a new result is stored
  (completed or cancelled). The Results page refreshes when it is shown or when the event fires
  (marshal to the UI thread).

### 2. Results page — states
- **No scan yet** (or last scan cancelled): a short message and a button **Go to Scan** that
  switches the navigation to the Scan page.
- **Result available**: a header line "Scanned <date/time> · <local size> local · <cloud-only> cloud-only"
  and two tabs: **Folders** and **Large files**.

### 3. Folders tab (D-031, D-032)
- Starts at the profile root. Breadcrumb from the root to the current folder; every part is
  clickable. **Up** button (disabled at the root).
- Table rows = subfolders of the current folder (`FolderNode.Children`), default sort by **Size**
  descending; clicking a column header sorts (standard `DataGrid` sorting is fine; numeric columns
  must sort by value, not by the formatted text).
- One extra row **(files in this folder)** showing the size and count of the files directly in
  the current folder (computed as current folder totals minus the sum of its children). Not
  openable. Hide it when it is 0 files.
- Columns:
  - **Name** — folder name;
  - **Size** — local size, Explorer format;
  - **% of parent** — share of the current folder's local size: a thin bar plus "12.3 %";
  - **Files** — total files in the subtree;
  - **Last modified** — newest write time in the subtree, local time, short date + time;
  - **Notes** — tags joined with ", ": Personal, Link, Cloud, Inaccessible. Add a tooltip that
    explains each tag in one sentence.
- Rows that cannot be opened (Link, Cloud not scanned, Inaccessible without children) do not
  navigate on double-click.
- Use `DataGrid` with UI virtualization on (profiles can have thousands of subfolders in one folder).
- Status/summary under the table: "<n> folders · <size> in this folder".

### 4. Large files tab (D-033)
- Table: **Path** (relative to the profile root, full path in tooltip), **Size**, **Last modified**.
  Default sort by Size descending; sortable by value. Empty state: "No files of 100 MB or more."

### 5. Actions (D-034) — read-only
- **Open in Explorer**: folder → open the folder; the "(files in this folder)" row → open the
  current folder; large file → `explorer.exe /select,"<path>"`. Extend `IShellLauncher` with a
  method for selecting a file. Never throws; failures are logged.
- **Copy path**: puts the full path on the clipboard via a small `IClipboardService` (WPF
  `Clipboard` can throw `COMException` when the clipboard is busy — catch, retry briefly, log).
- Both as buttons above each table (disabled when nothing is selected) and in a row context menu.
  Enter = open folder (Folders tab); Ctrl+C = Copy path.

### 6. Core logic (testable without WPF)
- Put navigation and row building in Core or in view models that do not touch WPF types:
  breadcrumb building, "files in this folder" row, percent of parent (0 when parent size is 0),
  notes tags, openability rule, relative path for large files.

### 7. Help (mandatory, EN + RU — D-026)
- Rewrite `docs/user-guide/en/results.md` and `ru/results.md`: both tabs, every column, every
  Notes tag, every button and shortcut, the "(files in this folder)" row, empty states, and tips
  (e.g. "sort by Size and open the biggest folders first", "AppData\Local usually holds caches").
- Update `index.md` (EN + RU): Results is no longer "planned"; order of work Scan → Results.
- Update `scan.md` (EN + RU): the "Next" link no longer says planned.
- UI labels in the Russian help exactly as in the app, bold (D-029).

### 8. Tests
- Breadcrumb, Up, navigation into openable/non-openable rows.
- "(files in this folder)" row values and hiding.
- Percent of parent including zero-size parent; notes tags for every flag combination.
- Sorting uses numeric values (view model exposes raw values).
- Large files relative paths; empty state.
- No-scan / cancelled state and refresh on `LastResultChanged`.
- Help tests keep passing (topic exists in both languages, links valid).

## Out of scope

- Deleting, selecting for cleanup, classification, evidence (later phases).
- Treemap, search/filter, export.

## Acceptance criteria

- [ ] Results shows "Go to Scan" before a scan and the result after it, without restarting.
- [ ] Folders tab: breadcrumb, Up, double-click/Enter navigation, sorting by value, all six columns.
- [ ] Notes tags shown and explained; links / cloud folders / inaccessible rows do not open.
- [ ] Large files tab with sorting and empty state.
- [ ] Open in Explorer (folder, current folder, file selected) and Copy path work; never crash.
- [ ] No write/delete API used; scanning code unchanged except the result event.
- [ ] Help `results.md`, `index.md`, `scan.md` updated in EN and RU and match the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. Start the app → **Results**: a message and **Go to Scan**. Press it → Scan page.
2. Run a scan, then open **Results**: header with date and sizes; Folders tab at the profile root,
   biggest folders on top.
3. Double-click `AppData` → `Local`: breadcrumb grows; click the profile name in the breadcrumb → root.
   **Up** works; at the root it is disabled.
4. Sort by **Name**, **Files**, **Last modified**, then **Size** again — numbers sort correctly
   (e.g. 900 MB below 1.2 GB).
5. Find a row with **Link** (e.g. `Application Data`) — it does not open; hover **Notes** for the explanation.
6. Personal folders (Documents, Downloads …) show **Personal**.
7. Select a folder → **Open in Explorer**; right-click → **Copy path**, paste into Notepad.
8. **Large files** tab: biggest files on top; **Open in Explorer** opens Explorer with the file selected.
9. Press **?** on Results — the help describes exactly what you see (check the Russian text too).

## Notes for the executor

- WPF `DataGrid` sorting: bind `SortMemberPath` to raw numeric properties; read the `DataGrid`
  docs on sorting and virtualization (`EnableRowVirtualization`, `VirtualizingPanel`) and cite them.
- `explorer.exe /select,` syntax: verify in Microsoft docs/sources; quote the path.
- `System.Windows.Clipboard` exceptions: check the docs (`SetText` / `SetDataObject` with retry).
- Remember the WPF `System.IO` implicit-using pitfall (CLAUDE.md).
- The view only binds; navigation logic lives in view models / Core and is unit-tested.

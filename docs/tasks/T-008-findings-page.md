# T-008: Findings page

- **Phase:** 2 — Classification
- **Branch:** `t-008-findings-page` (or the branch your session assigned)
- **Depends on:** T-006b (merged)

## Goal

One page that answers "what could I remove?": every leftover candidate and every cache / temp /
logs / crash-dump folder found by the rules, with size, confidence and a full explanation. It is
read-only now; in Phase 3 this page gets checkboxes and "Clean up selected…".

## Context

- Read `CLAUDE.md`, `docs/ARCHITECTURE.md`, decisions D-044 … D-051 and D-052 … D-055 (this task)
  in `docs/LEAD_HANDOVER.md`, finding #11 in `docs/PROGRESS.md`.
- Data after a completed scan (`ScanResult`): folder tree, `RuleMatchMap` (rule matches incl. cache
  categories and protection), `LeftoverMap` (verdicts for candidates), evidence.
- Results navigation: `FolderNavigator.NavigateTo(FolderNode)`; main navigation in `MainViewModel`.

## Owner decisions for this task

- **D-052** New navigation page **Findings** between **Programs** and **Cleanup**. In Phase 3 the
  selection for cleanup happens here; the Cleanup page will show confirmation and progress.
- **D-053** The list contains **leftovers** (High / Medium / Low) and folders whose primary rule match
  is **cache / temp / logs / crash dumps**. Default filter: High, Medium and all cache kinds;
  **Low hidden** by default.
- **D-054** Table + **details panel** + total line: columns **Path, Kind, Confidence, Size,
  Last modified, Program**; default sort Size descending.
- **D-055** Row actions: **Open in Explorer**, **Copy path**, **Show in Results**.

## Scope

### 1. Findings model (Core, pure)
- `Finding`: folder node, relative path (from the profile root), kind (`Leftover`, `Cache`, `Temp`,
  `Logs`, `CrashDumps`), confidence (leftovers only), local size, file count, last modified,
  program name (ownership rule display name, rule program, or the program from the verdict text
  source — null when unknown), explanation lines, matched rule (id + description) if any.
- Builder from `ScanResult`:
  - leftovers: every `LeftoverMap` entry with verdict `Leftover`;
  - caches: every folder whose **primary** rule match is a cache rule, but only the top-most one
    (a cache folder inside another listed cache folder is not listed separately);
  - **never** include anything personal or protected (check the folder and its ancestors — protection
    is absolute), skipped links, cloud folders not scanned, or folders with zero local size **and**
    zero files (nothing to gain);
  - a folder can be both a leftover and a cache — list it once, as Leftover, and mention the cache
    rule in the explanation.
- **Totals without double counting**: when a listed item lies inside another *visible* listed item
  (e.g. `discord\Cache` inside a leftover `discord`), the inner item is shown but excluded from the
  total, and its details say "Inside another finding: <path>".

### 2. Findings page (D-052 … D-055)
- No result yet / last scan cancelled: same pattern as Results (message + **Go to Scan**; keep the
  previous completed result after a cancel — D-039).
- Header: "Scanned <date/time>".
- Filters (above the table): checkboxes **High**, **Medium**, **Low** (Low off by default),
  **Cache**, **Temp**, **Logs**, **Crash dumps** (on by default), and a **Search** box (path or
  program, case-insensitive). Filter state is kept while the app runs (not saved).
- `DataGrid`: **Path**, **Kind**, **Confidence** (empty for cache kinds), **Size**, **Last modified**,
  **Program**; sortable by value; default sort Size descending with the arrow shown.
- Total line under the table: "<n> items · <size>" for the visible items (no double counting).
- Details panel under the table for the selected row: full path, kind, confidence, size, files,
  last modified, program, the explanation lines, the matched rule (description), and the
  "Inside another finding" note when applicable. Empty panel text when nothing is selected.
- Actions as buttons and context menu: **Open in Explorer**, **Copy path** (Ctrl+C),
  **Show in Results** — switches to Results, opens the parent folder of the item and selects its row.
- Help link (?/F1) to a new topic `findings`.
- Remember finding #7: overlays must not catch mouse clicks (`IsHitTestVisible="False"`).

### 3. Finding #11 — Programs count
- Status under the Programs table explains hidden entries, e.g.
  "476 of 871 programs · 395 hidden (system components and frameworks)"; when nothing is hidden by
  the checkbox, keep the short form. Update `programs.md` (EN + RU).

### 4. Help (mandatory, EN + RU — D-026)
- New topic `findings.md`: purpose, what is listed and what is never listed (personal/protected),
  each filter, each column, the details panel, the total and why nested items are not counted twice,
  the three actions, tips ("start with High and the biggest items", "Medium: check the explanation
  and Programs first", "Low is hidden because the program is probably still used").
- Mention that cleanup will come later on this page (say "planned", no phase numbers).
- `index.md`: topic list, navigation order and order of work (Scan → Findings, with Results /
  Programs for checking). `results.md` / `programs.md` where they mention neighbouring pages.

### 5. Tests
- Builder: leftovers included by verdict; top-most cache only; protected/personal (also by ancestor)
  never listed; links / cloud / empty excluded; leftover+cache listed once.
- Totals: nested visible items not double counted; nested item counted when the outer one is
  filtered out.
- Filters and search; default filter state; sorting by raw values; details text; empty states.
- Show in Results: navigation to the parent and selection of the row.
- Programs status text with and without hidden entries.
- Help consistency tests keep passing (new topic in both languages, links valid).

## Out of scope

- Selecting and deleting (Phase 3), new rules (T-007), saving filters.

## Acceptance criteria

- [ ] Findings page with filters, search, sortable table, details panel and correct totals.
- [ ] Nothing personal or protected ever appears on the page.
- [ ] Open in Explorer, Copy path, Show in Results work.
- [ ] Programs status explains hidden entries (#11).
- [ ] Help `findings` (new), `index`, `programs` (and neighbours) updated in EN + RU and match the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. **Findings** before a scan: message + **Go to Scan**.
2. After a scan: the list shows leftovers and caches, biggest first; the total at the bottom.
3. Tick **Low** — more rows appear; untick **Cache** — cache rows disappear and the total changes.
4. Type a program name in **Search** — only matching rows remain.
5. Select a row — the details panel explains why it is listed. Check a few High/Medium items.
6. **Show in Results** — Results opens in the parent folder with the item selected.
7. **Open in Explorer** and **Copy path** work.
8. No Documents/Desktop/… or AppData root folders anywhere in the list.
9. **Programs**: the status under the table now explains how many entries are hidden.
10. Press **?** on Findings and read the help in both languages.

## Notes for the executor

- Safety: the builder must re-check protection itself (do not rely only on the classifier).
- Keep all logic in Core / view models; the view only binds and forwards input.
- Remember: WPF `System.IO` pitfall, xUnit1051/xUnit1031, test folders named like personal folders
  are personal, every `IScanService` fake must implement new members.

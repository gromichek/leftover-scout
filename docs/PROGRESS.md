# Progress

Owned by the lead. Updated after every merged PR.

## Current state

- Phase 0 (Foundation) done. CI `build-and-test` is a required check on `main`.
- Phase 1: T-001 and T-002 accepted by the owner (manual tests passed 2026-11-07).
- T-018 (in-app help EN + RU) accepted by the owner (2026-11-07).
- T-003 (Results page) accepted by the owner (2026-11-08); D-031…D-034 accepted.
- T-004 (installed programs + Programs page) accepted by the owner (2026-11-09) with finding #7.
- Next task: T-005 (rules knowledge base) — task file written, session not started yet.

## Done

| Date | ID | Summary | PR |
|---|---|---|---|
| 2026-11-07 | T-000 | Project documents, CI (windows-latest, .NET 10), solution skeleton with 8 passing tests | #1 |
| 2026-11-07 | T-001 | App shell: DI + file logging, light Fluent theme, manifest, single instance, portable data folder + settings, left navigation, About page; 47 tests. Lead fixed 4 build errors (WPF `Path` ambiguity, xUnit1051) | #4 |
| 2026-11-07 | T-002 | Read-only scan engine (FindFirstFileExW, link/cloud rules, folder tree, large files, personal marking), Scan page; findings #1, #2 fixed; 134 tests, CI green on first run | #7 |
| 2026-11-07 | T-018 | In-app help EN + RU (Markdown topics embedded, Markdig renderer, Help page with EN/RU switch, ? button + F1 on every page), help consistency tests; finding #3 fixed; 238 tests. Lead fixed a `System.IO` build error | #11 |
| 2026-11-07 | T-003 | Results page: folder browser (breadcrumb, Up, sortable columns, % of parent, Notes tags, files-in-this-folder row), Large files tab, Open in Explorer / Copy path, `LastResultChanged`; help EN + RU; 306 tests. Lead fixed build errors (WPF clipboard has no retry overload, missing usings, test fake) | #14 |
| 2026-11-08 | T-004 | Evidence collector (Uninstall registry HKLM/HKCU 64/32, Store packages via PackageManager, program folders, Start menu names) run with every scan; Programs page; TFM `net10.0-windows10.0.19041.0`; finding #5 fixed; 427 tests, CI green on first run | #17 |

## Collected for the next session task

Findings from the owner's manual tests and review follow-ups. They are **not** fixed one by
one; the lead bundles them into the next suitable task. Only real breakage (crash, data loss)
is fixed immediately.

| # | Date | Source | Finding | Planned in |
|---|---|---|---|---|
| 1 | 2026-11-07 | Lead review #4 | Placeholder pages and user guide say Cleanup/History arrive in "Phase 2"; ROADMAP puts them in Phase 3 (`ServiceRegistration.cs`, `getting-started.md`) | T-002 ✅ |
| 2 | 2026-11-07 | Lead review #4 | `WindowPlacementService` treats the whole virtual screen as one area; with monitors of different sizes a window can be restored into an invisible gap. Use per-monitor work areas | T-002 ✅ |
| 3 | 2026-11-07 | Lead review #7 | `MonitorWorkAreas` lives in `LeftoverScout.Windows.Scanning` but has nothing to do with scanning; move to a `UI` namespace/folder | T-018 ✅ |
| 4 | 2026-11-07 | Lead review #7 | Monitor work areas are converted from pixels to DIPs with one (system) DPI scale; on mixed-DPI setups the visibility check is approximate. Low priority | later |
| 6 | 2026-11-08 | Lead review #17 | Size of the Windows SDK projection assemblies in the published exe not measured yet; check when the release build (T-015) exists | T-015 |
| 7 | 2026-11-09 | Owner test T-004 | Programs: the first row cannot be selected with the mouse and has no context menu (keyboard works). Cause: the empty-state `TextBlock` (`Margin="4,36,0,0"`, `ProgramsView.xaml`) lies over the grid and catches mouse clicks even when empty. Fix: `IsHitTestVisible="False"` and collapse it when there is no message; add the same check to any future overlay | T-005 |
| 5 | 2026-11-07 | Executor note #14 | Results: the default sort (Size descending) is applied by pre-sorting; the Size column header shows no sort arrow until the user clicks a header | T-004 ✅ |

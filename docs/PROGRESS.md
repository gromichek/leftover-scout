# Progress

Owned by the lead. Updated after every merged PR.

## Current state

- Phase 0 (Foundation) done. CI `build-and-test` is a required check on `main`.
- Phase 1: T-001 and T-002 accepted by the owner (manual tests passed 2026-11-07).
- T-018 (in-app help EN + RU) accepted by the owner (2026-11-07).
- Next task: T-003 (Results page) — task file written, session not started yet.

## Done

| Date | ID | Summary | PR |
|---|---|---|---|
| 2026-11-07 | T-000 | Project documents, CI (windows-latest, .NET 10), solution skeleton with 8 passing tests | #1 |
| 2026-11-07 | T-001 | App shell: DI + file logging, light Fluent theme, manifest, single instance, portable data folder + settings, left navigation, About page; 47 tests. Lead fixed 4 build errors (WPF `Path` ambiguity, xUnit1051) | #4 |
| 2026-11-07 | T-002 | Read-only scan engine (FindFirstFileExW, link/cloud rules, folder tree, large files, personal marking), Scan page; findings #1, #2 fixed; 134 tests, CI green on first run | #7 |
| 2026-11-07 | T-018 | In-app help EN + RU (Markdown topics embedded, Markdig renderer, Help page with EN/RU switch, ? button + F1 on every page), help consistency tests; finding #3 fixed; 238 tests. Lead fixed a `System.IO` build error | #11 |

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

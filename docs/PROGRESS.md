# Progress

Owned by the lead. Updated after every merged PR.

## Current state

- Phase 0 (Foundation) done. CI `build-and-test` is a required check on `main`.
- Phase 1: T-001 (app shell) merged; owner's manual test passed (2026-11-07).
- Next task: T-002 (scan engine + Scan page) — task file written, session not started yet.

## Done

| Date | ID | Summary | PR |
|---|---|---|---|
| 2026-11-07 | T-000 | Project documents, CI (windows-latest, .NET 10), solution skeleton with 8 passing tests | #1 |
| 2026-11-07 | T-001 | App shell: DI + file logging, light Fluent theme, manifest, single instance, portable data folder + settings, left navigation, About page; 47 tests. Lead fixed 4 build errors (WPF `Path` ambiguity, xUnit1051) | #4 |

## Collected for the next session task

Findings from the owner's manual tests and review follow-ups. They are **not** fixed one by
one; the lead bundles them into the next suitable task. Only real breakage (crash, data loss)
is fixed immediately.

| # | Date | Source | Finding | Planned in |
|---|---|---|---|---|
| 1 | 2026-11-07 | Lead review #4 | Placeholder pages and user guide say Cleanup/History arrive in "Phase 2"; ROADMAP puts them in Phase 3 (`ServiceRegistration.cs`, `getting-started.md`) | T-002 |
| 2 | 2026-11-07 | Lead review #4 | `WindowPlacementService` treats the whole virtual screen as one area; with monitors of different sizes a window can be restored into an invisible gap. Use per-monitor work areas | T-002 |

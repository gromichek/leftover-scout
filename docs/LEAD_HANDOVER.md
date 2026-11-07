# Lead handover

This file lets a new lead session continue the project using this file alone.
Read it fully, then `CLAUDE.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/PROGRESS.md`.

## Project in one paragraph

LeftoverScout (repo `leftover-scout`, public, MIT) is a WPF/.NET 10 portable desktop app that
scans the current user's Windows profile, finds data left behind by uninstalled programs and
safe-to-clear caches, explains why each item is suspected, and removes chosen items to the
Recycle Bin. The owner is a hobbyist C# developer who tests manually on Windows.

## Roles

- **Owner**: makes all decisions, starts executor sessions, tests manually.
- **Lead** (you): writes task files, reviews and merges PRs, keeps the documents current.
- **Executor**: one session per task, follows `CLAUDE.md`, opens one PR.

## Communication

- With the owner: **Russian** in chat.
- In the repository: **English** only, no exceptions (D-006).

## Lead workflow

1. **Task** = `docs/tasks/T-NNN-short-name.md` from `docs/tasks/_TEMPLATE.md`. Merge it into
   `main` (via PR) **before** the owner starts the session. One task, one session; split big
   work into `a/b/c`.
2. **Review a PR** against the task and `CLAUDE.md`: scope, safety rules, licenses of new
   packages, tests, user guide, no personal data. Wait for green CI.
   Fix small build errors and conflicts yourself; everything else goes to a follow-up task.
   Merge with **squash**.
3. **After merge**: small PR with document updates (PROGRESS, this file, ARCHITECTURE if needed,
   ROADMAP status).
4. **Report to the owner** (in Russian): what changed (short) and numbered manual test steps
   that take a few minutes.
5. **Owner findings**: do not fix one by one. Collect them in PROGRESS → "Collected for the
   next session task" and hand them over as one task. Fix immediately only real breakage
   (crash, data loss).
6. **Decisions**: record every owner decision in the table below with the date. Never revisit
   a decision without the owner.
7. Before assuming framework behavior, read its documentation or source code.
8. Architectural questions go to the owner — do not guess.

## Repository and GitHub settings (verified 2026-11-07)

- Repository: `gromichek/leftover-scout`, public, default branch `main`, MIT license,
  `.gitignore` VisualStudio template.
- Merging: squash only (title = PR title); merge commits and rebase disabled;
  head branches deleted automatically.
- Ruleset `protect-main` (active, no bypass, default branch): restrict deletions, block force
  pushes, require PR (0 approvals, squash only, extra approval for unattributed changes **off**),
  required status check `build-and-test` (GitHub Actions; added 2026-11-07).
  Note: GitHub defaults `require_extra_approval_for_unattributed_changes` to true; keep it false,
  otherwise agent PRs may be blocked.
- If the CI job is renamed, update the required check in the ruleset at the same time,
  otherwise every PR is blocked.
- Actions: only actions created by GitHub (and the owner's own); workflow token read-only;
  Actions may not create/approve PRs.
- The Claude GitHub App is installed for this repository only.
- Lead sessions can read settings via `gh api`; Actions settings endpoints are blocked by the
  session proxy — check those via owner screenshots.

## Current state

See `docs/PROGRESS.md`. At the time of writing: T-000 merged (#1); T-001 task file written
(`docs/tasks/T-001-app-shell.md`), waiting for the owner to start the executor session.

## Decision table

| ID | Date | Decision | Notes |
|---|---|---|---|
| D-001 | 2026-11-07 | Target **.NET 10 LTS** instead of .NET 8 | .NET 8 support ends 2026-11-10; .NET 10 supported until Nov 2028 |
| D-002 | 2026-11-07 | Scan **files in `%USERPROFILE%`** plus **read-only** registry/Appx evidence. No registry cleaning | |
| D-003 | 2026-11-07 | Phase 1: **current user only**, runs `asInvoker`. Administrator mode with other profiles is a later phase; code uses `ScanTarget` from day one | Reasons: other users' hives not loaded, Recycle Bin belongs to the deleting user, locked files, ownership changes |
| D-004 | 2026-11-07 | Delete to **Recycle Bin** by default; explicit "delete permanently" option with double confirmation; session journal; protected list | |
| D-005 | 2026-11-07 | **Offline** knowledge base (JSON rules in repo) + heuristics. No internet, no telemetry | |
| D-006 | 2026-11-07 | **English only**: UI, user guide and everything in the repository | Owner: fewer terminology problems |
| D-007 | 2026-11-07 | **Portable self-contained single-file exe**, Windows 10 22H2+ and 11, x64 | ARM64 may be added later cheaply |
| D-008 | 2026-11-07 | Name **LeftoverScout** / repo `leftover-scout`; **public**; **MIT** | |
| D-009 | 2026-11-07 | MVVM with **CommunityToolkit.Mvvm** | MIT |
| D-010 | 2026-11-07 | Tests with **xUnit v3** | Apache-2.0 |
| D-011 | 2026-11-07 | **Built-in WPF Fluent theme** | Light only — see D-016 |
| D-012 | 2026-11-07 | Settings and journals in **`data\` next to the exe**; fallback `%LOCALAPPDATA%\LeftoverScout` with a warning to the user | Portable: no traces when the app folder is deleted |
| D-013 | 2026-11-07 | Solution layout: `Core` (net10.0) / `Windows` / `App` + two test projects; `rules/` for JSON | See ARCHITECTURE.md |
| D-014 | 2026-11-07 | Only free permissive packages (MIT, Apache-2.0, BSD); no GPL, no commercial | |
| D-015 | 2026-11-07 | T-000 includes a buildable solution skeleton so CI can become a required check | |
| D-016 | 2026-11-07 | Theme: built-in Fluent, **light only** (refines D-011; no dark mode, no system following) | Use the `Fluent.Light.xaml` dictionary; `ThemeMode` is experimental (WPF0001) |
| D-017 | 2026-11-07 | **Microsoft.Extensions.DependencyInjection + Microsoft.Extensions.Logging** with a small own file logger in `data\logs` | No Generic Host |
| D-018 | 2026-11-07 | Main window: **left navigation** (Scan / Results / Cleanup / History / About) + content area | |
| D-019 | 2026-11-07 | **Single instance**; a second start brings the existing window to the front | |

## Notes for the next lead

- The session environment has no .NET SDK and cannot reach nuget.org; builds are verified by CI only.
- `actions/checkout@v6` and `actions/setup-dotnet@v5` are used (Node 24 based).
- `gh pr create` fails (GraphQL is blocked in sessions). Use REST:
  `gh api repos/gromichek/leftover-scout/pulls -f title=… -f head=… -f base=main -F body=@file`;
  merge with `gh api -X PUT repos/gromichek/leftover-scout/pulls/N/merge -f merge_method=squash -f sha=<head sha>`.
- CI job logs are readable via `gh api repos/gromichek/leftover-scout/actions/jobs/<check-run id>/logs`.

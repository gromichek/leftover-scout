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
- In the repository: **English** (D-006); only the user help is also in Russian (D-024).

## Lead workflow

1. **Task** = `docs/tasks/T-NNN-short-name.md` from `docs/tasks/_TEMPLATE.md`. Merge it into
   `main` (via PR) **before** the owner starts the session. One task, one session; split big
   work into `a/b/c`. Every task lists the help sections (EN + RU) it must add or change (D-026).
2. **Review a PR** against the task and `CLAUDE.md`: scope, safety rules, licenses of new
   packages, tests, **help matches the app in both languages** (D-026), no personal data.
   Wait for green CI.
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

See `docs/PROGRESS.md`. At the time of writing: T-004 accepted by the owner (finding #7 collected).
T-005 merged (#21), owner's manual test pending. Next: T-006 (orphan detection) — ask the owner first.

Lead note: manual test steps must describe only what the help/app promise. (T-004 step 5 wrongly said Calculator might be hidden by the system filter; it is a normal Store app.)

Process note: this time the executor session could open the PR itself (#14) — check for an
existing PR before opening one.

### Reviewing help (D-026)

For every PR: read the changed `en/` and `ru/` topics side by side, check they say the same,
and grep that every UI label the help names in bold exists in the code (pages, buttons, summary
lines). Executors repeatedly forget the WPF `System.IO` implicit-using pitfall — check CI logs
for CS0246 on `Stream`/`File`/`Path` first.

### Notes for future task files

- Personal marking (D-020) sets `FolderNode.IsPersonal` only on the personal folder itself, not
  on its descendants. Protection/classification (T-006, T-009) must check ancestors.
- Ownership rules exist for tools that have **no installer** (e.g. Gradle `.gradle`); for npm/pip/NuGet
  caches there may be no evidence at all. T-006 must not call such folders leftovers just because
  no installed program matches — use the rule category (cache) instead, and treat missing evidence
  as "unknown", not "orphan", unless the rule says the program is normally installed.
- Review every rule in rule PRs (paths + description + source); wrong rules are the main data-loss risk.
- The scanner keeps the last result in `IScanService.LastResult` (singleton) — T-003 reads it.

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
| D-020 | 2026-11-07 | Scan the **whole profile**; personal folders (Documents, Desktop, Pictures, Videos, Music, Downloads, OneDrive) are measured and marked personal (later protected) | |
| D-021 | 2026-11-07 | Scan result is a **folder tree** with aggregates; files are not stored except **large files** (≥ 100 MB) | Memory: profiles can contain millions of files |
| D-022 | 2026-11-07 | Size = **logical size**; cloud-only (online-only) files counted **separately**, not as local size | No size-on-disk calculation |
| D-023 | 2026-11-07 | T-002 includes a simple **Scan page** (start, cancel, progress, summary) | |
| D-024 | 2026-11-07 | User help in **English and Russian**. Only exception to D-006; everything else stays English | Owner request after T-002 |
| D-025 | 2026-11-07 | **Help** section in the left navigation, and a help link on **every page** that opens the matching help section | |
| D-026 | 2026-11-07 | **Rule:** every change/addition to the app updates the help (both languages) in the same PR so it always matches the app | Enforced in CLAUDE.md, PR template, lead review |
| D-027 | 2026-11-07 | Help source = Markdown in `docs/user-guide/en|ru/`, embedded in the exe; **Markdig** (BSD-2-Clause) + own FlowDocument renderer | One source for GitHub and the app |
| D-028 | 2026-11-07 | Help language follows the Windows UI language at first open; EN / RU switch, remembered | |
| D-029 | 2026-11-07 | **UI stays English**; Russian help names UI elements in English, bold | |
| D-030 | 2026-11-07 | **?** button top-right on every page + **F1** open the help topic of the current page | |
| D-031 | 2026-11-07 | Results: **one folder at a time** + breadcrumb + Up (not a multi-level tree) | Accepted 2026-11-08 |
| D-032 | 2026-11-07 | Results columns: Name, Size, % of parent (bar), Files, Last modified, Notes (Personal / Link / Cloud / Inaccessible); no treemap | Accepted 2026-11-08 |
| D-033 | 2026-11-07 | Results has a **Large files** tab | Accepted 2026-11-08 |
| D-034 | 2026-11-07 | Row actions: **Open in Explorer** (file selected) and **Copy path** | Accepted 2026-11-08 together with D-031…D-033 ("convenient for now") |
| D-035 | 2026-11-08 | Evidence sources: Uninstall registry (HKLM + HKCU, 64/32-bit), Store packages, Program folders names, Start menu shortcut names (never opened) | |
| D-036 | 2026-11-08 | Store packages via `PackageManager`; Windows/App (+ tests) target `net10.0-windows10.0.19041.0` | Current user needs no admin (MS docs) |
| D-037 | 2026-11-08 | New **Programs** page between Results and Cleanup | |
| D-038 | 2026-11-08 | Evidence collected automatically as part of every scan; failing source never fails the scan | |
| D-039 | 2026-11-08 | After a **cancelled** scan, pages keep showing the previous completed result | |
| D-040 | 2026-11-09 | Rules = JSON files in `rules/`, embedded in the app; no user-editable rules for now | |
| D-041 | 2026-11-09 | Rule kinds: ownership, cache (cache/temp/logs/crashDumps), protected; path tokens `{Profile}`, `{RoamingAppData}`, `{LocalAppData}`, `{LocalLow}` + `*` for one folder; no regex | Protection is absolute (personal folders included) |
| D-042 | 2026-11-09 | Initial rule set of 40–60 well-known rules, each with description and source; never invent paths | |
| D-043 | 2026-11-09 | Results Notes shows the matched rule (Cache/Owner/Protected…) with a tooltip | |

## Notes for the next lead

- The session environment has no .NET SDK and cannot reach nuget.org; builds are verified by CI only.
- `actions/checkout@v6` and `actions/setup-dotnet@v5` are used (Node 24 based).
- `gh pr create` fails (GraphQL is blocked in sessions). Use REST:
  `gh api repos/gromichek/leftover-scout/pulls -f title=… -f head=… -f base=main -F body=@file`;
  merge with `gh api -X PUT repos/gromichek/leftover-scout/pulls/N/merge -f merge_method=squash -f sha=<head sha>`.
- Executor sessions get a branch name from the session (`claude/<random>`), not `t-NNN-…`, and
  cannot open PRs. Expected flow: the executor pushes and reports the branch; the lead finds it
  via `gh api repos/gromichek/leftover-scout/branches`, opens the PR (title `T-NNN: …`) and reviews.
  Fetch such branches with an explicit refspec:
  `git fetch origin +refs/heads/<branch>:refs/remotes/origin/<alias>`.
- Executor sessions have no .NET SDK either, so their first CI run may fail on compile errors;
  the lead fixes small ones directly on the branch.
- CI job logs are readable via `gh api repos/gromichek/leftover-scout/actions/jobs/<check-run id>/logs`.

# CLAUDE.md — rules for every session in this repository

LeftoverScout is a WPF (.NET 10) desktop app that finds and safely cleans up leftover data
from uninstalled programs in the Windows user profile (`%USERPROFILE%`).

Read this file completely before you start. Then read, in this order:
1. Your task file `docs/tasks/T-NNN-*.md` — the only scope you work on.
2. `docs/ARCHITECTURE.md` — layers, safety model, where code belongs.
3. The decision table in `docs/LEAD_HANDOVER.md` — decisions you must not change.

## Roles

- **Owner** (human): makes decisions, starts sessions, tests manually on Windows.
- **Lead** (a Claude session): writes tasks, reviews and merges PRs, maintains project documents.
- **Executor** (you, unless your task says otherwise): implements exactly one task and opens one PR.

## Workflow for an executor session

1. Start from the latest `main`. Create a branch named `t-NNN-short-name` (same as the task file name).
2. Implement only what the task asks. Anything else you notice goes into the PR section
   "Follow-ups" — do not fix it in this PR.
3. If the task is unclear or contradicts this file or ARCHITECTURE.md: **do not guess**.
   Implement the unambiguous part, and list the question under "Open questions" in the PR.
4. Before you open the PR, run locally and make sure both pass:
   ```
   dotnet build LeftoverScout.slnx --configuration Release
   dotnet test LeftoverScout.slnx --configuration Release
   ```
   If you cannot run them (e.g. no Windows/.NET in your environment), say so in the PR.
5. Open one PR titled `T-NNN: <short summary>` and fill in `.github/pull_request_template.md`.
6. Do not merge your own PR. Do not edit `docs/PROGRESS.md`, `docs/LEAD_HANDOVER.md` or
   `docs/ROADMAP.md` — the lead owns them.

## Language

- Everything in the repository is in **English**: code, identifiers, comments, XML docs,
  UI texts, log messages, commit messages, PR texts, documents, user guide.
- There are no exceptions (decision D-006).

## Safety rules (non-negotiable)

This app deletes user files. A bug can destroy someone's data.

- **Scanning is read-only.** Scanning code never writes, moves, renames or deletes anything,
  and never changes attributes or timestamps.
- **Only the cleanup service deletes.** All deletion goes through the single cleanup service
  in `LeftoverScout.Windows` (see ARCHITECTURE.md). No other code calls `File.Delete`,
  `Directory.Delete` or similar APIs on user data.
- **Recycle Bin by default.** Permanent deletion only through the explicit option with double
  confirmation (decision D-004).
- **Protected list wins.** Anything matched by the protected list is never offered for deletion.
- **Do not follow reparse points** (junctions, symlinks) during scan or deletion.
- **Do not hydrate cloud files.** Never open/read OneDrive placeholder files just to inspect
  them; use file attributes and metadata only.
- **Tests never touch the real profile.** Tests use temporary folders created by the test
  and fictional names (e.g. `SampleUser`). Registry tests read only; they never write.

## Privacy and secrets (public repository)

- No personal data in git: no real user names, machine names, e-mail addresses, real paths
  from someone's computer, screenshots with personal data, logs from a real machine.
- No keys, tokens, passwords or connection strings — not even in tests or comments.
- The app works offline. It never sends data anywhere and has no telemetry (decision D-005).

## Dependencies

- Only free packages with a permissive license: **MIT, Apache-2.0, BSD**. No GPL/LGPL/AGPL,
  no commercial or "free for non-commercial use" packages.
- Prefer what .NET already provides. Every new package needs a reason in the PR.
- Versions go into `Directory.Packages.props` (central package management), never into a `.csproj`.
- Add each new package to "Third-party packages" in `docs/ARCHITECTURE.md` with its license.

## Code conventions

- C# latest, nullable enabled, warnings are errors. Fix warnings; do not suppress them
  without a comment explaining why.
- File-scoped namespaces; one public type per file; folder = namespace.
- MVVM with CommunityToolkit.Mvvm (decision D-009). No logic in code-behind except pure view code.
- Core (`LeftoverScout.Core`) must not reference Windows-only APIs. Put them in
  `LeftoverScout.Windows` behind an interface defined in Core.
- Long-running work is async, cancellable (`CancellationToken`) and reports progress.
  The UI thread must never block.
- Public types and members get XML doc comments.
- Before relying on assumed framework behavior (WPF, file system, Shell APIs), check the
  official documentation or the framework source code, and mention the source in the PR.

## Tests

- xUnit v3 (decision D-010). Every task adds or updates tests for the logic it touches.
- Core logic is tested without the real file system (use abstractions or temp folders).
- Name tests `Method_Condition_ExpectedResult` or a clear sentence-like name.

## User guide

- If your change alters what the user sees or does, update `docs/user-guide/` in the same PR.

## Commits

- English, imperative mood, short subject line (e.g. `Add size calculation to scanner`).
- Small, logical commits are welcome; the PR is squash-merged anyway.

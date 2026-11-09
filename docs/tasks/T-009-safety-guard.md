# T-009: Safety guard — protected list and checks before deletion

- **Phase:** 3 — Safe cleanup (first task)
- **Branch:** `t-009-safety-guard` (or the branch your session assigned)
- **Depends on:** T-008 (merged)

## Goal

Build the one gate that every future deletion must pass. Given a folder the user selected, the
guard answers **Allowed** or **Blocked (with reasons)**, using a **fresh** check of the real file
system and the rules — never trusting the scan result alone. This task deletes nothing and adds no
UI; T-010 (cleanup service) and T-011 (Cleanup page) will call this guard.

## Context

- Read `CLAUDE.md` ("Safety rules"), `docs/ARCHITECTURE.md`, `docs/RULES.md`, decisions D-003,
  D-004, D-020, D-041, D-049 and D-056 … D-061 in `docs/LEAD_HANDOVER.md`.
- Existing pieces to reuse: `ScanTarget`, `PersonalFolderMatcher.PersonalNames` and the known-folder
  paths, `RuleSet` / `RuleMatcher` / `PathPattern`, `EntryClassifier` (reparse and cloud rules),
  `IDirectoryReader` / `Win32DirectoryReader`, `AppDataLocation` (the app's own data folder).

## Owner decisions (Phase 3)

- **D-056** Findings get checkboxes; **nothing is preselected**; "select all visible" exists; Low can
  be selected but is flagged in the confirmation.
- **D-057** An item that does not fit into the Recycle Bin (too big, or the bin is off for that drive)
  is **not deleted**; it is skipped and reported; the user may delete it separately with
  "Delete permanently" (double confirmation).
- **D-058** Before cleanup, warn when the owning program seems to be running; locked files are
  skipped, the rest is removed; the result says "partly removed: N files in use".
- **D-059** Flow: Findings → **Clean up selected…** → Cleanup page (review, mode Recycle Bin /
  permanently with second confirmation, Start, progress, Cancel after the current item, summary).
  **Every item is re-checked right before deletion.**
- **D-060** A JSON journal per cleanup session in `data\journal\`; a **History** page with sessions,
  details and **Open Recycle Bin**. Journals stay on the user's computer.
- **D-061** Task order: T-009 guard → T-010 cleanup service → T-011 Cleanup page + selection →
  T-012 journal + History. Deleting becomes reachable from the UI only in T-011.

## Scope

### 1. Request model (Core)
- `CleanupItem`: full path, kind (leftover/cache/…), confidence, size and newest write time **as seen
  by the scan** (for warnings only).
- `CleanupRequest`: `ScanTarget` + items. A pure helper **reduces** the items: duplicates removed
  (case-insensitive, normalized), and an item inside another selected item is dropped (the outer one
  covers it) — report which were merged.

### 2. Guard result
- `GuardResult`: `IsAllowed`, list of `BlockReason` (enum + English text), list of `GuardWarning`
  (enum + English text). Any block reason ⇒ not allowed. Warnings never allow a blocked item.

### 3. Path checks (Core, pure — table-driven tests)
Block when:
1. the path is not fully qualified, contains `..` / `.` segments, wildcards, or is a device path;
2. after normalization it is **not strictly inside** the profile root (equal to the root also blocks),
   compared case-insensitively on whole segments (`C:\Users\Sam2` is not inside `C:\Users\Sam`);
3. it is one of the fixed roots: profile root, `AppData`, `AppData\Roaming`, `AppData\Local`,
   `AppData\LocalLow`;
4. it is the app's own data folder, the folder of the running exe, or inside / an ancestor of either;
5. it is personal: a known-folder path or a direct child of the profile root named like
   `PersonalFolderMatcher.PersonalNames` / starting with `OneDrive` — **or any ancestor is**;
6. it is protected by the rules: a `subtree` protected rule matches the path or any ancestor, or a
   `folder` protected rule matches the path itself. Add a path-based API for this
   (e.g. `RuleMatcher.MatchPath(relativeSegments)`), independent of a scan tree.

These checks are a **second, independent line** from the Findings builder: do not reuse its result.
Also add a small **hard-coded deny list in code** (not in JSON) for the fixed roots in rule 3 and the
Windows security folders listed in `rules/protected.json`, so that a broken or missing rule file can
never unprotect them.

### 4. Live file system checks (Windows implementation behind a Core interface)
Block when:
1. the path does not exist or is not a directory (Findings are folders);
2. the long form of the path differs from the given form in a way that changes a segment —
   resolve **8.3 short names** (`GetLongPathNameW`) and re-run the path checks on the long form
   (`APPDAT~1` must not bypass rule 3);
3. the item itself or **any ancestor between the profile root and the item** is a name-surrogate
   reparse point (junction / symlink) — a junction in the middle of the path could point anywhere;
4. the item or an ancestor is a cloud folder that is not populated (`RECALL_ON_OPEN`), or the item
   is inside a OneDrive root (personal);
5. reading the attributes fails (treat as blocked, reason "could not be checked").
Use only attribute queries (`GetFileAttributesExW` or `FindFirstFileExW` on the item itself, per
ancestor); never open files, never follow links. Use the `\\?\` prefix for long paths.

Warnings (not blocks):
- the newest write time of the folder's direct contents is newer than the scan saw
  ("changed since the scan");
- confidence Low; confidence Medium.

### 5. Combined guard
- `CleanupGuard.Check(CleanupRequest)` → per item `GuardResult`, plus the reduction info from §1.
  Order: pure checks first (cheap), live checks only for items that passed them.
- Logging: counts of allowed/blocked per reason; no paths in the log.

### 6. Tests
- Pure path checks: every block rule with positive and negative cases, including tricky inputs:
  trailing dots/spaces, mixed `/` and `\`, `\\?\` prefix, other drive, UNC, case differences,
  sibling-prefix (`Sam2`), the profile root itself, each fixed root, personal by name and by
  known-folder path, protection by ancestor subtree vs folder scope, the app's own folders.
- Deny list still protects when the rule set is empty.
- Request reduction: duplicates, nested items, unrelated items.
- Live checks in **temp folders** created by the test: missing path, file instead of folder,
  directory junction in the middle of the path (create with `mklink /J`-equivalent API; junctions do
  not need admin), junction as the item itself, long path > 260 characters, short-name resolution if
  8.3 names are enabled on the CI volume (skip with a clear reason otherwise).
- No test touches the real profile.

### 7. Documentation
- ARCHITECTURE.md: "Cleanup (planned)" → the guard: layers of protection (Findings builder,
  guard pure checks, hard-coded deny list, live checks), what blocks and what only warns.
- Help: **no change needed** (nothing user-visible yet) — say so in the PR.

## Out of scope

- Deleting anything (T-010), any UI (T-011), journal (T-012), process detection (T-010).

## Acceptance criteria

- [ ] `CleanupGuard` with pure and live checks exactly as specified; a fixed deny list in code.
- [ ] Every block rule covered by tests, including path tricks, 8.3 names (or documented skip) and
      junction ancestors.
- [ ] No code path deletes, moves or opens files.
- [ ] ARCHITECTURE updated; PR says "Help: no change needed".
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

None — nothing visible changes. The lead reviews the tests.

## Notes for the executor

- When unsure whether something should block: **block** and explain in "Open questions".
- Microsoft Learn pages may be unreachable from your session; name the APIs you relied on and the
  lead will verify them.
- Recurring mistakes to avoid: xUnit1031 (no `.GetAwaiter().GetResult()` in test methods),
  xUnit1051 (pass `TestContext.Current.CancellationToken`), missing
  `using LeftoverScout.Core.Tests.Scanning;`, fixed-list tests not updated, test folders named like
  personal folders are personal, WPF `System.IO` pitfall.

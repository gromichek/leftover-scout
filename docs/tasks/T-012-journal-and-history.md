# T-012: Cleanup journal and History page

- **Phase:** 3 — Safe cleanup (runs **before** T-011, decision D-065)
- **Branch:** `t-012-journal-and-history` (or the branch your session assigned)
- **Depends on:** T-010 (merged)

## Goal

Every cleanup session is recorded on the user's computer — what was selected, what happened to each
item and why — so that the very first real cleanup (T-011) is already traceable. A **History** page
shows the sessions. The journal is written **while** the cleanup runs, so even a crash or power loss
leaves a record of what was done.

## Context

- Read `CLAUDE.md`, `docs/ARCHITECTURE.md` (cleanup service), decisions D-012 (data folder),
  D-059, D-060, D-062 … D-065 in `docs/LEAD_HANDOVER.md`.
- `ICleanupService.RunAsync` (T-010) returns `CleanupSessionResult` with per-item `CleanupItemResult`.
- The History page exists as a placeholder; its help topic `history` says "planned".

## Decisions

- **D-060** (owner) JSON journal per session in `data\journal\`; History page with sessions, details
  and **Open Recycle Bin**; journals stay local.
- **D-065** (owner) T-012 before T-011.
- **D-066** (lead) The journal file is created **before** the first item, rewritten atomically after
  **each** item, and finalized at the end. A file that is not finalized is shown as **Interrupted**.
- **D-067** (lead) Journals are kept until the user deletes them; the app never deletes journals.

## Scope

### 1. Journal format (Core)
- File `data\journal\cleanup-<yyyyMMdd-HHmmss>-<short id>.json`, UTF-8, indented.
- Content: `schemaVersion: 1`, session `id` (GUID), app version, `startedAt` / `endedAt` (ISO 8601
  with offset), `state` (`InProgress`, `Completed`, `Cancelled`, `Failed`), `mode`, profile root,
  totals (items per outcome, bytes removed), and `items`: full path, relative path, kind, confidence,
  size at scan, outcome (or `Pending`), reason, items left, bytes removed,
  `deletedPermanentlyUnexpectedly`.
- Strongly typed DTOs + `System.Text.Json`; unknown properties rejected when reading.

### 2. Writer and reader (Core, file system behind small interfaces)
- `ICleanupJournal`: `Begin(request, mode)` → handle; `Record(handle, itemResult)`;
  `Complete(handle, sessionResult)`. Every write: temp file in the same folder, then replace (atomic).
- Writing never throws into the cleanup: failures are logged (no paths) and reported through a
  `JournalWarning` on the session result; the cleanup itself continues.
- `IJournalReader.ReadAll()` → sessions sorted newest first; a file that cannot be parsed is returned
  as an "unreadable" entry with its file name (never a crash); `InProgress` files whose session is not
  running in this process are reported as **Interrupted**.

### 3. Integration with the cleanup service
- A decorator (e.g. `JournalingCleanupService : ICleanupService`) wraps the T-010 service:
  `Begin` before the first item, `Record` after each item (use the service's per-item progress or a
  callback — add one to T-010's contract if needed, keeping its behavior), `Complete` at the end —
  also on cancel and on exceptions (`try/finally`; exception → state `Failed`).
- Register the decorator as the `ICleanupService` the app uses. Still **no UI** starts a cleanup.
- Raise an event (e.g. `SessionRecorded`) so the History page refreshes.

### 4. History page (D-060)
- Replace the placeholder. Empty state: "No cleanups yet." (plus one sentence that cleanups will be
  recorded here).
- Sessions list (newest first): **Date**, **Mode** (Recycle Bin / Permanently), **Items**,
  **Removed** (size), **Result** (e.g. "12 removed · 1 partly · 2 skipped", or **Interrupted** /
  **Unreadable journal**).
- Details of the selected session: table **Path** (relative), **Kind**, **Confidence**, **Size**,
  **Outcome**, **Reason**; a warning line when `deletedPermanentlyUnexpectedly` is set anywhere.
- Buttons: **Open Recycle Bin** (`explorer.exe shell:RecycleBinFolder` through `IShellLauncher`),
  **Open journal folder**, **Copy path** for a detail row. All read-only.
- Help link (?/F1) to `history`. No overlays that swallow clicks (`IsHitTestVisible`).

### 5. Help (mandatory, EN + RU — D-026)
- `history.md`: what is recorded and where (journal folder, local only, never sent), each column,
  outcomes explained in plain words (Removed, Partly removed, Skipped — too large / bin off / not
  recycled, Blocked, Not found, Failed, Not started), Interrupted sessions, Open Recycle Bin and how
  to restore an item from it, that the app never deletes journals.
- `index.md`: History is no longer planned; `getting-started.md`: the `journal` subfolder in the data
  folder. `cleanup.md` stays "planned".

### 6. Tests
- Writer: begin/record/complete sequence, atomic replace (no temp file left), file name format,
  write failure → warning, not exception.
- Reader: newest first, corrupt file → unreadable entry, unknown schema version → unreadable,
  `InProgress` not running → Interrupted.
- Decorator: journal written on success, on cancel (NotStarted items recorded), on exception (`Failed`).
- History view model: empty state, list texts, details, unexpected-permanent warning, refresh on event.
- Help consistency tests keep passing.

## Out of scope

- Starting a cleanup from the UI (T-011), deleting or exporting journals, emptying the Recycle Bin.

## Acceptance criteria

- [ ] Journal written before, during and after a session, atomically; interrupted sessions detectable.
- [ ] Journal failures never stop or fail a cleanup.
- [ ] History page with sessions, details, Open Recycle Bin, Open journal folder.
- [ ] Help `history` (and `index`, `getting-started`) updated in EN + RU and match the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. **History** shows "No cleanups yet." and the explanation.
2. **Open Recycle Bin** opens the Recycle Bin; **Open journal folder** opens the (possibly empty)
   `journal` folder in the data folder.
3. Press **?** on History and read the help in both languages.
(The first real session appears here after T-011.)

## Notes for the executor

- Recurring mistakes to avoid: xUnit1031, xUnit1051, missing `using LeftoverScout.Core.Tests.Scanning;`,
  fixed-list tests not updated (help topics, navigation items), test folders named like personal
  folders, WPF `System.IO` pitfall, every `ICleanupService` / `IScanService` fake must implement new members.
- Journals contain real paths: never log them, never put sample journals with real-looking user
  names into the repository (use `SampleUser`).

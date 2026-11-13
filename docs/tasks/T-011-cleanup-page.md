# T-011: Selection on Findings and the Cleanup page

- **Phase:** 3 — Safe cleanup (last task; runs after T-012 per D-065)
- **Branch:** `t-011-cleanup-page` (or the branch your session assigned)
- **Depends on:** T-012 (merged)

## Goal

The user selects items on **Findings**, reviews them on **Cleanup**, confirms, and LeftoverScout
removes them — to the Recycle Bin by default — showing progress and an honest summary. This is the
first task that lets the user delete from the UI. Everything goes through the journaling
`ICleanupService` (T-010, T-012) and the `CleanupGuard` (T-009). Nothing may bypass them.

## Context

- Read `CLAUDE.md` (safety, help rules), `docs/ARCHITECTURE.md` (guard, cleanup service, journal),
  decisions D-004, D-039, D-052 … D-067 in `docs/LEAD_HANDOVER.md`.
- Existing: Findings page (`FindingsPageViewModel`, `FindingFilter`), `CleanupGuard`
  (`CleanupGuardReport`), `IRunningProgramDetector`, `ICleanupService` (journaling decorator is the
  registered one), History page refreshes on `SessionRecorded`.

## Decisions (already taken)

- **D-056** Checkboxes on Findings, **nothing preselected**, "select all visible"; Low selectable but flagged.
- **D-057 / D-062** Items that do not fit into the Recycle Bin are skipped and reported.
- **D-058 / D-063 / D-064** Running-program warning before start (never a block); locked files → partly removed.
- **D-059** Findings → **Clean up selected…** → Cleanup page (review, mode, Start, progress, Cancel
  after the current item, summary). Permanent mode needs a **second** confirmation. Every item is
  re-checked right before deletion (the service already does this).
- **D-068** (lead) After a cleanup the scan result is **out of date**: Results and Findings show a
  banner "Results are out of date after a cleanup — scan again", and **Clean up selected…** stays
  disabled until a new scan has completed.
- **D-069** (lead) Changing filters on Findings **deselects** items that become hidden, so nothing
  hidden can be cleaned by accident. A new scan clears the selection.

## Scope

### 1. Selection on Findings (D-056, D-069)
- A checkbox column (first column); clicking the checkbox or pressing **Space** on a row toggles it.
- Buttons **Select all visible** and **Clear selection**.
- Selection line: "Selected: <n> items · <size>" — size without double counting (nested selected
  items are covered by the selected outer item; use `RequestReducer`).
- **Clean up selected…** (enabled when something is selected and the scan result is current) builds a
  `CleanupRequest` and opens the Cleanup page in the review state.
- Filters hide → deselect (D-069); new scan → clear; protected/personal never appear (as today).

### 2. Cleanup page — states
1. **Nothing selected**: text + **Go to Findings**.
2. **Review**:
   - Run `CleanupGuard.Check` and `IRunningProgramDetector` when entering the review
     (off the UI thread, short "Checking…" state).
   - Table: **Path**, **Kind**, **Confidence**, **Size**, **Status** — `Ready`, `Blocked: <reason>`
     (excluded), or warnings (`Medium confidence`, `Low confidence`, `Changed since the scan`).
   - Merged items from the reduction shown in a line ("2 items are inside other selected items and
     are included with them").
   - Running programs: a warning box "Close these programs first: Discord, …" (never blocks).
   - Mode: **Move to Recycle Bin** (default) / **Delete permanently**. Under it, for Recycle Bin:
     "Space is freed when you empty the Recycle Bin."
   - Totals of the ready items: "<n> items · <size>".
   - Buttons **Start cleanup** (disabled when nothing is ready) and **Back to Findings**.
3. **Confirmation** (via an `IDialogService` abstraction, testable):
   - Recycle Bin: "Move <n> items (<size>) to the Recycle Bin?" — plus "including <k> items with
     Low confidence" when applicable. Yes / No, default **No**.
   - Permanent: the same first question with "permanently", then a **second** dialog:
     "This cannot be undone. Delete <n> items (<size>) permanently?" Yes / No, default **No**.
4. **Running**: progress bar (items and bytes), current item (relative path), **Cancel** ("Stops after
   the current item"). While running: **Start scan** on the Scan page is disabled, and closing the
   window asks "A cleanup is running. Stop after the current item and exit?" (Yes waits for the
   current item, then closes; No keeps running).
5. **Summary**: counts per outcome in plain words, bytes removed, the Recycle Bin note, a table of
   items with **Outcome** and **Reason**, the journal warning if any, the unexpected-permanent warning
   if any; buttons **Open History**, **Open Recycle Bin**, **Scan again** (switches to Scan).
- Only the cleanup page starts `ICleanupService.RunAsync`; it passes only the **ready** items of the
  review (blocked ones never reach the service). The service re-checks anyway.

### 3. Out-of-date results (D-068)
- After any completed or cancelled cleanup: mark the current scan result stale; show the banner on
  Results and Findings; disable **Clean up selected…** until a new scan completes.

### 4. Help (mandatory, EN + RU — D-026)
- `cleanup.md`: full guide — the whole flow, every state, statuses and warnings, both modes, both
  dialogs, progress and Cancel, summary outcomes, how to restore from the Recycle Bin, safety
  promises (what is never offered, re-check before each item, journal), and **tips** ("start with
  High items", "close the programs first", "try the Recycle Bin mode first; empty the bin later").
- `findings.md`: selection, buttons, selection line, the deselect-on-filter rule, Clean up selected….
- `index.md`: Cleanup no longer planned; order of work Scan → Findings → Cleanup → History.
- `results.md` / `findings.md`: the out-of-date banner. `history.md`: link from the summary.

### 5. Tests
- Selection: toggle, select all visible, clear, deselect on filter change, clear on new scan,
  selection line without double counting, button enabled state (incl. stale result).
- Review: blocked items excluded and shown with reasons, warnings, merged line, running-program box,
  totals, Start disabled when nothing ready.
- Dialogs: Recycle Bin one dialog; permanent two dialogs; No at any step → nothing runs; Low note.
- Running: progress, Cancel → service token cancelled, Start scan disabled, exit confirmation logic.
- Summary texts for every outcome; journal / unexpected warnings; stale flag set after cleanup.
- Only ready items passed to the service (fake service records the request).
- Help consistency tests keep passing.

## Out of scope

- Emptying the Recycle Bin, scheduling, exporting reports, admin mode.

## Acceptance criteria

- [ ] Selection on Findings with D-056 / D-069 behavior.
- [ ] Cleanup page: review with guard + running programs, mode, confirmation (two dialogs for permanent),
      progress, Cancel after current item, summary.
- [ ] Only the journaling service is used; blocked items never reach it.
- [ ] Out-of-date banner and disabled Clean up selected… after a cleanup until a new scan.
- [ ] Help `cleanup`, `findings`, `index` (and neighbours) updated in EN + RU and match the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner (safe scenario)

Preparation (Explorer): in `%LOCALAPPDATA%` create two test folders, `ZzzScoutTestOne` and
`ZzzScoutTestTwo`, and put any small text file into each. Nothing else is cleaned in this test.

1. Scan → **Findings**: tick **Low**; search `ZzzScout` — both test folders appear (Low: just created).
2. Tick both → the selection line shows 2 items. Untick **Low** → both are deselected; tick **Low**
   again and select both again.
3. **Clean up selected…** → Cleanup review: both **Ready**, with the Low confidence warning.
4. Mode **Move to Recycle Bin** → **Start cleanup** → the dialog mentions Low items → answer **No**:
   nothing happens. Start again → **Yes**.
5. Summary: 2 removed; open the **Recycle Bin**: both folders are there; restore one if you like.
6. **History**: the session with both items and outcome Removed.
7. **Findings** / **Results** show "out of date — scan again"; **Clean up selected…** is disabled.
8. Create `ZzzScoutTestThree` with a file, scan, select it, choose **Delete permanently** → two
   dialogs; answer **No** in the second → nothing deleted. Repeat with **Yes** → removed, not in the
   Recycle Bin; History shows the session.
9. Press **?** on Cleanup and read the help in both languages.

## Notes for the executor

- Safety first: if something is unclear, keep the item out and ask in "Open questions".
- Recurring mistakes to avoid: xUnit1031, xUnit1051, xUnit2029, missing
  `using LeftoverScout.Core.Tests.Scanning;`, fixed-list tests (help topics, navigation), test folders
  named like personal folders, WPF `System.IO` pitfall, every fake of a changed interface updated,
  overlays with `IsHitTestVisible="False"`.

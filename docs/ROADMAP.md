# Roadmap

Owned by the lead. One task = one executor session = one PR. Large tasks are split into
parts (`T-NNNa`, `T-NNNb`, …). A task file is written in `docs/tasks/` and merged into `main`
**before** its session starts. Order inside a phase may change; scope changes need the owner. Rows are listed in execution
order; IDs are not renumbered when a task is inserted.

Status: ⬜ not started · 📝 task written · 🔄 in progress · ✅ merged

## Phase 0 — Foundation

| ID | Task | Status |
|---|---|---|
| T-000 | Project documents, CI, solution skeleton (done by the lead) | ✅ |

## Phase 1 — Read-only scan

Goal: the user sees what takes space in the profile. Nothing is deleted.

| ID | Task | Status |
|---|---|---|
| T-001 | App shell: MVVM, DI + logging, light Fluent theme, manifest (`asInvoker`), portable settings, left navigation, single instance | ✅ |
| T-002 | Scan engine (folder tree, cloud-safe, no links) + simple Scan page; includes findings #1, #2 | ✅ |
| T-018 | In-app Help (English + Russian): Help section in navigation, help link on every page, Russian help for existing pages (D-024…D-030) | ✅ |
| T-003 | Results page: folder browser (breadcrumb, sortable columns, notes), Large files tab, Open in Explorer / Copy path | ✅ |
| T-004 | Evidence: installed programs (Uninstall keys HKCU/HKLM, 32/64-bit), Appx packages of the current user, Start menu shortcuts — read-only | ⬜ |

## Phase 2 — Classification

Goal: each suspicious item gets a category, a confidence level and an explanation.

| ID | Task | Status |
|---|---|---|
| T-005 | Rules: JSON schema, loader, validation, first rule set for well-known locations | ⬜ |
| T-006 | Orphan detection: match items to evidence, confidence levels, explanation texts | ⬜ |
| T-007 | Known caches and temp locations (categories that are safe to clear) | ⬜ |
| T-008 | Classification UI: filters by category/confidence, details panel with evidence | ⬜ |

## Phase 3 — Safe cleanup

Goal: the user removes chosen items safely and can see what was done.

| ID | Task | Status |
|---|---|---|
| T-009 | Protected list and pre-deletion safety checks | ⬜ |
| T-010 | Cleanup service: Recycle Bin deletion via Shell API, locked/in-use files, per-item results | ⬜ |
| T-011 | Cleanup UI: review list, confirmation, explicit "delete permanently" with double confirmation | ⬜ |
| T-012 | Session journal and history view | ⬜ |

## Phase 4 — Polish and release

| ID | Task | Status |
|---|---|---|
| T-013 | Export a scan/cleanup report | ⬜ |
| T-014 | Review the complete help (EN + RU) end to end: order of work, tips, FAQ | ⬜ |
| T-015 | Release workflow: portable single-file x64 build on tag, checksums, GitHub Release | ⬜ |

## Phase 5 — Administrator mode

Needs a design discussion with the owner before tasks are written (see D-003).

| ID | Task | Status |
|---|---|---|
| T-016 | Relaunch elevated on request; choose another profile (`ScanTarget`) | ⬜ |
| T-017 | Evidence for profiles that are not logged on (offline registry hive) — design first | ⬜ |

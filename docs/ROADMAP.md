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
| T-004 | Evidence: installed programs (Uninstall registry, Store packages, program folders, Start menu names) + Programs page | ✅ |

## Phase 2 — Classification

Goal: each suspicious item gets a category, a confidence level and an explanation.

| ID | Task | Status |
|---|---|---|
| T-005 | Rules: JSON format, loader/validator/matcher, 40–60 initial rules, rule labels in Results Notes; includes finding #7 | ✅ |
| T-006 | Leftover detection: candidates, normalized name matching vs evidence, High/Medium/Low with explanations, Results labels; personal folders by name (#9), Spotify description (#8) | ✅ |
| T-006b | Safer package verdicts: complete Store query, vendor cross-check with installed programs, Microsoft packages max Medium (finding #10) | ✅ |
| T-007 | More rules (to ~110–130): caches/temp/logs/crash dumps and owners of popular programs, "never add" list with tests (after Phase 3) | 📝 |
| T-008 | Findings page: leftovers + caches, filters, search, details panel, totals, Show in Results; includes finding #11 | ✅ |

## Phase 3 — Safe cleanup

Goal: the user removes chosen items safely and can see what was done.

| ID | Task | Status |
|---|---|---|
| T-009 | Safety guard: request reduction, pure path checks, hard-coded deny list, live checks (8.3 names, junction ancestors, cloud) — no deletion, no UI | ✅ |
| T-010 | Cleanup service: Recycle Bin via Shell API (never silently permanent), permanent mode, locked files, running-program warning, per-item results, cancel | ✅ |
| T-012 | JSON session journal in `data\journal\` + History page (sessions, details, Open Recycle Bin) | ✅ |
| T-011 | Selection on Findings + Cleanup page: review, mode, double confirmation for permanent, progress, summary | ✅ |

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

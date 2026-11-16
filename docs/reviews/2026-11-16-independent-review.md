# Independent review — 2026-11-16

Requested by the owner. Reviewed commit: `2121605` (`main`, after PR #47; Phase 3 accepted).

**Scope:** the strategy of the app (how it finds and removes leftovers), defects and wrong
decisions in code, rules and documents, and recommendations for the lead.

**How it was done:** static reading of `docs/`, `rules/` and the source of Core, Windows and
the cleanup view model. **Nothing was built or run**: the review environment has no .NET SDK,
no Windows, and cannot reach nuget.org. Every finding below comes from reading code; findings
that depend on the content of a real profile are marked *"verify"* and need a test or a look
at a real machine before they are acted on. Two Windows behaviours were checked on the web
(sources at the end).

This file changes nothing in the app and decides nothing. Decisions stay with the owner.

---

## 1. Verdict in short

- **Deletion mechanics are the strongest part.** The guard (`CleanupGuard`,
  `CleanupPathChecker`, `ParsedPath`, `ProtectedDenyList`), the per-item re-check, the three
  layers against silent permanent deletion and the journal are careful and well tested.
  The `PreDeleteItem` veto is the same technique Electron uses. No way was found to make the
  app delete outside the profile, through a junction, or a personal folder.
- **Detection is the weakest part.** "No installed program has this folder name" is treated as
  evidence of a leftover. That is the well-known weak point of every tool of this kind, and
  the current guards around it (confidence levels, 6 months) do not cover several common
  cases: Windows' own folders in AppData, tools without an installer, valuable data
  (keys, wallets, saves, browser profiles).
- **The risk has moved.** The project invested most in "never delete the wrong *path*". The
  remaining risk is "delete the right path with the wrong *content*": a correctly guarded,
  correctly journaled removal of a folder the user needed.
- **The plan's next step (T-007, grow to 110–130 rules) multiplies two structural flaws**
  (R-02, R-03). Fix those first.

Priority order for the lead: **R-01 … R-06 before T-007 and before any public release.**

---

## 2. Findings — strategy and safety

Severity: **High** = can lose user data or the Recycle Bin safety net in normal use;
**Medium** = wrong result or bypass of a stated safety limit; **Low** = quality.

### R-01 (High) — A cleanup larger than the Recycle Bin silently loses the safety net

`CleanupService.ProcessItem` checks each item against the bin's `MaxCapacity` **on its own**
(`RecycleBinPolicy.Evaluate(settings, item.SizeBytes)`). Windows evicts the oldest bin
content when new items need room. Example: bin limit 10 GB, the user selects six caches of
4 GB each. Every item passes the pre-check and the veto, the summary and the journal say
"moved to the Recycle Bin" for all six — but only the last two can still be restored. Content
that was in the bin *before* the session is evicted too.

D-004/D-057/D-062 promise "never silently permanent"; this case is silent.

**Recommend:** in the review, compare the **sum** of the ready items per volume with the bin
capacity (and, if cheap, with what the bin already holds — `SHQueryRecycleBin`). When it does
not fit: say so before the confirmation and offer "clean only what fits" / "permanent for
caches". Record the fact in the journal.

### R-02 (High) — Cache / temp rules delete the folder itself, not its contents

A cache finding becomes a `CleanupItem` for the rule's folder, and `FolderRemover.Remove`
removes that folder. For the shipped rules this means the app tries to remove
`{LocalAppData}\Temp`, `…\Microsoft\Windows\INetCache`, `…\Microsoft\Windows\Explorer`,
`CrashDumps` **as folders**:

- `%TEMP%` normally survives only because some file in it is locked ("partly removed"). When
  it does go, programs that expect `%TEMP%` to exist fail until something recreates it.
- Special folders lose their attributes, `desktop.ini` and ACLs.
- Files created seconds ago by a running installer or editor are removed with the rest.

**Recommend (engine change, before T-007):**
1. Cache-kind items are cleaned as **"empty the folder, keep the folder"**.
2. Rules get an optional **minimum age** (e.g. `temp`: skip entries written in the last 24 h),
   as Disk Cleanup-style tools do.
3. Reconsider "Recycle Bin by default" **for caches**: recyclable caches free no space, are slow
   to move, and feed R-01. Default could be: leftovers → bin, caches → permanent after one
   clear question. Owner decision (touches D-004).

### R-03 (High) — Valuable data is offered as an ordinary leftover; protection is "optional"

Phase 3 (real deletion) is accepted, while the only planned protection for credentials is
"optional" in T-007 §3. Today:

- `{Profile}\.ssh`, `.gnupg`, `.aws`, `.azure`, `.kube`, `.docker`, `.android` (debug keystore)
  are **candidates** (dot-folders) and get `Leftover: Low` unless a program name happens to
  match. Low is hidden by default but selectable, and **Select all visible** takes it once the
  Low filter is on.
- A first-level AppData folder of an uninstalled program gets Medium after 6 months whatever
  it holds: crypto wallets (`wallet.dat`), password-manager databases, 2FA apps, mail
  profiles, game saves (`LocalLow\<Studio>\<Game>`, `.minecraft`), browser profiles
  (bookmarks, saved passwords).
- Ownership rules with `requiresInstallation: true` (`chrome-data`, `firefox-data`,
  `jetbrains-data`, `vscode-data` incl. `{Profile}\.vscode`) can rate such data **High**.

These are "leftovers" by the app's definition, but they are exactly the folders people keep on
purpose so a reinstall finds them again.

**Recommend:**
1. Make protection of credential folders **mandatory and immediate** (small data-only task):
   `.ssh`, `.gnupg`, `{RoamingAppData}\gnupg`, `.aws`, `.azure`, `.kube`, `.docker`, plus the
   same names in `ProtectedDenyList`.
2. Add a third data class next to "protected" and "cache": **"contains user data — keep unless
   sure"** (rule flag such as `valuable: true`, or a `kind: userData`). Such folders are never
   above Low, are never taken by **Select all visible**, and the explanation says *what* would
   be lost ("browser profile: bookmarks and saved passwords").
3. A cheap content heuristic without opening files: a candidate that contains well-known file
   names (`wallet.dat`, `*.kdbx`, `Login Data`, `places.sqlite`, `*.pst`, `*.ost`, `id_rsa`,
   `*.ppk`) is capped at Low with that reason. The scanner already lists names; it only has to
   keep a per-folder "has valuable file" flag (no per-file storage, D-021 stays).

### R-04 (High, verify) — Windows' own AppData folders outside `Microsoft\` are ordinary candidates

`MicrosoftChild → Unknown` protects only what lies under `…\Microsoft`. First-level system
folders have no rule and no entry in `GenericNames`, so they fall through to the time-based
verdict unless an installed program happens to share a token. By reading, this affects e.g.:

| Folder (`AppData\Local\…`) | What it is | Why removal hurts |
|---|---|---|
| `Package Cache` | per-user installer (Burn) cache | uninstall / repair of installed programs breaks |
| `VirtualStore` | UAC file virtualization | data of old programs lives here |
| `IsolatedStorage` | .NET isolated storage | settings of many small apps |
| `ConnectedDevicesPlatform`, `Comms`, `Publishers`, `PeerDistRepub`, `PlaceholderTileLogoFolder`, `D3DSCache` | Windows components | recreated at best, broken at worst |

Same mechanism for tools without an installer entry: `{RoamingAppData}\npm` (global npm
packages — Node.js registers as "Node.js"), `{RoamingAppData}\NuGet` (user config with feeds).

**Recommend:** a reviewed built-in list "Windows and platform folders" as `protected`
(`folder` or `subtree` as fits) **plus** a test fixture: a synthetic default Windows 11
profile tree (names only) for which the classifier must produce **zero** Leftover verdicts.
That fixture is the regression net this project lacks most (see P-03).

### R-05 (Medium) — An ownership rule gives High with no time check

`LeftoverClassifier.EvaluateOwnedFolder`: rule matches, hints do not match,
`requiresInstallation` → **High**, regardless of `NewestWriteTimeUtc`. A portable / zip /
Insiders install of a program that has a rule (VS Code is the obvious one) leaves no evidence,
so a folder **written yesterday** is rated High — the opposite of what the 6-month rule says
for every other path (and of the app's own "safety bias").

**Recommend:** High only when also unchanged for 6 months; otherwise Medium with
"changed recently — the program may run without an installer entry".

### R-06 (Medium) — "No verdict when evidence is incomplete" can be bypassed

The limit keys on `EvidenceSnapshot.Errors.Count == 0`, but sources swallow partial failures:

- `ProgramFolderSource` and `StartMenuSource` ignore `DirectoryListing.ErrorReason`
  ("a root that cannot be listed simply gives nothing").
- `WindowsUninstallRegistry` skips unreadable subkeys silently.

A failed listing of `Program Files` therefore yields *fewer programs, zero errors*, and the
classifier runs at full confidence on thin evidence.

The same gap exists on the scan side: `LeftoverCandidateFinder.IsEligible` looks only at the
candidate's own `IsAccessDenied` / `HasError`. A candidate with an unreadable **descendant**
has an incomplete size and newest-write time, yet can be "unchanged for 6 months".

**Recommend:** sources report partial failures as `EvidenceError`; `FolderNode` gets a
"subtree not fully read" flag set in `Aggregate`, and such candidates are Unknown.

### R-07 (Medium) — A name match on a vendor folder hides the leftovers inside it

In `LeftoverCandidateFinder.FindInAppData` a first-level folder is opened into its children
only when it matches a **publisher and nothing else** (`OnlyPublisher: true`). `Google`
matches the *name* "Google Drive", `Mozilla` matches "Mozilla Thunderbird", so the whole
folder becomes one candidate → **In use**, and an uninstalled Chrome / Firefox inside is never
evaluated. Consequences:

- Low recall exactly for the big, popular vendors.
- Second-level ownership rules (`chrome-data`, `firefox-data`) take effect only in the rare
  publisher-only case; under `Microsoft` (`edge-data`, `teams-classic-data`) never, because
  `MicrosoftChild` is always Unknown. They work as labels, not as detection.
- When nothing of the vendor is installed, the parent gets a generic time-based verdict and the
  rule knowledge (program name, description) is not used.

Safe direction (nothing wrong is deleted), but the main feature under-delivers.

**Recommend:** treat a folder as a vendor folder when an ownership rule exists for one of its
children, or when the name equals a known publisher even if a product name also contains it.

### R-08 (Medium) — Microsoft packages are recognised by name prefix, not by publisher

D-051 uses `VendorPart(family).StartsWith("microsoft")`. System and Microsoft packages that are
named differently are not covered: `MSTeams_8wekyb3d8bbwe`, `windows.immersivecontrolpanel_cw5n1h2txyewy`,
GUID-named shell packages `…_cw5n1h2txyewy`, `Clipchamp.Clipchamp_…`. If one of them is not
registered for the user and unchanged for 6 months it is **High**.

**Recommend:** key D-051 on the publisher id suffix (`_8wekyb3d8bbwe`, `_cw5n1h2txyewy`) in
addition to the name prefix.

### R-09 (Medium) — Rule `windows-inet-cache` covers Outlook's attachment working folder

`{LocalAppData}\Microsoft\Windows\INetCache` includes `Content.Outlook\<id>`, where Outlook
keeps attachments the user opened — including **edited and not yet saved-elsewhere copies**.
This fails the project's own bar (T-007: "loses no … unsynced work").

**Recommend:** narrow the rule to `INetCache\IE` and `INetCache\Low` (verify names), or exclude
`Content.Outlook`; add it to the "never add" tests.

### R-10 (Medium) — A scan result never expires

`CleanupState` marks a result stale only after a cleanup. A result from last week (the app left
open) can still be cleaned; the guard re-checks *paths*, not *evidence*. A program installed
after the scan keeps its "Leftover" verdict. The "changed since the scan" warning looks only at
the folder's direct entries.

**Recommend:** results older than N hours (owner picks N; 24 is a sane start) require a new
scan before **Clean up selected…**; cheap, uses `ScanResult.CompletedAt`.

### R-11 (Low) — Smaller points

- **Running programs are used only as a warning at cleanup**, never as evidence at scan time.
  A process whose image lies inside a candidate, or whose name equals it, is the strongest
  "in use" signal available and costs one process list. Portable apps would benefit most.
- **Evidence roots are fixed** (Program Files, `Local\Programs`). Common others: scoop
  (`{Profile}\scoop\apps`), `%LOCALAPPDATA%\Microsoft\WinGet\Packages`, Chocolatey
  (`%ProgramData%\chocolatey\lib`), `App Paths` registry keys.
- **`BytesRemoved` is the scan size**, not what was removed (exact only for `PartlyRemoved`).
- **Cancel works between items only**; one huge folder in permanent mode cannot be stopped.
- **Journal rewrite after every item** is O(n²) text for large sessions; fine now, note it.
- **History cannot restore.** The journal knows what went to the bin; "Restore this session"
  is the natural completion of the Recycle Bin promise (later feature).
- **Memory:** every `FolderNode` keeps its own `FullPath` string. Profiles with millions of
  folders (`node_modules`) cost hundreds of MB. Derive the path from the parent chain.
- **Match direction "entry tokens ⊂ folder tokens" with a single short token** ("Git" installed
  → `GitHubDesktop` In use). Safe direction, costs recall; require ≥ 2 tokens or equality.

---

## 3. Findings — process and documents

### P-01 — Nine of thirteen feature PRs needed fixes by the lead, mostly compile or analyzer errors

`PROGRESS.md` records lead fixes for T-001, T-018, T-003, T-006, T-008, T-009, T-010, T-012,
T-011. Cause (stated in `LEAD_HANDOVER.md`): neither executor nor lead sessions have a .NET
SDK; CI is the only compiler. Cost: an extra CI round and lead tokens per task, and the lead
edits code it is supposed to review.

**Recommend (highest process pay-off):** give sessions a working toolchain — an environment
setup that installs the .NET 10 SDK and allows `api.nuget.org` (owner: environment network
settings). Core and its tests target `net10.0` and can build and run on Linux; only the
Windows/WPF projects need CI. Until then: a Linux CI job for Core alone gives feedback in
about a minute.

### P-02 — Author and reviewer are the same kind of session with the same blind spots

Executor and lead are both Claude sessions reading the same documents; the owner tests "a few
minutes" per task. The findings above (R-01 … R-04) are of the kind this setup misses: each
piece is correct against its task file, the gap is between tasks.

**Recommend:** for every task that touches Classification, Rules or Cleanup, one extra session
with a single instruction — *"find a realistic way this change loses user data; you may not
edit code"* — before merge. Its output goes into the PR. Also schedule a review like this one
at the end of each phase, before owner acceptance.

### P-03 — Detection quality is measured on one machine, by hand

1130 tests check mechanisms; none checks *"what does the classifier say about a realistic
profile"*. Owner findings #9 and #10 (both safety issues) were found only by manual testing.

**Recommend:**
1. A **golden-profile test**: fixtures (folder names, sizes, dates, evidence lists — fictional
   but realistic) for "fresh Windows 11", "developer machine", "gamer machine". Assertions:
   zero Leftover on the fresh profile; listed known leftovers found; listed known traps not
   found. Every future owner finding becomes a fixture line.
2. **T-013 (export) first, and make it a diagnostic:** an anonymised verdict list (relative
   paths, verdict, reason; no user name) that the owner or a tester can attach to a finding.
   Logs never contain paths (good for privacy), so without the export a wrong verdict on
   someone else's machine cannot be diagnosed at all.
3. Manual tests of deletion in **Windows Sandbox or a VM**, with a script that builds a fake
   profile, rather than on the owner's working profile.

### P-04 — Release build is last; it should be first in Phase 4 (already proposed) and in CI

No release exists; the only binaries ever run are debug builds. D-007 (self-contained
single-file) has never been exercised together with the WinRT projection, `[GeneratedComClass]`
and embedded resources (finding #6 is still open). If single-file publish breaks the COM sink,
that is a deletion-path failure found late.

**Recommend:** add `dotnet publish` (single-file, self-contained, x64) to CI **now** as a
build-only step with the artifact uploaded; run the Windows integration tests against it when
T-015 lands.

### P-05 — Roadmap order: structure before data

T-007 adds ~70 rules on an engine with R-02 (folder deleted, no age filter) and without the
"valuable" class of R-03. Each new cache rule inherits both problems, and each will have to be
re-reviewed after the engine changes.

**Proposed order** (owner decides; the T-numbers are working names, the lead assigns real ones):

1. T-019 — mandatory protection: credential folders + Windows/platform folders (R-03.1, R-04), data only.
2. T-020 — classifier safety: R-05, R-06, R-08; golden-profile fixtures (P-03.1).
3. T-021 — cleanup semantics: contents-only for caches, minimum age, bin capacity per session (R-01, R-02), R-10.
4. T-015 — release build in CI (P-04).
5. T-007 — more rules, now with `valuable` / age fields available; narrow `windows-inet-cache` (R-09).
6. T-013 — export / diagnostic, then T-014.
7. Later: R-07 (vendor folders), running processes as evidence, restore from History.

### P-06 — Documents that no longer describe the project

| Where | Says | Reality |
|---|---|---|
| `README.md` | "the app cannot scan or clean anything yet" | scan, findings, cleanup, history all work |
| `ARCHITECTURE.md` header | "Status: updated in T-001 … *(planned)*" | describes T-012; no planned sections left |
| `CLAUDE.md` safety rules | "single cleanup service in `LeftoverScout.Windows`" | `CleanupService` is in Core; only `ShellEntryRemover` (Windows) deletes |
| `LEAD_HANDOVER.md` "Current state" | T-004 / T-006 narrative, "after T-007 propose Phase 4" | mixes three points in time; `PROGRESS.md` is the accurate one |
| D-013 | "two test projects" | three |
| `ShippedRulesTests` name | `…AreBetween40And60` | fine today, but encodes a number in a name; T-007 changes it |

The README matters most: it is the public face of a public repository for a tool that deletes
files. It should state the status honestly ("pre-release, test on data you can lose"), not
understate it.

### P-07 — Context cost of the documents

`ARCHITECTURE.md` is 39 KB of dense narrative and every executor must read it in full, plus a
69-row decision table. Much of it is history ("Lead docs check (2026-11-09) …").

**Recommend:** split into `docs/architecture/<area>.md` (scan, evidence, rules,
classification, cleanup, journal, help, app shell); each task file lists the two or three
areas to read. Move investigation notes into the task or PR they belong to. Keep the decision
table, but mark superseded rows (D-006 → D-024, D-061 → D-065) so nobody follows the old one.

### P-08 — Decisions taken by the lead

D-062 … D-064 and D-066 … D-069 are marked "Lead". They are sensible refinements and they are
recorded, which is the important part. Two of them are product behaviour the owner might want
to choose himself: **D-067** (journals are never deleted — they hold full paths forever) and
**D-069** (filter change silently deselects). Suggest the lead lists lead-made decisions in
each report to the owner with a one-line "say no if you disagree".

---

## 4. What is good and should stay as it is

- Guard design: pure path parsing that *rejects* instead of normalising; deny list in code,
  independent of the rule files; re-check per item; ancestors **and** descendants.
- Reparse and cloud rules in one place (`EntryClassifier`), used by scan, evidence, guard,
  remover and sizer alike.
- "Never silently permanent" in three layers, with Windows integration tests for the deletion
  path (junction target survives, both modes).
- Journal written before the first deletion, atomically, and unable to stop a cleanup.
- Logging without paths; no network; rule files validated with unknown members disallowed.
- Explanations on every verdict; the safety bias in name matching (D-045, no fuzzy matching).
- Decision log and task files: a new session really can continue from the documents.

---

## 5. Sources checked for this review

- Recycle Bin evicts the oldest items when its maximum size is reached (R-01):
  How-To Geek, "How to Change Recycle Bin Settings on Windows 10"; TenForums tutorial
  "Change Maximum Storage Size for Recycle Bin in Windows 10".
- `PreDeleteItem` without `TSF_DELETE_RECYCLE_IF_POSSIBLE` means the item cannot be recycled;
  returning a failure aborts (confirms D-062 layer 2): Electron commit "Fixing MoveItemToTrash
  behavior in Windows" (PR #3337); Microsoft Learn, `IFileOperationProgressSink::PreDeleteItem`,
  `IFileOperationProgressSink::PostDeleteItem`, `_TRANSFER_SOURCE_FLAGS`.

Not verified here (marked *verify* above): the exact verdicts for the folders in R-04 on a real
machine, and the sub-folder names proposed in R-09.

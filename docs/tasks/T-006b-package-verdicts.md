# T-006b: Safer verdicts for `AppData\Local\Packages` (finding #10)

- **Phase:** 2 — Classification (fix to T-006)
- **Branch:** `t-006b-package-verdicts` (or the branch your session assigned)
- **Depends on:** T-006 (merged)

## Goal

A package folder that belongs to an installed desktop program must never be called a leftover.
The owner found `AppData\Local\Packages\Kaspersky.ShellEx22_<id>` marked **Leftover: High**
although Kaspersky is installed (it is a shell-extension package installed together with the
desktop program). Make the Store evidence complete and make package verdicts cross-check the
other evidence. Safety first: a false "leftover" is much worse than a missed one.

## Context

- Read `CLAUDE.md`, decisions D-036, D-044 … D-047, D-050, D-051 in `docs/LEAD_HANDOVER.md`,
  finding #10 in `docs/PROGRESS.md`.
- Store evidence: `WinRtStorePackageProvider` calls `PackageManager.FindPackagesForUser(string.Empty)`.
- Package verdict: `LeftoverClassifier.EvaluatePackage` → not found + unchanged 6 months → High.

## Decisions for this task

- **D-050** A package folder that is not found among the user's packages, but whose **vendor part**
  (the text before the first `.` of the package name, e.g. `Kaspersky` in `Kaspersky.ShellEx22`)
  matches an installed program's name, publisher or registry key as a whole token, is
  **In use** ("probably belongs to installed Kaspersky") — never a leftover.
- **D-051** A package whose vendor part is `Microsoft` (or `MicrosoftCorporationII`, `MicrosoftWindows`
  and similar `Microsoft…` prefixes) and that is not found is rated **Medium at most**, never High:
  Windows installs many Microsoft packages in ways we cannot fully see.

## Scope

### 1. Complete Store evidence (investigate with docs, do not guess)
- Read Microsoft Learn for `PackageManager.FindPackagesForUser` and
  `FindPackagesForUserWithPackageTypes`. If the plain overload returns only some package types,
  switch to `FindPackagesForUserWithPackageTypes(string.Empty, <all types available on 10.0.19041>)`
  (e.g. Main, Framework, Resource, Bundle, Optional) and de-duplicate by full name.
- Find out from the docs whether packages "with external location" (sparse packages used by desktop
  apps for shell extensions) are returned; write what you found, with links, into the PR and into
  ARCHITECTURE.md ("Evidence"). If the docs do not say, write that — do not invent an answer.
- Keep per-package error handling; keep logging counts only.

### 2. Package verdict cross-check (D-050, D-051)
- In `EvaluatePackage`, when the family is not found:
  1. take the package name = folder name before the last `_`; the vendor part = text before its
     first `.` (if there is no `.`, use the whole name);
  2. normalize the vendor part; if it is generic (stop-list) or shorter than 3 characters, skip step 3;
  3. if it matches as a whole token an installed program **Name, Publisher or registry key name**
     (not Start menu or folder names) → **In use: <program>** with the explanation
     "Store package '<family>' is not registered for this user, but its name matches the installed
     program '<program>'. It probably belongs to that program.";
  4. otherwise, if the vendor part starts with `microsoft` (case-insensitive) → at most **Medium**
     with a sentence explaining why;
  5. otherwise keep the current High/Medium logic.
- `InUse` from step 3 wins over everything, like other `InUse` verdicts.

### 3. Help (EN + RU — D-026)
- `results.md`: update the **Leftover: High** description and the "How LeftoverScout decides"
  section: package folders are also compared with installed desktop programs; Microsoft packages are
  never rated High.

### 4. Tests
- Vendor extraction: `Kaspersky.ShellEx22_b81m8cbssw9gt` → `Kaspersky`; names without `.`; names
  without `_`.
- Kaspersky case: package not found, registry program "Kaspersky …" with publisher "AO Kaspersky Lab"
  → In use.
- Vendor matches only a Start menu name → still evaluated as before (not In use).
- Generic or short vendor part → no cross-check.
- `Microsoft.SomeRemovedApp_…` not found, unchanged > 6 months → **Medium**, not High.
- Unrelated vendor not found, unchanged → still High (no regression).
- Store provider: package types / de-duplication logic in a testable helper (fake package list).

## Out of scope

- Findings page (T-008), new rules (T-007), deletion.

## Acceptance criteria

- [ ] Store evidence uses the complete query per the docs; findings documented with links.
- [ ] A package folder whose vendor matches an installed program is In use, never Leftover.
- [ ] Microsoft packages that are not found are Medium at most.
- [ ] No regression for unrelated removed Store apps (still High when unchanged).
- [ ] Help `results` updated in EN + RU and matches the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. Scan → Results → `AppData\Local\Packages`: `Kaspersky.ShellEx22_…` shows **In use: …Kaspersky…**;
   hover to read why.
2. Other package folders of removed Store apps still show **Leftover** (Microsoft ones at most Medium).
3. **Programs** → Source = Store: the count may be higher than before (more package types).
4. Press **?** on Results — the Leftover: High description mentions the new cross-check (EN + RU).

## Notes for the executor

- Remember: WPF `System.IO` pitfall, xUnit1051, xUnit1031 (no blocking waits in tests), and that
  test folders named like personal folders (`Documents`, `OneDrive…`) are now personal.
- Read your code twice for compile errors before pushing if you cannot build locally.

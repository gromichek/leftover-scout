# T-006: Leftover detection (and personal folders protected by name)

- **Phase:** 2 — Classification
- **Branch:** `t-006-leftover-detection` (or the branch your session assigned)
- **Depends on:** T-005 (merged)

## Goal

For the folders where programs keep their data, decide — offline and explainably — whether the
folder probably belongs to a program that is no longer installed ("leftover"), with a confidence
level and a plain-English reason. Show it in the Results Notes. Safety first: when in doubt, the
answer is **not** a leftover.

Before that, close a safety gap found by the owner: personal folders that were **redirected** to
another drive left an ordinary, unprotected folder with the same name in the profile.

## Context

- Read `CLAUDE.md`, `docs/ARCHITECTURE.md`, `docs/RULES.md`, decisions D-020, D-035 … D-043 and
  D-044 … D-049 (this task), and the "Notes for future task files" in `docs/LEAD_HANDOVER.md`
  (tools without an installer).
- Inputs already available after a scan: folder tree (`FolderNode` with `NewestWriteTimeUtc`,
  flags), `EvidenceSnapshot` (installed programs from four sources), rule matches (`RuleMatchMap`).

## Owner decisions for this task

- **D-044** Candidates: first-level folders of `AppData\Roaming`, `AppData\Local`,
  `AppData\LocalLow`; second level when the first level is a **vendor** folder; dot-folders in the
  profile root; every folder in `AppData\Local\Packages`.
- **D-045** Name comparison: **normalized exact / whole-word** match only, no fuzzy similarity.
- **D-046** Confidence: **High / Medium / Low**, "not changed" threshold **6 months**.
- **D-047** Shown as a Notes label `Leftover: High|Medium|Low` with an explanation tooltip;
  the dedicated findings page comes in T-008.
- **D-048** Personal folders redirected outside the profile are **not** scanned (D-020 stays).
- **D-049** Standard personal folders in the profile root are **personal and protected by name**,
  wherever Windows currently points the known folder (finding #9).

## Scope

### 1. Finding #9 — personal folders by name (do this first)
- In Core, mark direct children of the profile root as personal (and therefore protected subtree,
  as today) when their name is one of: `Documents`, `Desktop`, `Downloads`, `Pictures`, `Videos`,
  `Music`, `Favorites`, `Contacts`, `Links`, `Saved Games`, `Searches`, `3D Objects`, or starts
  with `OneDrive` (covers `OneDrive - <Organization>`). Case-insensitive. This is **in addition**
  to the known-folder paths already used.
- Tests: a redirected Documents (known folder path elsewhere) still leaves `{Profile}\Documents`
  personal; `OneDrive - Contoso` is personal; `Documents2` is not.
- Help (EN + RU) `results.md`: Personal also covers these standard folders even if Windows stores
  your documents elsewhere; redirected folders on other drives are not scanned (D-048).

### 2. Finding #8 — Spotify rule description
- Rule `spotify-cache`: the description must say that Spotify may download offline songs again
  after clearing. Keep the path.

### 3. Rule format extension
- Add `requiresInstallation` (bool) to `program` in ownership rules: `true` when the program is
  normally installed (Discord, Chrome, Steam…), `false` for tools that often have no installer
  entry (e.g. Gradle). Required for ownership rules; update all existing ownership rules and
  `docs/RULES.md`; validator enforces it.

### 4. Evidence index and name normalization (Core, pure)
- Normalize a name: lower case; split into tokens on spaces, punctuation, `_`, `-`, `.` and
  camel-case/letter-digit boundaries; drop version tokens (digits only, or like `v2`, `2022`, `x64`,
  `x86`); drop company suffixes (`inc`, `ltd`, `llc`, `corp`, `corporation`, `gmbh`, `co`, `sro`, `ag`).
  Keep the token list and the joined form.
- Build an index once per scan from the evidence: program `Name`, `Publisher`, registry key name,
  Store package name and family name, program folder names and their subfolder names, Start menu
  shortcut and folder names.
- Match a folder name when: the joined normalized forms are equal, **or** all tokens of the folder
  name appear as whole tokens in one evidence entry (e.g. `JetBrains` ⊂ `JetBrains s.r.o.`),
  **or** all tokens of an evidence name appear in the folder name. Ignore tokens shorter than 3
  characters for the "contains" checks. **No fuzzy similarity.**
- Generic names never decide anything: a stop-list (e.g. `temp`, `tmp`, `cache`, `caches`, `logs`,
  `log`, `data`, `crashpad`, `crashdumps`, `updates`, `update`, `packages`, `programs`, `microsoft`,
  `windows`, `common`, `shared`, `local`, `roaming`, `default`, `user`, `users`, `app`, `apps`) →
  verdict **Unknown**.

### 5. Candidates (D-044)
- First level under Roaming, Local, LocalLow.
- **Vendor folder**: a first-level folder whose name matches only a **Publisher** (not a program
  name) → evaluate each of its subfolders as a candidate (second level) instead of the vendor folder.
  Never expand `Microsoft` (too many Windows components) — its children are **Unknown** for now.
- Dot-folders directly in the profile root (`.name`).
- Every folder directly in `AppData\Local\Packages`.
- Not candidates (verdict shown as today, no leftover label): personal, protected (any scope that
  covers the folder itself), cache/temp/logs/crash-dump rule matches, skipped links, cloud folders
  not scanned, inaccessible folders.

### 6. Verdict and confidence (D-046)
- Verdicts: `InUse` (matched installed program — remember which), `Leftover` with
  `High` / `Medium` / `Low`, `Unknown`.
- **High**:
  - `Packages\<family>` whose family name is not among the current user's Store packages and that
    has not changed for 6 months;
  - an ownership rule with `requiresInstallation: true` whose program hints match **no** evidence.
- **Medium**:
  - `Packages\<family>` not installed but changed within 6 months;
  - a candidate that matches no evidence and has not changed for 6 months, or contains no files.
- **Low**: a candidate that matches no evidence but changed within the last 6 months.
- **Unknown**: generic name, Microsoft children, ownership rule with `requiresInstallation: false`
  and no evidence, folders without a usable name.
- `InUse` wins over everything: any evidence match (or an ownership rule whose hints match) →
  not a leftover.
- "Changed" uses `NewestWriteTimeUtc` of the subtree and an injected clock (`TimeProvider`).
- Each verdict carries a short English explanation, e.g.
  "No installed program named 'FooSoft' was found. Not changed since 2023-05." or
  "Store package 'Contoso.App_abc123' is not installed for this user." or
  "Belongs to Discord (installed)."

### 7. Results (D-047)
- Notes labels: `Leftover: High`, `Leftover: Medium`, `Leftover: Low`, `In use: <program>`.
  Unknown shows no label. Tooltip: the explanation (plus existing tag explanations).
- Classification runs once after each completed scan (with rule matching) and is stored with the result.

### 8. Help (mandatory, EN + RU — D-026)
- `results.md`: the new labels, what each confidence means **in practice** (High: very likely
  safe to remove later; Medium: check first; Low: probably still used), how LeftoverScout decides
  (names compared with installed programs; nothing sent anywhere), that it errs on the safe side,
  and the personal-folder changes from section 1.
- `index.md` if the overview mentions what the app can do now.

### 9. Tests
- Normalizer: table-driven (versions, suffixes, camel case, punctuation, short tokens).
- Matcher: equal, folder ⊂ evidence, evidence ⊂ folder, stop-list, no fuzzy matches
  (e.g. `Foo` must not match `Food`).
- Candidates: levels, vendor expansion, Microsoft not expanded, dot-folders, Packages.
- Verdicts: every rule in section 6, threshold boundary (exactly 6 months), empty folder,
  `requiresInstallation` true/false, InUse precedence, non-candidates.
- Personal by name (section 1), validator for `requiresInstallation`.
- Results tags/tooltips for verdicts.

## Out of scope

- Findings page, filters, selection (T-008); known caches beyond the current rules (T-007);
  any deletion; scanning redirected folders outside the profile.

## Acceptance criteria

- [ ] `{Profile}\Documents` and the other standard folders are Personal even when redirected; OneDrive* too.
- [ ] Spotify rule description warns about re-downloading.
- [ ] `requiresInstallation` in all ownership rules; validator and RULES.md updated.
- [ ] Candidates, matching and verdicts exactly as specified; every rule covered by tests.
- [ ] Results shows Leftover/In use labels with explanations; nothing is deleted.
- [ ] Help `results` (and `index` if needed) updated in EN + RU and matches the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. Scan → **Results**: `Documents` in the profile now shows **Personal** (even though your real
   Documents is on another drive).
2. `AppData` → `Roaming` and `Local`: folders of programs you use show **In use: …**.
3. Look for **Leftover: High / Medium** labels; hover to read why. For each, ask yourself:
   is that program really uninstalled? Note any wrong ones (program installed but marked leftover).
4. `AppData\Local\Packages`: entries of removed Store apps show **Leftover**; installed ones **In use**.
5. Dot-folders in the profile root (e.g. `.vscode`, `.gradle`) get a sensible label or none.
6. Press **?** on Results and read the new section in both languages.

## Notes for the executor

- Safety bias: a false "leftover" is much worse than a missed one. When a rule here is ambiguous,
  choose the safer verdict and ask in "Open questions".
- Keep all logic in Core and pure (no file system, no registry, injected `TimeProvider`).
- Remember: WPF `System.IO` pitfall, xUnit1051, every `IScanService` fake must implement new members.

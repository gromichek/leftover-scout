# T-005: Rules knowledge base (ownership, caches, protected locations)

- **Phase:** 2 — Classification (first task)
- **Branch:** `t-005-rules` (or the branch your session assigned)
- **Depends on:** T-004 (merged)

## Goal

Give LeftoverScout offline knowledge about well-known locations in the profile: which program
owns a folder, which folders are caches/temp/logs that can be cleared, and which locations must
never be offered for deletion. Show the matched rule on the Results page so the owner can check
the rules on a real profile. Deciding "leftover or not" is T-006 — not here.

## Context

- Read `CLAUDE.md` (safety, help rules), `docs/ARCHITECTURE.md` ("Rules (planned)"), decisions
  D-005 (offline JSON rules), D-020 (personal folders become protected), D-040 … D-043 (this task)
  in `docs/LEAD_HANDOVER.md`.
- Results already shows Notes tags (`FolderTags`, `FolderTagText`, `FolderRowBuilder`).
- `ScanTarget.ProfileRootPath` is the profile root; never use `Environment` paths for rules
  (admin mode will scan other profiles later — D-003).

## Owner decisions for this task

- **D-040** Rules are JSON files in `rules/` at the repository root, **embedded** into the app.
  No user-editable rules for now.
- **D-041** Three rule kinds — **ownership** (folder → program), **cache** (folder → category
  `cache` / `temp` / `logs` / `crashDumps`), **protected** (never offered for deletion).
  Paths use tokens `{Profile}`, `{RoamingAppData}`, `{LocalAppData}`, `{LocalLow}` and `*` for
  exactly one folder name. **No regular expressions.**
- **D-042** Initial set of **40–60 well-known rules**, each with a short description and a source note.
  Only widely known locations — **never invent a path**.
- **D-043** Results **Notes** shows the matched rule (e.g. `Cache: Discord`, `Owner: Discord`,
  `Protected`) with an explanation in the tooltip.

## Scope

### 1. Rule format (`rules/*.json`)
- Suggested files: `protected.json`, `caches.json`, `ownership.json` (more files allowed).
- Each file: `{ "schemaVersion": 1, "rules": [ … ] }`. Each rule:
  - `id` — unique, kebab-case (e.g. `discord-cache`);
  - `kind` — `ownership` | `cache` | `protected`;
  - `paths` — one or more path patterns (see below);
  - `scope` — `folder` (only that folder) or `subtree` (folder and everything below); required
    for `protected`, default `subtree` otherwise;
  - `category` — for `cache`: `cache` | `temp` | `logs` | `crashDumps`;
  - `program` — for `ownership` (optional for `cache`): `displayName` plus match hints
    `names` (program/display names), `publishers`, `uninstallKeys`, `packageFamilies`,
    `programFolders` — all optional lists of plain strings (case-insensitive, no wildcards);
  - `description` — one short English sentence shown in the tooltip;
  - `source` — where the knowledge comes from (vendor docs, well-known location); not shown in the UI.
- Path patterns: start with exactly one token; segments separated by `\`; a segment is a literal
  name or `*` (one whole folder name). Not allowed: absolute paths, drive letters, `..`, `.`,
  empty segments, partial wildcards (`Cache*`), environment variables other than the four tokens.
- Token mapping relative to the profile root: `{Profile}` = root, `{RoamingAppData}` =
  `AppData\Roaming`, `{LocalAppData}` = `AppData\Local`, `{LocalLow}` = `AppData\LocalLow`.

### 2. Loader and validator (Core)
- Load the embedded files (logical names `Rules/<file>.json`, embed from `rules/` without copying),
  parse with `System.Text.Json`, validate, build a `RuleSet`.
- Validation errors (tested; the shipped files must have none): unknown kind/category/token,
  bad pattern, duplicate id, missing required field for the kind, `cache` or `ownership` path that
  lies inside a `subtree`-protected path (protection is absolute — no rule may contradict it).
- At runtime an invalid rule is skipped and logged (by id) — it never crashes the app.

### 3. Matcher (Core)
- Input: `ScanTarget`, scan result tree; output per folder: matched rules.
- Matching is on whole path segments, case-insensitive (ordinal ignore case).
- **Protected** with `subtree` applies to the folder and all descendants; with `folder` only to
  that folder. Personal folders from the scan (`IsPersonal`) are protected `subtree` automatically
  (source "personal folder") — absolute, like D-020 promised.
- Several matches: protected wins; otherwise the most specific pattern (more literal segments)
  is the primary match; all matches are kept for the tooltip.
- Efficient for large trees: walk the tree once; do not test every rule against every node naively
  if the profile has 100 000+ folders (index rules by first literal segments).

### 4. Initial rule set (D-042) — 40–60 rules
Write only locations you are confident about; when unsure, leave it out and list it under
"Follow-ups". Each rule has `description` and `source`. Cover:
- **Protected** (must have):
  - roots themselves, `folder` scope: `{Profile}\AppData`, `{RoamingAppData}`, `{LocalAppData}`, `{LocalLow}`;
  - Windows security data, `subtree`: `{RoamingAppData}\Microsoft\Protect`,
    `{RoamingAppData}\Microsoft\Credentials`, `{LocalAppData}\Microsoft\Credentials`,
    `{RoamingAppData}\Microsoft\SystemCertificates`, `{RoamingAppData}\Microsoft\Crypto`,
    `{LocalAppData}\Microsoft\Vault`;
  - `{RoamingAppData}\Microsoft\Windows\Start Menu`, `{LocalAppData}\Microsoft\WindowsApps`;
  - add others only if clearly Windows-critical.
- **Cache / temp / logs / crash dumps**: `{LocalAppData}\Temp` (temp), `{LocalAppData}\CrashDumps`
  (crashDumps), Windows thumbnail/INet caches if you are sure of the exact folder, browser caches
  (Chrome, Edge, Firefox — use `*` for profile folder names), Discord, Microsoft Teams, Zoom,
  Spotify, Steam (htmlcache), VS Code (Cache, CachedData), JetBrains caches, NVIDIA/AMD shader
  caches, npm, NuGet (`{Profile}\.nuget\packages` and http cache), pip, Gradle, Yarn — as far as
  you are sure about the paths.
- **Ownership**: the same programs' main data folders (e.g. `{RoamingAppData}\discord` → Discord)
  with match hints (`names`, `publishers`, `uninstallKeys`, `packageFamilies`, `programFolders`)
  so T-006 can compare them with the evidence from T-004.
- `docs/RULES.md` (new, English): how to write a rule, the format, tokens, validation, and the
  requirement to give a `source` — for future contributors.

### 5. Results page (D-043)
- Notes tags gain rule labels: **Protected**, **Cache: <program or category>**,
  **Temp**, **Logs**, **Crash dumps**, **Owner: <program>**. Personal folders keep **Personal**
  (help explains that personal also means protected).
- Tooltip: the explanation of each tag, plus the matched rule descriptions (primary first).
- Matching runs once after each completed scan and is stored with the result; navigation must not
  re-match.

### 6. Collected finding (PROGRESS #7)
- Programs page: the empty-state `TextBlock` lies over the grid and swallows mouse clicks on the
  first row. Set `IsHitTestVisible="False"` and collapse it when there is no message. Check the
  Results view for the same pattern.

### 7. Help (mandatory, EN + RU — D-026)
- `results.md` (EN + RU): the new Notes labels, what Protected means (never offered for deletion,
  including personal folders), a short "How does LeftoverScout know?" section (built-in rules,
  offline, will grow over time).
- `index.md` (EN + RU) only if the order of work or the overview changes.

### 8. Tests
- Validator: each error case from section 2; all shipped files load with zero errors; rule count
  in the 40–60 range; every rule has `description` and `source`.
- Patterns: tokens, `*`, case-insensitivity, no partial match on segment prefixes
  (`Cache` must not match `CacheStorage`).
- Matcher: scope folder vs subtree, protected inheritance, personal → protected, specificity,
  multiple matches, performance smoke test on a synthetic tree with 100 000 folders (fast, no timing
  asserts tighter than a few seconds).
- Results: tags and tooltip text for rule matches.
- Programs: empty-state overlay is not hit-testable (view model / visibility logic).

## Out of scope

- Deciding orphan/leftover, confidence, comparing ownership hints with evidence (T-006).
- Any deletion, selection for cleanup, user-editable rules.

## Acceptance criteria

- [ ] `rules/*.json` with 40–60 valid rules, each with description and source; `docs/RULES.md`.
- [ ] Loader/validator/matcher in Core with tests; shipped rules validated in CI.
- [ ] Protection is absolute: no cache/ownership rule inside a protected subtree; personal folders protected.
- [ ] Results Notes shows rule labels with explanations.
- [ ] Finding #7 fixed.
- [ ] Help `results` updated in EN + RU and matches the app.
- [ ] Build and tests pass (Release); CI green.

## Manual test steps for the owner

1. Run a scan → **Results** → `AppData` → `Local`: `Temp` shows **Temp**; `Microsoft` → `WindowsApps` shows **Protected**.
2. `AppData` itself shows **Protected**; hover Notes — the tooltip explains it.
3. Open a folder of a program you use that is in the rule set (e.g. Discord, Chrome, VS Code, Steam,
   if installed): its cache folder shows **Cache: …**, its main folder **Owner: …**.
4. Documents / Downloads still show **Personal**; the help says personal folders are protected.
5. **Programs**: the first row can now be selected with the mouse and has a context menu.
6. Press **?** on Results and read the new section in both languages.

## Notes for the executor

- `System.Text.Json`: use source-generated or strongly typed DTOs; reject unknown properties
  (`JsonUnmappedMemberHandling.Disallow`) so typos in rule files fail validation. Cite the docs.
- Embedded resources from outside the project folder: same approach as the help files (T-018).
- Quality over quantity: a wrong rule can later mark a needed folder as removable. If you are not
  sure about a path, leave it out and mention it in the PR.
- Remember the WPF `System.IO` pitfall and xUnit1051 (CLAUDE.md).

# T-007: More rules — caches, temp, logs, crash dumps and owners of popular programs

- **Phase:** 2 — Classification (rule data; done after Phase 3)
- **Branch:** `t-007-more-rules` (or the branch your session assigned)
- **Depends on:** T-011 (merged)

## Goal

Grow the built-in knowledge base from 57 to roughly **110–130 rules**, so that Findings shows more
folders that are safe to clear and fewer unknown ones. This task changes **data** (`rules/*.json`)
and the rule-count test — no engine changes unless a rule cannot be expressed (then ask, do not
change the format).

## Context

- Read `CLAUDE.md`, `docs/RULES.md` (format, tokens, validation, `requiresInstallation`),
  `docs/ARCHITECTURE.md`, decisions D-040 … D-043, D-049, D-050 … D-051 in `docs/LEAD_HANDOVER.md`.
- Existing files: `rules/protected.json` (16), `rules/caches.json` (28), `rules/ownership.json` (13).
- Lesson from finding #8 (Spotify): when clearing a cache has a side effect for the user (downloads
  again, sign-in again, slower first start), the **description must say so**.

## The safety bar (read twice)

A wrong rule is the main way this app could destroy data. A rule is allowed only when **all** hold:

1. The location is **widely documented** (vendor documentation, vendor support article, or a location
   that appears consistently in well-known tools/guides). Put the source in `source` — a vendor doc
   title/URL, or "well-known location" plus where it is documented. **Never invent a path.** If you
   are not sure about the exact folder name or nesting, **leave the rule out** and list it under
   "Follow-ups" in the PR with your doubt.
2. For **cache / temp / logs / crashDumps**: the program rebuilds the content automatically, and
   removing it loses **no user data, settings, sessions, keys or unsynced work**.
3. The path is as **specific** as possible (the cache folder itself, not its parent).

**Never add cache rules for** (examples of what fails rule 2): browser profile roots, cookies,
`Login Data`, `Local Storage`, `IndexedDB`, `Service Worker` of web apps that keep offline data,
Telegram `tdata`, password managers, mail stores (Outlook `.ost/.pst`, Thunderbird profiles),
Office upload/document caches (`OfficeFileCache`), `Microsoft\Windows\WebCache`, game saves,
IDE settings, SSH/GPG/cloud CLI credentials (`.ssh`, `.gnupg`, `.aws`, `.azure`, `.kube`, `.docker`
config). When in doubt: **protected or nothing**, never cache.

## Scope

### 1. New cache / temp / logs / crash-dump rules (aim: 40–55)
Cover, as far as the bar allows:
- **Windows**: Windows Error Reporting archives/queues (`{LocalAppData}\Microsoft\Windows\WER\…`),
  DirectX shader cache (`{LocalAppData}\D3DSCache`), other per-user Windows caches you can source.
- **Browsers** (cache folders only, with `*` for profile names): Opera, Opera GX, Brave, Vivaldi,
  Yandex Browser, Chromium; Edge/Chrome `Code Cache` and `GPUCache` if not yet covered.
- **Communication**: Microsoft Teams (new) package caches if documented, Skype, Viber, Zoom caches,
  Slack/Discord `Code Cache` / `GPUCache` if not yet covered.
- **Dev tools**: Maven `{Profile}\.m2\repository` (with a description that projects download again),
  Cargo `{Profile}\.cargo\registry\cache`, Go build cache `{LocalAppData}\go-build`, Gradle daemon
  logs, `.android` caches, Unity cache, JetBrains `log`/`caches` if not covered, VS Code extension
  caches (`CachedExtensionVSIXs`), Visual Studio `ComponentModelCache` (exists) — check for gaps.
- **Games/launchers**: Steam `htmlcache` (exists), Epic Games Launcher web cache, Battle.net cache,
  EA app cache, Ubisoft Connect cache — only the cache folders, never game data or saves.
- **Adobe**: media cache files / media cache database, if documented.
- **Java**: `{LocalLow}\Sun\Java\Deployment\cache`.

### 2. New ownership rules (aim: 15–25)
- Main data folders of the programs above (and other very common ones: Zoom, Skype, Viber, Telegram
  Desktop `{RoamingAppData}\Telegram Desktop` — **ownership only**, OBS Studio, Notepad++,
  7-Zip, VLC, Audacity, GIMP, Blender, Postman, Docker Desktop, Epic Games Launcher, Battle.net,
  EA app, Ubisoft Connect), with match hints (`names`, `publishers`, `uninstallKeys`,
  `packageFamilies`, `programFolders`) and `requiresInstallation` set correctly (`false` for tools
  that often run without an installer entry).

### 3. Protected additions (optional, only if clearly needed)
- Add `protected` rules only for locations whose removal would break Windows or lose credentials
  and that are not yet covered (e.g. `.ssh`, `.gnupg` — note they are dot-folders that T-006 rates
  at most Low; protection makes them never selectable). Keep scopes minimal.

### 4. Validation and tests
- All files must load with zero validation errors (existing tests).
- Update the count test to the new range (e.g. **100–140**) and keep "every rule has description
  and source".
- Add tests that the "never add" examples above do **not** match any cache rule
  (e.g. `…\User Data\Default`, `…\User Data\Default\Local Storage`, `Telegram Desktop\tdata`,
  `Microsoft\Windows\WebCache`, `.ssh`): they must be unmatched, owned or protected — never cache.
- Every new cache rule path lies outside protected subtrees (validator already enforces this).

### 5. Help and docs
- `results.md` / `findings.md` (EN + RU): update only sentences that describe the size of the rule
  list ("small today") — say it covers common Windows locations and popular programs.
- `docs/RULES.md`: add the "never add" list and the side-effect-in-description rule.
- PR: a table of all new rules (id, kind, path, one-line source) so the lead can review each one.

## Out of scope

- Engine or format changes, user-editable rules, rules for locations outside the profile.

## Acceptance criteria

- [ ] 100–140 rules in total; every new rule has description (with side effects) and source.
- [ ] No cache rule for anything on the "never add" list; tests prove the examples.
- [ ] Validator passes; count test updated; CI green.
- [ ] RULES.md updated; help sentences about the rule list updated in EN + RU.
- [ ] PR contains the table of new rules; doubtful candidates listed under Follow-ups.

## Manual test steps for the owner

1. Scan → **Findings**: more rows with **Cache** / **Temp** / **Logs** / **Crash dumps** than before
   (compare the total with the previous scan if you remember it).
2. Open a few new cache rows — the details panel shows the rule description; check that it makes
   sense for a program you use.
3. Programs you use show **In use: …** on their main folders in **Results**.
4. Nothing personal, no browser profile root, no `tdata`, no `.ssh` appears on Findings.

## Notes for the executor

- Quality over quantity: 100 correct rules are better than 130 with three wrong ones.
- Your session may not reach the web; rely on locations you are certain about, cite them, and put
  every doubt into Follow-ups — the lead will verify sources during review.
- Recurring mistakes to avoid: fixed-list/count tests not updated, xUnit2029/1031/1051.

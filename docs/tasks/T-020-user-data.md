# T-020: User data — folders that hold data people keep on purpose

- **Phase:** 3b — Safety hardening (D-070)
- **Branch:** `t-020-user-data` (or the branch your session assigned)
- **Depends on:** T-019 (merged)

## Goal

Folders that probably hold valuable user data (browser profiles, mail, game saves, password databases,
wallets, keys) are never rated above **Low**, show a **User data** tag with what would be lost, and are
never ticked by **Select all visible**. The user can still select such a folder by hand.

## Context

- Read `docs/reviews/2026-11-16-independent-review.md`, finding **R-03** points 2 and 3. Point 1 was done in T-019.
- Read `docs/RULES.md` (rule format, validation, `requiresInstallation`), D-040 … D-046, D-069, D-070, D-075 … D-078
  in `docs/LEAD_HANDOVER.md`.
- Today an ownership rule with `requiresInstallation: true` whose program is missing gives **High**
  (`LeftoverClassifier.EvaluateOwnedFolder`). A first-level AppData folder without evidence that is unchanged for
  6 months gives **Medium** (`EvaluateByTime`). Neither looks at what the folder holds.
- The scanner (`ProfileScanner`) sees every file name while listing; it stores no per-file data except the large-files
  list (D-021). This must stay so: only per-folder flags.
- Scanning is read-only and must not open files (CLAUDE.md). The heuristic below uses **names only**.

## Scope

1. **Rule field `userData`** (optional, ownership rules only): a short text that says what would be lost, e.g.
   `"userData": "browser profile: bookmarks, history, saved passwords"`. Add it to `RuleDto`, `Rule` (or
   `ProgramHints`, wherever it fits best), the loader and validator. Validation: allowed only on `ownership` rules,
   must not be empty or whitespace when present. Unknown members stay an error (as today). Document it in `docs/RULES.md`.

2. **Set `userData` on rules** in `rules/ownership.json`:
   - `chrome-data`, `edge-data`, `firefox-data`: browser profile — bookmarks, history, saved passwords.
   - New rule `thunderbird-data`: `{RoamingAppData}\Thunderbird` — mail profile: messages, address book, settings.
     `requiresInstallation: true`, program Mozilla Thunderbird.
   - New rule `minecraft-data`: `{RoamingAppData}\.minecraft` — game worlds and settings.
     `requiresInstallation: false` (several launchers exist). Give each new rule a description and a source.

3. **Valuable file names** (scan side). A new type in `LeftoverScout.Core.Scanning` holds a fixed list of file-name
   patterns, each with a short label:

   | Pattern | Label |
   |---|---|
   | `wallet.dat`, `*.wallet` | crypto wallet |
   | `*.kdbx`, `*.kdb` | password database |
   | `Login Data`, `logins.json`, `key4.db` | saved browser passwords |
   | `places.sqlite`, `Bookmarks` | browser bookmarks |
   | `*.pst`, `*.ost` | mail data file |
   | `id_rsa`, `id_ecdsa`, `id_ed25519`, `*.ppk` | private key |
   | `*.pfx`, `*.p12`, `*.jks`, `*.keystore` | certificate or signing key |

   Matching is case-insensitive on the file name only. While scanning, `FolderNode` gets the label of the first
   matching file **in its subtree**, set in `Aggregate` (e.g. `ValuableDataHint`; `null` when none). Only one string
   per folder, no list of files.

4. **Game data in LocalLow**: a candidate whose path lies under `{LocalLow}` (Unity and many other games keep saves
   and settings there) is never rated above Low; the explanation says "LocalLow usually holds game saves and settings" (D-077).

5. **Classifier**: `LeftoverVerdict` gets an optional user-data note (e.g. `UserDataNote`). When a candidate
   - matches an ownership rule with `userData`, or
   - has a `ValuableDataHint` (point 3), or
   - lies under `{LocalLow}` (point 4),

   then a Leftover verdict is **capped at Low** and the note is set. Examples: "browser profile: bookmarks, history,
   saved passwords", "contains a password database", "LocalLow usually holds game saves and settings". The
   explanation names the reason. In use and Unknown verdicts are unchanged.

6. **Findings page**:
   - Rows with a user-data note show a **User data** tag (in the same place as the other row tags, your choice
     consistent with the page) and the note in the details panel.
   - **Select all visible** skips such rows. The status line or a short text near the button says how many were
     skipped, e.g. "3 items with user data were not selected — tick them by hand if you are sure".
   - Ticking such a row by hand works as before. The Cleanup page review lists it like any other item.

7. **Golden profile "gamer and mail"**: extend the golden-profile tests from T-019 (same style, one list of paths)
   with a second fixture. Folders: `AppData\LocalLow\SomeStudio\SomeGame` (old files), `AppData\Roaming\.minecraft`,
   `AppData\Roaming\Thunderbird` (Thunderbird **not** installed), `AppData\Local\Google\Chrome` (Chrome **not**
   installed, Google not installed), an unknown first-level folder with a `wallet.dat` deep inside, an unknown
   first-level folder with a `*.kdbx`. All files older than 6 months. Assertions: every one of these is either not a
   leftover or **Leftover Low with a user-data note**. Also one plain old folder of an uninstalled program without such
   files is still **Leftover Medium** (the feature does not hide everything).

8. **Help (EN + RU)**: `findings.md` (User data tag, Select all visible skips it), `results.md` if the verdict
   explanations are described there, and a tip: "Folders marked User data often hold things you would want to keep
   after a reinstall; open them before you delete." Same content in both languages.

## Out of scope

- Ownership rule High without time check (R-05), partial evidence failures (R-06), system package ids (R-08) → T-022.
- Recycle Bin capacity, caches, result expiry, journal retention → T-021.
- Opening or reading files to detect data (never; names only).
- More rules than the two named above → T-007.

## Acceptance criteria

- [ ] `userData` field: loader, validator, docs, tests (valid, empty, on a non-ownership rule).
- [ ] Valuable-name matching and `Aggregate` propagation tested (match deep inside, case-insensitive, no match).
- [ ] Classifier caps at Low with a note for all three reasons; tests for each and for "unchanged otherwise".
- [ ] Findings: tag, details note, Select all visible skips and reports the count; view-model tests.
- [ ] Golden profile "gamer and mail" passes; the T-019 fresh-Windows fixture still passes.
- [ ] Build and tests pass (`dotnet build` / `dotnet test`, Release; see CLAUDE.md for cloud sessions).
- [ ] Help updated in English and Russian; page help links work.
- [ ] No new package.
- [ ] Report file `docs/reports/T-020-report.md` on the branch (CLAUDE.md, D-075).

## Manual test steps for the owner

1. Create two test folders:
   `%APPDATA%\LeftoverScoutTest-Mail` containing a file named `test.pst`, and `%APPDATA%\LeftoverScoutTest-Plain`
   with an ordinary `notes.txt`. The files may be empty text files renamed accordingly.
2. Scan. On **Findings** turn on **Low** and **Medium**: `LeftoverScoutTest-Mail` shows **User data**, at most Low,
   and the details say it contains a mail data file. `LeftoverScoutTest-Plain` is listed without the tag.
3. If you have folders in `AppData\LocalLow`, any of them listed as a leftover is Low with **User data**.
4. Press **Select all visible**: `LeftoverScoutTest-Mail` stays unticked, and the page says how many were skipped.
5. Tick `LeftoverScoutTest-Mail` by hand: it can be selected. Clear the selection and delete both test folders by hand.
6. Help for **Findings** describes the tag and the skip, in English and in Russian.

## Notes for the executor

- `FolderNode` setters are `internal`; the scanner and `Aggregate` are in the same assembly.
- Keep the valuable-name list and the LocalLow reason in one place each; the help names them in general words only.
- The verdict cap must not raise anything: an In use or Unknown verdict stays as it is; a Low stays Low.
- Recurring build mistakes in this project: xUnit1051, xUnit1031, xUnit2029, WPF `System.IO` implicit using
  (`System.IO.Path` in the App project), fixed-list tests not updated (rule ids, help topics).
- Write the report file (CLAUDE.md step 5) even if everything went well.

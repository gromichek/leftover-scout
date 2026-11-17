# T-019: Mandatory protection — keys, wallets and Windows' own AppData folders

- **Phase:** 3b — Safety hardening (after the independent review, D-070)
- **Branch:** `t-019-mandatory-protection` (or the branch your session assigned)
- **Depends on:** T-011 (merged)

## Goal

Folders that hold credentials or crypto wallets, and folders that Windows itself keeps in
AppData, are **never** offered for deletion. Today they can be rated as leftovers. After this task,
a clean Windows 11 profile produces **zero** leftover verdicts, and this is checked by a test.

## Context

- Read `docs/reviews/2026-11-16-independent-review.md`: findings **R-03** (point 1) and **R-04**.
  This task implements only those two points. Everything else in the review is planned in other tasks.
- Read `docs/RULES.md` (rule format, `scope: folder|subtree`), decisions D-040 … D-043, D-049 and D-070
  in `docs/LEAD_HANDOVER.md`.
- Protected rules live in `rules/protected.json` (16 rules today). Protected rules win over everything
  (D-041). A protected first-level AppData folder is not a candidate at all: see
  `LeftoverCandidateFinder.IsEligible`/`FindInAppData`. Its children are not examined either.
- The guard also has a **hard-coded** deny list in code: `src/LeftoverScout.Core/Cleanup/ProtectedDenyList.cs`.
  It works even if the rule files are broken (D-053). Credential folders belong there too.
- Today `{Profile}\.ssh` and similar folders are dot-folder candidates and can be rated Low. A user can
  select them on Findings when the Low filter is on.

## Scope

1. **Credential and wallet folders**: new `protected` rules, `scope: subtree`, in `rules/protected.json`.
   Give each rule a clear `description` (what is inside and why it is kept) and a `source`.

   | Rule id | Paths | What it holds |
   |---|---|---|
   | `protected-ssh` | `{Profile}\.ssh` | SSH keys and known hosts (OpenSSH) |
   | `protected-gnupg` | `{Profile}\.gnupg`, `{RoamingAppData}\gnupg` | GnuPG keys (`gnupg` is the Gpg4win location) |
   | `protected-aws` | `{Profile}\.aws` | AWS CLI credentials and settings |
   | `protected-azure` | `{Profile}\.azure` | Azure CLI sign-in and settings |
   | `protected-kube` | `{Profile}\.kube` | Kubernetes configuration with cluster credentials |
   | `protected-docker-config` | `{Profile}\.docker` | Docker CLI settings, including registry sign-ins |
   | `protected-android` | `{Profile}\.android` | Android debug signing key and adb keys (also emulator images: say so — the folder can be large, and the user removes it by hand if needed) |
   | `protected-crypto-wallets` | `{RoamingAppData}\Bitcoin`, `{RoamingAppData}\Electrum`, `{RoamingAppData}\Exodus`, `{RoamingAppData}\Ethereum` | Crypto wallets; losing a wallet file loses the money |

2. **Hard-coded deny list**: add the same credential and wallet paths to `ProtectedDenyList` as subtree
   entries (all paths from the table in point 1, including `.android`). Tests: the guard blocks each
   of them and a file deep inside each, also when the rule files are empty.

3. **Windows and platform folders**: new `protected` rules in `rules/protected.json`.

   | Rule id | Path | Scope | What it is |
   |---|---|---|---|
   | `protected-package-cache` | `{LocalAppData}\Package Cache` | subtree | Installer cache; uninstall and repair of installed programs need it |
   | `protected-virtual-store` | `{LocalAppData}\VirtualStore` | subtree | UAC file virtualization; data of older programs lives here |
   | `protected-isolated-storage` | `{LocalAppData}\IsolatedStorage` | subtree | .NET isolated storage; settings of many small programs |
   | `protected-windows-local-folders` | `{LocalAppData}\ConnectedDevicesPlatform`, `{LocalAppData}\Comms`, `{LocalAppData}\Publishers`, `{LocalAppData}\PeerDistRepub`, `{LocalAppData}\PlaceholderTileLogoFolder` | subtree | Windows components |
   | `protected-d3d-shader-cache` | `{LocalAppData}\D3DSCache` | folder | DirectX shader cache. Only the folder itself is protected, so it is never a leftover. Clearing it may become a cache rule later (T-007) |

4. **Tools without an installer entry**: new `ownership` rules in `rules/ownership.json` with
   `requiresInstallation: false`, so the verdict is Unknown, never Leftover:
   - `npm-global` — `{RoamingAppData}\npm` (global npm packages; program "Node.js" / npm).
   - `nuget-config` — `{RoamingAppData}\NuGet` (NuGet user settings and package feeds).

   Update the fixed-list test `ShippedRulesTests.ShippedRules_GradleDoesNotRequireInstallation_AndTheOthersDo`
   (or rename it) so that it lists all rules with `requiresInstallation: false`.

5. **Rule-count test**: `ShippedRules_AreBetween40And60` has a number in its name and will fail. Replace
   it with a range that fits the new count, named without numbers (e.g. `ShippedRules_CountIsInExpectedRange`).

6. **"Fresh Windows 11" golden-profile test** (new test class in `LeftoverScout.Core.Tests/Classification`):
   build a synthetic profile tree (names only, fictional user `SampleUser`, no real disk access) with
   - `AppData\Local`: `Comms`, `ConnectedDevicesPlatform`, `CrashDumps`, `D3DSCache`, `IsolatedStorage`,
     `Microsoft` (with a few children, e.g. `Windows`, `Edge`, `OneDrive`), `Package Cache`, `Packages`,
     `PeerDistRepub`, `PlaceholderTileLogoFolder`, `Programs`, `Publishers`, `Temp`, `VirtualStore`;
   - `AppData\Roaming`: `Microsoft`, `npm`, `NuGet`;
   - `AppData\LocalLow`: `Microsoft`;
   - profile root: `.ssh`, `.gnupg`, `.aws`, `.azure`, `.kube`, `.docker`, `.android`;
   - `AppData\Local\Packages`: a few Microsoft and system package folders, for example
     `Microsoft.WindowsCalculator_8wekyb3d8bbwe`, `MicrosoftWindows.Client.CBS_cw5n1h2txyewy`,
     `windows.immersivecontrolpanel_cw5n1h2txyewy`, `MSTeams_8wekyb3d8bbwe`. All of them are
     **registered** packages in the evidence. Unregistered packages are T-020 (R-08).

   Every folder has files whose newest write time is **older than 6 months** (the worst case). The
   evidence has only a few Microsoft programs. Use the **shipped rules**. Assertion: the classifier
   gives **no Leftover verdict** (any confidence) for any folder. If an assertion fails, report the
   folder and verdict in the message.

   Keep the fixture easy to extend (one list of relative paths). T-020 adds "developer" and "gamer" profiles.

7. **Help (EN + RU)**: `results.md` (Protected tag) and `findings.md` (what is never offered) name
   examples of protected locations. Add the new kinds: SSH and cloud-tool keys, crypto wallets,
   Windows' own folders in AppData. Same content in both languages. `docs/RULES.md`: update the rule counts.

## Out of scope

- The "contains user data" class, the valuable-file heuristic, R-05, R-06, R-08 → T-020.
- Recycle Bin capacity, empty-caches-instead-of-removing, minimum age, result expiry, journal retention → T-021.
- New cache rules (including `D3DSCache`, `npm-cache`) → T-007.
- Any engine or rule-format change. If something in the tables above cannot be expressed in the
  current format, leave it out and ask under "Open questions".

## Acceptance criteria

- [ ] All rules from points 1, 3 and 4 exist, load without errors and have a description and a source.
- [ ] The deny list blocks every credential and wallet path (and descendants) even without rule files.
- [ ] Golden-profile test: zero Leftover verdicts for the fresh Windows 11 fixture.
- [ ] Existing tests updated (rule count, `requiresInstallation` list); no test encodes the rule count in its name.
- [ ] Build and tests pass (`dotnet build` / `dotnet test`, Release).
- [ ] Help updated in English and Russian to match the app; page help links work.
- [ ] No new package.

## Manual test steps for the owner

1. Scan. Open **Results**, go to `AppData\Local`: `Package Cache`, `VirtualStore`, `Comms`,
   `ConnectedDevicesPlatform`, `Publishers` (the ones you have) show the **Protected** tag.
2. In your profile folder, `.ssh`, `.aws`, `.docker`, `.android` (the ones you have) show **Protected**.
3. Open **Findings**, turn on **Low**: none of the folders from steps 1–2 is listed.
4. Check that Findings still lists other leftovers and caches as before (nothing else disappeared).
5. Help: the **Protected** description on the Results and Findings help pages mentions keys, wallets and
   Windows folders, in English and in Russian.

## Notes for the executor

- `scope: subtree` protects everything inside; `scope: folder` protects only the folder itself. A protected
  first-level AppData folder is skipped by the candidate finder together with its children.
- Paths in rules use tokens only (`{Profile}`, `{RoamingAppData}`, `{LocalAppData}`, `{LocalLow}`); never a real
  user name.
- `source` for well-known tool folders: name the tool and its documentation, e.g. "OpenSSH for Windows
  documentation (Microsoft Learn): user keys in %USERPROFILE%\.ssh". If you cannot name a source for a row, keep
  the rule anyway (it only **protects**), write "well-known location" and list the row under "Open questions" —
  the lead verifies it in the review.
- Recurring build mistakes in this project: xUnit1051 (pass `TestContext.Current.CancellationToken`),
  xUnit1031 (no blocking `.GetAwaiter().GetResult()` in tests), xUnit2029 (`Assert.Empty(x.Where(…))` →
  `Assert.DoesNotContain`), missing `using LeftoverScout.Core.Tests.Scanning;`, fixed-list tests not updated.
- Look at `ClassificationTestKit.cs` for helpers that build folder trees and evidence; reuse them.

# Rules (knowledge base)

LeftoverScout knows a list of well-known locations in the Windows user profile. The knowledge lives in JSON files in
the `rules/` folder at the repository root. The files are embedded into the app (decisions D-040 … D-043); the app
works fully offline and has no user-editable rules.

Rules only **describe** folders. They never delete anything. Deciding whether a folder is a leftover is a separate
step (classification, see `docs/ARCHITECTURE.md`); ownership rules feed it through `requiresInstallation` and the program hints.

## Rule kinds

| Kind | Meaning | Shown in Results as |
|---|---|---|
| `ownership` | The folder belongs to a program. | **Owner: <program>** |
| `cache` | The folder holds a cache, temporary files, logs or crash dumps. | **Cache: <program>**, **Temp**, **Logs**, **Crash dumps** |
| `protected` | The folder must **never** be offered for deletion. | **Protected** |

Personal folders (Documents, Desktop, Downloads, Pictures, Videos, Music, Favorites, Contacts, Links, Saved Games, Searches,
3D Objects, OneDrive*) are protected automatically with subtree scope; no rule is needed. They are recognised by their
known-folder paths and, directly in the profile root, **by name** (decision D-049). **Protection is absolute**: if several rules match, `protected` wins, and the
loader rejects any `cache` or `ownership` rule whose path could lie inside a `subtree`-protected path.

## Files

`rules/*.json`, for example `protected.json`, `caches.json`, `ownership.json`. More files are allowed; every `*.json`
file in `rules/` is embedded with the logical name `Rules/<file>.json`. Each file looks like this:

```json
{
  "schemaVersion": 1,
  "rules": [ { "...": "..." } ]
}
```

Unknown properties are errors (a typo such as `categroy` is reported, not ignored).

## Rule fields

| Field | Required | Description |
|---|---|---|
| `id` | yes | Unique, kebab-case (`discord-cache`). Never reuse or rename an id for a different meaning. |
| `kind` | yes | `ownership`, `cache` or `protected`. |
| `paths` | yes | One or more path patterns (below). |
| `scope` | `protected`: yes; otherwise optional | `folder` (only that folder) or `subtree` (the folder and everything below). Default `subtree`. |
| `category` | `cache`: yes; other kinds: not allowed | `cache`, `temp`, `logs` or `crashDumps`. |
| `program` | `ownership`: yes; `cache`: optional; `protected`: not allowed | The program, see below. |
| `description` | yes | One short English sentence shown in the Results tooltip. |
| `source` | yes | Where the knowledge comes from. Not shown in the UI. |

`program`:

| Field | Description |
|---|---|
| `displayName` | Required. Name shown to the user (**Owner: Discord**). |
| `requiresInstallation` | **Required for ownership rules** (`true` or `false`); optional for cache rules (then `false`). `true` when the program is normally installed with an installer entry (Discord, Chrome, Steam, ...): if no installed program matches, leftover detection rates the folder a leftover. `false` for tools that often have no installer entry (for example Gradle): missing evidence then proves nothing and the verdict stays *unknown*. When unsure, use `false`. |
| `names`, `publishers`, `uninstallKeys`, `packageFamilies`, `programFolders` | Optional lists of plain strings, compared case-insensitively, **no wildcards**. Leftover detection compares them with the installed programs found on the computer (publisher names are not used as proof that the program itself is installed): program or display names, publisher names, registry Uninstall key names, Store package family names and install folder names. |

## Path patterns

A pattern starts with exactly one token, followed by folder names separated by `\`:

| Token | Meaning (relative to the scanned profile root) |
|---|---|
| `{Profile}` | The profile root. |
| `{RoamingAppData}` | `AppData\Roaming` |
| `{LocalAppData}` | `AppData\Local` |
| `{LocalLow}` | `AppData\LocalLow` |

A segment is either a literal folder name or `*`, which stands for **exactly one whole folder name**
(`{LocalAppData}\Google\Chrome\User Data\*\Cache`). Names are compared case-insensitively, on whole segments:
`Cache` does not match `CacheStorage`.

Not allowed: regular expressions, partial wildcards (`Cache*`), absolute paths, drive letters, `..`, `.`, empty
segments (`a\\b`, trailing `\`), environment variables (`%APPDATA%`), and any token other than the four above.
Rules never use the `Environment` paths of the running user: the tokens are resolved against the profile root of the
scan, because later versions can scan other profiles.

## Matching

- The scan tree is walked once. Rules are indexed by their path segments, so a large rule set stays fast.
- `scope: folder` applies to the folder itself; `scope: subtree` applies to the folder and all its descendants.
- If several rules match one folder: `protected` wins; otherwise a rule that matches the folder directly beats one
  inherited from a parent; then the pattern with more literal segments wins. All matches are kept for the tooltip.

## Validation

The loader checks every rule and skips an invalid one (the app logs the rule id and the reason, and keeps running).
The shipped files must have **no** errors; the tests fail otherwise. Errors include: unknown kind, scope, category or
token; a bad pattern; a duplicate id; a missing required field (including `requiresInstallation` of an ownership rule); wildcards in program hints; a `cache` or `ownership`
path that lies inside a `subtree`-protected path; a misspelled property.

## Adding or changing a rule

1. Only add locations you are **sure** about. A wrong rule can later make a needed folder look removable. If you are
   not sure, leave it out.
2. Give every rule a `description` and a `source`: vendor documentation, a support page, or a clearly well-known
   location. "I saw it on my computer" is not a source.
3. Put the rule in the file of its kind and run `dotnet test LeftoverScout.slnx --configuration Release`; the
   shipped-rules tests validate the files and check that the rule count stays in the agreed range.
4. If a rule changes what the user sees on the Results page, update the help in English and Russian.
5. No personal data: never copy a real user name or machine name into a path or description.

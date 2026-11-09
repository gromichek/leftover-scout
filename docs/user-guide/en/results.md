# Results

The **Results** page shows where the space in your profile goes. It uses the result of the last
completed [Scan](scan.md). **Nothing is changed or deleted here**: you can only look, open a location in
Explorer and copy a path.

## Before the first scan

If no scan has finished yet, the page shows a short message and the button **Go to Scan**, which takes
you to the **Scan** page. If you cancel a later scan, the result of the previous completed scan stays
on the page. A new completed scan replaces the result and returns the table to the profile folder.

## The header

Above the tabs you see when the scan finished and its totals, for example
"Scanned 3/4/2025 12:30 PM · 120.5 GB local · 8.0 GB cloud-only". Cloud-only files exist only in the
cloud and are not part of the local size.

## Tab Folders

The table shows the contents of **one folder at a time**, starting with your profile folder.

- **Breadcrumb**: the path from the profile folder to the current folder. Click any part to jump back to it.
- **Up**: goes to the parent folder. It is disabled in the profile folder.
- **Open a folder**: double-click it, or select it and press **Enter**.
- **Sorting**: click a column header. Click again to reverse the order. Sizes, counts, percentages and
  dates sort by their real value, so 900 MB comes before 1.2 GB when sorting by **Size**. The first
  view is sorted by **Size**, largest first, and the arrow on the **Size** header shows it.
- Below the table: "<n> folders · <size> in this folder".

### Columns

| Column | Meaning |
|---|---|
| **Name** | Folder name. |
| **Size** | Local size of the folder with everything inside it (like Explorer's "Size"). |
| **% of parent** | A thin bar and a number: the share of the current folder's local size. It is 0 % when the current folder is empty. |
| **Files** | Number of files inside the folder, including all subfolders. |
| **Last modified** | The newest file date inside the folder, in your local time. Empty when the folder has no files. |
| **Notes** | Short tags, see below. Hover over the cell for an explanation. |

### The row (files in this folder)

Files that lie directly in the current folder (not in a subfolder) are summed up in one extra row named
**(files in this folder)**. It shows their size and count. It cannot be opened, and it is hidden when the
folder has no files of its own.

### Notes tags

| Tag | Meaning |
|---|---|
| **Personal** | One of your own folders (Documents, Desktop, Downloads, Pictures, Videos, Music, Favorites, Contacts, Links, Saved Games, Searches, 3D Objects, OneDrive). It is measured, and it is protected: personal folders are never offered for cleanup. |
| **Link** | A junction or symbolic link. It is not followed and not counted, so nothing is counted twice. Windows creates a few old ones (such as `Application Data`) in every profile. |
| **Cloud** | An online-only OneDrive folder that is not available on this computer. It was not read, so nothing is downloaded. |
| **Inaccessible** | The folder could not be read completely (access denied or another error). Its size may be too small. |

These standard folders in your profile folder are **Personal** by name, even if Windows stores your
documents or pictures on another drive; a folder whose name starts with `OneDrive` (for example
`OneDrive - Contoso`) is **Personal** too. Personal folders that were moved to another drive are **not scanned**,
because they lie outside your profile.

Rows tagged **Link** or **Cloud** cannot be opened. An **Inaccessible** folder opens only if some of its
content could be read.

### Labels from the built-in rules

After each scan, LeftoverScout compares the folders with a built-in list of well-known locations. When a folder
matches, a label is added to **Notes**. These labels only describe the folder: **nothing is selected or deleted.**

| Label | Meaning |
|---|---|
| **Protected** | The folder must never be offered for deletion. This covers the `AppData` folders themselves and important Windows data such as saved credentials, keys and certificates, the Start menu and the Store app links. **Personal** folders are protected too, even though they show the **Personal** tag instead. |
| **Cache: …** | A cache of the program named after the colon (for example **Cache: Discord**). A program can download or build it again. |
| **Temp** | Temporary files. |
| **Logs** | Log files. |
| **Crash dumps** | Files that Windows or a program wrote after a failure. |
| **Owner: …** | The folder belongs to the program named after the colon (for example **Owner: Discord**). It does not tell you whether the program is still installed. |

Protection and labels apply to a folder and, for most rules, to everything inside it, so subfolders of a protected
folder are protected too. If several rules match, **Protected** wins; otherwise the most specific rule is shown.

Hover over the **Notes** cell: the tooltip explains the label and lists the description of every matching rule,
the most important first.

### How does LeftoverScout know?

The rules are built into the app: a list of well-known locations of Windows and popular programs, each with a short
description. LeftoverScout works fully offline and never downloads rules. The list is small today and will grow
with new versions. A folder without a label is simply not in the list; that says nothing about whether it is needed.

### Leftover labels

After each scan, LeftoverScout also checks the folders where programs keep their data: the first level of
`AppData\Roaming`, `AppData\Local` and `AppData\LocalLow`, the subfolders of a vendor folder (such as `JetBrains`),
the folders that start with a dot in your profile folder (such as `.gradle`) and every folder in
`AppData\Local\Packages`. It asks one question: does this folder probably belong to a program that is **no longer
installed**? The answer is a label in **Notes**. Labels only describe: **nothing is selected or deleted.**

| Label | Meaning in practice |
|---|---|
| **Leftover: High** | Very likely safe to remove later: no installed program matches, and either it is a Store app package that is not installed for you, does not match any installed program and has not changed for 6 months (packages from Microsoft are never rated **High**), or the folder belongs to a well-known program that should have been found installed but was not. |
| **Leftover: Medium** | Check first. No installed program matches the folder name, and the folder has not changed for 6 months or is empty; or it is the data of a Store app that is not installed but changed recently. |
| **Leftover: Low** | Probably still used. No installed program matches the name, but the folder changed within the last 6 months. |
| **In use: …** | The folder belongs to an installed program, named after the colon (for example **In use: Discord**). |

A folder in `AppData\Local\Packages` whose package is not registered for you is also compared with your installed desktop programs: if its vendor name (for example `Kaspersky` in `Kaspersky.ShellEx22_…`) matches the name, publisher or registry key of an installed program, it is **In use**, because such packages are often installed together with a desktop program.

Hover over the label to read the reason, for example "No installed program named 'FooSoft' was found. Not changed
since 2023-05." A folder without a leftover label was either not checked (personal, protected, cache, link, cloud or
unreadable folders) or LeftoverScout could not decide.

How LeftoverScout decides:

- It compares the **names** of the folders with the names of the installed programs it found (registry, Store apps,
  program folders and the Start menu) and with a built-in list of well-known programs. Only whole words are compared
  after removing versions and endings such as "Inc" or "Ltd"; there is no guessing by similarity. Nothing is sent
  anywhere.
- **It errs on the safe side.** Generic names (such as `Cache`, `Temp`, `Logs`, `Data`), long numbers and
  identifiers, and the contents of `Microsoft` folders are left without a label. Tools that often have no installer
  (such as Gradle) are not called leftovers just because no program was found. A folder that starts with a dot is
  never rated higher than **Low** unless a built-in rule knows the program. If the installed programs could not be
  read completely, no leftover labels are shown.
- Packages from Microsoft that are not found are rated **Medium** at most, because Windows installs many of them in ways LeftoverScout cannot fully see.
- "Not changed" means that the newest file in the folder is at least 6 months old.
- A wrong label is possible. Before you ever remove something, check the reason and make sure that you no longer
  need the program.

## Tab Large files

Lists the local files of **100 MB or more** found by the scan, largest first.

| Column | Meaning |
|---|---|
| **Path** | Path relative to your profile folder. Hover to see the full path. |
| **Size** | File size. |
| **Last modified** | Last write date in your local time. |

Click a column header to sort. If the scan found no such files, the tab says "No files of 100 MB or more."
Cloud-only files are not listed, because they take no space here.

## Buttons and shortcuts

The buttons are above each table and also in the right-click menu of a row. They are disabled until you
select a row.

- **Open in Explorer**: opens the selected folder in Explorer. On the **(files in this folder)** row it
  opens the current folder. On the **Large files** tab it opens the folder with the file selected.
- **Copy path**: puts the full path on the clipboard. Shortcut: **Ctrl+C**. If another program is using the
  clipboard, the status bar says so; try again.
- **Enter** (Folders tab): opens the selected folder.
- **F1** or the **?** button: opens this help topic.

## Tips

- Sort by **Size** and open the biggest folders first.
- `AppData\Local` usually holds caches and the data of installed programs; `AppData\Roaming` holds settings.
- A large **Files** count with a small size usually means many tiny cache files.
- Big folders tagged **Personal** are your own documents and media. Leave them alone; they are protected.
- Start with **Leftover: High** and **Leftover: Medium** folders, read the reason in the tooltip and ask yourself whether you really uninstalled that program. **Leftover: Low** folders are probably still used.
- **Cache**, **Temp**, **Logs** and **Crash dumps** labels show where space is spent on data that can be recreated. This version does not delete anything.
- Sizes are logical file sizes and do not match Explorer to the byte, see [Scan](scan.md).

Next: [Cleanup](cleanup.md) (planned).

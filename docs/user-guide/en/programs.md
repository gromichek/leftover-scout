# Programs

The **Programs** page lists what is installed on this computer. Every [Scan](scan.md) collects the list
automatically, together with the folder sizes. Later versions will use it to tell whether a folder in your
profile still belongs to an installed program.

**Nothing is changed.** The registry is only read, shortcut files are never opened, and no program is started
or uninstalled from here.

## Before the first scan

If no scan has finished yet, the page shows a short message and the button **Go to Scan**. If you cancel a later
scan, the list from the previous completed scan stays on the page. A new completed scan replaces it.

## The header

Above the table you see when the list was collected and how many entries it has, for example
"Collected 3/4/2025 12:30 PM · 312 programs". If a source could not be read, one short line below it says so, for
example "Store apps could not be read." The other sources are still shown.

## Where the entries come from

LeftoverScout looks in four places. A program often appears in several of them, so the same name can occur more
than once. That is normal.

| Source | What it is |
|---|---|
| **Registry** | The list behind Windows Settings → Apps → Installed apps (the `Uninstall` registry keys of the computer and of your user, 64-bit and 32-bit). Updates and patches are left out. |
| **Store** | Apps from the Microsoft Store and other packaged (MSIX) apps installed for your user. |
| **Program folder** | The folders directly inside `Program Files`, `Program Files (x86)` and `%LOCALAPPDATA%\Programs`. Only the folder name is used. |
| **Start menu** | The names of shortcuts and folders in your Start menu and in the Start menu for all users. The shortcut files themselves are never opened. |

## Search and filters

- **Search**: type part of a name or a publisher. The table filters as you type; upper and lower case do not matter.
- **Source**: **All**, **Registry**, **Store**, **Program folder** or **Start menu**.
- **Show system components and frameworks**: off by default. It hides Windows components, system apps and
  framework packages, which you rarely need. Tick it to see everything.

Below the table, "<shown> of <total> programs" tells how many entries pass the filters. While system
components and frameworks are hidden, the line also says how many entries that hides, for example
"476 of 871 programs · 395 hidden (system components and frameworks)". If nothing is hidden, the short form is
shown. If no entry passes the filters, the page says so.

## The table

| Column | Meaning |
|---|---|
| **Name** | Name of the program, app, folder or shortcut. |
| **Publisher** | Who made it, when known. |
| **Version** | Version, when known. |
| **Source** | Where the entry was found, see above. |
| **Install location** | Folder where it is installed, when known. |
| **Installed** | Install date, when the program recorded one. |

Click a column header to sort; click again to reverse. The table starts sorted by **Name**, and the arrow on
the header shows it. Empty cells mean the information is not available from that source.

## Buttons and shortcuts

The buttons are above the table and also in the right-click menu of a row. They are disabled until you select a row.

- **Open install location**: opens the install folder in Explorer. It is available only when the entry has an
  install location and that folder exists.
- **Copy name**: puts the name on the clipboard. Shortcut: **Ctrl+C**.
- **F1** or the **?** button: opens this help topic.

## Tips

- Compare a few entries with Windows Settings → Apps → Installed apps to see that the list is plausible.
- Entries from **Program folder** and **Start menu** are only hints. A folder can stay after an uninstall.
- To find leftovers of a program you removed, search for its name here: if it is absent from every source, it
  is probably gone.

Next: [Findings](findings.md) lists what you could remove, [Results](results.md) shows where the space goes.

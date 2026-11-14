# Findings

The **Findings** page answers one question: **what could I remove?** It gathers, from the last completed
[Scan](scan.md), the folders that probably belong to programs that are no longer installed (**leftovers**) and the
folders that hold data a program can recreate (**caches**, **temp** files, **logs** and **crash dumps**). Every item
comes with its size, how sure LeftoverScout is, and the reason.

**Nothing is changed or deleted on this page.** You can look, open a folder in Explorer, copy a path and **select**
items for cleanup. The actual removal happens on [Cleanup](cleanup.md), after a review and your confirmation.

## Before the first scan

If no scan has finished yet, the page shows a short message and the button **Go to Scan**. If you cancel a later
scan, the findings of the previous completed scan stay on the page. A new completed scan replaces them. Your filter
settings are kept while the app is running (they are not saved when you close it).

## What is listed, and what never is

Listed:

- **Leftovers**: folders rated **High**, **Medium** or **Low** on the [Results](results.md) page.
- **Cache, Temp, Logs and Crash dumps**: folders that a built-in rule recognizes as such. If such a folder lies
  inside another listed cache folder, only the outer one is listed.
- A folder that is both a leftover and a cache is listed once, as a leftover; the details mention the cache rule.

Never listed:

- **Personal** folders (Documents, Desktop, Pictures, ...) and everything inside them.
- **Protected** locations, for example saved credentials, keys and the Start menu.
- Links (junctions and symbolic links), online-only cloud folders that were not read, and folders that are empty
  (no size and no files), because there is nothing to gain.

## The header and the filters

Above the table you see when the scan finished, for example "Scanned 3/4/2025 12:30 PM". Under it:

- **High**, **Medium**, **Low**: which leftovers are shown. **Low** is off by default, because those folders were
  changed recently and the program is probably still used.
- **Cache**, **Temp**, **Logs**, **Crash dumps**: which kinds of recreatable data are shown. All are on by default.
- **Search**: type part of a path or of a program name. Upper and lower case do not matter.

The table updates as you change a filter. If nothing passes the filters, the page says so.

**Changing a filter deselects the items that become hidden**, so nothing you cannot see can be cleaned by accident. If
you show them again, they are not selected any more. A new completed scan clears the whole selection.

If a cleanup has run since the scan, a yellow banner says "Results are out of date after a cleanup — scan again."
(with the button **Go to Scan**). The table is still shown, but **Clean up selected…** stays disabled until a new scan has completed.

## The table

| Column | Meaning |
|---|---|
| (checkbox) | Select the item for cleanup. Nothing is selected at first. |
| **Path** | Path of the folder below your profile folder. |
| **Kind** | **Leftover**, **Cache**, **Temp**, **Logs** or **Crash dumps**. |
| **Confidence** | **High**, **Medium** or **Low** for leftovers; empty for the other kinds. |
| **Size** | Local size of the folder with everything inside it. |
| **Last modified** | The newest file date inside the folder, in your local time. |
| **Program** | The program the folder belongs to, when LeftoverScout knows it; otherwise empty. |

Click a column header to sort; click again to reverse. Sizes, dates and confidence sort by their real value
(**High** is above **Medium** and **Low**). The table starts sorted by **Size**, largest first, and the arrow on the
header shows it.

## The total

Under the table: "<n> items · <size>" for the items that pass the filters. An item that lies **inside another listed
item** (for example the `Cache` folder inside a leftover folder) is shown, but its size is **not added again**, so
the total never counts the same bytes twice. If you hide the outer item with a filter, the inner one is counted.

## Selecting items for cleanup

Tick the checkbox in the first column, or select a row and press **Space**. Nothing is ticked by default, and you can
tick **Low** items too: they are flagged as **Low confidence** on the Cleanup page and in the question you answer before the cleanup starts.

- **Select all visible**: ticks every item that passes the current filters.
- **Clear selection**: unticks everything.
- The line **Selected: <n> items · <size>** shows how many items are ticked and how much they take. An item that lies
  inside another ticked item is included with the outer one and is **not counted twice**.
- **Clean up selected…**: opens [Cleanup](cleanup.md) with the ticked items. It is disabled while nothing is ticked,
  while a cleanup is running, and after a cleanup until a new scan has completed.

## The details panel

Select a row to see, below the table: the full path, the kind, the confidence, the size and the number of files, the
last modified time, the program, the **reason** (why the folder is listed), the rule that matched, and, for a nested
item, the line "Inside another finding: <path>". Without a selected row the panel asks you to select one.

## Buttons and shortcuts

The row buttons are below the table and also in the right-click menu of a row. They are disabled until you select a row.

- **Open in Explorer**: opens the folder in Explorer.
- **Copy path**: puts the full path on the clipboard. Shortcut: **Ctrl+C**.
- **Show in Results**: switches to [Results](results.md), opens the parent folder of the item and selects its row.
- **Select all visible**, **Clear selection**, **Clean up selected…**: see "Selecting items for cleanup" above.
- **F1** or the **?** button: opens this help topic.

## Tips

- Start with **High** and the biggest items.
- For **Medium**, read the reason first and check [Programs](programs.md): if the program is still installed under another name, leave the folder.
- **Low** is hidden because the program is probably still used.
- Use **Show in Results** to see what lies around an item before you decide.
- Cache, temp, logs and crash dumps can be recreated by the programs, but a leftover may hold settings or saves you still want.
- Tick only what you understand. You review everything again on [Cleanup](cleanup.md), and by default items go to the Recycle Bin.

Next: [Cleanup](cleanup.md).

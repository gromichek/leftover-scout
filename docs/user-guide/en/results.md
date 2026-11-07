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
  view is sorted by **Size**, largest first.
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
| **Personal** | One of your own folders (Documents, Desktop, Pictures, Videos, Music, Downloads, OneDrive). It is measured, but it will be protected from cleanup. |
| **Link** | A junction or symbolic link. It is not followed and not counted, so nothing is counted twice. Windows creates a few old ones (such as `Application Data`) in every profile. |
| **Cloud** | An online-only OneDrive folder that is not available on this computer. It was not read, so nothing is downloaded. |
| **Inaccessible** | The folder could not be read completely (access denied or another error). Its size may be too small. |

Rows tagged **Link** or **Cloud** cannot be opened. An **Inaccessible** folder opens only if some of its
content could be read.

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
- Big folders tagged **Personal** are your own documents and media. Leave them alone.
- Sizes are logical file sizes and do not match Explorer to the byte, see [Scan](scan.md).

Next: [Cleanup](cleanup.md) (planned).

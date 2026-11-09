# Scan

Open the **Scan** page and press **Start scan**. LeftoverScout walks through your user profile
(`C:\Users\<you>`) and measures what takes space.

Every scan also collects the list of installed programs (see [Programs](programs.md)). It runs at the same time
and is fast.

**Nothing is changed.** The scan only reads folder listings: it does not open your files, does not
delete, move or rename anything, and does not change dates. The window stays usable while the scan runs.

## Buttons

- **Start scan**: starts a scan. It is disabled while a scan is running.
- **Cancel**: stops the scan, usually within about a second. A cancelled scan shows "Scan cancelled."
  and has no totals. It is disabled when no scan is running.

## While the scan runs

You see the folder being read, how many folders and files were found, the size so far and the elapsed time.
If the programs are still being collected when the folders are done, the line shows "Collecting installed programs…".
The status bar at the bottom shows "Scanning…", then "Scan completed in 42 s" or "Scan cancelled".

## The summary

| Line | Meaning |
|---|---|
| Local size | Size of all files stored on this computer (logical file size, as Explorer shows under "Size"). Hard-linked files are counted once per link. |
| Cloud-only size | Size of files that exist only in the cloud (for example OneDrive "online-only" files). They take no space here and are **not** part of the local size. They are never downloaded by the scan. |
| Personal folders | Local size of Documents, Desktop, Pictures, Videos, Music, Downloads and OneDrive. They are measured but marked personal and protected: they are never offered for deletion. |
| Folders / Files | How many were found. |
| Skipped links | Junctions and symbolic links. LeftoverScout does not follow them, so nothing is counted twice or outside your profile. Windows creates a few old ones (such as `Application Data`) in every profile, so this is normally not zero. |
| Cloud folders not scanned | OneDrive folders that are not available on this computer. Listing them would make OneDrive download them, so LeftoverScout leaves them alone. |
| Inaccessible locations | Folders that could not be read (access denied or another error). The scan continues without them, so the totals may be slightly smaller than reality. |
| Duration | How long the scan took. |

Below the summary, **Largest top-level folders** lists the ten biggest folders directly inside your
profile (by local size). `AppData` is often among them.

## Tips

- Explorer's *Properties* dialog also counts cloud files, follows some links and cannot read some folders.
  Expect a difference: the scan shows where the space goes, it does not match Explorer to the byte.
- If a source of installed programs cannot be read, the scan still completes; the [Programs](programs.md) page says which source failed.
- A scan of a large profile can take a while. You can keep using the window or press **Cancel**.
- Files of 100 MB or more are remembered as "large files" and listed on the **Results** page.
- The last completed result (folders, large files and installed programs) stays in memory while the program runs.
  A new completed scan replaces it. A cancelled scan keeps the previous result.

Next: [Results](results.md) shows where the space goes, [Programs](programs.md) lists what is installed.

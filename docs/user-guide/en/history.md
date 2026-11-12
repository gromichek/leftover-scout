# History

The **History** page shows every cleanup LeftoverScout has run: what was selected, what happened to each
folder, and why. It is a record, not a control panel: nothing on this page removes, restores or changes anything.

Cleaning up from the app is not available yet (see [Cleanup](cleanup.md)), so until then the page stays empty and
shows **No cleanups yet.** Every cleanup you run later is recorded here.

## What is recorded, and where

For every cleanup session LeftoverScout writes one file in the `journal` folder inside its data folder
(`data\journal`, see [Getting started](getting-started.md)). The file is created **before** the first folder is
touched and updated **after each folder**, so even if the program is closed or the computer loses power in the middle
of a cleanup, there is still a record of what was done up to that point.

- The journals stay on your computer. They are never sent anywhere.
- They contain real folder paths, so do not post them publicly.
- LeftoverScout **never deletes journals**. If you want to remove old ones, delete the files yourself.

## The list of sessions

The newest session is on top. The columns are:

| Column | Meaning |
|---|---|
| **Date** | When the cleanup began. |
| **Mode** | **Recycle Bin** (the items could be restored) or **Permanently** (they could not). |
| **Items** | How many folders were part of the cleanup. |
| **Removed** | How much space was freed. |
| **Result** | A summary such as `12 removed · 1 partly · 2 skipped`, or **Interrupted**, **In progress** or **Unreadable journal**. A cancelled or failed cleanup says so in front of the numbers. |

Click a session to see its details below.

## The details of a session

Every folder of the selected session is one row:

| Column | Meaning |
|---|---|
| **Path** | The folder, relative to your user profile. |
| **Kind** | **Leftover**, **Cache**, **Temp**, **Logs** or **Crash dumps**, as on [Findings](findings.md). |
| **Confidence** | How sure the scan was that a leftover is one (**High**, **Medium**, **Low**); empty for caches. |
| **Size** | The size the scan saw before the cleanup. |
| **Outcome** | What happened, see below. |
| **Reason** | Why, for every outcome other than a complete removal. |

If Windows deleted a folder permanently although the Recycle Bin was requested, a warning line appears above the
table. Such a folder cannot be restored.

## Outcomes

- **Removed**: the folder was removed completely.
- **Partly removed**: part of the folder was removed, but some entries remain, for example files that another program
  was using. The reason says how many entries are left.
- **Skipped (too large)**: in Recycle Bin mode the folder is larger than the Recycle Bin allows, so it was left alone
  instead of being deleted permanently.
- **Skipped (bin off)**: the Recycle Bin is turned off for that drive, so the folder was left alone.
- **Not recycled**: Windows could not move the folder to the Recycle Bin, so nothing was deleted.
- **Blocked**: the safety check refused the folder (for example it is protected, a link, or no longer where it was).
  Nothing was touched.
- **Not found**: the folder did not exist any more.
- **Failed**: the folder could not be removed; the reason says why.
- **Not started**: the cleanup was cancelled or stopped before this folder.
- **Pending**: only in an interrupted session: the folder was not recorded. Check it in Explorer.

## Interrupted sessions

If a session shows **Interrupted**, the program was closed or the computer stopped during that cleanup. The folders
before the break are recorded; the ones marked **Pending** were not. Open them in Explorer to see whether they still exist.

A session that is still running shows **In progress**. A journal file that cannot be read (damaged, or written by a
newer version) is listed as **Unreadable journal** with its file name; it is never changed.

## The buttons

- **Open Recycle Bin** opens the Windows Recycle Bin. To restore a folder, right-click it there and choose
  **Restore**. A folder removed in **Permanently** mode is not in the Recycle Bin.
- **Open journal folder** opens the folder with the journal files in Explorer. The folder is created if it does not exist yet.
- **Copy path** copies the full path of the selected row of the details table (also in its right-click menu).

## Tips

- After a cleanup, look at the **Result** column first: anything other than **removed** is worth a look in the details.
- Restoring from the Recycle Bin works only while the item is still in the bin. Emptying the bin removes it for good.

See also [Cleanup](cleanup.md) and [Findings](findings.md).

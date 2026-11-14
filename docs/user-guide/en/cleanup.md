# Cleanup

The **Cleanup** page removes the items you selected on [Findings](findings.md). By default they are moved to the
**Recycle Bin**, so you can restore them. You review the items, choose how to remove them, confirm, watch the progress
and read an honest summary. Every cleanup is recorded on [History](history.md).

## The order of work

1. On [Findings](findings.md) tick the items you want to remove and press **Clean up selected…**.
2. On **Cleanup** read the review. Items marked **Ready** will be cleaned; the others will not.
3. Choose the mode, press **Start cleanup** and answer the question.
4. Wait for the progress, or press **Cancel**.
5. Read the summary. Use **Open History**, **Open Recycle Bin** or **Scan again**.

## Nothing selected

If you open **Cleanup** without a selection, the page says so and shows the button **Go to Findings**. Nothing is
selected for you: you always choose the items yourself.

## The review

When you arrive, LeftoverScout first shows **Checking…** for a moment. It checks every item again against the safety
rules and the real folder on disk (not only the scan) and looks for running programs that seem to belong to the items.
Then the review appears.

| Column | Meaning |
|---|---|
| **Path** | Path of the folder below your profile folder. |
| **Kind** | **Leftover**, **Cache**, **Temp**, **Logs** or **Crash dumps**. |
| **Confidence** | **High**, **Medium** or **Low** for leftovers; empty for the other kinds. |
| **Size** | Size of the folder as the scan saw it. |
| **Status** | **Ready**, a warning, or **Blocked** with the reason. |

The statuses:

- **Ready**: the folder passed every check and will be cleaned.
- **Ready · Medium confidence** or **Ready · Low confidence**: it will be cleaned, but look at it again first. A **Low**
  folder was changed recently and is probably still used.
- **Ready · Changed since the scan**: the folder has newer content than the scan saw. Check that you still want it removed.
- **Blocked** with a reason: the folder will **not** be touched, for example because it no longer exists, is a personal or
  protected folder, is a link (junction or symbolic link), or a check could not be completed. Blocked items never reach the cleanup.

Under the table you may see:

- A line such as "2 items are inside other selected items and are included with them". An item inside another selected
  item goes together with the outer one and is not listed twice.
- A yellow box **Close these programs first: Discord, ...** when a running program seems to belong to the items. This is
  only a warning: you can still start. Files that are in use are skipped, and the folder is then **partly removed**.
- The total of the ready items: "<n> items · <size>".

## The mode

- **Move to Recycle Bin** (the default): the folders go to the Recycle Bin. The space is freed when you empty the
  Recycle Bin. A folder that is too large for the Recycle Bin, or a drive where the Recycle Bin is turned off, is
  **skipped and reported**; LeftoverScout never deletes it permanently on its own in this mode.
- **Delete permanently**: the folders are deleted at once and cannot be restored. You are asked twice.

## The questions

After **Start cleanup** a question appears; the default answer is **No**.

- Recycle Bin: "Move <n> items (<size>) to the Recycle Bin?" If some items have Low confidence, the question says "Including <k> items with Low confidence".
- Permanently: the same question with "permanently", and then a **second** question: "This cannot be undone. Delete <n> items (<size>) permanently?"

If you answer **No** at any step, nothing is removed and you stay on the review.

## While it runs

You see a progress bar (items), the number of items and bytes done, and the folder being processed. **Cancel** stops
the cleanup **after the current item**: the folder that is being removed is finished first, the rest is not started.
While a cleanup runs, **Start scan** is disabled. If you close the window, LeftoverScout asks "A cleanup is running. Stop
after the current item and exit?" **Yes** waits for the current item and then closes; **No** keeps running.

## The summary

The summary says in plain words what happened: how many items were moved to the Recycle Bin (or deleted permanently),
how many were removed only partly, skipped, blocked, not found, failed or not started (after **Cancel**), and how much
space was removed. A table lists every item with its **Outcome** and the **Reason** when it was not removed completely.

- If a warning says that Windows deleted something permanently instead of moving it to the Recycle Bin, those items
  cannot be restored.
- If a warning says that the journal could not be written completely, the cleanup itself was not affected, but
  [History](history.md) may be incomplete.

Buttons: **Open History** (the full record of the session), **Open Recycle Bin** and **Scan again** (switches to [Scan](scan.md)).

## After a cleanup

The scan result no longer matches the disk. [Results](results.md) and [Findings](findings.md) show the banner
"Results are out of date after a cleanup — scan again", and **Clean up selected…** stays disabled until a new scan has completed.

## Restoring from the Recycle Bin

Press **Open Recycle Bin**, right-click the folder and choose **Restore**. Restoring works only while the item is still in
the bin; emptying the bin removes it for good. Items removed in **Delete permanently** mode are not in the bin.

## What LeftoverScout promises

- Personal folders (Documents, Desktop, ...) and protected locations are never offered, and are blocked again here.
- Junctions, symbolic links and online-only cloud folders are never followed or downloaded.
- Every item is checked again right before it is removed, even after you confirmed.
- The default is the Recycle Bin; permanent deletion needs two confirmations.
- Every session is written to the journal before the first item and after each item, so [History](history.md) shows what happened even if the program stops.
- Nothing is sent anywhere.

## Tips

- Start with the **High** items and the biggest ones.
- Close the programs named in the yellow box first.
- Try the Recycle Bin mode first and empty the bin later, when you are sure nothing is missing.
- For **Low** and **Medium** items read the reason on [Findings](findings.md) first.

See also [Findings](findings.md), [History](history.md) and [Results](results.md).

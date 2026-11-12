# LeftoverScout user guide

> LeftoverScout is in early development. Today it can scan your profile, let you browse what takes
> space, show the programs installed on this computer, mark folders that probably belong to uninstalled programs and list what you could remove. Cleaning up is planned; the **History** page is ready to record it. This guide always describes what the app does now.

## What LeftoverScout does

Over the years, programs leave data in your Windows user profile (`C:\Users\<you>`):
settings, caches and logs that often stay behind even after the program is uninstalled.
LeftoverScout is built to:

1. **Scan** your profile and show what takes space. Scanning never changes anything.
2. **Explain** which folders probably belong to programs that are no longer installed, and why. The **Results** page shows this as **Leftover** labels, and the **Findings** page collects them in one list.
3. **Clean up** only the items you select, by default into the **Recycle Bin**, so you can restore them.

## Order of work

1. Open **Scan** and press **Start scan**. See [Scan](scan.md).
2. Open **Findings** to see what you could remove: leftovers and caches, biggest first, each with its reason. See [Findings](findings.md).
   To check an item, open **Results** (browse folders by size, see large files; look for **Leftover** and **In use** labels in the **Notes** column, see [Results](results.md)) or **Programs** (which programs are installed on this computer, see [Programs](programs.md)).
3. **Cleanup** will remove the items you select on **Findings**. See [Cleanup](cleanup.md). *(planned)*
4. **History** shows what each cleanup removed and what happened to every item. See [History](history.md).

The navigation order is **Scan**, **Results**, **Programs**, **Findings**, **Cleanup**, **History**, **Help**, **About**.
At the moment **Scan**, **Results**, **Programs**, **Findings**, **History**, **Help** and **About** have content.

## What is safe

- Scanning only reads folder listings. It does not open, change, move or delete your files.
- Collecting the installed programs only reads: the registry is never written to, and shortcut files are never opened.
- Nothing will be deleted without your explicit confirmation.
- Important system and personal locations will be protected and never offered for deletion.

## Requirements

- Windows 10 version 22H2 or Windows 11, 64-bit (x64).
- No installation and no administrator rights needed. LeftoverScout is a single portable file.
- No internet connection needed. LeftoverScout never sends any data.

## Topics

- [Getting started](getting-started.md)
- [Scan](scan.md)
- [Results](results.md)
- [Programs](programs.md)
- [Findings](findings.md)
- [Cleanup](cleanup.md)
- [History](history.md)
- [About](about.md)
- [Using this help](help.md)

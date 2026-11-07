# LeftoverScout user guide

> LeftoverScout is in early development. Today it can scan your profile and show what takes
> space. Explaining and cleaning up are planned. This guide always describes what the app does now.

## What LeftoverScout does

Over the years, programs leave data in your Windows user profile (`C:\Users\<you>`):
settings, caches and logs that often stay behind even after the program is uninstalled.
LeftoverScout is built to:

1. **Scan** your profile and show what takes space. Scanning never changes anything.
2. **Explain** which folders probably belong to programs that are no longer installed, and why.
3. **Clean up** only the items you select, by default into the **Recycle Bin**, so you can restore them.

## Order of work

1. Open **Scan** and press **Start scan**. See [Scan](scan.md).
2. **Results** will list suspected leftovers. See [Results](results.md). *(planned)*
3. **Cleanup** will remove the items you select. See [Cleanup](cleanup.md). *(planned)*
4. **History** will show what was removed. See [History](history.md). *(planned)*

At the moment only **Scan**, **Help** and **About** have content.

## What is safe

- Scanning only reads folder listings. It does not open, change, move or delete your files.
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
- [Cleanup](cleanup.md)
- [History](history.md)
- [About](about.md)
- [Using this help](help.md)

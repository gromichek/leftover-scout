# Getting started

## Starting LeftoverScout

LeftoverScout is portable. There is nothing to install: start `LeftoverScout.exe` from any
folder. It runs as your normal user and never asks for administrator rights.

The window has a navigation list on the left:

| Section | What it is for |
|---|---|
| **Scan** | Scans your user profile (read-only) and shows what takes space. See [Scan](scan.md). |
| **Results** | Shows where the space goes: browse folders by size and list large files. See [Results](results.md). |
| **Programs** | Lists the programs installed on this computer, collected by every scan. See [Programs](programs.md). |
| **Findings** | Lists what you could remove: leftovers and caches with size, confidence and reason. See [Findings](findings.md). |
| **Cleanup** | Will move selected items to the Recycle Bin. Planned. See [Cleanup](cleanup.md). |
| **History** | Will show what earlier cleanups removed. Planned. See [History](history.md). |
| **Help** | This help. See [Using this help](help.md). |
| **About** | Version, license, project link and the data folder. See [About](about.md). |

At the moment **Scan**, **Results**, **Programs**, **Findings**, **Help** and **About** have content; **Cleanup** and **History**
show a short note about what they will do.

## Only one window

LeftoverScout runs once per Windows user session. If you start it a second time while it is
already open (even minimized), the existing window is brought to the front and the second start
ends quietly.

## Where settings and logs are stored

LeftoverScout keeps its own files in a `data` folder **next to the program**:

- `data\settings.json`: window size and position, and the help language you chose.
- `data\logs\leftoverscout-<date>.log`: a diagnostic log. The seven newest files are kept.

Deleting the program folder removes all of it. Nothing is written to the registry.
The **About** page shows the exact folder and has an **Open data folder** button.

## The yellow information bar

If the program folder is not writable (for example, it is in `Program Files`), LeftoverScout
stores its files in `%LOCALAPPDATA%\LeftoverScout` instead and shows:

> Settings are stored in `<path>` because the program folder is not writable.

This is only a notice. Click **Dismiss** to hide it. To keep everything portable, move the
program to a folder you can write to.

## If a settings file is damaged

If `settings.json` cannot be read, LeftoverScout starts with default settings and writes a
warning to the log. The file is replaced with fresh settings.

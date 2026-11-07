# Getting started

## Starting LeftoverScout

LeftoverScout is portable. There is nothing to install: start `LeftoverScout.exe` from any
folder. It runs as your normal user and never asks for administrator rights.

The window has a navigation list on the left with five sections:

| Section | What it is for |
|---|---|
| Scan | Will scan your user profile (read-only). Available in Phase 1. |
| Results | Will list suspected leftovers and explain why. Available in Phase 1. |
| Cleanup | Will move selected items to the Recycle Bin. Available in Phase 2. |
| History | Will show what earlier cleanups removed. Available in Phase 2. |
| About | Version, license, project link and the data folder. |

At the moment only **About** has content; the other pages are placeholders.

## Only one window

LeftoverScout runs once per Windows user session. If you start it a second time while it is
already open (even minimized), the existing window is brought to the front and the second start
ends quietly.

## Where settings and logs are stored

LeftoverScout keeps its own files in a `data` folder **next to the program**:

- `data\settings.json` — window size and position.
- `data\logs\leftoverscout-<date>.log` — a diagnostic log. The seven newest files are kept.

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

# UnoTextPad

A small, fast, tabbed plain-text editor built with [Uno Platform](https://platform.uno/),
in the spirit of Notepad++.

Windows, macOS and Linux are served by a **single build** of the `net10.0-desktop` target
framework, which renders with Skia inside a native shell (Win32, AppKit, X11).

## Features

- **Multiple tabs** — open, reorder, and close documents; middle of the road keyboard support.
- **Session restore** — the tabs that were open when you quit come back when you start again,
  including buffers that were never saved to a file.
- **Light and dark mode** — a toggle in the toolbar; on first run it follows the operating system.
- **Font family and size** — any font installed on the machine, plus word wrap.
- **Format preserving** — a file's text encoding (UTF-8, UTF-8 BOM, UTF-16) and line endings
  (LF, CRLF, CR) are detected on open and written back unchanged.

## Keyboard shortcuts

`Ctrl` on Windows and Linux, `Cmd` on macOS (both are accepted everywhere).

| Shortcut | Action |
| --- | --- |
| `Ctrl/Cmd + N` | New tab |
| `Ctrl/Cmd + O` | Open files |
| `Ctrl/Cmd + S` | Save |
| `Ctrl/Cmd + Shift + S` | Save as |
| `Ctrl/Cmd + W` | Close tab |
| `Ctrl + Tab` | Next tab |

## Running it

```bash
dotnet run --project UnoTextPad -f net10.0-desktop
```

On Linux the Skia desktop shell needs Mesa, DBus and fontconfig installed; see
[the Uno Platform requirements](https://platform.uno/docs/articles/get-started-with-linux.html).

## Tests

```bash
dotnet test
```

Tests use [xUnit v3](https://xunit.net/), which runs on Microsoft.Testing.Platform. That
runner is selected for the solution by the `test` section of `global.json`.

## Where state is stored

Preferences and the session live in the per-user application data folder:

| Platform | Location |
| --- | --- |
| Windows | `%LOCALAPPDATA%\UnoTextPad\com.unotextpad.app\LocalState` |
| macOS | `~/Library/Application Support/UnoTextPad/com.unotextpad.app/LocalState` |
| Linux | `~/.local/share/UnoTextPad/com.unotextpad.app/LocalState` |

`settings.json` holds the preferences, `session.json` the list of open tabs, and `Backups/`
one file per tab that has unsaved changes. Tabs without unsaved changes are re-read from
disk instead of being copied, so the session stays small.

## Project layout

```
UnoTextPad/
  Features/
    Documents/   Document state, caret math, encodings, line endings and text-file I/O
    Editor/      Main page, editor orchestration, dialogs and file pickers
    Session/     Workspace snapshots, backup persistence and session restoration
    Settings/    Preferences, fonts and theme behavior
  Infrastructure/
    Storage/     Shared app-data paths and JSON persistence
    Windowing/   Access to the current application window
    DependencyInjection/  Application composition root
  Platforms/     Uno target-specific bootstrap code
UnoTextPad.Tests/
  Features/      Tests mirroring the production feature slices
  Infrastructure/  Tests for shared infrastructure
  TestInfrastructure/  Fakes and temporary test resources shared by slices
```

Code that changes for the same product capability is kept together. Cross-cutting adapters stay
under `Infrastructure`, while Uno's required platform-specific bootstrap remains under
`Platforms`. Every service is consumed through an interface and registered in
`Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, the single application
composition root.

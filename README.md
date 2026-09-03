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

## Publishing

Every platform is published from the same `net10.0-desktop` target framework, as a
self-contained app: the .NET runtime travels inside the app, so nothing has to be
installed on the machine that runs it. One publish profile per runtime identifier lives
in `UnoTextPad/Properties/PublishProfiles`.

Packaging runs the host platform's own tools, so each platform's installer has to be
built on that platform.

### macOS

There is no disk-image manifest to write by hand. `PackageFormat` set to `dmg` — which the
`osx-*` publish profiles do — makes the Uno SDK run three extra steps after `Publish`: it
renders `Assets/Icons/icon.svg` into an `icon.icns`, assembles `UnoTextPad.app` around the
published output with a generated `Info.plist`, and builds a disk image holding that bundle
beside a symlink to `/Applications`.

```bash
./publish-macos.sh            # Apple Silicon (default)
./publish-macos.sh x64        # Intel
./publish-macos.sh universal  # one bundle that runs on both
```

The first two are a single publish, and the script only picks the architecture:

```bash
dotnet publish UnoTextPad -f net10.0-desktop -c Release -r osx-arm64 -p:PublishProfile=osx-arm64
```

`UnoTextPad.dmg` lands beside the published output, in
`UnoTextPad/bin/Release/net10.0-desktop/osx-arm64/publish/`. Opening it shows the usual
drag-onto-`Applications` window, and from then on the app starts from Launchpad, Spotlight
or a double-click like any other, under the icon from `Assets/Icons`. It is about 70 MB
and unpacks to roughly 210 MB, most of which is the bundled runtime and the ReadyToRun
code the profiles compile ahead of time.

`universal` has more to it. A fat bundle cannot be published directly, so the script
publishes both architectures as bare bundles, merges them with the SDK's `UnoMergeBundles`
target, and builds the disk image from the merged result. That one lands in
`UnoTextPad/bin/Release/osx-universal/` instead, and is about twice the size, since it
carries two copies of everything native.

`PackageFormat` also takes `app` for the bare bundle and `pkg` for an installer package.

The bundle needs **macOS 15 or newer**: `LSMinimumSystemVersion` is read back from the .NET
apphost, which is built against that minimum.

Trimming is switched off in the profiles. ILLink needs well over half an hour on this
assembly graph, and what it removes from a Skia/XAML app tends to surface only at runtime;
`-p:PublishTrimmed=true` turns it on for anyone willing to test the result.

The disk-image step occasionally fails with `temp.dmg ... is being used by another
process` — `hdiutil` has not let go of the image by the time the SDK copies it. Running
the publish again clears it.

#### Signing and Gatekeeper

The published bundle is only ad-hoc signed. That is enough to run it on the machine that
built it, and enough for a disk image copied over with a USB stick or `scp`. Anything that
downloads the image — a browser, a mail client, AirDrop, Slack — attaches a quarantine flag,
and Gatekeeper then refuses to open an app that is not signed with a Developer ID
certificate and notarized by Apple. On a machine you control, clear the flag by hand:

```bash
xattr -d -r com.apple.quarantine /Applications/UnoTextPad.app
```

Distributing it to anyone else means joining the Apple Developer Program and setting these
MSBuild properties on the publish:

| Property | Purpose |
| --- | --- |
| `CodesignKey` | Name of the *Developer ID Application* certificate in the keychain. |
| `UnoMacOSHardenedRuntime` | `true`; notarization rejects bundles without it. |
| `UnoMacOSEntitlements` | Path to an entitlements plist, if the hardened runtime needs exceptions. |
| `DiskImageSigningKey` | Certificate used to sign the `.dmg` itself. |
| `PackageSigningKey` | *Developer ID Installer* certificate, for `PackageFormat=pkg`. |
| `UnoMacOSNotarizeKeychainProfile` | Profile stored by `xcrun notarytool store-credentials`. Setting it makes the publish submit and staple the result. |
| `UnoMacOSCustomInfoPlist` | Extra `Info.plist` entries merged into the generated one — file associations, for instance. |

### Windows

The `win-*` profiles publish a self-contained folder; `UnoTextPad.exe` inside it runs
without a .NET install.

```bash
dotnet publish UnoTextPad -f net10.0-desktop -c Release -r win-x64
```

### Linux

`PackageFormat=snap` builds a Snap package, which needs `snapcraft` and its dependencies
installed and has to be built on the distribution being targeted.

```bash
dotnet publish UnoTextPad -f net10.0-desktop -c Release -r linux-x64 -p:PackageFormat=snap
```

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

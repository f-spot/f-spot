# Migrating F-Spot to Avalonia

F-Spot was the GNOME photo manager, a C# application on GTK# and Mono. This tree is that application rebuilt on [Avalonia](https://avaloniaui.net) 12 and .NET 10. The interface was rewritten. GTK widgets were not ported.

The Avalonia client was added on the shared Core catalog on 11 October 2026 (`5a0efe315`). The GTK client, gtk-sharp, Mono.Addins, and the .NET Framework target are gone. `dotnet build` and `dotnet run` are the launch path. Mono is not required.

## Catalog

An existing library is still a `photos.db` file. Opening one does not rewrite the catalog. Import, browse, tag, rate, and search use that database.

A catalog-wide move of stored timestamps onto UTC is not in this port. That change would shift times already stored.

## What came across

The daily library is in the Avalonia client:

* Import photos, including assigning them to a roll.
* Browse, tag, rate, and search. The metadata pane reads tags with TagLibSharp.
* Edit: rotate left and right, crop, and exposure and contrast. Versions keep the original file.
* Slideshow transitions are cover, dissolve, and push. Fullscreen, print, send mail, and set as desktop background are in the client.
* Export to a folder, a zip, and a static HTML gallery.
* Update stored paths when a folder moves, merge another catalog, pair a raw file with its JPEG, and develop a raw file with an external ufraw.

Keyboard shortcuts for browse, tag, rate, and navigate are in the README.

## What was left behind

These GTK features were not rewritten:

* Flickr, 23hq, and Zooomr export. An old catalog can still hold the export record. SmugMug was already gone the same way.
* CD burning through Brasero.
* The live web gallery.
* The Mono.Addins host. Editors and tools are compiled into the client. A plugin host is not required until an extension ships outside this repository.
* GTK translations. Avalonia strings are English. The Mallard pages under `help/` are still in the tree. Avalonia does not display them yet.

Retroactive Roll, which grouped the selection into a new roll, was not part of the port.

## Not in this port

HEIC, an in-app raw decoder, and watching imported folders for new files are later work. External raw develop remains ufraw. There is no separate New or Open library window.

Colour management still uses lcmsNET. Soft proof, 16-bit pixels, and OkLab or `.cube` lookups are not in this port. The macOS publish copies `lib/native/osx-x64/liblcms2.2.dylib` next to the app, because that is the library the project file ships when the build runs on macOS. It is an x86_64 binary. Windows and Linux builds do not bundle liblcms2. Colour transforms on those systems need a system Little CMS 2. Opening and browsing a library does not load it.

## Builds

Branch pushes and pull requests run the CI workflow. They build and test. They do not publish.

A push of any tag runs `.github/workflows/publish.yml`. That workflow publishes a self-contained folder for each runtime and attaches the zip to the GitHub Release for the tag:

| Runtime | Runner | Archive |
| --- | --- | --- |
| `win-x64` | `windows-latest` | `F-Spot-win-x64.zip` |
| `osx-arm64` | `macos-latest` | `F-Spot-osx-arm64.zip` |
| `linux-x64` | `ubuntu-latest` | `F-Spot-linux-x64.zip` |

The archives are not signed or notarized. The macOS build is not sandboxed and is not an App Store app. The publish is a folder of files, not a single-file binary, so the native Skia libraries stay beside the executable.

Unzip and run `FSpot.Avalonia` (`FSpot.Avalonia.exe` on Windows). On macOS, Gatekeeper can block the unsigned binary. On Linux the .NET runtime is inside the archive. The process still needs the usual X11 libraries (X11, ICE, SM, and fontconfig). Wayland was not the target.

GTK screenshots are the figures under `help/C/figures`. Avalonia screenshots are added to the GitHub Release for the tag. They are not stored in this file.

## Licence

The tree is MIT. See `LICENSE`. `COPYING` is the same MIT grant, with the older Novell copyright. Historical F-Spot releases were GPL. This rewrite is the MIT licence in the repository.

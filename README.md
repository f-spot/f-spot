# F-Spot Photo Manager

No website currently. PLEASE DO NOT VISIT THE OLD WEBSITE

# WARNING

The code base is in heavy flux right now. It might be completely broken right now.

Known issues:

* Doesn't start
* AdjustTimeDialog is completely broken due to removing Gnome.DateTime

# Build status

[![CI](https://github.com/decriptor/FSpot.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/decriptor/FSpot.Avalonia/actions/workflows/ci.yml)

GitHub Actions (`CI`) runs restore/build/test on Ubuntu, macOS, and Windows for `main` and `modernize`.

# Requirements

Build:

* .NET SDK 10 (`global.json` pins `10.0.400`, `rollForward: latestFeature`)

Runtime (GTK 2 client):

* Mono, to run the `net472` `f-spot.exe`
* GTK 2
* liblcms2

gtk-sharp 2.12 is vendored under `lib/gtk-sharp-2.12/` for compile. NuGet packages (TagLibSharp, MSTest, Moq, and the rest) come from `Directory.Packages.props` via `dotnet restore`.

# Build

	dotnet build F-Spot.slnx
	dotnet test F-Spot.Tests.slnf --filter "TestCategory!=RequiresGdk&TestCategory!=RequiresGtk"

GTK and Avalonia builds rasterize `icons/src/*.svg` into `icons/*.png` when a PNG is missing or the SVG is newer (`Icons.targets`). Commit the generated PNGs; they are not rewritten on an up-to-date tree. PNG-only artwork (no SVG) is left alone. Force a refresh with:

	dotnet run --project tools/RasterizeIcons -- icons

Linux/Freedesktop layout (desktop files, hicolor icons, English Mallard help,
PATH wrappers) lives in `LinuxDesktop.targets`. Translated `help/` trees are
parked; only `help/C` is installed. A Gtk build stages icons under
`bin/share/f-spot`. To install into a prefix:

	dotnet msbuild LinuxDesktop.targets -t:InstallLinux -p:Prefix=/usr/local

# Launch

From the build tree: `bin/f-spot --debug` (Linux; next to `f-spot.exe`).

![Alt](https://repobeats.axiom.co/api/embed/fade091fb5fad392ab9b7997918d611c788fabee.svg "Repobeats analytics image")

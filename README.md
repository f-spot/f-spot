# F-Spot Photo Manager

No website currently. PLEASE DO NOT VISIT THE OLD WEBSITE

# Build status

[![CI](https://github.com/decriptor/FSpot.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/decriptor/FSpot.Avalonia/actions/workflows/ci.yml)

GitHub Actions (`CI`) restores and builds `F-Spot.slnx` on Ubuntu, macOS, and Windows for pushes and pull requests to `main` and `modernize`. The test job runs `FSpot.Tests`, `FSpot.Gtk.Tests` (skipping tests that need Gdk or GTK), `FSpot.Avalonia.Tests`, and `RasterizeIcons.Tests`.

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

Committed `icons/*.png` are not regenerated during `dotnet build`. After you change an SVG under `icons/src/`, refresh the PNGs and commit them:

	dotnet run --project tools/RasterizeIcons -- icons

Linux/Freedesktop layout (desktop files, hicolor icons, English Mallard help,
PATH wrappers) lives in `LinuxDesktop.targets`. Translated `help/` trees are
parked; only `help/C` is installed. A Gtk build stages icons under
`bin/share/f-spot`. To install into a prefix:

	dotnet msbuild LinuxDesktop.targets -t:InstallLinux -p:Prefix=/usr/local

# Launch

Avalonia client (modern .NET, including macOS):

	dotnet run --project src/Clients/FSpot.Avalonia

The built binary is `artifacts/fspot-avalonia/FSpot.Avalonia`.

Crash reports go to Sentry unless you turn them off in Preferences. `FSPOT_SENTRY=0` (or `false`, `off`, or `no`) forces them off. The same setting is stored for the GTK client. Events are scrubbed of home-directory paths and tag names before they are sent. The About window states this.

GTK 2 client (Linux, Mono): `bin/f-spot --debug`, next to `f-spot.exe`. Print opens the page-setup window and prints. Send Mail prepares the selected photos and opens the mail client. Set as Background changes the desktop wallpaper on macOS, GNOME, and Windows.

![Alt](https://repobeats.axiom.co/api/embed/fade091fb5fad392ab9b7997918d611c788fabee.svg "Repobeats analytics image")

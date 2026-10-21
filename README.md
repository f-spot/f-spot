# F-Spot Photo Manager

No website currently. PLEASE DO NOT VISIT THE OLD WEBSITE

# Build status

[![CI](https://github.com/decriptor/FSpot.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/decriptor/FSpot.Avalonia/actions/workflows/ci.yml)

GitHub Actions (`CI`) restores and builds `F-Spot.slnx` on Ubuntu, macOS, and Windows for pushes and pull requests to `main` and `modernize`. The test job runs `FSpot.Tests`, `FSpot.Avalonia.Tests`, and `RasterizeIcons.Tests`.

# Requirements

Build:

* .NET SDK 10 (`global.json` pins `10.0.400`, `rollForward: latestFeature`)

Runtime:

* liblcms2, for colour management

NuGet packages (TagLibSharp, MSTest, Moq, and the rest) come from `Directory.Packages.props` via `dotnet restore`.

# Build

	dotnet build F-Spot.slnx
	dotnet test F-Spot.Tests.slnf

Committed `icons/*.png` are not regenerated during `dotnet build`. After you change an SVG under `icons/src/`, refresh the PNGs and commit them:

	dotnet run --project tools/RasterizeIcons -- icons

# Launch

Avalonia client (modern .NET, including macOS):

	dotnet run --project src/Clients/FSpot.Avalonia

The built binary is `artifacts/fspot-avalonia/FSpot.Avalonia`.

The current manual is the English Mallard pages in `help/C`. Avalonia does not show them yet. A new manual can replace that tree later. Translated pages are the `help/<locale>` catalogs.

Crash reports go to Sentry unless you turn them off in Preferences. `FSPOT_SENTRY=0` (or `false`, `off`, or `no`) forces them off. Events are scrubbed of paths (including quoted paths and paths with spaces), tag names, and the text of structured fields before they are sent. A file name that is not part of a path and is not a tag is left as it is. The About window states whether reports are on. Print opens the page-setup window and prints. Send Mail prepares the selected photos and opens the mail client. Set as Background changes the desktop wallpaper on macOS, GNOME, and Windows.

![Alt](https://repobeats.axiom.co/api/embed/fade091fb5fad392ab9b7997918d611c788fabee.svg "Repobeats analytics image")

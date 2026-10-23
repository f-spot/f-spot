# F-Spot Photo Manager

The project site is <https://f-spot.app>. Help in the application opens that site. Do not use the old website.

The port from the GTK client is written up in [MIGRATION.md](MIGRATION.md).

# Build status

[![CI](https://github.com/f-spot/f-spot/actions/workflows/ci.yml/badge.svg)](https://github.com/f-spot/f-spot/actions/workflows/ci.yml)

GitHub Actions (`CI`) restores and builds `F-Spot.slnx` on Ubuntu, macOS, and Windows for pushes and pull requests to `main` and `modernize`. The test job runs `FSpot.Tests`, `FSpot.Avalonia.Tests`, and `RasterizeIcons.Tests`.

Any git tag publishes self-contained Windows, macOS, and Linux builds and attaches them to that tag's GitHub Release. Branch builds do not publish. See [MIGRATION.md](MIGRATION.md).

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

Avalonia client (modern .NET, including macOS), from the repository root:

	dotnet run

`dotnet run F-Spot.slnx` starts the same client. The solution name is not a project file, and `build.msbuild` is the build driver, so the root `F-Spot.csproj` is what `dotnet run` launches.

The built binary is `artifacts/fspot-avalonia/FSpot.Avalonia`.

The current manual is the English Mallard pages in `help/C`. Avalonia does not show them yet. A new manual can replace that tree later. Translated pages are the `help/<locale>` catalogs.

# Keyboard

In the library grid, the arrow keys move the selection. `H` `J` `K` `L` do the same. `Home` and `End` jump to the first and last photo, `Page Up` and `Page Down` move by a page, `Space` toggles the photo in the selection, and `Enter` opens it.

`Delete` removes the selected photos from the catalog after a confirmation. The files stay on disk. `Shift+Delete` deletes the files from the drive after a separate confirmation, including every version. Fullscreen uses the same two keys.

`0` through `5` set the rating. `Alt` plus a digit does the same. Digits do not rate while a text box is focused. In the open photo, `0`, `1`, and `2` zoom, so rate with `Alt` and a digit there. `T` opens the tag box. `Ctrl+T` attaches the selected tag.

`[` and `]` rotate left and right. `/` or `Ctrl+F` opens Find. `V` toggles the loupe. `F5` starts the slideshow, `F11` the fullscreen view, `F9` the sidebar, and `F1` Help. `Escape` closes Find.

Crash reports go to Sentry unless you turn them off in Preferences. `FSPOT_SENTRY=0` (or `false`, `off`, or `no`) forces them off. Events are scrubbed of paths (including quoted paths and paths with spaces), tag names, and the text of structured fields before they are sent. A file name that is not part of a path and is not a tag is left as it is. The About window states whether reports are on. Print opens the page-setup window and prints. Send Mail prepares the selected photos and opens the mail client. Set as Background changes the desktop wallpaper on macOS, GNOME, and Windows.

![Alt](https://repobeats.axiom.co/api/embed/fade091fb5fad392ab9b7997918d611c788fabee.svg "Repobeats analytics image")

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

F-Spot is a personal photo management application for the GNOME desktop environment, written in C# and using Mono/.NET, GTK#, and various native libraries. The codebase is in heavy flux with known issues including application startup problems and broken dialogs.

## Build System

F-Spot uses a hybrid build system supporting both traditional autotools (for Linux/Unix) and MSBuild/.NET (for cross-platform development).

### Linux/Unix Build (Autotools)
```bash
# Traditional autotools build
./autogen.sh
make
sudo make install

# Quick setup script
./prep_linux_build.sh prefix={path}  # e.g., ~/staging
```

### Cross-Platform Build (MSBuild)
```bash
# Restore NuGet packages
dotnet restore F-Spot.sln

# Build entire solution
dotnet build F-Spot.sln

# Or use MSBuild project
msbuild build.proj

# Run the application
msbuild build.proj -t:Run
```

### Testing
```bash
# Run unit tests (autotools)
cd tests && make test

# Run unit tests (MSBuild)
msbuild build.proj -t:Test

# Manual NUnit execution
packages/Nunit.ConsoleRunner.3.12.0/tools/nunit3-console.exe --labels=OnOutputOnly tests/FSpot.UnitTest.dll tests/FSpot.Gtk.UnitTest.dll tests/Hyena.UnitTest.dll
```

## Architecture

### High-Level Structure
- **Core (`src/Core/`)**: Core photo management logic, database layer, and business objects
  - `FSpot/`: Main business logic, database, imaging, import, query system
  - `FSpot.Resources/`: Embedded resources and localization
  - `FSpot.UnitTest/`: Core unit tests
- **Clients (`src/Clients/`)**: User-facing applications
  - `FSpot.Gtk/`: Main GTK# desktop application
  - `FSpot.Console/`: Command-line interface (stub)
- **Extensions (`src/Extensions/`)**: Plugin-based extensibility system
  - `Editors/`: Image editing plugins (BW, Blackout, Flip, Pixelate, Resize)
  - `Exporters/`: Export plugins (CD, Flickr, Folder, Gallery, Zip, Facebook, PicasaWeb, SmugMug)
  - `Tools/`: Utility tools (ChangePhotoPath, DevelopInUFraw, LiveWebGallery, MergeDb, RawPlusJpeg, RetroactiveRoll)
  - `Transitions/`: Slideshow transition effects (Cover, Dissolve, Push)
- **Libraries (`lib/`)**: Support libraries
  - `Hyena/` & `Hyena.Gui/`: UI framework and utilities (borrowed from Banshee)
  - `gtk-sharp-beans/`: Custom GTK# extensions
  - `libfspot/`: Native C library for screen utilities

### Key Technologies
- **Mono/.NET**: Runtime and framework (requires Mono 6.0+ or .NET)
- **GTK#**: GUI toolkit (version 2.12.2+)
- **SQLite**: Database backend for photo metadata
- **TagLib#**: Metadata reading/writing (via NuGet)
- **Cairo**: Graphics rendering
- **lcms2**: Color management
- **Mono.Addins**: Plugin architecture

### Database Layer
- SQLite-based storage in `src/Core/FSpot/Database/`
- Stores: `PhotoStore`, `TagStore`, `RollStore`, `JobStore`, `MetaStore`, `ExportStore`
- Database versioning and migration system via `Updater.cs`
- Test databases in `tests/data/` for different F-Spot versions

### Plugin Architecture
- Uses Mono.Addins framework
- Extensions defined via `.addin.xml` files
- Extension points: Editors, Exporters, Tools, Transitions
- Modular loading system allows runtime plugin discovery

## Development Environment

### Prerequisites
- GNOME development libraries 2.4+
- Mono 6.0+ or .NET
- gtk-sharp 2.12.2+
- SQLite 2.8.6+
- liblcms2+
- hicolor-icon-theme 0.10+
- adwaita-icon-theme 3.18.0+
- NuGet package manager

### IDE Setup
For MonoDevelop/Visual Studio:
1. Run `./autogen.sh` first
2. Build dependencies: `cd build && make && cd ../lib && make`
3. Install libfspot: `cd libfspot && sudo make install`
4. Open `F-Spot.sln` in IDE

### Coding Standards
- Follow Mono coding conventions (see HACKING file)
- Use tabs for indentation
- C-style braces for methods, same-line braces for control structures
- Space before parentheses in method calls
- Private variables use under_scored naming
- Tag buggy code with "FIXME" comments
- Unit tests follow Arrange/Act/Assert pattern

### Certificate Setup
On Fedora/Mageia systems, sync root certificates:
```bash
# Debian/Ubuntu
sudo cert-sync /etc/ssl/certs/ca-certificates.crt

# Red Hat derivatives
sudo cert-sync /etc/pki/tls/certs/ca-bundle.crt
```

## Common Issues

### Current Known Problems
- Application doesn't start
- AdjustTimeDialog broken (missing Gnome.DateTime)
- Autotools configuration issues due to heavy changes
- Build system in flux

### Platform-Specific Notes
- Linux: Primary development platform, uses autotools
- Windows: Supported via MSBuild, may require certificate setup
- macOS: Supported via MSBuild

## File Locations

### Configuration
- Main executable: `src/Clients/FSpot.Gtk/f-spot.in` (template)
- MSBuild entry: `build.proj`
- Solution file: `F-Spot.sln`
- Build props: `fspot.props`

### Build Outputs
- Binary output: `bin/` directory
- Test assemblies: `tests/*.UnitTest.dll`

### Resources
- Icons and images: `src/Core/FSpot.Resources/Resources/`
- UI definitions: `src/Clients/FSpot.Gtk/ui/`
- Localization: `po/` directory
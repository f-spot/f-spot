# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

F-Spot is a GNOME photo management application written in C# targeting .NET Framework 4.7.2 (Mono). The codebase is undergoing heavy refactoring and may be unstable.

## Build Commands

```bash
# Full build (Linux) - requires Mono 6.0+, gtk-sharp 2.12+
./autogen.sh --enable-tests
make

# Run tests
make test

# Build with MSBuild/dotnet directly (after autogen)
msbuild F-Spot.sln
```

### Prerequisites (Ubuntu/Debian)
```bash
sudo apt install automake libtool intltool nuget
sudo apt install libgtk2.0-dev libglib2.0-dev liblcms2-dev libjpeg-dev
sudo apt install libgtk2.0-cil-dev libglib2.0-cil-dev gtk-sharp2-gapi
```

## Architecture

### Source Layout
- `src/Core/FSpot/` - Core library: database, imaging, import, query, settings, thumbnails
- `src/Clients/FSpot.Gtk/` - Main GTK# application (entry point: f-spot executable)
- `src/Clients/FSpot.Console/` - Console test client for database operations
- `src/Extensions/` - Mono.Addins-based plugin system:
  - `Editors/` - Photo editing plugins (BW, Blackout, Flip, Pixelate, Resize)
  - `Exporters/` - Export plugins (CD, Facebook, Flickr, Folder, Gallery, PicasaWeb, SmugMug, Zip)
  - `Tools/` - Utility plugins (ChangePhotoPath, DevelopInUFRaw, LiveWebGallery, MergeDb)
  - `Transitions/` - Slideshow transitions (Cover, Dissolve, Push)

### Shared Libraries (`lib/`)
- `Hyena/` - Core utilities and data structures
- `Hyena.Gui/` - GTK# UI helpers and widgets
- `gtk-sharp-beans/` - Extended GTK# bindings
- `Mono.Google/`, `SmugMugNet/` - API clients for photo services

### Test Projects
- `src/Core/FSpot.UnitTest/` - Core library tests
- `src/Clients/FSpot.Gtk.UnitTest/` - GTK client tests
- `lib/Hyena.UnitTest/` - Hyena library tests

## Coding Style (Mono Conventions)

- Use **tabs** for indentation
- **Space before parentheses**: `Method (args)`, `array [index]`
- Braces on **same line** for control flow, **new line** for methods/types
- Properties: brace on same line as declaration
- Switch cases at same indentation as switch keyword
- Private/local variables: `under_scored` (not camelCase)
- Tag bugs with `FIXME` (not XXX or TODO)
- Unit tests follow Arrange/Act/Assert pattern

## Key Dependencies

- **gtk-sharp 2.12** - GTK# bindings (from GAC)
- **Mono.Addins** - Extension/plugin framework (in `external/`)
- **TagLibSharp** - Photo metadata reading
- **Serilog** - Logging
- **TinyIoC** - Dependency injection
- **NUnit/Moq/Shouldly** - Testing

## Configuration

- `Directory.Build.props` - Central MSBuild properties
- `Directory.Packages.props` - Central NuGet package versions
- `.editorconfig` - Code style settings
- Output goes to `bin/` with extensions in `bin/Extensions/`

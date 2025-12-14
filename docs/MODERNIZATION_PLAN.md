# F-Spot Modernization Plan: Migration to Avalonia UI

## Executive Summary

This plan outlines a phased approach to modernize F-Spot from GTK# 2.x / .NET Framework 4.7.2 to Avalonia UI / .NET 8+. The migration addresses the current instability issues while enabling cross-platform deployment (Windows, macOS, Linux, and potentially mobile/web).

**Estimated Timeline**: 9-12 months (phased approach allows incremental delivery)

---

## Current State Analysis

### Architecture Issues Identified

| Layer | Status | Issues |
|-------|--------|--------|
| Database | Good | Clean interfaces, Hyena.Data.Sqlite abstraction |
| Settings | Good | UI-agnostic |
| FileSystem | Good | No GTK# dependencies |
| Query | Good | Clean business logic |
| **Imaging** | **Critical** | Returns `Gdk.Pixbuf` throughout |
| **Thumbnail** | **Critical** | `ThumbnailService` returns `Pixbuf` |
| **Core/Tag** | **Problem** | Domain model stores `Pixbuf Icon` |
| **Utils** | **Problem** | GtkUtil, PixbufUtils mixed with business logic |
| **Gui (in Core)** | **Problem** | 21 GTK widgets in wrong layer |

### GTK# Coupling Points (Must Address)

```
src/Core/FSpot/
├── Imaging/BaseImageFile.cs      → Returns Gdk.Pixbuf
├── Imaging/ImageLoaderThread.cs  → Uses Gdk threading
├── Thumbnail/ThumbnailService.cs → Returns Gdk.Pixbuf
├── Core/Tag.cs                   → Stores Gdk.Pixbuf Icon
├── Utils/PixbufUtils.cs          → Pixbuf transformations
├── Utils/GdkUtils.cs             → P/Invoke to libgdk
├── Utils/GtkUtil.cs              → Menu building (UI code)
└── Gui/FSpot.Widgets/            → 21 custom GTK widgets
```

---

## Target Architecture

### Technology Stack

| Component | Current | Target |
|-----------|---------|--------|
| UI Framework | GTK# 2.12 | Avalonia UI 11.x |
| Runtime | .NET Framework 4.7.2 (Mono) | .NET 8 LTS |
| Image Library | Gdk.Pixbuf | SkiaSharp / ImageSharp |
| MVVM | None (code-behind) | CommunityToolkit.Mvvm |
| DI Container | TinyIoC | Microsoft.Extensions.DI |
| Database | Hyena.Data.Sqlite | Keep (works with .NET 8) |
| Logging | Serilog | Keep |

### Proposed Project Structure

```
F-Spot.sln
├── src/
│   ├── Core/
│   │   ├── FSpot.Core/              # Domain models, interfaces (no UI)
│   │   ├── FSpot.Database/          # Data access layer
│   │   ├── FSpot.Imaging/           # Image processing (ImageSharp/SkiaSharp)
│   │   └── FSpot.Services/          # Business logic services
│   │
│   ├── Application/
│   │   ├── FSpot.ViewModels/        # MVVM ViewModels (shared)
│   │   └── FSpot.Application/       # App services, DI setup
│   │
│   ├── Clients/
│   │   ├── FSpot.Desktop/           # Avalonia desktop app
│   │   ├── FSpot.Desktop.Windows/   # Windows-specific
│   │   ├── FSpot.Desktop.Linux/     # Linux-specific
│   │   └── FSpot.Desktop.macOS/     # macOS-specific
│   │
│   └── Extensions/                   # Keep plugin architecture
│       ├── FSpot.Extensions.Core/   # Extension interfaces
│       └── ... (Editors, Exporters, Tools)
│
├── lib/
│   ├── Hyena/                       # Keep, migrate to .NET 8
│   └── Hyena.Data.Sqlite/           # Keep
│
└── tests/
    ├── FSpot.Core.Tests/
    ├── FSpot.Imaging.Tests/
    └── FSpot.ViewModels.Tests/
```

---

## Migration Phases

### Phase 1: Foundation & Abstractions (Weeks 1-8)

**Goal**: Create abstraction layer without breaking existing GTK# app

#### 1.1 Image Abstraction Layer

Create UI-agnostic image interfaces:

```csharp
// FSpot.Core/Imaging/IImage.cs
public interface IImage : IDisposable
{
    int Width { get; }
    int Height { get; }
    byte[] GetPixelData();
    Stream AsStream(ImageFormat format);
    IImage Resize(int width, int height);
    IImage Rotate(RotateDirection direction);
}

// FSpot.Core/Imaging/IImageLoader.cs
public interface IImageLoader
{
    Task<IImage> LoadAsync(Uri uri, CancellationToken ct = default);
    Task<IImage> LoadThumbnailAsync(Uri uri, int maxSize, CancellationToken ct = default);
}

// FSpot.Core/Imaging/IThumbnailService.cs
public interface IThumbnailService
{
    Task<IImage?> GetThumbnailAsync(IPhoto photo, ThumbnailSize size);
    Task GenerateThumbnailAsync(IPhoto photo);
    void InvalidateThumbnail(IPhoto photo);
}
```

#### 1.2 Domain Model Cleanup

Remove Pixbuf from domain models:

```csharp
// Before (Tag.cs)
public Gdk.Pixbuf Icon { get; set; }

// After
public byte[]? IconData { get; set; }
public string? IconPath { get; set; }
```

#### 1.3 Create GTK# Adapter

Implement interfaces with existing GTK# code (keeps app working):

```csharp
// Temporary adapter during migration
public class GdkImageAdapter : IImage
{
    private readonly Gdk.Pixbuf _pixbuf;

    public int Width => _pixbuf.Width;
    public int Height => _pixbuf.Height;
    // ... implement interface using Pixbuf
}
```

#### 1.4 Deliverables
- [ ] `IImage`, `IImageLoader`, `IThumbnailService` interfaces
- [ ] GTK# adapter implementations
- [ ] Domain models cleaned of Pixbuf references
- [ ] Existing app still functional with adapters

---

### Phase 2: Core Library Modernization (Weeks 9-16)

**Goal**: Migrate core library to .NET 8, implement modern image backend

#### 2.1 .NET 8 Migration

Update project files:

```xml
<!-- FSpot.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

#### 2.2 SkiaSharp Image Implementation

```csharp
// FSpot.Imaging/SkiaImage.cs
public class SkiaImage : IImage
{
    private readonly SKBitmap _bitmap;

    public int Width => _bitmap.Width;
    public int Height => _bitmap.Height;

    public IImage Resize(int width, int height)
    {
        var resized = _bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
        return new SkiaImage(resized);
    }

    public IImage Rotate(RotateDirection direction)
    {
        // Use SkiaSharp rotation
    }
}
```

#### 2.3 Move Widgets Out of Core

Relocate `src/Core/FSpot/Gui/FSpot.Widgets/` to client layer:
- These become reference implementations for Avalonia rewrites
- Core library becomes truly UI-agnostic

#### 2.4 Deliverables
- [ ] FSpot.Core targeting .NET 8
- [ ] FSpot.Imaging with SkiaSharp backend
- [ ] All widgets moved to presentation layer
- [ ] Unit tests passing on .NET 8

---

### Phase 3: Avalonia Application Shell (Weeks 17-24)

**Goal**: Create working Avalonia app with basic photo browsing

#### 3.1 Project Setup

```bash
dotnet new avalonia.app -n FSpot.Desktop -o src/Clients/FSpot.Desktop
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Extensions.DependencyInjection
```

#### 3.2 Core Views to Implement

| View | Priority | Description |
|------|----------|-------------|
| MainWindow | P0 | App shell with sidebar, toolbar, content area |
| PhotoGridView | P0 | Thumbnail grid (virtualized) |
| PhotoView | P0 | Single photo viewer with zoom/pan |
| SidebarView | P1 | Tags, folders, timeline navigation |
| ImportDialog | P1 | Photo import workflow |
| TagEditor | P2 | Create/edit tags |
| PreferencesDialog | P2 | Settings UI |

#### 3.3 MVVM Structure

```csharp
// ViewModels/MainWindowViewModel.cs
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPhotoStore _photoStore;
    private readonly IThumbnailService _thumbnails;

    [ObservableProperty]
    private ObservableCollection<PhotoViewModel> _photos = new();

    [ObservableProperty]
    private PhotoViewModel? _selectedPhoto;

    [RelayCommand]
    private async Task ImportPhotosAsync() { }

    [RelayCommand]
    private async Task ExportSelectedAsync() { }
}
```

#### 3.4 Key Avalonia Controls Needed

```xml
<!-- Photo Grid with Virtualization -->
<ItemsRepeater Items="{Binding Photos}"
               ItemTemplate="{StaticResource PhotoThumbnailTemplate}">
    <ItemsRepeater.Layout>
        <UniformGridLayout MinItemWidth="150" MinItemHeight="150"/>
    </ItemsRepeater.Layout>
</ItemsRepeater>

<!-- Photo Viewer with Pan/Zoom -->
<Panel>
    <Image Source="{Binding CurrentPhoto.Image}"
           RenderTransform="{Binding ImageTransform}"/>
</Panel>
```

#### 3.5 Deliverables
- [ ] Avalonia app shell running
- [ ] Photo grid view with thumbnails
- [ ] Single photo view
- [ ] Basic sidebar navigation
- [ ] Can browse existing F-Spot database

---

### Phase 4: Feature Parity (Weeks 25-36)

**Goal**: Implement remaining features, migrate extensions

#### 4.1 Features to Port

| Feature | Complexity | Notes |
|---------|------------|-------|
| Timeline view | Medium | Date-based grouping |
| Tag management | Medium | Hierarchical tags |
| Face detection regions | High | Port existing logic |
| Photo editing | High | Color, crop, rotate |
| Slideshow | Medium | Transitions system |
| Import workflow | Medium | Camera, folder import |
| Export plugins | Medium | Migrate extension system |
| Search/Query | Low | Query logic is clean |
| Ratings/Flags | Low | UI only |

#### 4.2 Extension System Migration

Migrate from Mono.Addins to modern plugin system:

```csharp
// FSpot.Extensions.Core/IExporter.cs
public interface IExporter
{
    string Name { get; }
    string Description { get; }
    Task ExportAsync(IReadOnlyList<IPhoto> photos, ExportOptions options);
}

// Load via reflection or MEF
[Export(typeof(IExporter))]
public class FlickrExporter : IExporter { }
```

#### 4.3 Platform-Specific Features

| Platform | Features |
|----------|----------|
| Linux | XDG directories, DBus integration, tray icon |
| Windows | Thumbnail handler, jump lists |
| macOS | Touch Bar, Finder integration |

---

### Phase 5: Polish & Release (Weeks 37-44)

- Performance optimization (virtualization, caching)
- Accessibility (screen reader, keyboard navigation)
- Localization (port existing .po files)
- Installer/packaging (Flatpak, MSIX, DMG)
- Documentation
- Beta testing

---

## Technical Decisions

### Why Avalonia over Alternatives?

| Framework | Pros | Cons | Decision |
|-----------|------|------|----------|
| **Avalonia** | True cross-platform, Skia rendering, WPF-like, MIT license, mature | Learning curve from GTK | **Selected** |
| MAUI | Official Microsoft | No Linux desktop, mobile-focused | Rejected |
| Uno Platform | Wide platform support | Complex, larger runtime | Considered |
| GTK4 + gir.core | Stay in GTK ecosystem | Limited .NET tooling, fewer developers | Considered |

### Image Library Choice

| Library | Pros | Cons | Decision |
|---------|------|------|----------|
| **SkiaSharp** | Fast, Avalonia uses Skia, good format support | Larger binary | **Primary** |
| ImageSharp | Pure .NET, no native deps | Slower for some ops | Fallback |
| LibVips | Very fast | Native dependency | Consider for batch |

### Database Migration

Keep SQLite with Hyena.Data.Sqlite or migrate to EF Core:

**Recommendation**: Keep Hyena.Data.Sqlite initially, consider EF Core later
- Existing schema works
- Reduces migration risk
- Can migrate data layer separately

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Extended timeline | High | Phased delivery, working app at each phase |
| Performance regression | Medium | Benchmark critical paths, virtualization |
| Data migration issues | High | Keep same SQLite schema initially |
| Plugin compatibility | Medium | Provide migration guide, adapters |
| Platform-specific bugs | Medium | CI on all platforms from Phase 3 |

---

## Success Metrics

1. **Phase 1**: Existing GTK# app works with abstraction layer
2. **Phase 3**: Can browse 10,000+ photos smoothly in Avalonia
3. **Phase 4**: Feature parity with current F-Spot
4. **Phase 5**:
   - Startup time < 2 seconds
   - Memory usage < 500MB for 50K photo library
   - Works on Windows 10+, Ubuntu 22.04+, macOS 12+

---

## Getting Started

### Prerequisites for Development

```bash
# Install .NET 8 SDK
# Ubuntu
sudo apt install dotnet-sdk-8.0

# Install Avalonia templates
dotnet new install Avalonia.Templates

# IDE: JetBrains Rider (recommended) or VS Code with Avalonia extension
```

### First Steps

1. Create feature branch: `git checkout -b feature/avalonia-migration`
2. Add Avalonia project alongside existing GTK# app
3. Implement `IImage` interface
4. Create adapters for both GTK# (Pixbuf) and Avalonia (SkiaSharp)
5. Incrementally migrate

---

## Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Avalonia GitHub](https://github.com/AvaloniaUI/Avalonia)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [SkiaSharp](https://github.com/mono/SkiaSharp)
- [From WPF to Avalonia Guide](https://avaloniaui.net/blog/from-wpf-to-avalonia-a-guide-for-net-developers-exploring-cross-platform-ui-frameworks)

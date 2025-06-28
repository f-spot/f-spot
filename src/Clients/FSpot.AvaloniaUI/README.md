# F-Spot AvaloniaUI Client

This is a modern cross-platform client for F-Spot Photo Manager built with AvaloniaUI.

## Features

### Implemented
- ✅ **Modern UI Framework**: Built with AvaloniaUI 11.x for cross-platform compatibility
- ✅ **MVVM Architecture**: Clean separation of concerns with ReactiveUI
- ✅ **Photo Browser**: Grid-based photo browsing with thumbnails
- ✅ **Photo Viewer**: Full-size image display with zoom controls
- ✅ **Tag Management**: Hierarchical tag system integration
- ✅ **Database Integration**: Uses existing F-Spot SQLite database
- ✅ **Logging**: Comprehensive logging with Serilog
- ✅ **Dependency Injection**: Modern DI container setup

### In Progress
- 🔄 **Thumbnail Generation**: SkiaSharp-based thumbnail creation
- 🔄 **Image Import**: File dialog and batch import functionality
- 🔄 **Search**: Advanced photo search capabilities

### Planned
- 📋 **Image Editing**: Basic editing tools integration
- 📋 **Export**: Web service and file export functionality
- 📋 **Plugin System**: Modern plugin architecture
- 📋 **Performance**: Memory management and performance optimization

## Architecture

### Project Structure
```
FSpot.AvaloniaUI/
├── ViewModels/           # MVVM ViewModels with ReactiveUI
├── Views/               # XAML Views and UserControls
├── Services/            # Business logic and data access
├── Assets/              # Images, icons, and resources
└── appsettings.json     # Application configuration
```

### Key Technologies
- **AvaloniaUI 11.x**: Cross-platform UI framework
- **ReactiveUI**: MVVM framework with reactive extensions
- **SkiaSharp**: High-performance image processing
- **Serilog**: Structured logging
- **Microsoft.Extensions**: Dependency injection and configuration

### Data Flow
```
View → ViewModel → Service → F-Spot Core → Database
  ↑                              ↓
  ←←←←←← ReactiveUI Binding ←←←←←←
```

## Building and Running

### Prerequisites
- .NET 6.0 or later
- F-Spot core libraries built
- Modern development environment (VS 2022, Rider, or VS Code)

### Development Setup
```bash
# Navigate to the project directory
cd src/Clients/FSpot.AvaloniaUI

# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Cross-Platform Testing
```bash
# Windows
dotnet run --framework net6.0

# Linux (requires X11 or Wayland)
dotnet run --framework net6.0

# macOS
dotnet run --framework net6.0
```

## Configuration

### Application Settings
The application uses `appsettings.json` for configuration:

```json
{
  "FSpot": {
    "ThumbnailCacheSize": 1000,
    "ThumbnailSize": 256,
    "DefaultImportPath": "",
    "EnableDatabaseBackup": true
  }
}
```

### Logging Configuration
Logging is configured through `appsettings.json` and outputs to:
- Console (development)
- Rolling log files in `logs/` directory
- Structured logging with Serilog

## Performance Considerations

### Memory Management
- Thumbnail caching with configurable size limits
- Proper disposal of image resources
- Reactive subscriptions cleanup

### Image Processing
- SkiaSharp for hardware-accelerated operations
- Async/await patterns for non-blocking UI
- Progressive loading for large photo collections

### Database Performance
- Connection pooling and proper disposal
- Async database operations
- Efficient querying patterns

## Integration with F-Spot Core

### Database Compatibility
- Uses existing F-Spot SQLite database schema
- Compatible with F-Spot GTK client databases
- Supports database migration and versioning

### Plugin System
- Future integration with Mono.Addins-based plugins
- Modern plugin architecture for new extensions
- Adapter patterns for legacy plugin compatibility

### File System Integration
- Native file dialogs for all platforms
- Proper file association handling
- Cross-platform path management

## Development Guidelines

### MVVM Patterns
- ViewModels inherit from `ReactiveObject`
- Use `ReactiveCommand` for user actions
- Implement `INotifyPropertyChanged` through ReactiveUI

### Service Layer
- Interface-based service design
- Dependency injection for all services
- Async/await for all I/O operations

### Error Handling
- Comprehensive logging at all levels
- User-friendly error messages
- Graceful degradation for missing features

## Future Enhancements

### Phase 1: Core Functionality
- Complete photo import/export
- Advanced search and filtering
- Basic image editing tools

### Phase 2: Modern Features
- Cloud synchronization
- AI-powered tagging
- Performance optimization

### Phase 3: Advanced Features
- Video support
- Advanced editing tools
- Modern web service integrations

## Contributing

When contributing to the AvaloniaUI client:

1. Follow MVVM architectural patterns
2. Use ReactiveUI for data binding and commands
3. Implement proper async/await patterns
4. Add comprehensive logging
5. Write unit tests for ViewModels and Services
6. Follow Avalonia UI design guidelines

## Comparison with GTK# Client

| Feature | GTK# Client | AvaloniaUI Client |
|---------|-------------|-------------------|
| **Platform Support** | Linux (broken on modern systems) | Windows, Linux, macOS |
| **UI Framework** | GTK# 2.12 (deprecated) | AvaloniaUI 11.x (modern) |
| **Architecture** | Mixed patterns | Clean MVVM |
| **Performance** | Synchronous operations | Async/reactive patterns |
| **Maintenance** | High complexity | Modern patterns |
| **Future Viability** | Limited | Excellent |

The AvaloniaUI client represents the future of F-Spot, providing a modern, maintainable, and cross-platform foundation for continued development.
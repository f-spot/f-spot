# Plugin Modernization Strategy

## Overview

F-Spot's plugin system, while architecturally sound, requires modernization to address dependency issues, improve performance, and support contemporary .NET development practices. This document outlines a comprehensive strategy for updating the plugin ecosystem while maintaining backward compatibility.

## Current State Assessment

### Plugin Status Analysis

**Working Plugins** (6 enabled by default):
- Folder Export - File system export with web gallery generation
- Flickr Export - Photo sharing service integration  
- Gallery Export - Remote Gallery2 server upload
- PicasaWeb Export - Google Photos integration
- SmugMug Export - Photo hosting service
- Zip Export - Archive creation for sharing

**Broken/Disabled Plugins** (16 disabled):
- **All Editors** (BW, Blackout, Flip, Pixelate, Resize) - Dependency issues
- **Advanced Tools** (ChangePhotoPath, MergeDb, RawPlusJpeg) - Runtime errors
- **Specialized Features** (LiveWebGallery, DevelopInUFraw) - External dependencies
- **All Transitions** (Cover, Dissolve, Push) - Cairo rendering issues

### Root Cause Analysis

#### 1. Dependency Mismatch Issues
**Problem**: Plugins reference older Mono/.NET API versions
```xml
<!-- Legacy dependency specifications -->
<Dependencies>
    <Addin id="Core" version="0.9"/>
    <Addin id="FSpot" version="0.9"/>
</Dependencies>
```

**Affected Files**:
- All `.addin.xml` manifests in `src/Extensions/`
- Project references in `.csproj` files
- Assembly binding redirects

#### 2. Build System Fragmentation
**Problem**: Dual autotools/MSBuild support creates maintenance complexity

**Evidence**:
- Makefile.am files alongside .csproj files
- Inconsistent output paths between build systems
- Different dependency resolution mechanisms

#### 3. GTK# Version Conflicts
**Problem**: Plugins use different GTK# API versions
```csharp
// Some plugins use older GTK# patterns
using Gtk;
using Gdk;
// vs newer approach with explicit versioning
```

## Modernization Roadmap

### Phase 1: Dependency Standardization (2-3 weeks)

#### 1.1 Update Plugin Manifests
**Goal**: Standardize all plugin dependencies to current F-Spot version

**Updated Manifest Template**:
```xml
<Addin namespace="FSpot"
    id="ModernPlugin"
    version="0.18.0"
    compatVersion="0.18.0"
    name="Modern Plugin"
    description="Updated plugin with modern dependencies"
    author="F-Spot Team"
    category="Editors"
    defaultEnabled="true">
    
    <Dependencies>
        <Addin id="Core" version="0.18.0"/>
        <Addin id="FSpot" version="0.18.0"/>
        <Assembly name="System.Threading.Tasks"/>
        <Assembly name="System.IO"/>
    </Dependencies>
    
    <Extension path="/FSpot/Editors">
        <Editor EditorType="FSpot.Extensions.Modern.ModernEditor"/>
    </Extension>
</Addin>
```

#### 1.2 Consolidate Build System
**Goal**: Migrate all plugins to MSBuild/.NET SDK project format

**Modern Project Template**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net472</TargetFramework>
        <OutputPath>$(OutputExtensionPath)</OutputPath>
        <AssemblyName>FSpot.Extensions.$(MSBuildProjectName)</AssemblyName>
        <RootNamespace>FSpot.Extensions.$(MSBuildProjectName)</RootNamespace>
        <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    </PropertyGroup>

    <ItemGroup>
        <EmbeddedResource Include="Resources\*.addin.xml" />
        <EmbeddedResource Include="Resources\*.ui" />
        <EmbeddedResource Include="Resources\*.png" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Core\FSpot\FSpot.csproj" />
        <ProjectReference Include="..\..\Clients\FSpot.Gtk\FSpot.Gtk.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Mono.Addins" Version="1.4.0" />
        <PackageReference Include="GtkSharp" Version="3.24.24.95" />
    </ItemGroup>
</Project>
```

### Phase 2: Plugin Interface Modernization (3-4 weeks)

#### 2.1 Async Plugin Interfaces
**Goal**: Add async support to plugin interfaces for better responsiveness

**Modernized Editor Interface**:
```csharp
public interface IAsyncEditor : IEditor
{
    Task<Pixbuf> ProcessAsync(Pixbuf input, Cms.Profile inputProfile, 
        IProgress<double> progress = null, CancellationToken cancellationToken = default);
        
    Task<Widget> CreateConfigurationWidgetAsync(CancellationToken cancellationToken = default);
    
    bool SupportsAsync { get; }
}

public abstract class AsyncEditor : Editor, IAsyncEditor
{
    public virtual bool SupportsAsync => true;
    
    public abstract Task<Pixbuf> ProcessAsync(Pixbuf input, Cms.Profile inputProfile, 
        IProgress<double> progress = null, CancellationToken cancellationToken = default);
    
    // Backward compatibility
    protected sealed override Pixbuf Process(Pixbuf input, Cms.Profile inputProfile)
    {
        return ProcessAsync(input, inputProfile).GetAwaiter().GetResult();
    }
}
```

**Modernized Exporter Interface**:
```csharp
public interface IAsyncExporter : IExporter
{
    Task<ExportResult> RunAsync(IBrowsableCollection selection, 
        IProgress<ExportProgress> progress = null, CancellationToken cancellationToken = default);
        
    bool SupportsAsync { get; }
}

public class ExportResult
{
    public bool Success { get; set; }
    public int ExportedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class ExportProgress
{
    public int Completed { get; set; }
    public int Total { get; set; }
    public string CurrentFile { get; set; }
    public double ProgressPercentage => (double)Completed / Total * 100;
}
```

#### 2.2 Enhanced Error Handling
**Goal**: Implement standardized error handling and logging across all plugins

**Plugin Error Handling Framework**:
```csharp
public abstract class ModernPluginBase
{
    protected readonly ILogger logger;
    
    protected ModernPluginBase(ILogger logger = null)
    {
        this.logger = logger ?? NullLogger.Instance;
    }
    
    protected T ExecuteWithErrorHandling\<T\>(Func\<T\> operation, string operationName)
    {
        try
        {
            logger.LogDebug($"Starting {operationName}");
            var result = operation();
            logger.LogDebug($"Completed {operationName}");
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to execute {operationName}");
            ShowErrorDialog($"Operation failed: {ex.Message}");
            throw;
        }
    }
    
    protected async Task\<T\> ExecuteWithErrorHandlingAsync\<T\>(Func<Task\<T\>> operation, string operationName)
    {
        try
        {
            logger.LogDebug($"Starting async {operationName}");
            var result = await operation().ConfigureAwait(false);
            logger.LogDebug($"Completed async {operationName}");
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to execute async {operationName}");
            await Application.InvokeOnMainThread(() => ShowErrorDialog($"Operation failed: {ex.Message}"));
            throw;
        }
    }
}
```

### Phase 3: Performance Optimization (2-3 weeks)

#### 3.1 Plugin Loading Optimization
**Goal**: Implement smart loading and caching strategies

**Lazy Plugin Loader**:
```csharp
public class LazyPluginLoader\<T\> where T : class
{
    private readonly Lazy\<T\> lazyInstance;
    private readonly ExtensionNode node;
    
    public LazyPluginLoader(ExtensionNode node)
    {
        this.node = node;
        this.lazyInstance = new Lazy\<T\>(() => CreateInstance(), LazyThreadSafetyMode.ExecutionAndPublication);
    }
    
    public T Instance => lazyInstance.Value;
    public bool IsLoaded => lazyInstance.IsValueCreated;
    
    private T CreateInstance()
    {
        try
        {
            return (T)node.CreateInstance(typeof(T));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create plugin instance: {ex.Message}");
            return null;
        }
    }
}

public class PluginCache
{
    private readonly ConcurrentDictionary<string, WeakReference> pluginCache = new();
    
    public T GetOrCreate\<T\>(string pluginId, Func\<T\> factory) where T : class
    {
        if (pluginCache.TryGetValue(pluginId, out var weakRef) && 
            weakRef.Target is T cachedInstance)
        {
            return cachedInstance;
        }
        
        var newInstance = factory();
        pluginCache[pluginId] = new WeakReference(newInstance);
        return newInstance;
    }
}
```

#### 3.2 Background Plugin Discovery
**Goal**: Move plugin discovery off the UI thread

**Async Plugin Discovery**:
```csharp
public class AsyncPluginManager
{
    private readonly ConcurrentDictionary<string, List<ExtensionNode>> extensionCache = new();
    
    public async Task<IEnumerable\<T\>> GetExtensionsAsync\<T\>(string extensionPath) where T : ExtensionNode
    {
        if (extensionCache.TryGetValue(extensionPath, out var cached))
        {
            return cached.Cast\<T\>();
        }
        
        return await Task.Run(() =>
        {
            var extensions = AddinManager.GetExtensionNodes(extensionPath).Cast\<T\>().ToList();
            extensionCache[extensionPath] = extensions.Cast<ExtensionNode>().ToList();
            return extensions;
        }).ConfigureAwait(false);
    }
    
    public async Task PreloadExtensionsAsync()
    {
        var extensionPaths = new[]
        {
            "/FSpot/Editors",
            "/FSpot/Menus/Exports", 
            "/FSpot/Menus/PhotoPopup",
            "/FSpot/SlideShow"
        };
        
        var tasks = extensionPaths.Select(path => GetExtensionsAsync<ExtensionNode>(path));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
```

### Phase 4: Modern Features Integration (3-4 weeks)

#### 4.1 Dependency Injection Support
**Goal**: Enable modern dependency injection patterns in plugins

**Plugin Service Container**:
```csharp
public interface IPluginServiceProvider
{
    T GetService\<T\>();
    IEnumerable\<T\> GetServices\<T\>();
    void RegisterService\<T\>(T service);
    void RegisterService<TInterface, TImplementation>() where TImplementation : class, TInterface;
}

public class PluginServiceContainer : IPluginServiceProvider
{
    private readonly Dictionary<Type, object> singletonServices = new();
    private readonly Dictionary<Type, Type> serviceTypes = new();
    
    public T GetService\<T\>()
    {
        var serviceType = typeof(T);
        
        if (singletonServices.TryGetValue(serviceType, out var singleton))
            return (T)singleton;
            
        if (serviceTypes.TryGetValue(serviceType, out var implementationType))
        {
            var instance = Activator.CreateInstance(implementationType);
            singletonServices[serviceType] = instance;
            return (T)instance;
        }
        
        throw new InvalidOperationException($"Service {serviceType.Name} not registered");
    }
}

// Plugin base class with DI support
public abstract class ModernPlugin
{
    protected IPluginServiceProvider Services { get; }
    protected ILogger Logger => Services.GetService<ILogger>();
    protected IDatabase Database => Services.GetService<IDatabase>();
    
    protected ModernPlugin(IPluginServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}
```

#### 4.2 Configuration and Settings Framework
**Goal**: Standardize plugin configuration management

**Plugin Settings Framework**:
```csharp
public interface IPluginSettings
{
    T GetSetting\<T\>(string key, T defaultValue = default);
    void SetSetting\<T\>(string key, T value);
    Task SaveAsync();
    Task LoadAsync();
}

public class PluginSettings : IPluginSettings
{
    private readonly string pluginId;
    private readonly Dictionary<string, object> settings = new();
    private readonly string settingsPath;
    
    public PluginSettings(string pluginId)
    {
        this.pluginId = pluginId;
        this.settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "f-spot", "plugins", $"{pluginId}.json");
    }
    
    public T GetSetting\<T\>(string key, T defaultValue = default)
    {
        if (settings.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
    
    public void SetSetting\<T\>(string key, T value)
    {
        settings[key] = value;
    }
    
    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, json).ConfigureAwait(false);
    }
    
    public async Task LoadAsync()
    {
        if (!File.Exists(settingsPath)) return;
        
        var json = await File.ReadAllTextAsync(settingsPath).ConfigureAwait(false);
        var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        
        foreach (var kvp in loadedSettings)
        {
            settings[kvp.Key] = kvp.Value;
        }
    }
}
```

### Phase 5: Plugin Development Tools (2-3 weeks)

#### 5.1 Plugin Development Template
**Goal**: Provide Visual Studio/VS Code templates for plugin development

**Plugin Project Template** (`.template.config/template.json`):
```json
{
    "$schema": "http://json.schemastore.org/template",
    "author": "F-Spot Team",
    "name": "F-Spot Plugin",
    "identity": "FSpot.Plugin.Template",
    "shortName": "fspot-plugin",
    "tags": {
        "language": "C#",
        "type": "project"
    },
    "symbols": {
        "PluginName": {
            "type": "parameter",
            "datatype": "string",
            "description": "The name of the plugin",
            "defaultValue": "MyPlugin"
        },
        "PluginType": {
            "type": "parameter",
            "datatype": "choice",
            "choices": [
                { "choice": "Editor", "description": "Image editing plugin" },
                { "choice": "Exporter", "description": "Photo export plugin" },
                { "choice": "Tool", "description": "Utility tool plugin" },
                { "choice": "Transition", "description": "Slideshow transition plugin" }
            ],
            "defaultValue": "Editor"
        }
    }
}
```

#### 5.2 Plugin Testing Framework
**Goal**: Provide testing infrastructure for plugin development

**Plugin Test Base Class**:
```csharp
public abstract class PluginTestBase\<T\> where T : class
{
    protected IPluginServiceProvider Services { get; private set; }
    protected T Plugin { get; private set; }
    
    [SetUp]
    public virtual void SetUp()
    {
        Services = CreateTestServiceProvider();
        Plugin = CreatePlugin();
    }
    
    protected virtual IPluginServiceProvider CreateTestServiceProvider()
    {
        var services = new PluginServiceContainer();
        services.RegisterService<ILogger>(new NullLogger());
        services.RegisterService<IDatabase>(new MockDatabase());
        services.RegisterService<IPluginSettings>(new InMemoryPluginSettings());
        return services;
    }
    
    protected abstract T CreatePlugin();
    
    protected IBrowsableCollection CreateTestPhotoCollection(int count = 5)
    {
        var photos = Enumerable.Range(1, count)
            .Select(i => new MockPhoto { Id = (uint)i, Name = $"test{i}.jpg" })
            .ToList();
        return new MockBrowsableCollection(photos);
    }
}

[TestFixture]
public class ExampleEditorTests : PluginTestBase<ExampleEditor>
{
    protected override ExampleEditor CreatePlugin() => new(Services);
    
    [Test]
    public async Task ProcessAsync_WithValidImage_ShouldReturnProcessedImage()
    {
        var inputPixbuf = CreateTestPixbuf(100, 100);
        var progress = new TestProgress<double>();
        
        var result = await Plugin.ProcessAsync(inputPixbuf, null, progress);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(progress.LastValue, Is.EqualTo(1.0));
    }
}
```

## Migration Strategy

### 1. Backward Compatibility Approach

**Dual Interface Support**:
```csharp
// Maintain old interface for existing plugins
public abstract class Editor : IEditor
{
    protected abstract Pixbuf Process(Pixbuf input, Cms.Profile inputProfile);
    
    // Default async implementation wrapping sync method
    public virtual Task<Pixbuf> ProcessAsync(Pixbuf input, Cms.Profile inputProfile, 
        IProgress<double> progress = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            progress?.Report(0.0);
            var result = Process(input, inputProfile);
            progress?.Report(1.0);
            return result;
        }, cancellationToken);
    }
}
```

### 2. Gradual Plugin Migration

**Phase 1**: Update build system and dependencies (all plugins)  
**Phase 2**: Migrate exporters to async interfaces (highest user impact)  
**Phase 3**: Modernize editors with progress reporting  
**Phase 4**: Update tools and transitions  
**Phase 5**: Enable all plugins by default

### 3. Testing and Validation

**Automated Testing**:
- Unit tests for each plugin
- Integration tests with mock F-Spot environment
- Performance regression tests
- Memory leak detection

**User Testing**:
- Beta testing with core plugin functionality
- Performance benchmarking on large photo collections
- User experience validation with real workflows

## Expected Benefits

### Performance Improvements
- **Startup Time**: 30-50% reduction through background plugin loading
- **UI Responsiveness**: Elimination of blocking operations
- **Memory Usage**: 20-30% reduction through better caching
- **Plugin Loading**: 60-80% faster plugin instantiation

### Developer Experience
- **Modern Tooling**: VS Code/Visual Studio template support
- **Better Documentation**: Comprehensive plugin development guide
- **Testing Framework**: Standardized testing infrastructure
- **Debugging Tools**: Enhanced error reporting and logging

### User Experience
- **All Plugins Working**: Enable previously broken functionality
- **Progress Feedback**: Clear progress reporting for long operations
- **Cancellation Support**: Ability to cancel long-running operations
- **Better Error Messages**: User-friendly error reporting

## Implementation Timeline

**Total Duration**: 12-16 weeks

**Week 1-3**: Dependency standardization and build system migration  
**Week 4-7**: Plugin interface modernization and async support  
**Week 8-10**: Performance optimization and caching improvements  
**Week 11-13**: Modern features integration (DI, settings, etc.)  
**Week 14-16**: Development tools and documentation  

**Milestones**:
- Week 3: All plugins building and loading
- Week 7: Core plugins (editors/exporters) working with async support
- Week 10: Performance targets met
- Week 13: Advanced features implemented
- Week 16: Complete plugin ecosystem modernized

## File Locations for Plugin Work

### Core Infrastructure Updates
- `src/Core/FSpot/FSpot.addins` - Updated plugin registry
- `src/Clients/FSpot.Gtk/FSpot.Extensions/` - Modernized plugin interfaces
- Plugin project templates in `templates/`

### Plugin Modernization
- `src/Extensions/*/` - All plugin projects updated
- `src/Extensions/Common/` - Shared plugin infrastructure (new)
- `tools/plugin-dev/` - Plugin development tools (new)

### Testing Infrastructure
- `tests/PluginTests/` - Plugin testing framework (new)
- `tests/IntegrationTests/` - Plugin integration tests (new)

This modernization strategy will transform F-Spot's plugin system into a contemporary, high-performance, and developer-friendly extensibility framework while maintaining full backward compatibility with existing plugins.
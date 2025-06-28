# Plugin Development Guide

## Overview

This guide provides comprehensive instructions for developing plugins for F-Spot using the Mono.Addins framework. F-Spot supports four types of plugins: Editors, Exporters, Tools, and Transitions, each serving different purposes in the photo management workflow.

## Getting Started

### Prerequisites

- **.NET Framework 4.7.2** or later / **Mono 6.0+**
- **Visual Studio 2019+** or **VS Code** with C# extension
- **F-Spot source code** and development environment set up
- **Basic GTK# knowledge** for UI development

### Development Environment Setup

1. **Clone F-Spot Repository**:
```bash
git clone https://github.com/f-spot/f-spot.git
cd f-spot
```

2. **Build F-Spot**:
```bash
dotnet restore F-Spot.sln
dotnet build F-Spot.sln
```

3. **Verify Plugin Loading**:
```bash
msbuild build.proj -t:Run
# Check that existing plugins load in Tools→Extensions
```

## Plugin Types Overview

### 1. Editors
**Purpose**: Modify images directly (filters, adjustments, transformations)  
**UI Location**: Edit mode toolbar  
**Base Class**: `Editor`  
**Example**: Convert to black & white, resize, rotate

### 2. Exporters  
**Purpose**: Export photos to external services or formats  
**UI Location**: File→Export menu  
**Interface**: `IExporter`  
**Example**: Upload to Flickr, create web gallery, burn to CD

### 3. Tools
**Purpose**: Utility functions and maintenance operations  
**UI Location**: Photo context menu  
**Interface**: `ICommand`  
**Example**: Merge databases, change file paths, RAW processing

### 4. Transitions
**Purpose**: Slideshow transition effects  
**UI Location**: Slideshow mode  
**Base Class**: `SlideShowTransition`  
**Example**: Fade, slide, zoom effects

## Creating Your First Plugin

### Step 1: Project Structure

Create a new plugin project using this structure:
```
FSpot.Extensions.MyPlugin/
├── FSpot.Extensions.MyPlugin.csproj
├── MyPlugin/
│   ├── MyPluginEditor.cs           # Main plugin class
│   ├── MyPluginDialog.cs           # UI dialog (optional)
│   └── MyPluginSettings.cs         # Settings management (optional)
├── Resources/
│   ├── MyPlugin.addin.xml          # Plugin manifest
│   ├── MyPluginDialog.ui           # GTK UI definition
│   └── icon.png                    # Plugin icon
└── AssemblyInfo.cs
```

### Step 2: Project File Configuration

**FSpot.Extensions.MyPlugin.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net472</TargetFramework>
        <OutputPath>$(OutputExtensionPath)</OutputPath>
        <AssemblyName>FSpot.Extensions.MyPlugin</AssemblyName>
        <RootNamespace>FSpot.Extensions.MyPlugin</RootNamespace>
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

### Step 3: Plugin Manifest

**Resources/MyPlugin.addin.xml**:
```xml
<Addin namespace="FSpot"
    id="MyPlugin"
    version="1.0"
    compatVersion="1.0"
    name="My Plugin"
    description="Example plugin demonstrating F-Spot extensibility"
    author="Your Name"
    category="Editors"
    defaultEnabled="true">
    
    <Dependencies>
        <Addin id="Core" version="0.18.0"/>
        <Addin id="FSpot" version="0.18.0"/>
    </Dependencies>
    
    <Extension path="/FSpot/Editors">
        <Editor EditorType="FSpot.Extensions.MyPlugin.MyPluginEditor"/>
    </Extension>
</Addin>
```

## Plugin Implementation Examples

### Editor Plugin Example

**MyPlugin/MyPluginEditor.cs**:
```csharp
using System;
using Gdk;
using Gtk;
using FSpot;
using FSpot.Cms;
using FSpot.Editors;

namespace FSpot.Extensions.MyPlugin
{
    public class MyPluginEditor : Editor
    {
        private double brightness = 0.0;
        private Scale brightnessScale;
        
        public MyPluginEditor() : base("Brightness Adjustment", "icon.png")
        {
            CanHandleMultiple = false;
            HasSettings = true;
        }
        
        protected override Pixbuf Process(Pixbuf input, Profile inputProfile)
        {
            // Create a copy of the input pixbuf
            var output = new Pixbuf(input.Colorspace, input.HasAlpha, 
                input.BitsPerSample, input.Width, input.Height);
            
            unsafe
            {
                byte* inputPixels = (byte*)input.Pixels;
                byte* outputPixels = (byte*)output.Pixels;
                int channels = input.HasAlpha ? 4 : 3;
                
                for (int i = 0; i < input.Width * input.Height * channels; i += channels)
                {
                    // Apply brightness adjustment to RGB channels
                    for (int c = 0; c < 3; c++)
                    {
                        int value = inputPixels[i + c] + (int)(brightness * 255);
                        outputPixels[i + c] = (byte)Math.Max(0, Math.Min(255, value));
                    }
                    
                    // Copy alpha channel if present
                    if (input.HasAlpha)
                        outputPixels[i + 3] = inputPixels[i + 3];
                }
            }
            
            return output;
        }
        
        public override Widget ConfigurationWidget()
        {
            var vbox = new VBox(false, 6);
            
            var label = new Label("Brightness:");
            vbox.PackStart(label, false, false, 0);
            
            brightnessScale = new HScale(-1.0, 1.0, 0.01);
            brightnessScale.Value = brightness;
            brightnessScale.ValueChanged += OnBrightnessChanged;
            vbox.PackStart(brightnessScale, false, false, 0);
            
            vbox.ShowAll();
            return vbox;
        }
        
        private void OnBrightnessChanged(object sender, EventArgs e)
        {
            brightness = brightnessScale.Value;
            // Trigger preview update
            OnSettingsChanged();
        }
    }
}
```

### Exporter Plugin Example

**MyPlugin/MyExporter.cs**:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FSpot.Core;
using FSpot.Extensions;

namespace FSpot.Extensions.MyPlugin
{
    public class MyExporter : IExporter
    {
        public void Run(IBrowsableCollection selection)
        {
            var dialog = new MyExportDialog(selection);
            
            if (dialog.Run() == (int)ResponseType.Ok)
            {
                var options = dialog.GetExportOptions();
                _ = Task.Run(() => ExportAsync(selection, options));
            }
            
            dialog.Destroy();
        }
        
        private async Task ExportAsync(IBrowsableCollection selection, ExportOptions options)
        {
            var progressDialog = new ProgressDialog("Exporting Photos", selection.Count);
            
            try
            {
                for (int i = 0; i < selection.Count; i++)
                {
                    var photo = selection[i];
                    await ExportPhoto(photo, options);
                    
                    Application.Invoke(() => {
                        progressDialog.Update(i + 1, $"Exported {photo.Name}");
                    });
                }
                
                Application.Invoke(() => {
                    progressDialog.Destroy();
                    ShowCompletionMessage($"Successfully exported {selection.Count} photos");
                });
            }
            catch (Exception ex)
            {
                Application.Invoke(() => {
                    progressDialog.Destroy();
                    ShowErrorMessage($"Export failed: {ex.Message}");
                });
            }
        }
        
        private async Task ExportPhoto(IPhoto photo, ExportOptions options)
        {
            var sourcePath = photo.DefaultVersion.Uri.LocalPath;
            var fileName = Path.GetFileName(sourcePath);
            var destinationPath = Path.Combine(options.OutputDirectory, fileName);
            
            // Copy file with optional processing
            if (options.ResizeImages)
            {
                await ResizeAndSaveImage(sourcePath, destinationPath, options.MaxSize);
            }
            else
            {
                File.Copy(sourcePath, destinationPath, true);
            }
        }
    }
}
```

### Tool Plugin Example

**MyPlugin/MyTool.cs**:
```csharp
using System;
using System.Linq;
using FSpot.Extensions;
using FSpot.Database;

namespace FSpot.Extensions.MyPlugin
{
    public class MyTool : ICommand
    {
        public void Run(object o, EventArgs e)
        {
            var mainWindow = o as MainWindow;
            if (mainWindow?.SelectedPhotos()?.Count() > 0)
            {
                var dialog = new MyToolDialog(mainWindow.SelectedPhotos());
                dialog.Run();
                dialog.Destroy();
            }
            else
            {
                ShowMessage("Please select one or more photos first.");
            }
        }
        
        private void ShowMessage(string message)
        {
            var dialog = new MessageDialog(null, DialogFlags.Modal, 
                MessageType.Info, ButtonsType.Ok, message);
            dialog.Run();
            dialog.Destroy();
        }
    }
}
```

### Transition Plugin Example

**MyPlugin/MyTransition.cs**:
```csharp
using System;
using Cairo;
using Gdk;
using FSpot.Transitions;

namespace FSpot.Extensions.MyPlugin
{
    public class MyTransition : SlideShowTransition
    {
        public override void Draw(Drawable drawable, Pixbuf prev, Pixbuf next, 
            int width, int height, double progress)
        {
            using (var ctx = CairoHelper.Create(drawable))
            {
                // Simple fade transition
                if (prev != null)
                {
                    CairoHelper.SetSourcePixbuf(ctx, prev, 0, 0);
                    ctx.PaintWithAlpha(1.0 - progress);
                }
                
                if (next != null)
                {
                    CairoHelper.SetSourcePixbuf(ctx, next, 0, 0);
                    ctx.PaintWithAlpha(progress);
                }
            }
        }
    }
}
```

## User Interface Development

### GTK Builder Integration

Create UI files using Glade and integrate them with GTK Builder:

**Resources/MyPluginDialog.ui**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<interface>
    <object class="GtkDialog" id="my_dialog">
        <property name="title">My Plugin Settings</property>
        <property name="width_request">400</property>
        <property name="height_request">300</property>
        <child internal-child="vbox">
            <object class="GtkVBox">
                <child>
                    <object class="GtkLabel" id="title_label">
                        <property name="label">Configure My Plugin</property>
                        <property name="margin">12</property>
                    </object>
                </child>
                <child>
                    <object class="GtkHScale" id="setting_scale">
                        <property name="adjustment">
                            <object class="GtkAdjustment">
                                <property name="lower">0</property>
                                <property name="upper">100</property>
                                <property name="step_increment">1</property>
                            </object>
                        </property>
                    </object>
                </child>
            </object>
        </child>
        <child internal-child="action_area">
            <object class="GtkButtonBox">
                <child>
                    <object class="GtkButton" id="cancel_button">
                        <property name="label">Cancel</property>
                    </object>
                </child>
                <child>
                    <object class="GtkButton" id="ok_button">
                        <property name="label">OK</property>
                    </object>
                </child>
            </object>
        </child>
    </object>
</interface>
```

**Loading UI in Code**:
```csharp
public class MyPluginDialog : Dialog
{
    [GtkBeans.Builder.Object] Gtk.Scale setting_scale;
    [GtkBeans.Builder.Object] Gtk.Button ok_button;
    [GtkBeans.Builder.Object] Gtk.Button cancel_button;
    
    public MyPluginDialog() : base(IntPtr.Zero)
    {
        var builder = new GtkBeans.Builder("MyPluginDialog.ui");
        builder.Autoconnect(this);
        
        ok_button.Clicked += OnOkClicked;
        cancel_button.Clicked += OnCancelClicked;
    }
    
    private void OnOkClicked(object sender, EventArgs e)
    {
        Response((int)ResponseType.Ok);
    }
    
    private void OnCancelClicked(object sender, EventArgs e)
    {
        Response((int)ResponseType.Cancel);
    }
}
```

## Plugin Configuration and Settings

### Persistent Settings

```csharp
public class PluginSettings
{
    private readonly string pluginId;
    private readonly Dictionary<string, object> settings;
    
    public PluginSettings(string pluginId)
    {
        this.pluginId = pluginId;
        this.settings = LoadSettings();
    }
    
    public T GetSetting\<T\>(string key, T defaultValue = default)
    {
        return settings.TryGetValue(key, out var value) && value is T typedValue 
            ? typedValue : defaultValue;
    }
    
    public void SetSetting\<T\>(string key, T value)
    {
        settings[key] = value;
        SaveSettings();
    }
    
    private Dictionary<string, object> LoadSettings()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return new Dictionary<string, object>();
            
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }
    
    private void SaveSettings()
    {
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(path, json);
    }
    
    private string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "f-spot", "plugins", $"{pluginId}.json");
    }
}
```

## Testing Your Plugin

### Unit Testing

Create unit tests for your plugin logic:

```csharp
[TestFixture]
public class MyPluginEditorTests
{
    private MyPluginEditor editor;
    private Pixbuf testPixbuf;
    
    [SetUp]
    public void SetUp()
    {
        editor = new MyPluginEditor();
        testPixbuf = new Pixbuf(Colorspace.Rgb, false, 8, 100, 100);
    }
    
    [TearDown]
    public void TearDown()
    {
        testPixbuf?.Dispose();
        editor?.Dispose();
    }
    
    [Test]
    public void Process_ValidPixbuf_ReturnsProcessedImage()
    {
        var result = editor.Process(testPixbuf, null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Width, Is.EqualTo(testPixbuf.Width));
        Assert.That(result.Height, Is.EqualTo(testPixbuf.Height));
    }
    
    [Test]
    public void ConfigurationWidget_ReturnsValidWidget()
    {
        var widget = editor.ConfigurationWidget();
        
        Assert.That(widget, Is.Not.Null);
        Assert.That(widget, Is.InstanceOf<VBox>());
    }
}
```

### Integration Testing

Test your plugin within F-Spot:

1. Build your plugin: `dotnet build`
2. Start F-Spot: `msbuild build.proj -t:Run`
3. Check Tools→Extensions to verify plugin is loaded
4. Test plugin functionality with actual photos

## Performance Best Practices

### Memory Management

```csharp
public class EfficientEditor : Editor
{
    protected override Pixbuf Process(Pixbuf input, Profile inputProfile)
    {
        // Always dispose of intermediate pixbufs
        using (var temp = ProcessStep1(input))
        using (var temp2 = ProcessStep2(temp))
        {
            return ProcessStep3(temp2);
        }
    }
    
    private Pixbuf ProcessStep1(Pixbuf input)
    {
        // Create new pixbuf, caller is responsible for disposal
        return new Pixbuf(input.Colorspace, input.HasAlpha, 
            input.BitsPerSample, input.Width, input.Height);
    }
}
```

### Async Operations

```csharp
public class AsyncExporter : IExporter
{
    public void Run(IBrowsableCollection selection)
    {
        // Start async operation without blocking UI
        _ = Task.Run(async () => await ExportAsync(selection));
    }
    
    private async Task ExportAsync(IBrowsableCollection selection)
    {
        foreach (var photo in selection)
        {
            await ProcessPhotoAsync(photo);
            
            // Update UI on main thread
            Application.Invoke(() => UpdateProgress());
        }
    }
}
```

## Debugging and Troubleshooting

### Common Issues

1. **Plugin Not Loading**:
   - Check `.addin.xml` syntax and embedding
   - Verify assembly references
   - Check F-Spot console output for errors

2. **Runtime Errors**:
   - Use try-catch blocks with proper error reporting
   - Check GTK object disposal
   - Verify thread safety for UI operations

3. **Performance Issues**:
   - Profile memory usage with dotMemory
   - Use async patterns for I/O operations
   - Optimize image processing algorithms

### Debug Output

```csharp
public class DebuggablePlugin : Editor
{
    private static readonly ILog log = LogManager.GetLogger<DebuggablePlugin>();
    
    protected override Pixbuf Process(Pixbuf input, Profile inputProfile)
    {
        log.Debug($"Processing image: {input.Width}x{input.Height}");
        
        try
        {
            var result = DoActualProcessing(input, inputProfile);
            log.Debug("Processing completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            log.Error("Processing failed", ex);
            throw;
        }
    }
}
```

## Distribution and Packaging

### Creating Plugin Packages

1. **Build Release Version**:
```bash
dotnet build -c Release FSpot.Extensions.MyPlugin.csproj
```

2. **Package Files**:
   - Plugin assembly (`.dll`)
   - Dependencies (if any)
   - Documentation and screenshots

3. **Installation Instructions**:
   - Copy plugin assembly to F-Spot extensions directory
   - Restart F-Spot
   - Enable plugin in Tools→Extensions

### Publishing Guidelines

- Include comprehensive documentation
- Provide example photos for testing
- Include source code and licensing information
- Test on multiple platforms if targeting cross-platform support

## Advanced Topics

### Custom Extension Points

You can create custom extension points for specialized plugin types:

```csharp
[ExtensionPoint]
public interface IMyCustomExtension
{
    void Execute(MyCustomContext context);
}

// Usage in main application
foreach (IMyCustomExtension extension in 
    AddinManager.GetExtensionObjects<IMyCustomExtension>())
{
    extension.Execute(context);
}
```

### Plugin Communication

Plugins can communicate through shared services:

```csharp
public interface IPluginCommunication
{
    void RegisterService\<T\>(T service);
    T GetService\<T\>();
    void SendMessage(string message, object data);
    event EventHandler<PluginMessageEventArgs> MessageReceived;
}
```

## Resources and References

### Documentation
- [Mono.Addins Documentation](http://monoaddins.codeplex.com/)
- [GTK# Documentation](http://docs.go-mono.com/?link=N%3aGtk)
- [F-Spot Source Code](https://github.com/f-spot/f-spot)

### Sample Plugins
- Study existing plugins in `src/Extensions/`
- Folder Export: Good example of file operations
- Flickr Export: Web service integration example
- BW Editor: Image processing example

### Community
- F-Spot development mailing list
- GitHub issues and discussions
- GNOME development resources

This guide provides a comprehensive foundation for F-Spot plugin development. Start with the simple examples and gradually add more sophisticated features as you become familiar with the plugin architecture.
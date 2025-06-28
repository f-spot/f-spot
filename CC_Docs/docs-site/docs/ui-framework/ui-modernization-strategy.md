# F-Spot UI Framework Modernization Strategy

## Executive Summary

F-Spot's migration from GTK# 2.12 to a modern UI framework represents the **most challenging aspect** of the revival project. This document outlines a comprehensive strategy for migrating to Avalonia UI while preserving all existing functionality.

**Migration Timeline**: 6-8 months  
**Effort Estimate**: 12-18 person-months  
**Recommended Framework**: Avalonia UI

## Current State Analysis

### GTK# 2.12 Dependencies

F-Spot currently relies on deprecated GTK# 2.12 assemblies:

```xml
<!-- Current dependencies (OBSOLETE) -->
<Reference Include="gtk-sharp, Version=2.12.0.0" />
<Reference Include="gdk-sharp, Version=2.12.0.0" />
<Reference Include="glib-sharp, Version=2.12.0.0" />
<Reference Include="pango-sharp, Version=2.12.0.0" />
<Reference Include="atk-sharp, Version=2.12.0.0" />
```

**Critical Issues**:
- GTK# 2.12 is **completely deprecated** and unmaintained
- No longer available on modern Linux distributions
- Incompatible with modern .NET (6+)
- Limited to obsolete GTK+2 features
- No high-DPI support or modern theming

### Architecture Challenges

**MainWindow God Class** (`src/Clients/FSpot.Gtk/FSpot/MainWindow.cs`):
- **3,000+ lines** of tightly coupled UI code
- Direct database access mixed with UI logic
- Event-driven architecture without proper MVVM
- Manual window state management

**Custom Widget Complexity**:
- **ImageView**: 1,000+ lines of Cairo-based image display
- **PhotoImageView**: Complex photo-specific image viewer
- **CellGridView**: Custom grid controls with virtualization
- **QueryView**: Photo browser with complex selection logic

## Framework Evaluation

### Avalonia UI (Recommended Choice)

**Why Avalonia?**

✅ **Cross-platform**: Windows, macOS, Linux native support  
✅ **XAML-based**: Declarative UI with MVVM binding  
✅ **Modern .NET**: Full .NET 6+ compatibility  
✅ **Performance**: Hardware acceleration and efficient rendering  
✅ **Image Support**: Advanced image display and manipulation  
✅ **Community**: Active development and strong ecosystem  
✅ **Licensing**: MIT license, commercial-friendly  

**Avalonia Advantages for F-Spot**:
- **Advanced Image Controls**: Built-in image display with zoom/pan
- **Grid Virtualization**: Efficient handling of large photo collections
- **Styling System**: Modern theming and high-DPI support
- **Data Binding**: Excellent MVVM support reduces code complexity
- **Custom Controls**: Easy to create specialized photo management widgets

### Alternative Frameworks Considered

#### MAUI (Microsoft Multi-platform App UI)
❌ **Limited Linux Support**: Poor Linux desktop integration  
❌ **Mobile-first**: Not optimized for desktop photo management  
❌ **Microsoft-centric**: Heavy Windows bias in tooling  

#### GTK4 with .NET Bindings
❌ **Limited Tooling**: Poor Visual Studio integration  
❌ **Complex Interop**: Difficult P/Invoke patterns  
❌ **Maintenance Burden**: Custom binding maintenance required  

#### Electron.NET
❌ **Performance**: Poor performance for image-intensive operations  
❌ **Memory Usage**: High memory footprint inappropriate for photo management  
❌ **Desktop Integration**: Limited native desktop integration  

## Migration Strategy

### Phase 1: Architecture Preparation (4-6 weeks)

#### 1.1 MVVM Foundation

**Create ViewModel Layer**:
```csharp
// MainViewModel.cs - Replace MainWindow logic
public class MainViewModel : ViewModelBase {
    private readonly IPhotoRepository _photoRepository;
    private readonly ITagService _tagService;
    private readonly INavigationService _navigation;
    
    private ObservableCollection<PhotoViewModel> _photos;
    private PhotoViewModel _selectedPhoto;
    private string _searchText;
    
    public ObservableCollection<PhotoViewModel> Photos {
        get => _photos;
        set => SetProperty(ref _photos, value);
    }
    
    public PhotoViewModel SelectedPhoto {
        get => _selectedPhoto;
        set => SetProperty(ref _selectedPhoto, value);
    }
    
    public ICommand ImportCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand ExportCommand { get; }
    
    public async Task LoadPhotosAsync() {
        var photos = await _photoRepository.GetAllAsync();
        Photos = new ObservableCollection<PhotoViewModel>(
            photos.Select(p => new PhotoViewModel(p)));
    }
}
```

**PhotoViewModel Design**:
```csharp
public class PhotoViewModel : ViewModelBase {
    private readonly Photo _photo;
    private readonly IThumbnailService _thumbnailService;
    
    public string Title => _photo.Description ?? Path.GetFileName(_photo.DefaultVersion.Filename);
    public DateTime DateTaken => _photo.Time;
    public int Rating => (int)_photo.Rating;
    
    private ImageSource _thumbnail;
    public ImageSource Thumbnail {
        get => _thumbnail;
        private set => SetProperty(ref _thumbnail, value);
    }
    
    public async Task LoadThumbnailAsync() {
        var thumbnailUri = await _thumbnailService.GetThumbnailAsync(
            _photo.DefaultVersion.Uri, ThumbnailSize.Large);
        Thumbnail = new Bitmap(thumbnailUri.LocalPath);
    }
}
```

#### 1.2 Service Layer Extraction

**Extract UI Services**:
```csharp
public interface INavigationService {
    Task NavigateToPhotoDetailAsync(PhotoViewModel photo);
    Task NavigateToLibraryAsync();
    Task ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : ViewModelBase;
}

public interface IDialogService {
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<string> ShowInputDialogAsync(string title, string prompt);
    Task<ImportResult> ShowImportDialogAsync();
}

public interface IPhotoEditingService {
    Task<EditResult> CropPhotoAsync(PhotoViewModel photo, Rectangle cropArea);
    Task<EditResult> RotatePhotoAsync(PhotoViewModel photo, double angle);
    Task<EditResult> AdjustColorAsync(PhotoViewModel photo, ColorAdjustments adjustments);
}
```

### Phase 2: Core UI Migration (8-12 weeks)

#### 2.1 Main Window Shell (Weeks 1-2)

**MainWindow XAML**:
```xml
<Window x:Class="FSpot.Views.MainWindow"
        xmlns="https://github.com/avaloniaui"
        Title="F-Spot Photo Manager"
        Width="1200" Height="800">
    
    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>
    
    <DockPanel>
        <!-- Menu Bar -->
        <Menu DockPanel.Dock="Top">
            <MenuItem Header="_File">
                <MenuItem Header="_Import Photos..." Command="{Binding ImportCommand}" />
                <MenuItem Header="_Export..." Command="{Binding ExportCommand}" />
                <Separator />
                <MenuItem Header="E_xit" Command="{Binding ExitCommand}" />
            </MenuItem>
            <MenuItem Header="_Edit">
                <MenuItem Header="_Preferences..." Command="{Binding ShowPreferencesCommand}" />
            </MenuItem>
            <MenuItem Header="_View">
                <MenuItem Header="_Library View" Command="{Binding ShowLibraryCommand}" />
                <MenuItem Header="_Photo View" Command="{Binding ShowPhotoCommand}" />
                <MenuItem Header="_Fullscreen" Command="{Binding FullscreenCommand}" />
            </MenuItem>
        </Menu>
        
        <!-- Toolbar -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Classes="toolbar">
            <Button Command="{Binding ImportCommand}">
                <StackPanel Orientation="Horizontal">
                    <Image Source="/Assets/Icons/import.png" Width="16" Height="16" />
                    <TextBlock Text="Import" Margin="4,0,0,0" />
                </StackPanel>
            </Button>
            <Button Command="{Binding EditCommand}">
                <StackPanel Orientation="Horizontal">
                    <Image Source="/Assets/Icons/edit.png" Width="16" Height="16" />
                    <TextBlock Text="Edit" Margin="4,0,0,0" />
                </StackPanel>
            </Button>
        </StackPanel>
        
        <!-- Status Bar -->
        <StatusBar DockPanel.Dock="Bottom">
            <TextBlock Text="{Binding StatusText}" />
            <ProgressBar Value="{Binding ProgressValue}" 
                         IsVisible="{Binding IsProgressVisible}" 
                         Width="200" />
        </StatusBar>
        
        <!-- Main Content Area -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250" />
                <ColumnDefinition Width="5" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            
            <!-- Sidebar -->
            <UserControl Grid.Column="0" Content="{Binding SidebarContent}" />
            
            <!-- Splitter -->
            <GridSplitter Grid.Column="1" />
            
            <!-- Photo Content -->
            <ContentControl Grid.Column="2" Content="{Binding CurrentView}" />
        </Grid>
    </DockPanel>
</Window>
```

#### 2.2 Photo Grid View (Weeks 3-5)

**PhotoGridView Implementation**:
```xml
<UserControl x:Class="FSpot.Views.PhotoGridView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        
        <!-- Search and Filter Bar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="5">
            <TextBox Text="{Binding SearchText}" 
                     Watermark="Search photos..." 
                     Width="200" />
            <ComboBox ItemsSource="{Binding TagFilters}" 
                      SelectedItem="{Binding SelectedTagFilter}"
                      Width="150" />
            <ComboBox ItemsSource="{Binding DateRanges}"
                      SelectedItem="{Binding SelectedDateRange}"
                      Width="150" />
        </StackPanel>
        
        <!-- Virtualized Photo Grid -->
        <ListBox Grid.Row="1" 
                 ItemsSource="{Binding Photos}"
                 SelectedItem="{Binding SelectedPhoto}"
                 ScrollViewer.HorizontalScrollBarVisibility="Disabled">
            
            <ListBox.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel Orientation="Horizontal" />
                </ItemsPanelTemplate>
            </ListBox.ItemsPanel>
            
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <Border BorderBrush="Gray" BorderThickness="1" 
                            Margin="5" Padding="5">
                        <StackPanel Width="150">
                            <!-- Thumbnail Image -->
                            <Image Source="{Binding Thumbnail}" 
                                   Width="140" Height="105"
                                   Stretch="UniformToFill" />
                            
                            <!-- Photo Info -->
                            <TextBlock Text="{Binding Title}" 
                                       FontWeight="Bold"
                                       TextTrimming="CharacterEllipsis"
                                       MaxLines="1" />
                            
                            <TextBlock Text="{Binding DateTaken, StringFormat='d'}" 
                                       Foreground="Gray"
                                       FontSize="11" />
                            
                            <!-- Rating Stars -->
                            <ItemsControl ItemsSource="{Binding Stars}">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <StackPanel Orientation="Horizontal" />
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Path Data="{StaticResource StarGeometry}"
                                              Fill="{Binding Fill}"
                                              Width="12" Height="12" />
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Grid>
</UserControl>
```

#### 2.3 Image Display Component (Weeks 5-8)

**Advanced Image Viewer**:
```xml
<UserControl x:Class="FSpot.Views.PhotoImageView">
    <Grid>
        <ScrollViewer Name="ImageScroller"
                      ZoomMode="Enabled"
                      HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Auto"
                      Background="Black">
            
            <!-- Main Image -->
            <Image Name="MainImage"
                   Source="{Binding CurrentPhoto.FullSizeImage}"
                   Stretch="Uniform"
                   RenderTransform="{Binding ImageTransform}" />
        </ScrollViewer>
        
        <!-- Overlay Canvas for Editing -->
        <Canvas Name="OverlayCanvas"
                IsVisible="{Binding IsEditMode}">
            
            <!-- Crop Rectangle -->
            <Rectangle Name="CropRectangle"
                       Stroke="Red" StrokeThickness="2"
                       Fill="Transparent"
                       Canvas.Left="{Binding CropRect.X}"
                       Canvas.Top="{Binding CropRect.Y}"
                       Width="{Binding CropRect.Width}"
                       Height="{Binding CropRect.Height}"
                       IsVisible="{Binding IsCropping}" />
            
            <!-- Selection Handles -->
            <ItemsControl ItemsSource="{Binding SelectionHandles}"
                          IsVisible="{Binding ShowSelectionHandles}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Ellipse Width="8" Height="8" 
                                 Fill="White" Stroke="Black"
                                 Canvas.Left="{Binding X}"
                                 Canvas.Top="{Binding Y}" />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </Canvas>
        
        <!-- Image Navigation Controls -->
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Center"
                    VerticalAlignment="Bottom"
                    Margin="0,0,0,20"
                    Background="#80000000">
            
            <Button Command="{Binding PreviousPhotoCommand}">
                <Path Data="{StaticResource PreviousIcon}" Fill="White" />
            </Button>
            
            <Button Command="{Binding NextPhotoCommand}">
                <Path Data="{StaticResource NextIcon}" Fill="White" />
            </Button>
            
            <Button Command="{Binding ZoomInCommand}">
                <Path Data="{StaticResource ZoomInIcon}" Fill="White" />
            </Button>
            
            <Button Command="{Binding ZoomOutCommand}">
                <Path Data="{StaticResource ZoomOutIcon}" Fill="White" />
            </Button>
            
            <Button Command="{Binding FitToWindowCommand}">
                <Path Data="{StaticResource FitToWindowIcon}" Fill="White" />
            </Button>
        </StackPanel>
    </Grid>
</UserControl>
```

**Image Viewer ViewModel**:
```csharp
public class PhotoImageViewModel : ViewModelBase {
    private PhotoViewModel _currentPhoto;
    private Transform _imageTransform = Transform.Identity;
    private bool _isEditMode;
    private Rect _cropRect;
    
    public PhotoViewModel CurrentPhoto {
        get => _currentPhoto;
        set => SetProperty(ref _currentPhoto, value);
    }
    
    public Transform ImageTransform {
        get => _imageTransform;
        set => SetProperty(ref _imageTransform, value);
    }
    
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand FitToWindowCommand { get; }
    public ICommand RotateLeftCommand { get; }
    public ICommand RotateRightCommand { get; }
    
    private void ZoomIn() {
        var currentScale = GetCurrentScale();
        var newScale = Math.Min(currentScale * 1.2, 10.0);
        ApplyZoom(newScale);
    }
    
    private void FitToWindow() {
        // Calculate optimal scale to fit image in viewport
        var viewportSize = GetViewportSize();
        var imageSize = CurrentPhoto.ImageSize;
        
        var scaleX = viewportSize.Width / imageSize.Width;
        var scaleY = viewportSize.Height / imageSize.Height;
        var scale = Math.Min(scaleX, scaleY);
        
        ImageTransform = new ScaleTransform(scale, scale);
    }
}
```

### Phase 3: Advanced Features (Weeks 9-16)

#### 3.1 Photo Editing Interface (Weeks 9-12)

**Editing Sidebar Component**:
```xml
<UserControl x:Class="FSpot.Views.EditingSidebar">
    <StackPanel>
        <Expander Header="Basic Adjustments" IsExpanded="True">
            <StackPanel>
                <!-- Brightness -->
                <Grid Margin="0,5">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="80" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="50" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="Brightness" VerticalAlignment="Center" />
                    <Slider Grid.Column="1" 
                            Minimum="-100" Maximum="100" 
                            Value="{Binding BrightnessAdjustment}"
                            TickFrequency="10" />
                    <TextBox Grid.Column="2" 
                             Text="{Binding BrightnessAdjustment}" 
                             Width="40" />
                </Grid>
                
                <!-- Contrast -->
                <Grid Margin="0,5">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="80" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="50" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="Contrast" VerticalAlignment="Center" />
                    <Slider Grid.Column="1" 
                            Minimum="-100" Maximum="100" 
                            Value="{Binding ContrastAdjustment}"
                            TickFrequency="10" />
                    <TextBox Grid.Column="2" 
                             Text="{Binding ContrastAdjustment}" 
                             Width="40" />
                </Grid>
                
                <!-- Saturation -->
                <Grid Margin="0,5">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="80" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="50" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="Saturation" VerticalAlignment="Center" />
                    <Slider Grid.Column="1" 
                            Minimum="-100" Maximum="100" 
                            Value="{Binding SaturationAdjustment}"
                            TickFrequency="10" />
                    <TextBox Grid.Column="2" 
                             Text="{Binding SaturationAdjustment}" 
                             Width="40" />
                </Grid>
            </StackPanel>
        </Expander>
        
        <Expander Header="Crop & Rotate">
            <StackPanel>
                <Button Command="{Binding StartCropCommand}" Content="Crop" />
                <Button Command="{Binding RotateLeftCommand}" Content="Rotate Left" />
                <Button Command="{Binding RotateRightCommand}" Content="Rotate Right" />
                <Button Command="{Binding FlipHorizontalCommand}" Content="Flip Horizontal" />
            </StackPanel>
        </Expander>
        
        <Expander Header="Effects">
            <StackPanel>
                <Button Command="{Binding BlackWhiteCommand}" Content="Black & White" />
                <Button Command="{Binding SepiaCommand}" Content="Sepia" />
                <Button Command="{Binding SharpenCommand}" Content="Sharpen" />
                <Button Command="{Binding BlurCommand}" Content="Blur" />
            </StackPanel>
        </Expander>
        
        <!-- Action Buttons -->
        <StackPanel Orientation="Horizontal" Margin="0,10">
            <Button Command="{Binding ApplyChangesCommand}" Content="Apply" />
            <Button Command="{Binding ResetChangesCommand}" Content="Reset" />
            <Button Command="{Binding CancelEditingCommand}" Content="Cancel" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

#### 3.2 Import/Export Dialogs (Weeks 12-14)

**Modern Import Dialog**:
```xml
<Window x:Class="FSpot.Views.ImportDialog"
        Title="Import Photos"
        Width="600" Height="500">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        
        <!-- Source Selection -->
        <StackPanel Grid.Row="0" Margin="10">
            <TextBlock Text="Select Import Source:" FontWeight="Bold" />
            <Grid Margin="0,5">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <TextBox Grid.Column="0" Text="{Binding SourcePath}" />
                <Button Grid.Column="1" Content="Browse..." 
                        Command="{Binding BrowseSourceCommand}" />
            </Grid>
        </StackPanel>
        
        <!-- Preview Area -->
        <Border Grid.Row="1" BorderBrush="Gray" BorderThickness="1" Margin="10">
            <ListBox ItemsSource="{Binding PreviewPhotos}"
                     ScrollViewer.HorizontalScrollBarVisibility="Disabled">
                <ListBox.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel />
                    </ItemsPanelTemplate>
                </ListBox.ItemsPanel>
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <CheckBox IsChecked="{Binding IsSelected}">
                            <StackPanel Width="100">
                                <Image Source="{Binding Thumbnail}" 
                                       Width="80" Height="60" />
                                <TextBlock Text="{Binding FileName}" 
                                           FontSize="10"
                                           TextTrimming="CharacterEllipsis" />
                            </StackPanel>
                        </CheckBox>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Border>
        
        <!-- Import Options -->
        <Expander Grid.Row="2" Header="Import Options" Margin="10">
            <StackPanel>
                <CheckBox IsChecked="{Binding CopyFiles}" 
                          Content="Copy files to photo library" />
                <CheckBox IsChecked="{Binding CreateRoll}" 
                          Content="Create new roll for imported photos" />
                <Grid Margin="0,5">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="Tag photos as:" 
                               VerticalAlignment="Center" />
                    <TextBox Grid.Column="1" Text="{Binding ImportTag}" 
                             Margin="5,0" />
                </Grid>
            </StackPanel>
        </Expander>
        
        <!-- Buttons -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" 
                    HorizontalAlignment="Right" Margin="10">
            <TextBlock Text="{Binding SelectedCount, StringFormat='{0} photos selected'}" 
                       VerticalAlignment="Center" Margin="0,0,10,0" />
            <Button Content="Import" Command="{Binding ImportCommand}" 
                    IsDefault="True" />
            <Button Content="Cancel" Command="{Binding CancelCommand}" 
                    IsCancel="True" />
        </StackPanel>
    </Grid>
</Window>
```

#### 3.3 Plugin System Integration (Weeks 14-16)

**Plugin Host for Avalonia**:
```csharp
public class AvaloniaPluginHost : IPluginHost {
    public UserControl CreateConfigurationUI(IImageEditor editor) {
        // Create Avalonia UserControl from editor configuration
        var config = editor.CreateConfigurationWidget();
        return ConvertToAvaloniaControl(config);
    }
    
    public async Task<IImage> ProcessImageAsync(IImageEditor editor, IImage input) {
        // Bridge between old and new image processing
        var pixbuf = ConvertToPixbuf(input);
        var result = await Task.Run(() => editor.Process(pixbuf, null));
        return ConvertFromPixbuf(result);
    }
    
    private UserControl ConvertToAvaloniaControl(Widget gtkWidget) {
        // Adapter pattern to integrate GTK widgets in Avalonia
        // This allows gradual migration of plugins
        return new GtkWidgetAdapter(gtkWidget);
    }
}
```

## Migration Challenges and Solutions

### Challenge 1: Complex Custom Controls

**Problem**: F-Spot's ImageView has 1000+ lines of complex Cairo rendering code.

**Solution**: Leverage Avalonia's built-in image controls and extend as needed:
```csharp
public class AdvancedImageControl : UserControl {
    private Image _mainImage;
    private Canvas _overlayCanvas;
    private ScrollViewer _scrollViewer;
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);
        
        _mainImage = e.NameScope.Find<Image>("PART_MainImage");
        _overlayCanvas = e.NameScope.Find<Canvas>("PART_Overlay");
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        
        SetupImageHandling();
    }
    
    private void SetupImageHandling() {
        _mainImage.PointerPressed += OnImagePointerPressed;
        _mainImage.PointerMoved += OnImagePointerMoved;
        _mainImage.PointerWheelChanged += OnImageWheelChanged;
    }
}
```

### Challenge 2: Performance with Large Collections

**Problem**: F-Spot needs to handle thousands of photos efficiently.

**Solution**: Use Avalonia's virtualization and async loading:
```csharp
public class VirtualizedPhotoCollection : IAsyncEnumerable<PhotoViewModel> {
    private readonly IPhotoRepository _repository;
    private readonly IThumbnailCache _thumbnailCache;
    
    public async IAsyncEnumerator<PhotoViewModel> GetAsyncEnumerator(
        CancellationToken cancellationToken = default) {
        
        await foreach (var photo in _repository.GetPhotosAsync()) {
            var viewModel = new PhotoViewModel(photo);
            
            // Load thumbnail in background
            _ = Task.Run(async () => {
                var thumbnail = await _thumbnailCache.GetThumbnailAsync(photo.Uri);
                viewModel.Thumbnail = thumbnail;
            });
            
            yield return viewModel;
        }
    }
}
```

### Challenge 3: Plugin Compatibility

**Problem**: Existing plugins use GTK# widgets and Mono.Addins.

**Solution**: Create adapter layer and migration path:
```csharp
public class PluginAdapter {
    public static UserControl AdaptGtkWidget(Widget gtkWidget) {
        // Create Avalonia wrapper for GTK widget
        return new GtkWidgetHost(gtkWidget);
    }
    
    public static IImageEditor AdaptLegacyEditor(MonoAddinsEditor legacyEditor) {
        return new LegacyEditorAdapter(legacyEditor);
    }
}

// Gradual migration support
public class HybridPluginSystem {
    private readonly MonoAddinsManager _legacyManager;
    private readonly MEFContainer _modernContainer;
    
    public IEnumerable<IImageEditor> GetAllEditors() {
        // Return both legacy and modern editors
        var legacy = _legacyManager.GetExtensions<MonoAddinsEditor>()
            .Select(e => PluginAdapter.AdaptLegacyEditor(e));
        
        var modern = _modernContainer.GetExports<IImageEditor>()
            .Select(e => e.Value);
        
        return legacy.Concat(modern);
    }
}
```

## Testing Strategy

### Visual Regression Testing

```csharp
[TestFixture]
public class VisualRegressionTests {
    [Test]
    public async Task PhotoGridView_RendersCorrectly() {
        var window = new MainWindow();
        var viewModel = CreateTestViewModel();
        window.DataContext = viewModel;
        
        // Render to bitmap
        var bitmap = await RenderToBitmapAsync(window);
        
        // Compare with reference image
        var reference = LoadReferenceImage("PhotoGridView_Expected.png");
        Assert.That(bitmap, Is.VisuallyEqualTo(reference));
    }
}
```

### Performance Testing

```csharp
[TestFixture]
public class PerformanceTests {
    [Test]
    public async Task PhotoGrid_HandlesLargeCollections() {
        var photos = GenerateTestPhotos(10000);
        var viewModel = new PhotoGridViewModel();
        
        var stopwatch = Stopwatch.StartNew();
        await viewModel.LoadPhotosAsync(photos);
        stopwatch.Stop();
        
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
    }
}
```

## Migration Timeline

| Week | Milestone | Deliverable |
|------|-----------|-------------|
| 1-2 | Architecture Setup | MVVM foundation, service layer |
| 3-4 | Main Window Shell | Basic layout and navigation |
| 5-6 | Photo Grid | Thumbnail display and selection |
| 7-8 | Image Viewer | Zoom, pan, navigation |
| 9-10 | Basic Editing | Crop, rotate, basic adjustments |
| 11-12 | Advanced Editing | Color correction, effects |
| 13-14 | Import/Export | Dialog implementation |
| 15-16 | Plugin Integration | Adapter layer and testing |
| 17-20 | Polish & Testing | Performance, visual testing |

## Success Criteria

### Functional Parity
- [ ] All photo browsing features work
- [ ] Image editing maintains quality
- [ ] Import/export functionality preserved
- [ ] Plugin system operational
- [ ] Performance equal or better than GTK# version

### Modern Features
- [ ] High-DPI support
- [ ] Modern theming
- [ ] Touch/gesture support
- [ ] Better accessibility
- [ ] Cross-platform deployment

### Code Quality
- [ ] Proper MVVM separation
- [ ] Unit test coverage >80%
- [ ] Performance benchmarks met
- [ ] Memory usage optimized
- [ ] Visual regression tests passing

## Conclusion

The UI modernization represents the most significant technical challenge in F-Spot's revival, but Avalonia UI provides an excellent migration target that can preserve functionality while enabling modern features.

**Key Success Factors**:
1. **Gradual Migration**: Phase approach minimizes risk
2. **Architecture First**: MVVM foundation enables clean UI separation
3. **Performance Focus**: Virtualization and async patterns handle large collections
4. **Plugin Compatibility**: Adapter pattern preserves existing extensions
5. **Comprehensive Testing**: Visual and performance testing ensure quality

The estimated **6-8 month timeline** is aggressive but achievable with dedicated focus on the migration. The result will be a modern, cross-platform photo management application that preserves F-Spot's strengths while enabling future development.
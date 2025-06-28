# Image Processing and Editing System Analysis

## Overview

F-Spot implements a sophisticated image processing and editing system that provides non-destructive editing capabilities through a plugin-based editor architecture. The system combines color management, pixel-level image manipulation, and filter pipelines to deliver professional-grade photo editing functionality.

## Architecture Overview

### System Components

```
Image Processing Architecture
├── Editor Framework
│   ├── Editor Base Class (Common editor functionality)
│   ├── Editor State Management (Selection, preview)
│   ├── Editor Nodes (Plugin integration)
│   └── Processing Pipeline (Async operations)
├── Color Management System
│   ├── ICC Profile Handling (Color space conversion)
│   ├── Color Adjustment Algorithms (LCh, RGB, LAB)
│   ├── White Balance Correction (Temperature/tint)
│   └── Histogram Analysis (Exposure metrics)
├── Image Processing Algorithms
│   ├── Geometric Operations (Crop, rotate, straighten)
│   ├── Pixel Operations (Brightness, contrast, saturation)
│   ├── Filter Operations (Sharpen, blur, noise reduction)
│   └── Effect Processors (Sepia, desaturate, artistic)
└── Version Management
    ├── Non-Destructive Editing (Original preservation)
    ├── Version Control (Edit history)
    ├── Metadata Preservation (EXIF/XMP retention)
    └── Undo/Redo System (Operation reversal)
```

## Editor Framework Architecture

### Base Editor Class (`src/Clients/FSpot.Gtk/FSpot.Editors/Editor.cs`)

**Core Editor Abstraction**:
```csharp
public abstract class Editor
{
    // Event handling for progress reporting
    public delegate void ProcessingStartedHandler(string name, int count);
    public delegate void ProcessingStepHandler(int done);
    public delegate void ProcessingFinishedHandler();
    
    public event ProcessingStartedHandler ProcessingStarted;
    public event ProcessingStepHandler ProcessingStep;
    public event ProcessingFinishedHandler ProcessingFinished;
    
    // Editor state management
    EditorState state;
    public EditorState State
    {
        get
        {
            if (!StateInitialized)
                throw new ApplicationException("Editor has not been initialized yet!");
            return state;
        }
    }
    
    // Editor capabilities
    public bool NeedsSelection = false;           // Selection requirement
    public bool CanHandleMultiple = false;       // Batch processing support
    public bool HasSettings = false;             // Configuration UI
    
    // Editor identity
    public readonly string Label;               // Human-readable name
    public string ApplyLabel { get; protected set; } // Action button text
    
    // Core processing method
    protected abstract Pixbuf Process(Pixbuf input, Cms.Profile input_profile);
    
    // Configuration UI (optional)
    public virtual Widget ConfigurationWidget() => null;
    
    // Photo loading with color management
    protected void LoadPhoto(Photo photo, out Pixbuf photo_pixbuf, 
                           out Cms.Profile photo_profile)
    {
        using var img = App.Instance.Container
            .Resolve<IImageFileFactory>().Create(photo.DefaultVersion.Uri);
        photo_pixbuf = img.Load();
        photo_profile = img.GetProfile();
    }
    
    // Main execution method
    public void Apply(IBrowsableCollection selection)
    {
        if (!CanBeApplied)
            return;
            
        ProcessingStarted?.Invoke(Label, selection.Count);
        
        var processed = 0;
        foreach (var photo in selection.Items)
        {
            ProcessPhoto(photo);
            ProcessingStep?.Invoke(++processed);
        }
        
        ProcessingFinished?.Invoke();
    }
}
```

### Editor State Management (`src/Clients/FSpot.Gtk/FSpot.Editors/EditorState.cs`)

**State Container for Editor Context**:
```csharp
public class EditorState
{
    // Photo selection context
    public IBrowsableCollection Selection { get; set; }
    public Photo CurrentPhoto { get; set; }
    
    // Image view state
    public PhotoImageView PhotoView { get; set; }
    public Rectangle SelectionBounds { get; set; }
    public bool HasSelection => !SelectionBounds.IsEmpty;
    
    // Processing state
    public Pixbuf OriginalPixbuf { get; set; }
    public Pixbuf PreviewPixbuf { get; set; }
    public Cms.Profile ColorProfile { get; set; }
    
    // Preview management
    bool previewEnabled = true;
    public bool PreviewEnabled
    {
        get => previewEnabled;
        set
        {
            if (previewEnabled != value)
            {
                previewEnabled = value;
                if (value)
                    UpdatePreview();
                else
                    RestoreOriginal();
            }
        }
    }
    
    public void UpdatePreview()
    {
        if (PreviewPixbuf != null)
            PhotoView.Pixbuf = PreviewPixbuf;
    }
    
    public void RestoreOriginal()
    {
        if (OriginalPixbuf != null)
            PhotoView.Pixbuf = OriginalPixbuf;
    }
}
```

### Plugin Integration (`src/Clients/FSpot.Gtk/FSpot.Editors/EditorNode.cs`)

**Editor Plugin Node**:
```csharp
[ExtensionNode(ExtensionAttributeType = typeof(EditorExtensionAttribute))]
public class EditorNode : TypeExtensionNode
{
    // Editor metadata
    [NodeAttribute("label")]
    public string Label { get; set; }
    
    [NodeAttribute("icon")]
    public string IconName { get; set; }
    
    [NodeAttribute("priority")]
    public int Priority { get; set; } = 0;
    
    // Editor instantiation
    public Editor CreateEditor()
    {
        return (Editor)CreateInstance();
    }
}

// Extension point for editor plugins
[ExtensionPoint]
public interface IEditorExtensionPoint
{
    void RegisterEditor(EditorNode node);
}
```

## Color Management and Adjustment

### Color Adjustment Framework (`src/Clients/FSpot.Gtk/FSpot.ColorAdjustment/`)

**Base Adjustment Class**:
```csharp
public abstract class Adjustment
{
    protected const int nsteps = 20;  // Color LUT resolution
    
    protected Pixbuf Input { get; }
    protected Cms.Profile InputProfile { get; }
    protected Cms.Profile DestinationProfile { get; }
    
    public Adjustment(Pixbuf input, Cms.Profile input_profile)
    {
        Input = input;
        InputProfile = input_profile ?? Cms.Profile.CreateSRgb();
        DestinationProfile = Cms.Profile.CreateSRgb(); // Default output
    }
    
    // Generate color transformation profiles
    protected abstract List<Cms.Profile> GenerateAdjustments();
    
    // Apply color adjustments
    public virtual Pixbuf Adjust()
    {
        var profiles = GenerateAdjustments();
        if (profiles.Count < 2)
            return Input.Copy();
            
        // Create transform chain
        var transform = new Cms.Transform(profiles.ToArray(),
            Cms.Format.Rgb8, Cms.Format.Rgb8, Cms.Intent.Perceptual, 0);
            
        // Apply transformation
        var output = new Pixbuf(Input.Colorspace, Input.HasAlpha, 
                               Input.BitsPerSample, Input.Width, Input.Height);
        
        ApplyTransform(Input, output, transform);
        
        return output;
    }
    
    unsafe void ApplyTransform(Pixbuf input, Pixbuf output, Cms.Transform transform)
    {
        var input_pixels = (byte*)input.Pixels;
        var output_pixels = (byte*)output.Pixels;
        
        int width = input.Width;
        int height = input.Height;
        int rowstride = input.Rowstride;
        int channels = input.NChannels;
        
        // Process pixel by pixel with color management
        for (int row = 0; row < height; row++)
        {
            var input_row = input_pixels + (row * rowstride);
            var output_row = output_pixels + (row * rowstride);
            
            for (int col = 0; col < width; col++)
            {
                var input_pixel = input_row + (col * channels);
                var output_pixel = output_row + (col * channels);
                
                // Apply color transformation
                transform.Apply(input_pixel, output_pixel, 1);
                
                // Preserve alpha channel if present
                if (channels == 4)
                    output_pixel[3] = input_pixel[3];
            }
        }
    }
}
```

### Full Color Adjustment (`src/Clients/FSpot.Gtk/FSpot.ColorAdjustment/FullColorAdjustment.cs`)

**Comprehensive Color Correction**:
```csharp
public class FullColorAdjustment : Adjustment
{
    readonly double exposure;      // Exposure compensation
    readonly double brightness;    // Brightness adjustment
    readonly double contrast;      // Contrast modification
    readonly double hue;          // Hue shift
    readonly double saturation;   // Saturation adjustment
    
    Cms.ColorCIEXYZ src_wp;      // Source white point
    Cms.ColorCIEXYZ dest_wp;     // Destination white point
    
    public FullColorAdjustment(Pixbuf input, Cms.Profile input_profile,
                              double exposure, double brightness, double contrast,
                              double hue, double saturation,
                              Cms.ColorCIEXYZ src_wp, Cms.ColorCIEXYZ dest_wp)
        : base(input, input_profile)
    {
        this.exposure = exposure;
        this.brightness = brightness;
        this.contrast = contrast;
        this.hue = hue;
        this.saturation = saturation;
        this.src_wp = src_wp;
        this.dest_wp = dest_wp;
    }
    
    protected override List<Cms.Profile> GenerateAdjustments()
    {
        var profiles = new List<Cms.Profile>();
        
        // Input profile
        profiles.Add(InputProfile);
        
        // Abstract adjustment profile
        var adjustmentProfile = Cms.Profile.CreateAbstract(
            nsteps,
            Math.Pow(Math.Sqrt(2.0), exposure),  // Exposure as power curve
            brightness,                          // Linear brightness
            contrast,                           // Contrast curve
            hue,                               // Hue rotation
            saturation,                        // Saturation scaling
            null,                              // No gamma override
            src_wp.ToxyY(),                    // Source white point
            dest_wp.ToxyY()                    // Target white point
        );
        
        profiles.Add(adjustmentProfile);
        
        // Output profile
        profiles.Add(DestinationProfile);
        
        return profiles;
    }
}
```

### Color Editor Implementation (`src/Clients/FSpot.Gtk/FSpot.Editors/ColorEditor.cs`)

**Interactive Color Adjustment Interface**:
```csharp
class ColorEditor : Editor
{
    // UI control references
    [GtkBeans.Builder.Object] Gtk.HScale exposure_scale;
    [GtkBeans.Builder.Object] Gtk.HScale temp_scale;        // Color temperature
    [GtkBeans.Builder.Object] Gtk.HScale temptint_scale;    // Tint adjustment
    [GtkBeans.Builder.Object] Gtk.HScale brightness_scale;
    [GtkBeans.Builder.Object] Gtk.HScale contrast_scale;
    [GtkBeans.Builder.Object] Gtk.HScale hue_scale;
    [GtkBeans.Builder.Object] Gtk.HScale sat_scale;         // Saturation
    
    public ColorEditor() : base("Adjust Colors", "adjust-colors")
    {
        HasSettings = true;
        ApplyLabel = "Adjust";
    }
    
    public override Widget ConfigurationWidget()
    {
        builder = new GtkBeans.Builder(null, "color_editor_prefs_window.ui", null);
        builder.Autoconnect(this);
        AttachInterface();
        return new VBox(builder.GetRawObject("color_editor_prefs"));
    }
    
    void AttachInterface()
    {
        // Initialize default values
        temp_scale.Value = 5000;  // Standard daylight temperature
        
        // Connect change handlers
        exposure_scale.ValueChanged += RangeChanged;
        temp_scale.ValueChanged += RangeChanged;
        temptint_scale.ValueChanged += RangeChanged;
        brightness_scale.ValueChanged += RangeChanged;
        contrast_scale.ValueChanged += RangeChanged;
        hue_scale.ValueChanged += RangeChanged;
        sat_scale.ValueChanged += RangeChanged;
    }
    
    protected override Pixbuf Process(Pixbuf input, Cms.Profile input_profile)
    {
        // Calculate white balance adjustment
        var src_wp = Cms.ColorCIExyY.WhitePointFromTemperature(5000).ToXYZ();
        var dest_wp = Cms.ColorCIExyY.WhitePointFromTemperature((int)temp_scale.Value).ToXYZ();
        
        // Apply tint correction
        var dest_lab = dest_wp.ToLab(src_wp);
        dest_lab.a += temptint_scale.Value;  // Green-magenta axis
        dest_wp = dest_lab.ToXYZ(src_wp);
        
        // Create comprehensive adjustment
        var adjust = new FullColorAdjustment(input, input_profile,
            exposure_scale.Value,      // Exposure
            brightness_scale.Value,    // Brightness
            contrast_scale.Value,      // Contrast
            hue_scale.Value,          // Hue
            sat_scale.Value,          // Saturation
            src_wp, dest_wp);         // White balance
            
        return adjust.Adjust();
    }
}
```

## Image Processing Algorithms

### Geometric Operations

#### Crop Editor (`src/Clients/FSpot.Gtk/FSpot.Editors/CropEditor.cs`)

**Interactive Cropping with Constraints**:
```csharp
class CropEditor : Editor
{
    Rectangle crop_rectangle;
    
    public CropEditor() : base("Crop", "crop")
    {
        NeedsSelection = true;  // Requires user selection
        ApplyLabel = "Crop";
    }
    
    protected override Pixbuf Process(Pixbuf input, Cms.Profile input_profile)
    {
        // Get selection bounds from editor state
        var selection = State.SelectionBounds;
        
        // Validate selection bounds
        var crop_x = Math.Max(0, Math.Min(selection.X, input.Width - 1));
        var crop_y = Math.Max(0, Math.Min(selection.Y, input.Height - 1));
        var crop_width = Math.Max(1, Math.Min(selection.Width, input.Width - crop_x));
        var crop_height = Math.Max(1, Math.Min(selection.Height, input.Height - crop_y));
        
        // Create cropped pixbuf
        var cropped = new Pixbuf(input.Colorspace, input.HasAlpha, 
                                input.BitsPerSample, crop_width, crop_height);
        
        // Copy selected region
        input.CopyArea(crop_x, crop_y, crop_width, crop_height, cropped, 0, 0);
        
        return cropped;
    }
    
    // Aspect ratio constraint options
    public enum AspectRatio
    {
        Free,        // No constraint
        Square,      // 1:1
        Photo,       // 4:3
        Postcard,    // 3:2
        Golden,      // 1.618:1
        Custom       // User-defined
    }
}
```

#### Straighten/Tilt Editor (`src/Clients/FSpot.Gtk/FSpot.Editors/TiltEditor.cs`)

**Rotation Correction**:
```csharp
class TiltEditor : Editor
{
    double angle = 0.0;  // Rotation angle in degrees
    
    protected override Pixbuf Process(Pixbuf input, Cms.Profile input_profile)
    {
        if (Math.Abs(angle) < 0.01)
            return input.Copy();
            
        // Calculate rotated dimensions
        var radians = angle * Math.PI / 180.0;
        var cos_a = Math.Abs(Math.Cos(radians));
        var sin_a = Math.Abs(Math.Sin(radians));
        
        var new_width = (int)(input.Width * cos_a + input.Height * sin_a);
        var new_height = (int)(input.Width * sin_a + input.Height * cos_a);
        
        // Create rotated image with Cairo
        using var surface = new Cairo.ImageSurface(Cairo.Format.Argb32, 
                                                  new_width, new_height);
        using var cr = new Cairo.Context(surface);
        
        // Set up transformation matrix
        cr.Translate(new_width / 2.0, new_height / 2.0);
        cr.Rotate(radians);
        cr.Translate(-input.Width / 2.0, -input.Height / 2.0);
        
        // Draw rotated image
        Gdk.CairoHelper.SetSourcePixbuf(cr, input, 0, 0);
        cr.Paint();
        
        // Convert back to Pixbuf
        return CairoUtils.SurfaceToPixbuf(surface);
    }
}
```

### Pixel-Level Operations

#### Auto Stretch Editor (`src/Clients/FSpot.Gtk/FSpot.Editors/AutoStretchEditor.cs`)

**Automatic Contrast Enhancement**:
```csharp
class AutoStretchEditor : Editor
{
    protected override Pixbuf Process(Pixbuf input, Cms.Profile input_profile)
    {
        // Calculate histogram
        var histogram = CalculateHistogram(input);
        
        // Find shadow and highlight points (1% and 99% percentiles)
        var shadow_point = FindPercentile(histogram, 0.01);
        var highlight_point = FindPercentile(histogram, 0.99);
        
        // Create auto-stretch adjustment
        var adjustment = new AutoStretch(input, input_profile, 
                                       shadow_point, highlight_point);
        
        return adjustment.Adjust();
    }
    
    int[] CalculateHistogram(Pixbuf pixbuf)
    {
        var histogram = new int[256];
        
        unsafe
        {
            var pixels = (byte*)pixbuf.Pixels;
            int width = pixbuf.Width;
            int height = pixbuf.Height;
            int rowstride = pixbuf.Rowstride;
            int channels = pixbuf.NChannels;
            
            for (int row = 0; row < height; row++)
            {
                var row_pixels = pixels + (row * rowstride);
                
                for (int col = 0; col < width; col++)
                {
                    var pixel = row_pixels + (col * channels);
                    
                    // Calculate luminance
                    var luminance = (int)(0.299 * pixel[0] + 
                                        0.587 * pixel[1] + 
                                        0.114 * pixel[2]);
                    
                    histogram[Math.Max(0, Math.Min(255, luminance))]++;
                }
            }
        }
        
        return histogram;
    }
    
    int FindPercentile(int[] histogram, double percentile)
    {
        var total_pixels = histogram.Sum();
        var target_count = total_pixels * percentile;
        var running_count = 0;
        
        for (int i = 0; i < histogram.Length; i++)
        {
            running_count += histogram[i];
            if (running_count >= target_count)
                return i;
        }
        
        return 255;
    }
}
```

### Filter Operations

#### Sharpening Filter (`src/Clients/FSpot.Gtk/FSpot.Widgets/Sharpener.cs`)

**Unsharp Mask Implementation**:
```csharp
public class Sharpener : Loupe  // Inherits preview functionality
{
    Gtk.SpinButton amount_spin = new(0.5, 100.0, 0.01);    // Sharpening strength
    Gtk.SpinButton radius_spin = new(5.0, 50.0, 0.01);     // Blur radius
    Gtk.SpinButton threshold_spin = new(0.0, 50.0, 0.01);  // Edge threshold
    
    protected override void UpdateSample()
    {
        base.UpdateSample();
        
        if (overlay != null)
            overlay.Dispose();
            
        overlay = null;
        
        if (source != null)
        {
            // Apply unsharp mask to preview
            overlay = PixbufUtils.UnsharpMask(source,
                radius_spin.Value,      // Gaussian blur radius
                amount_spin.Value,      // Sharpening amount
                threshold_spin.Value,   // Edge detection threshold
                null);                  // No progress callback for preview
        }
    }
    
    public void DoSharpening()
    {
        var photo = view.Item.Current as Photo;
        if (photo == null) return;
        
        try
        {
            var original = view.Pixbuf;
            
            // Apply sharpening with progress reporting
            var sharpened = PixbufUtils.UnsharpMask(original,
                radius_spin.Value,
                amount_spin.Value,
                threshold_spin.Value,
                progressDialog);
            
            // Create new version
            CreateNewVersion(photo, sharpened, "Sharpened");
        }
        catch (Exception ex)
        {
            Logger.Log.Error("Sharpening failed", ex);
        }
    }
}
```

**Unsharp Mask Algorithm** (`src/Core/FSpot/Utils/PixbufUtils.cs`):
```csharp
public static Pixbuf UnsharpMask(Pixbuf src, double radius, double amount, 
                                double threshold, IProgressStatus progress)
{
    // Step 1: Create Gaussian blur
    var blurred = GaussianBlur(src, radius);
    
    // Step 2: Calculate difference (mask)
    var mask = CreateDifferenceMask(src, blurred);
    
    // Step 3: Apply threshold
    ApplyThreshold(mask, threshold);
    
    // Step 4: Blend with original using amount
    var result = BlendUnsharpMask(src, mask, amount);
    
    blurred.Dispose();
    mask.Dispose();
    
    return result;
}

static Pixbuf GaussianBlur(Pixbuf src, double radius)
{
    // Gaussian kernel generation
    var kernel_size = (int)(radius * 6) | 1;  // Ensure odd size
    var kernel = GenerateGaussianKernel(radius, kernel_size);
    
    // Two-pass separable filter
    var temp = ApplyHorizontalConvolution(src, kernel);
    var result = ApplyVerticalConvolution(temp, kernel);
    
    temp.Dispose();
    return result;
}

static Pixbuf BlendUnsharpMask(Pixbuf original, Pixbuf mask, double amount)
{
    var result = original.Copy();
    
    unsafe
    {
        var orig_pixels = (byte*)original.Pixels;
        var mask_pixels = (byte*)mask.Pixels;
        var result_pixels = (byte*)result.Pixels;
        
        int width = original.Width;
        int height = original.Height;
        int rowstride = original.Rowstride;
        int channels = original.NChannels;
        
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                var orig_offset = row * rowstride + col * channels;
                var mask_offset = row * rowstride + col * channels;
                var result_offset = row * rowstride + col * channels;
                
                for (int ch = 0; ch < 3; ch++)  // RGB channels only
                {
                    var original_value = orig_pixels[orig_offset + ch];
                    var mask_value = mask_pixels[mask_offset + ch] - 128;  // Center around 0
                    
                    var sharpened = original_value + (mask_value * amount);
                    result_pixels[result_offset + ch] = (byte)Math.Max(0, Math.Min(255, sharpened));
                }
                
                // Preserve alpha
                if (channels == 4)
                    result_pixels[result_offset + 3] = orig_pixels[orig_offset + 3];
            }
        }
    }
    
    return result;
}
```

### Artistic Effects

#### Sepia Tone Effect (`src/Clients/FSpot.Gtk/FSpot.Editors/SepiaEditor.cs`)

**Color Tone Transformation**:
```csharp
class SepiaEditor : Editor
{
    public SepiaEditor() : base("Sepia Tone", "color-sepia")
    {
        CanHandleMultiple = true;  // Supports batch processing
    }
    
    protected override Pixbuf Process(Pixbuf input, Cms.Profile input_profile)
    {
        var sepia = new SepiaTone(input, input_profile);
        return sepia.Adjust();
    }
}

public class SepiaTone : Adjustment
{
    // Sepia transformation matrix
    static readonly double[,] sepiaMatrix = {
        { 0.393, 0.769, 0.189 },  // Red channel
        { 0.349, 0.686, 0.168 },  // Green channel
        { 0.272, 0.534, 0.131 }   // Blue channel
    };
    
    protected override List<Cms.Profile> GenerateAdjustments()
    {
        var profiles = new List<Cms.Profile>();
        profiles.Add(InputProfile);
        
        // Create sepia profile using matrix transformation
        var sepiaProfile = CreateSepiaProfile();
        profiles.Add(sepiaProfile);
        
        profiles.Add(DestinationProfile);
        return profiles;
    }
    
    unsafe Pixbuf ApplySepiaDirect(Pixbuf input)
    {
        var output = input.Copy();
        var pixels = (byte*)output.Pixels;
        
        int width = output.Width;
        int height = output.Height;
        int rowstride = output.Rowstride;
        int channels = output.NChannels;
        
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                var offset = row * rowstride + col * channels;
                
                var r = pixels[offset];
                var g = pixels[offset + 1];
                var b = pixels[offset + 2];
                
                // Apply sepia transformation
                var new_r = Math.Min(255, (int)(sepiaMatrix[0,0] * r + 
                                               sepiaMatrix[0,1] * g + 
                                               sepiaMatrix[0,2] * b));
                var new_g = Math.Min(255, (int)(sepiaMatrix[1,0] * r + 
                                               sepiaMatrix[1,1] * g + 
                                               sepiaMatrix[1,2] * b));
                var new_b = Math.Min(255, (int)(sepiaMatrix[2,0] * r + 
                                               sepiaMatrix[2,1] * g + 
                                               sepiaMatrix[2,2] * b));
                
                pixels[offset] = (byte)new_r;
                pixels[offset + 1] = (byte)new_g;
                pixels[offset + 2] = (byte)new_b;
            }
        }
        
        return output;
    }
}
```

#### Desaturate Effect (`src/Clients/FSpot.Gtk/FSpot.Editors/DesaturateEditor.cs`)

**Monochrome Conversion**:
```csharp
class DesaturateEditor : Editor
{
    public DesaturateEditor() : base("Desaturate", "color-desaturate")
    {
        CanHandleMultiple = true;
    }
    
    protected override Pixbuf Process(Pixbuf input, Cms.Profile input_profile)
    {
        var desaturate = new Desaturate(input, input_profile);
        return desaturate.Adjust();
    }
}

public class Desaturate : Adjustment
{
    public enum DesaturateMode
    {
        Lightness,    // (Max + Min) / 2
        Luminosity,   // Weighted RGB average
        Average       // Simple RGB average
    }
    
    DesaturateMode mode;
    
    public Desaturate(Pixbuf input, Cms.Profile input_profile, 
                     DesaturateMode mode = DesaturateMode.Luminosity)
        : base(input, input_profile)
    {
        this.mode = mode;
    }
    
    unsafe Pixbuf ApplyDesaturation(Pixbuf input)
    {
        var output = input.Copy();
        var pixels = (byte*)output.Pixels;
        
        int width = output.Width;
        int height = output.Height;
        int rowstride = output.Rowstride;
        int channels = output.NChannels;
        
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                var offset = row * rowstride + col * channels;
                
                var r = pixels[offset];
                var g = pixels[offset + 1];
                var b = pixels[offset + 2];
                
                byte gray;
                switch (mode)
                {
                    case DesaturateMode.Lightness:
                        gray = (byte)((Math.Max(r, Math.Max(g, b)) + 
                                     Math.Min(r, Math.Min(g, b))) / 2);
                        break;
                        
                    case DesaturateMode.Luminosity:
                        gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
                        break;
                        
                    case DesaturateMode.Average:
                    default:
                        gray = (byte)((r + g + b) / 3);
                        break;
                }
                
                pixels[offset] = gray;
                pixels[offset + 1] = gray;
                pixels[offset + 2] = gray;
            }
        }
        
        return output;
    }
}
```

## Version Management and Non-Destructive Editing

### Version Creation System

**Non-Destructive Edit Pipeline**:
```csharp
public class NonDestructiveEditor
{
    public static uint CreateEditedVersion(Photo photo, string versionName, 
                                         Func<Pixbuf, Pixbuf> processor)
    {
        // Load original image
        using var originalFile = App.Instance.Container
            .Resolve<IImageFileFactory>().Create(photo.DefaultVersion.Uri);
        var originalPixbuf = originalFile.Load();
        var colorProfile = originalFile.GetProfile();
        
        try
        {
            // Apply processing
            var processedPixbuf = processor(originalPixbuf);
            
            // Generate version filename
            var versionUri = GenerateVersionUri(photo, versionName);
            
            // Save processed image with metadata preservation
            SaveProcessedImage(processedPixbuf, versionUri, originalFile);
            
            // Create version record
            var versionId = photo.CreateVersion(versionName, photo.DefaultVersionId, true);
            photo.DefaultVersionId = versionId;
            
            // Update database
            App.Instance.Database.Photos.Commit(photo);
            
            return versionId;
        }
        finally
        {
            originalPixbuf.Dispose();
        }
    }
    
    static void SaveProcessedImage(Pixbuf processed, SafeUri destination, 
                                  IImageFile originalFile)
    {
        // Preserve original metadata
        var metadata = originalFile.GetExifData();
        var xmpData = originalFile.GetXmpData();
        
        // Save image with quality preservation
        var quality = DetermineOptimalQuality(originalFile);
        processed.Save(destination.LocalPath, "jpeg", 
                      new[] { "quality", quality.ToString() });
        
        // Write back metadata
        WriteMetadata(destination, metadata, xmpData);
        
        // Update processing history
        AddProcessingHistory(destination, "F-Spot Edit");
    }
}
```

### Undo/Redo System

**Edit History Management**:
```csharp
public class EditHistoryManager
{
    Stack<EditOperation> undoStack = new();
    Stack<EditOperation> redoStack = new();
    
    public void ExecuteOperation(EditOperation operation)
    {
        // Execute the operation
        operation.Execute();
        
        // Add to undo stack
        undoStack.Push(operation);
        
        // Clear redo stack (new operations invalidate redo)
        redoStack.Clear();
        
        // Trigger UI updates
        OperationExecuted?.Invoke(this, new EditEventArgs(operation));
    }
    
    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;
    
    public void Undo()
    {
        if (!CanUndo) return;
        
        var operation = undoStack.Pop();
        operation.Undo();
        redoStack.Push(operation);
        
        OperationUndone?.Invoke(this, new EditEventArgs(operation));
    }
    
    public void Redo()
    {
        if (!CanRedo) return;
        
        var operation = redoStack.Pop();
        operation.Execute();
        undoStack.Push(operation);
        
        OperationRedone?.Invoke(this, new EditEventArgs(operation));
    }
}

public abstract class EditOperation
{
    public string Description { get; protected set; }
    public DateTime Timestamp { get; protected set; }
    
    public abstract void Execute();
    public abstract void Undo();
}

public class VersionCreateOperation : EditOperation
{
    readonly Photo photo;
    readonly uint versionId;
    readonly string versionName;
    
    public override void Execute()
    {
        // Version creation logic
    }
    
    public override void Undo()
    {
        // Delete created version and revert to previous
        photo.DeleteVersion(versionId, false, true);
        App.Instance.Database.Photos.Commit(photo);
    }
}
```

## Performance Optimizations

### Async Processing Pipeline

**Background Processing System**:
```csharp
public class AsyncImageProcessor
{
    readonly SemaphoreSlim processingLimiter;
    readonly TaskScheduler uiScheduler;
    
    public AsyncImageProcessor(int maxConcurrency = Environment.ProcessorCount)
    {
        processingLimiter = new SemaphoreSlim(maxConcurrency);
        uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }
    
    public async Task<ProcessingResult> ProcessImageAsync(
        IPhoto photo, IEditor editor, IProgress<ProcessingProgress> progress,
        CancellationToken cancellationToken = default)
    {
        await processingLimiter.WaitAsync(cancellationToken);
        
        try
        {
            return await Task.Run(() => 
                ProcessImageCore(photo, editor, progress, cancellationToken),
                cancellationToken);
        }
        finally
        {
            processingLimiter.Release();
        }
    }
    
    ProcessingResult ProcessImageCore(IPhoto photo, IEditor editor,
                                    IProgress<ProcessingProgress> progress,
                                    CancellationToken cancellationToken)
    {
        try
        {
            // Load image
            progress?.Report(new ProcessingProgress("Loading image...", 0.1));
            editor.LoadPhoto(photo, out var pixbuf, out var profile);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            // Process image
            progress?.Report(new ProcessingProgress("Processing...", 0.5));
            var processed = editor.Process(pixbuf, profile);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            // Save result
            progress?.Report(new ProcessingProgress("Saving...", 0.9));
            var versionId = NonDestructiveEditor.CreateEditedVersion(
                photo, editor.Label, _ => processed);
            
            progress?.Report(new ProcessingProgress("Complete", 1.0));
            
            return new ProcessingResult(true, versionId);
        }
        catch (OperationCanceledException)
        {
            return new ProcessingResult(false, "Processing cancelled");
        }
        catch (Exception ex)
        {
            return new ProcessingResult(false, ex.Message);
        }
    }
}
```

### Memory Management

**Large Image Handling**:
```csharp
public class LargeImageProcessor
{
    public static Pixbuf ProcessLargeImage(Pixbuf source, 
                                         Func<Pixbuf, Pixbuf> processor,
                                         int tileSize = 1024)
    {
        // Check if tiling is necessary
        if (source.Width <= tileSize && source.Height <= tileSize)
            return processor(source);
        
        // Process in tiles
        var result = new Pixbuf(source.Colorspace, source.HasAlpha,
                               source.BitsPerSample, source.Width, source.Height);
        
        for (int y = 0; y < source.Height; y += tileSize)
        {
            for (int x = 0; x < source.Width; x += tileSize)
            {
                var tileWidth = Math.Min(tileSize, source.Width - x);
                var tileHeight = Math.Min(tileSize, source.Height - y);
                
                // Extract tile
                var tile = ExtractTile(source, x, y, tileWidth, tileHeight);
                
                try
                {
                    // Process tile
                    var processedTile = processor(tile);
                    
                    try
                    {
                        // Insert processed tile back
                        InsertTile(result, processedTile, x, y);
                    }
                    finally
                    {
                        processedTile.Dispose();
                    }
                }
                finally
                {
                    tile.Dispose();
                }
            }
        }
        
        return result;
    }
}
```

## Conclusion

F-Spot's image processing and editing system demonstrates sophisticated capabilities for professional photo editing. Key architectural strengths include:

**Editor Framework Excellence**:
- Plugin-based architecture enabling extensible editing capabilities
- Non-destructive editing with version management
- Real-time preview with efficient state management
- Comprehensive color management integration

**Processing Algorithm Sophistication**:
- Professional-grade color adjustments with ICC profile support
- Advanced filter operations including unsharp masking
- Geometric operations with high-quality interpolation
- Artistic effects with customizable parameters

**Performance and Usability**:
- Async processing pipeline for responsive UI
- Memory-efficient large image handling
- Progress reporting and cancellation support
- Undo/redo system for operation reversal

**Modernization Opportunities**:
- GPU acceleration for compute-intensive operations
- Machine learning-based automatic adjustments
- Advanced raw processing capabilities
- Real-time collaborative editing features

The system provides a solid foundation for professional photo editing that could be enhanced with modern processing techniques while maintaining its flexible, extensible architecture and non-destructive editing philosophy.
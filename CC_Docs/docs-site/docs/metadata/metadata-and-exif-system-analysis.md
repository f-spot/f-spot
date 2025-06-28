# Metadata and EXIF System Analysis

## Overview

F-Spot implements a comprehensive metadata management system that handles EXIF, IPTC, and XMP metadata standards for digital photos. The system provides robust metadata extraction, manipulation, and preservation capabilities while supporting both embedded metadata and external sidecar files for non-destructive metadata workflows.

## Architecture Overview

### System Components

```
Metadata Management Architecture
├── Metadata Parsing Framework
│   ├── TagLib# Integration (Multi-format support)
│   ├── File Abstraction Layer (IO abstraction)
│   ├── Metadata Detection (MIME type detection)
│   └── Error Recovery (Fallback strategies)
├── Metadata Standards Support
│   ├── EXIF (Camera and technical data)
│   ├── IPTC (Editorial and cataloging data)
│   ├── XMP (Adobe Extensible Metadata)
│   └── Custom Extensions (F-Spot specific)
├── Sidecar File Management
│   ├── XMP Sidecar Reading/Writing
│   ├── Sidecar Discovery (Multiple naming conventions)
│   ├── Non-Destructive Workflow (Original preservation)
│   └── Sync Management (File-sidecar consistency)
└── Metadata Integration
    ├── Import Processing (Automatic extraction)
    ├── Database Storage (Searchable metadata)
    ├── Export Preservation (Metadata transfer)
    └── Edit History (Processing records)
```

## Metadata Parsing Framework

### Core Metadata Utilities (`src/Core/FSpot/Utils/Metadata.cs`)

**Primary Metadata Interface**:
```csharp
public static class MetadataUtils
{
    public static TagLib.Image.File Parse(SafeUri uri)
    {
        // Step 1: MIME type detection with fallback
        string mime = new DotNetFile().GetMimeType(uri);
        
        // Handle broken metadata detection
        if (mime.StartsWith("application/x-extension-"))
        {
            // Works around GNOME mime-type detection issues
            mime = $"taglib/{mime.Substring(24)}";
        }
        
        // Step 2: Create file abstraction
        var res = new TagLibFileAbstraction(uri);
        var sidecar_uri = GetSidecarUri(uri);
        var sidecar_res = new TagLibFileAbstraction(sidecar_uri);
        
        TagLib.Image.File file;
        try
        {
            // Primary parsing attempt with MIME type
            file = TagLib.File.Create(res, mime, ReadStyle.Average) as TagLib.Image.File;
        }
        catch (Exception)
        {
            Logger.Log.Debug($"Loading of metadata failed for file: {uri}, trying extension fallback");
            
            try
            {
                // Fallback to extension-based detection
                file = TagLib.File.Create(res, ReadStyle.Average) as TagLib.Image.File;
            }
            catch (Exception e)
            {
                Logger.Log.Debug($"Loading of metadata failed for file: {uri}");
                Logger.Log.Debug(e, "");
                return null;
            }
        }
        
        // Step 3: Load XMP sidecar if available
        if (System.IO.File.Exists(sidecar_uri.AbsolutePath))
            file.ParseXmpSidecar(sidecar_res);
            
        return file;
    }
}
```

**Supported File Formats**:
- **JPEG**: Full EXIF, IPTC, XMP support
- **TIFF**: Complete metadata support
- **RAW Formats**: Canon CR2, Nikon NEF, Adobe DNG, Fuji RAF
- **PNG**: Limited metadata support
- **GIF**: Basic metadata extraction

### File Abstraction Layer (`src/Core/FSpot/Utils/TagLibFileAbstraction.cs`)

**Abstracted File Access**:
```csharp
public class TagLibFileAbstraction : TagLib.File.IFileAbstraction
{
    readonly SafeUri uri;
    
    public TagLibFileAbstraction(SafeUri uri)
    {
        this.uri = uri;
    }
    
    public string Name => uri.AbsolutePath;
    public Stream ReadStream => File.OpenRead(uri.LocalPath);
    public Stream WriteStream => File.Create(uri.LocalPath);
    
    public void CloseStream(Stream stream)
    {
        stream?.Close();
    }
}
```

**Benefits of Abstraction**:
- **Testability**: Mockable file operations
- **Error Handling**: Consistent exception handling
- **Cross-Platform**: Platform-independent file access
- **Security**: Safe URI handling

## EXIF Data Extraction and Management

### EXIF Data Model

**Camera Technical Data**:
```csharp
public class ExifData
{
    // Camera identification
    public string Make { get; set; }           // Camera manufacturer
    public string Model { get; set; }          // Camera model
    public string SerialNumber { get; set; }   // Camera serial number
    
    // Image capture settings
    public DateTime? DateTime { get; set; }    // Capture date/time
    public double? ExposureTime { get; set; }  // Shutter speed
    public double? FNumber { get; set; }       // Aperture
    public int? ISOSpeedRatings { get; set; }  // ISO sensitivity
    public double? FocalLength { get; set; }   // Lens focal length
    
    // Image properties
    public int ImageWidth { get; set; }        // Image width in pixels
    public int ImageHeight { get; set; }       // Image height in pixels
    public int Orientation { get; set; }       // Image orientation
    public double? XResolution { get; set; }   // Horizontal resolution
    public double? YResolution { get; set; }   // Vertical resolution
    
    // GPS coordinates
    public GpsCoordinate? GpsCoordinates { get; set; }
    public double? GpsAltitude { get; set; }
    public DateTime? GpsDateTime { get; set; }
    
    // Flash information
    public bool FlashFired { get; set; }
    public string FlashMode { get; set; }
    
    // White balance and color
    public string WhiteBalance { get; set; }
    public string ColorSpace { get; set; }
    
    // Lens information
    public string LensModel { get; set; }
    public double? LensMinFocalLength { get; set; }
    public double? LensMaxFocalLength { get; set; }
}

public struct GpsCoordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LatitudeRef { get; set; }   // N/S
    public string LongitudeRef { get; set; }  // E/W
}
```

### EXIF Extraction Implementation

**Comprehensive EXIF Reading**:
```csharp
public class ExifExtractor
{
    public static ExifData ExtractExifData(TagLib.Image.File file)
    {
        var exifData = new ExifData();
        
        if (file?.ImageTag?.Exif == null)
            return exifData;
            
        var exif = file.ImageTag.Exif;
        
        // Camera information
        exifData.Make = exif.Make?.Trim();
        exifData.Model = exif.Model?.Trim();
        exifData.SerialNumber = GetExifString(exif, ExifEntryTag.BodySerialNumber);
        
        // Capture settings
        exifData.DateTime = exif.DateTime;
        exifData.ExposureTime = GetExposureTime(exif);
        exifData.FNumber = GetFNumber(exif);
        exifData.ISOSpeedRatings = GetISOSpeed(exif);
        exifData.FocalLength = GetFocalLength(exif);
        
        // Image properties
        exifData.ImageWidth = (int)(exif.PhotoWidth ?? 0);
        exifData.ImageHeight = (int)(exif.PhotoHeight ?? 0);
        exifData.Orientation = (int)(exif.Orientation ?? 1);
        
        // GPS data
        exifData.GpsCoordinates = ExtractGpsCoordinates(exif);
        exifData.GpsAltitude = GetGpsAltitude(exif);
        
        // Flash information
        exifData.FlashFired = (exif.Flash & 0x01) != 0;
        exifData.FlashMode = GetFlashMode(exif);
        
        // Color and white balance
        exifData.WhiteBalance = GetWhiteBalance(exif);
        exifData.ColorSpace = GetColorSpace(exif);
        
        // Lens information
        exifData.LensModel = GetExifString(exif, ExifEntryTag.LensModel);
        ExtractLensInformation(exif, exifData);
        
        return exifData;
    }
    
    static double? GetExposureTime(TagLib.IFD.IFDTag exif)
    {
        var exposureEntry = exif.GetEntry(ExifEntryTag.ExposureTime);
        if (exposureEntry is RationalIFDEntry rationalEntry)
        {
            var rational = rationalEntry.Value;
            return rational.Numerator / (double)rational.Denominator;
        }
        return null;
    }
    
    static double? GetFNumber(TagLib.IFD.IFDTag exif)
    {
        var fNumberEntry = exif.GetEntry(ExifEntryTag.FNumber);
        if (fNumberEntry is RationalIFDEntry rationalEntry)
        {
            var rational = rationalEntry.Value;
            return rational.Numerator / (double)rational.Denominator;
        }
        return null;
    }
    
    static GpsCoordinate? ExtractGpsCoordinates(TagLib.IFD.IFDTag exif)
    {
        try
        {
            var latEntry = exif.GetEntry(ExifEntryTag.GPSLatitude);
            var lonEntry = exif.GetEntry(ExifEntryTag.GPSLongitude);
            var latRefEntry = exif.GetEntry(ExifEntryTag.GPSLatitudeRef);
            var lonRefEntry = exif.GetEntry(ExifEntryTag.GPSLongitudeRef);
            
            if (latEntry != null && lonEntry != null)
            {
                var latitude = ConvertGpsCoordinate(latEntry);
                var longitude = ConvertGpsCoordinate(lonEntry);
                var latRef = GetExifString(exif, ExifEntryTag.GPSLatitudeRef);
                var lonRef = GetExifString(exif, ExifEntryTag.GPSLongitudeRef);
                
                // Apply hemisphere corrections
                if (latRef == "S") latitude = -latitude;
                if (lonRef == "W") longitude = -longitude;
                
                return new GpsCoordinate
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    LatitudeRef = latRef,
                    LongitudeRef = lonRef
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Debug("Failed to extract GPS coordinates", ex);
        }
        
        return null;
    }
    
    static double ConvertGpsCoordinate(IFDEntry entry)
    {
        if (entry is RationalArrayIFDEntry rationalArray && rationalArray.Values.Length >= 3)
        {
            var degrees = rationalArray.Values[0].Numerator / (double)rationalArray.Values[0].Denominator;
            var minutes = rationalArray.Values[1].Numerator / (double)rationalArray.Values[1].Denominator;
            var seconds = rationalArray.Values[2].Numerator / (double)rationalArray.Values[2].Denominator;
            
            return degrees + minutes / 60.0 + seconds / 3600.0;
        }
        
        return 0.0;
    }
}
```

## XMP and IPTC Support

### XMP Metadata Management

**XMP Data Model**:
```csharp
public class XmpMetadata
{
    // Dublin Core schema
    public string Title { get; set; }
    public string Description { get; set; }
    public string Creator { get; set; }
    public string Subject { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    // Rights management
    public string Copyright { get; set; }
    public string Rights { get; set; }
    public string UsageTerms { get; set; }
    
    // EXIF schema in XMP
    public string CameraMake { get; set; }
    public string CameraModel { get; set; }
    public DateTime? DateTimeOriginal { get; set; }
    
    // Custom F-Spot schema
    public string[] Keywords { get; set; }
    public int Rating { get; set; }
    public string ColorLabel { get; set; }
    
    // Processing history
    public ProcessingStep[] ProcessingHistory { get; set; }
}

public class ProcessingStep
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; }
    public string Software { get; set; }
    public string SoftwareVersion { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
}
```

### IPTC Metadata Extraction

**IPTC Core Implementation**:
```csharp
public class IptcExtractor
{
    public static IptcMetadata ExtractIptcData(TagLib.Image.File file)
    {
        var iptcData = new IptcMetadata();
        
        if (file?.ImageTag?.IPTC == null)
            return iptcData;
            
        var iptc = file.ImageTag.IPTC;
        
        // Editorial information
        iptcData.Headline = iptc.Headline;
        iptcData.Caption = iptc.Caption;
        iptcData.Keywords = iptc.Keywords?.ToArray() ?? new string[0];
        iptcData.Category = iptc.Category;
        iptcData.SupplementalCategories = iptc.SupplementalCategories?.ToArray();
        
        // Author information
        iptcData.Byline = iptc.Byline;
        iptcData.BylineTitle = iptc.BylineTitle;
        iptcData.Credit = iptc.Credit;
        iptcData.Source = iptc.Source;
        
        // Location information
        iptcData.City = iptc.City;
        iptcData.ProvinceState = iptc.ProvinceState;
        iptcData.CountryName = iptc.CountryName;
        iptcData.CountryCode = iptc.CountryCode;
        
        // Date information
        iptcData.DateCreated = iptc.DateCreated;
        iptcData.TimeCreated = iptc.TimeCreated;
        
        // Copyright information
        iptcData.Copyright = iptc.Copyright;
        iptcData.UsageTerms = iptc.UsageTerms;
        
        return iptcData;
    }
}
```

## Sidecar File Management

### XMP Sidecar System (`src/Core/FSpot/Utils/SidecarXmpExtensions.cs`)

**Sidecar File Operations**:
```csharp
public static class SidecarXmpExtensions
{
    /// <summary>
    /// Parses XMP sidecar file and merges with main file metadata
    /// </summary>
    public static bool ParseXmpSidecar(this TagLib.Image.File file, 
                                      TagLib.File.IFileAbstraction resource)
    {
        string xmp;
        
        try
        {
            // Read sidecar file content
            using var stream = resource.ReadStream;
            using var reader = new StreamReader(stream);
            xmp = reader.ReadToEnd();
        }
        catch (Exception e)
        {
            Logger.Log.Debug($"Sidecar cannot be read for file {file.Name}");
            Logger.Log.Debug(e, "");
            return false;
        }
        
        XmpTag tag = null;
        try
        {
            // Parse XMP content
            tag = new XmpTag(xmp, file);
        }
        catch (Exception e)
        {
            Logger.Log.Debug($"Metadata of Sidecar cannot be parsed for file {file.Name}");
            Logger.Log.Debug(e, "");
            return false;
        }
        
        // Merge with existing XMP data
        var xmp_tag = file.GetTag(TagLib.TagTypes.XMP, true) as XmpTag;
        xmp_tag.ReplaceFrom(tag);
        
        return true;
    }
    
    /// <summary>
    /// Saves metadata to XMP sidecar file
    /// </summary>
    public static bool SaveXmpSidecar(this TagLib.Image.File file, 
                                     TagLib.File.IFileAbstraction resource)
    {
        var xmp_tag = file.GetTag(TagLib.TagTypes.XMP, false) as XmpTag;
        if (xmp_tag == null)
        {
            // No XMP data to save - could delete sidecar
            return true;
        }
        
        // Render XMP to string
        var xmp = xmp_tag.Render();
        
        try
        {
            // Write sidecar file
            using var stream = resource.WriteStream;
            stream.SetLength(0);
            using var writer = new StreamWriter(stream);
            writer.Write(xmp);
            resource.CloseStream(stream);
        }
        catch (Exception e)
        {
            Logger.Log.Debug($"Sidecar cannot be saved: {resource.Name}");
            Logger.Log.Debug(e, "");
            return false;
        }
        
        return true;
    }
}
```

### Sidecar File Discovery

**Multiple Naming Convention Support**:
```csharp
public static class SidecarManager
{
    // Sidecar naming strategies
    static readonly GenerateSideCarName[] SidecarNameGenerators = {
        (p) => new SafeUri(p.AbsoluteUri + ".xmp"),      // image.jpg.xmp
        (p) => p.ReplaceExtension(".xmp"),               // image.xmp
        (p) => p.ReplaceExtension(".XMP"),               // image.XMP (uppercase)
        (p) => GetDotDirSidecar(p),                      // .image.jpg.xmp (hidden)
    };
    
    public static SafeUri GetSidecarUri(SafeUri photoUri)
    {
        // First probe for existing sidecar files
        foreach (var generator in SidecarNameGenerators)
        {
            var name = generator(photoUri);
            if (File.Exists(name.AbsolutePath))
            {
                return name;
            }
        }
        
        // Fall back to default strategy
        return SidecarNameGenerators[0](photoUri);
    }
    
    public static bool HasSidecar(SafeUri photoUri)
    {
        return SidecarNameGenerators.Any(generator =>
            File.Exists(generator(photoUri).AbsolutePath));
    }
    
    static SafeUri GetDotDirSidecar(SafeUri photoUri)
    {
        var directory = Path.GetDirectoryName(photoUri.LocalPath);
        var filename = Path.GetFileName(photoUri.LocalPath);
        var sidecarPath = Path.Combine(directory, $".{filename}.xmp");
        return new SafeUri(sidecarPath);
    }
}
```

### Safe Metadata Writing

**Non-Destructive Metadata Preservation**:
```csharp
public static class SafeMetadataWriter
{
    public static void SaveSafely(this TagLib.Image.File metadata, SafeUri photoUri, 
                                 bool alwaysSidecar)
    {
        if (alwaysSidecar || !metadata.Writeable || metadata.PossiblyCorrupt)
        {
            if (!alwaysSidecar && metadata.PossiblyCorrupt)
            {
                Logger.Log.Warning($"Metadata of file {photoUri} may be corrupt, " +
                                  "refusing to write to it, falling back to XMP sidecar.");
            }
            
            // Use sidecar file for safety
            var sidecar_res = new TagLibFileAbstraction(SidecarManager.GetSidecarUri(photoUri));
            metadata.SaveXmpSidecar(sidecar_res);
        }
        else
        {
            // Safe to write directly to file
            try
            {
                metadata.Save();
            }
            catch (Exception ex)
            {
                Logger.Log.Warning($"Failed to save metadata to {photoUri}, " +
                                  "falling back to sidecar", ex);
                                  
                // Fallback to sidecar
                var sidecar_res = new TagLibFileAbstraction(SidecarManager.GetSidecarUri(photoUri));
                metadata.SaveXmpSidecar(sidecar_res);
            }
        }
    }
}
```

## Metadata Import Integration

### Import-Time Metadata Processing

**Automatic Metadata Extraction**:
```csharp
public class MetadataImporter
{
    readonly TagStore tagStore;
    
    public MetadataImporter(TagStore tagStore)
    {
        this.tagStore = tagStore;
    }
    
    public void ImportMetadata(Photo photo, SafeUri imageUri)
    {
        var metadataFile = MetadataUtils.Parse(imageUri);
        if (metadataFile == null)
        {
            Logger.Log.Debug($"No metadata found for {imageUri}");
            return;
        }
        
        try
        {
            // Import basic photo information
            ImportBasicMetadata(photo, metadataFile);
            
            // Import camera and technical data
            ImportTechnicalMetadata(photo, metadataFile);
            
            // Import keywords as tags
            ImportKeywords(photo, metadataFile);
            
            // Import GPS coordinates
            ImportGpsData(photo, metadataFile);
            
            // Import rating and color labels
            ImportRatingAndLabels(photo, metadataFile);
            
            // Import copyright and rights information
            ImportRightsMetadata(photo, metadataFile);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to import metadata for {imageUri}", ex);
        }
        finally
        {
            metadataFile.Dispose();
        }
    }
    
    void ImportBasicMetadata(Photo photo, TagLib.Image.File metadataFile)
    {
        // Set capture date from EXIF
        if (metadataFile.ImageTag?.DateTime != null)
        {
            photo.Time = metadataFile.ImageTag.DateTime.Value.ToUniversalTime();
        }
        
        // Set description from various sources
        var description = metadataFile.ImageTag?.Comment ??
                         metadataFile.Tag?.Comment ??
                         GetXmpDescription(metadataFile);
        
        if (!string.IsNullOrWhiteSpace(description))
        {
            photo.Description = description.Trim();
        }
    }
    
    void ImportKeywords(Photo photo, TagLib.Image.File metadataFile)
    {
        var keywords = new HashSet<string>();
        
        // Collect keywords from multiple sources
        if (metadataFile.ImageTag?.Keywords != null)
            keywords.UnionWith(metadataFile.ImageTag.Keywords);
            
        if (metadataFile.Tag?.Genres != null)
            keywords.UnionWith(metadataFile.Tag.Genres);
            
        // Import XMP keywords
        var xmpKeywords = GetXmpKeywords(metadataFile);
        if (xmpKeywords != null)
            keywords.UnionWith(xmpKeywords);
        
        // Create/find tags and assign to photo
        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var tag = GetOrCreateTag(keyword.Trim());
            if (tag != null && !photo.HasTag(tag))
            {
                photo.AddTag(tag);
            }
        }
    }
    
    void ImportGpsData(Photo photo, TagLib.Image.File metadataFile)
    {
        var exifData = ExifExtractor.ExtractExifData(metadataFile);
        
        if (exifData.GpsCoordinates.HasValue)
        {
            var coords = exifData.GpsCoordinates.Value;
            
            // Store GPS coordinates as metadata
            photo.SetMetadata("GPS:Latitude", coords.Latitude.ToString("F6"));
            photo.SetMetadata("GPS:Longitude", coords.Longitude.ToString("F6"));
            photo.SetMetadata("GPS:LatitudeRef", coords.LatitudeRef);
            photo.SetMetadata("GPS:LongitudeRef", coords.LongitudeRef);
            
            if (exifData.GpsAltitude.HasValue)
            {
                photo.SetMetadata("GPS:Altitude", exifData.GpsAltitude.Value.ToString("F2"));
            }
        }
    }
    
    Tag GetOrCreateTag(string keyword)
    {
        // Handle hierarchical keywords (keyword1/keyword2/keyword3)
        if (keyword.Contains('/'))
        {
            return CreateHierarchicalTag(keyword);
        }
        
        // Check for existing tag
        var existingTag = tagStore.GetTagByName(keyword);
        if (existingTag != null)
            return existingTag;
            
        // Create new tag
        return tagStore.CreateTag(null, keyword);
    }
    
    Tag CreateHierarchicalTag(string hierarchicalKeyword)
    {
        var parts = hierarchicalKeyword.Split('/');
        Category parentCategory = null;
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var categoryName = parts[i].Trim();
            var category = tagStore.GetCategoryByName(categoryName);
            
            if (category == null)
            {
                category = tagStore.CreateCategory(parentCategory, categoryName);
            }
            
            parentCategory = category;
        }
        
        var tagName = parts[parts.Length - 1].Trim();
        return tagStore.CreateTag(parentCategory, tagName);
    }
}
```

## Metadata Search and Indexing

### Searchable Metadata Storage

**Database Schema for Metadata**:
```sql
-- Photo metadata table
CREATE TABLE photo_metadata (
    photo_id INTEGER REFERENCES photos(id),
    name TEXT NOT NULL,
    value TEXT,
    PRIMARY KEY (photo_id, name)
);

-- EXIF data table
CREATE TABLE exif_data (
    photo_id INTEGER PRIMARY KEY REFERENCES photos(id),
    camera_make TEXT,
    camera_model TEXT,
    lens_model TEXT,
    focal_length REAL,
    aperture REAL,
    shutter_speed REAL,
    iso_speed INTEGER,
    flash_fired BOOLEAN,
    gps_latitude REAL,
    gps_longitude REAL,
    gps_altitude REAL
);

-- Metadata search indexes
CREATE INDEX idx_photo_metadata_name ON photo_metadata(name);
CREATE INDEX idx_photo_metadata_value ON photo_metadata(value);
CREATE INDEX idx_exif_camera_make ON exif_data(camera_make);
CREATE INDEX idx_exif_focal_length ON exif_data(focal_length);
CREATE INDEX idx_exif_gps ON exif_data(gps_latitude, gps_longitude);
```

### Metadata Query System

**Advanced Metadata Searching**:
```csharp
public class MetadataQueryBuilder
{
    public class MetadataQuery : IQueryCondition
    {
        public string MetadataKey { get; set; }
        public string Operator { get; set; }  // =, !=, <, >, LIKE, etc.
        public object Value { get; set; }
        
        public string SqlClause()
        {
            return $@"
                photos.id IN (
                    SELECT photo_id FROM photo_metadata 
                    WHERE name = '{MetadataKey}' AND value {Operator} @value
                )";
        }
        
        public DbCommand SqlCommand(string path)
        {
            var command = new FSpotCommand();
            command.CommandText = SqlClause();
            command.Parameters.Add(new FSpotParameter("value", Value));
            return command;
        }
    }
    
    // Camera-specific queries
    public static IQueryCondition CameraMake(string make)
    {
        return new MetadataQuery 
        { 
            MetadataKey = "EXIF:Make", 
            Operator = "LIKE", 
            Value = $"%{make}%" 
        };
    }
    
    public static IQueryCondition FocalLengthRange(double minMm, double maxMm)
    {
        return new AndOperator(
            new MetadataQuery 
            { 
                MetadataKey = "EXIF:FocalLength", 
                Operator = ">=", 
                Value = minMm 
            },
            new MetadataQuery 
            { 
                MetadataKey = "EXIF:FocalLength", 
                Operator = "<=", 
                Value = maxMm 
            }
        );
    }
    
    public static IQueryCondition HasGpsCoordinates()
    {
        return new AndOperator(
            new MetadataQuery 
            { 
                MetadataKey = "GPS:Latitude", 
                Operator = "IS NOT NULL", 
                Value = null 
            },
            new MetadataQuery 
            { 
                MetadataKey = "GPS:Longitude", 
                Operator = "IS NOT NULL", 
                Value = null 
            }
        );
    }
}
```

## Metadata Export and Preservation

### Export-Time Metadata Handling

**Metadata Preservation During Export**:
```csharp
public class MetadataPreservationFilter : IFilter
{
    public SafeUri Process(SafeUri input, SafeUri output)
    {
        // Load original metadata
        var originalMetadata = MetadataUtils.Parse(input);
        if (originalMetadata == null)
        {
            // No metadata to preserve, just copy file
            File.Copy(input.LocalPath, output.LocalPath);
            return output;
        }
        
        try
        {
            // Process image file (resize, adjust, etc.)
            ProcessImageFile(input, output);
            
            // Load processed file for metadata writing
            var outputMetadata = MetadataUtils.Parse(output);
            if (outputMetadata == null)
                return output;
                
            try
            {
                // Preserve essential metadata
                PreserveEssentialMetadata(originalMetadata, outputMetadata);
                
                // Add processing history
                AddProcessingHistory(outputMetadata, "F-Spot Export");
                
                // Save metadata safely
                outputMetadata.SaveSafely(output, false);
            }
            finally
            {
                outputMetadata.Dispose();
            }
        }
        finally
        {
            originalMetadata.Dispose();
        }
        
        return output;
    }
    
    void PreserveEssentialMetadata(TagLib.Image.File source, TagLib.Image.File dest)
    {
        // Preserve copyright and rights
        if (source.Tag != null && dest.Tag != null)
        {
            dest.Tag.Copyright = source.Tag.Copyright;
            dest.Tag.Comment = source.Tag.Comment;
        }
        
        // Preserve EXIF camera data (but not image dimensions)
        if (source.ImageTag?.Exif != null && dest.ImageTag?.Exif != null)
        {
            var sourceExif = source.ImageTag.Exif;
            var destExif = dest.ImageTag.Exif;
            
            // Camera identification
            destExif.Make = sourceExif.Make;
            destExif.Model = sourceExif.Model;
            
            // Capture settings
            destExif.DateTime = sourceExif.DateTime;
            destExif.ExposureTime = sourceExif.ExposureTime;
            destExif.FNumber = sourceExif.FNumber;
            destExif.ISOSpeedRatings = sourceExif.ISOSpeedRatings;
            destExif.FocalLength = sourceExif.FocalLength;
            
            // GPS data
            CopyGpsData(sourceExif, destExif);
        }
        
        // Preserve keywords and ratings
        if (source.ImageTag != null && dest.ImageTag != null)
        {
            dest.ImageTag.Keywords = source.ImageTag.Keywords;
            dest.ImageTag.Rating = source.ImageTag.Rating;
        }
    }
    
    void AddProcessingHistory(TagLib.Image.File metadata, string operation)
    {
        var xmpTag = metadata.GetTag(TagLib.TagTypes.XMP, true) as XmpTag;
        if (xmpTag != null)
        {
            // Add to XMP processing history
            var historyEntry = new ProcessingHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                SoftwareAgent = "F-Spot Photo Manager",
                Operation = operation,
                Parameters = GetProcessingParameters()
            };
            
            AddToXmpHistory(xmpTag, historyEntry);
        }
    }
}
```

## Error Handling and Recovery

### Robust Metadata Processing

**Error Recovery Strategies**:
```csharp
public class RobustMetadataProcessor
{
    public bool ProcessMetadataSafely(SafeUri uri, Action<TagLib.Image.File> processor)
    {
        TagLib.Image.File metadataFile = null;
        var success = false;
        
        try
        {
            // Try to parse metadata
            metadataFile = MetadataUtils.Parse(uri);
            if (metadataFile == null)
            {
                Logger.Log.Warning($"No metadata support for file: {uri}");
                return false;
            }
            
            // Create backup before processing
            var backupPath = CreateMetadataBackup(uri);
            
            try
            {
                // Process metadata
                processor(metadataFile);
                
                // Save changes
                metadataFile.SaveSafely(uri, false);
                success = true;
                
                // Remove backup on success
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Metadata processing failed for {uri}", ex);
                
                // Restore from backup
                RestoreFromBackup(uri, backupPath);
                throw;
            }
        }
        catch (TagLib.UnsupportedFormatException)
        {
            Logger.Log.Debug($"Unsupported format for metadata: {uri}");
        }
        catch (TagLib.CorruptFileException ex)
        {
            Logger.Log.Warning($"Corrupt metadata in file {uri}, using sidecar", ex);
            
            // Try sidecar-only approach
            success = ProcessWithSidecarOnly(uri, processor);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Unexpected error processing metadata for {uri}", ex);
        }
        finally
        {
            metadataFile?.Dispose();
        }
        
        return success;
    }
    
    string CreateMetadataBackup(SafeUri uri)
    {
        var backupPath = uri.LocalPath + ".metadata.backup";
        
        // Backup original file
        File.Copy(uri.LocalPath, backupPath, true);
        
        // Backup sidecar if exists
        var sidecarUri = SidecarManager.GetSidecarUri(uri);
        if (File.Exists(sidecarUri.LocalPath))
        {
            File.Copy(sidecarUri.LocalPath, sidecarUri.LocalPath + ".backup", true);
        }
        
        return backupPath;
    }
    
    void RestoreFromBackup(SafeUri uri, string backupPath)
    {
        if (File.Exists(backupPath))
        {
            File.Copy(backupPath, uri.LocalPath, true);
            File.Delete(backupPath);
        }
        
        // Restore sidecar backup
        var sidecarBackup = SidecarManager.GetSidecarUri(uri).LocalPath + ".backup";
        if (File.Exists(sidecarBackup))
        {
            File.Copy(sidecarBackup, SidecarManager.GetSidecarUri(uri).LocalPath, true);
            File.Delete(sidecarBackup);
        }
    }
}
```

## Performance Optimizations

### Metadata Caching System

**Efficient Metadata Access**:
```csharp
public class MetadataCache
{
    readonly ConcurrentDictionary<string, CachedMetadata> cache = new();
    readonly Timer cleanupTimer;
    readonly TimeSpan maxAge = TimeSpan.FromMinutes(30);
    
    public MetadataCache()
    {
        cleanupTimer = new Timer(CleanupExpiredEntries, null, 
                               TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public CachedMetadata GetMetadata(SafeUri uri)
    {
        var key = uri.ToString();
        
        if (cache.TryGetValue(key, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < maxAge)
            {
                return cached;
            }
            
            // Remove expired entry
            cache.TryRemove(key, out _);
        }
        
        // Load and cache metadata
        var metadata = LoadMetadata(uri);
        var cachedMetadata = new CachedMetadata
        {
            Uri = uri,
            ExifData = metadata.ExifData,
            Keywords = metadata.Keywords,
            Rating = metadata.Rating,
            CachedAt = DateTime.UtcNow
        };
        
        cache.TryAdd(key, cachedMetadata);
        return cachedMetadata;
    }
    
    public void InvalidateCache(SafeUri uri)
    {
        cache.TryRemove(uri.ToString(), out _);
    }
    
    void CleanupExpiredEntries(object state)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var expiredKeys = cache.Where(kvp => kvp.Value.CachedAt < cutoff)
                              .Select(kvp => kvp.Key)
                              .ToList();
        
        foreach (var key in expiredKeys)
        {
            cache.TryRemove(key, out _);
        }
    }
}
```

## Conclusion

F-Spot's metadata and EXIF system demonstrates comprehensive support for digital photo metadata standards. Key architectural strengths include:

**Metadata Standards Excellence**:
- Full support for EXIF, IPTC, and XMP metadata standards
- Robust metadata extraction with fallback strategies
- Comprehensive camera and technical data handling
- GPS coordinate processing and storage

**Sidecar File Management**:
- Non-destructive metadata workflows with XMP sidecars
- Multiple sidecar naming convention support
- Safe metadata writing with corruption protection
- Automatic sidecar discovery and loading

**Import/Export Integration**:
- Automatic metadata extraction during import
- Keyword-to-tag conversion with hierarchical support
- Metadata preservation during export operations
- Processing history tracking

**Search and Query Capabilities**:
- Database-backed metadata searching
- Advanced query builders for technical metadata
- GPS-based location queries
- Camera equipment filtering

**Modernization Opportunities**:
- Async metadata processing for better performance
- Modern metadata standards (HEIF, WebP)
- Cloud metadata synchronization
- Machine learning-based metadata enhancement
- Real-time metadata editing with live preview

The system provides a solid foundation for professional metadata management that supports photographers' workflows while maintaining data integrity and enabling powerful search capabilities.
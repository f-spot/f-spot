# Import/Export System Architecture Analysis

## Overview

F-Spot implements a comprehensive import/export system that handles photo ingestion from various sources and export to multiple destinations. The system is built around a plugin-based architecture that enables extensible import sources and export targets while maintaining consistent workflows and user experiences.

## Architecture Overview

### System Components

```
Import/Export Architecture
├── Import System
│   ├── Import Controller (Core Logic)
│   ├── Import Sources (File System, Cameras, etc.)
│   ├── Metadata Importer (EXIF/XMP extraction)
│   └── Photo File Tracker (Duplicate detection)
└── Export System
    ├── Export Interface (IExporter)
    ├── Export Providers (Plugins)
    ├── Filter Pipeline (Image processing)
    └── Account Management (Authentication)
```

## Import System Architecture

### Import Controller (`src/Core/FSpot/Import/ImportController.cs`)

**Core Import Logic**:
```csharp
public class ImportController : IImportController
{
    // Dependencies
    readonly IFileSystem fileSystem;
    readonly IThumbnailLoader thumbnailLoader;
    
    // Import tracking
    PhotoFileTracker photo_file_tracker;
    MetadataImporter metadata_importer;
    Stack\<SafeUri\> created_directories;
    List<uint> imported_photos;
    List\<SafeUri\> failedImports;
    Roll createdRoll;
    
    public void DoImport(IDb db, IBrowsableCollection photos, 
                        IList\<Tag\> tagsToAttach, ImportPreferences preferences,
                        IProgress<int> progress, CancellationToken token)
    {
        // Transaction setup
        db.Sync = false;
        created_directories = new Stack\<SafeUri\>();
        imported_photos = new List<uint>();
        
        // Initialize import infrastructure
        photo_file_tracker = new PhotoFileTracker(fileSystem);
        metadata_importer = new MetadataImporter(db.Tags);
        createdRoll = db.Rolls.Create();
        
        try {
            // Process each photo
            foreach (var info in photos.Items) {
                if (token.IsCancellationRequested) {
                    RollbackImport(db);
                    return;
                }
                
                ImportPhoto(db, info, createdRoll, tagsToAttach, 
                           preferences.DuplicateDetect, preferences.CopyFiles);
            }
            
            FinishImport(preferences.RemoveOriginals);
        }
        catch (Exception) {
            RollbackImport(db);
            throw;
        }
    }
}
```

**Import Features**:
- **Transactional Import**: Rollback capability on failures
- **Progress Reporting**: Real-time import progress updates
- **Cancellation Support**: User-initiated import cancellation
- **Duplicate Detection**: MD5-based duplicate prevention
- **Error Recovery**: Individual photo failures don't stop import
- **Rollback Support**: Complete rollback on critical failures

### Import Sources

#### File Import Source (`src/Core/FSpot/Import/FileImportSource.cs`)

**File System Import**:
```csharp
public class FileImportSource : IImportSource
{
    public string Name => "File System";
    public string IconName => "folder";
    
    public IEnumerable<IImportInfo> GetImportableItems(SafeUri uri)
    {
        // Recursive file enumeration
        var enumerator = new RecursiveFileEnumerator(fileSystem);
        foreach (var file in enumerator.EnumerateFiles(uri))
        {
            if (IsImageFile(file))
                yield return new FileImportInfo(file);
        }
    }
    
    bool IsImageFile(SafeUri uri)
    {
        var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
        return imageFileFactory.SupportsExtension(extension);
    }
}
```

#### Multi-File Import Source

**Batch File Processing**:
```csharp
public class MultiFileImportSource : IImportSource
{
    public IEnumerable<IImportInfo> GetImportableItems(IEnumerable\<SafeUri\> uris)
    {
        var fileList = new List<FileImportInfo>();
        
        foreach (var uri in uris)
        {
            if (fileSystem.Directory.Exists(uri.LocalPath))
            {
                // Recursive directory processing
                var dirSource = new FileImportSource(fileSystem, imageFileFactory);
                fileList.AddRange(dirSource.GetImportableItems(uri)
                    .Cast<FileImportInfo>());
            }
            else if (fileSystem.File.Exists(uri.LocalPath))
            {
                // Individual file processing
                fileList.Add(new FileImportInfo(uri));
            }
        }
        
        return fileList.OrderBy(f => f.DateTime);
    }
}
```

### Metadata Import System

#### Metadata Importer (`src/Core/FSpot/Import/MetadataImporter.cs`)

**EXIF/XMP Processing**:
```csharp
public class MetadataImporter
{
    readonly TagStore tagStore;
    
    public void Import(Photo photo, IImageFile imageFile)
    {
        // Extract creation date from EXIF
        var exifData = imageFile.GetExifData();
        if (exifData.DateTime.HasValue)
        {
            photo.Time = exifData.DateTime.Value.ToUniversalTime();
        }
        
        // Import keywords as tags
        var keywords = imageFile.GetKeywords();
        foreach (var keyword in keywords)
        {
            var tag = GetOrCreateTag(keyword);
            if (tag != null && !photo.HasTag(tag))
                photo.AddTag(tag);
        }
        
        // Import GPS coordinates
        if (exifData.GpsCoordinates.HasValue)
        {
            // Store GPS metadata
            var coords = exifData.GpsCoordinates.Value;
            photo.SetMetadata("GPS:Latitude", coords.Latitude.ToString());
            photo.SetMetadata("GPS:Longitude", coords.Longitude.ToString());
        }
        
        // Import camera information
        ImportCameraMetadata(photo, exifData);
        
        // Import rating information
        if (exifData.Rating.HasValue)
            photo.Rating = Math.Max(0, Math.Min(5, exifData.Rating.Value));
    }
    
    Tag GetOrCreateTag(string keyword)
    {
        // Check for existing tag
        var existingTag = tagStore.GetTagByName(keyword);
        if (existingTag != null)
            return existingTag;
            
        // Create new tag with hierarchical structure
        if (keyword.Contains('/'))
        {
            return CreateHierarchicalTag(keyword);
        }
        
        return tagStore.CreateTag(null, keyword);
    }
}
```

### Photo File Tracker

**Duplicate Detection System**:
```csharp
public class PhotoFileTracker
{
    readonly Dictionary<string, SafeUri> md5ToUri = new();
    readonly Dictionary<SafeUri, string> uriToMd5 = new();
    
    public bool IsDuplicate(SafeUri uri, out Photo existingPhoto)
    {
        existingPhoto = null;
        
        // Calculate MD5 hash
        var md5 = HashUtils.ComputeMD5(uri);
        
        // Check for existing hash
        if (md5ToUri.TryGetValue(md5, out var existingUri))
        {
            existingPhoto = photoStore.GetPhotoByUri(existingUri);
            return true;
        }
        
        // Track new file
        md5ToUri[md5] = uri;
        uriToMd5[uri] = md5;
        
        return false;
    }
    
    public void UpdateTracking(SafeUri oldUri, SafeUri newUri)
    {
        if (uriToMd5.TryGetValue(oldUri, out var md5))
        {
            uriToMd5.Remove(oldUri);
            uriToMd5[newUri] = md5;
            md5ToUri[md5] = newUri;
        }
    }
}
```

## Export System Architecture

### Export Interface Design

**IExporter Interface**:
```csharp
public interface IExporter
{
    string Name { get; }
    string IconName { get; }
    
    void Run(IBrowsableCollection selection);
}
```

### Export Implementations

#### Folder Export (`src/Extensions/Exporters/FSpot.Exporters.Folder/`)

**Static Gallery Generation**:
```csharp
public class FolderExport : IExporter
{
    // Export configuration
    bool scale;           // Resize images
    bool exportTags;      // Include tag information
    bool exportTagIcons;  // Include tag icons
    int size;            // Target image size
    string description;   // Gallery description
    
    public void Run(IBrowsableCollection selection)
    {
        // Initialize export directory
        dest = new DirectoryInfo(uri_chooser.Uri.LocalPath);
        if (!dest.Exists)
            dest.Create();
            
        // Create gallery structure
        var gallery = CreateGallery(selection);
        
        // Export images with processing
        foreach (var photo in selection.Items)
        {
            ExportPhoto(photo, dest, gallery);
        }
        
        // Generate gallery files
        gallery.WriteGallery(dest);
    }
    
    void ExportPhoto(IPhoto photo, DirectoryInfo dest, IGallery gallery)
    {
        var source = photo.DefaultVersion.Uri;
        var target = new SafeUri(Path.Combine(dest.FullName, photo.Name));
        
        // Apply image filters
        var filterSet = new FilterSet();
        
        if (scale)
            filterSet.Add(new ResizeFilter((uint)size));
            
        filterSet.Add(new SharpFilter());
        
        // Process and save image
        var request = new FilterRequest(source, target, filterSet);
        request.Process();
        
        // Add to gallery
        gallery.AddPhoto(photo, target);
    }
}
```

**Gallery Generation Types**:
- **Static Gallery**: HTML pages with CSS styling
- **Original Gallery**: Original files with metadata
- **Plain Gallery**: Simple file copying

#### Flickr Export (`src/Extensions/Exporters/FSpot.Exporters.Flickr/`)

**OAuth Authentication Flow**:
```csharp
public class FlickrExport : IExporter
{
    FlickrRemote fr;
    OAuthAccessToken token;
    State authState;
    
    enum State
    {
        Disconnected,           // No authentication
        RequestTokenReceived,   // Got request token
        Authenticated          // Full authentication
    }
    
    public void AuthenticateUser()
    {
        // Step 1: Get request token
        var requestToken = fr.GetRequestToken();
        
        // Step 2: Open browser for user authorization
        var authUrl = fr.GetAuthorizationUrl(requestToken, AuthLevel.Write);
        Process.Start(authUrl);
        
        authState = State.RequestTokenReceived;
        
        // Step 3: Wait for verification code
        // User enters code in dialog
    }
    
    public void CompleteAuthentication(string verificationCode)
    {
        // Exchange verification code for access token
        token = fr.GetAccessToken(verificationCode);
        authState = State.Authenticated;
        
        // Save token for future use
        SaveAuthToken(token);
    }
    
    public void UploadPhotos(IBrowsableCollection selection)
    {
        foreach (var photo in selection.Items)
        {
            try
            {
                UploadSinglePhoto(photo);
            }
            catch (FlickrException ex)
            {
                HandleUploadError(photo, ex);
            }
        }
    }
}
```

**Upload Features**:
- **OAuth 1.0a Authentication**: Secure API authentication
- **Privacy Settings**: Public, friends, family visibility
- **Tag Upload**: F-Spot tags mapped to Flickr tags
- **Hierarchical Tags**: Tag hierarchy preservation
- **Batch Upload**: Multiple photo processing
- **Error Recovery**: Individual photo failure handling

#### Web Service Exports

**Generic Web Service Pattern**:
```csharp
public abstract class WebServiceExport : IExporter
{
    protected abstract IWebServiceAPI CreateAPI();
    protected abstract IAccountManager GetAccountManager();
    
    public void Run(IBrowsableCollection selection)
    {
        // 1. Authentication
        var account = GetAuthenticatedAccount();
        if (account == null) {
            ShowAuthenticationDialog();
            return;
        }
        
        // 2. Album selection/creation
        var album = SelectOrCreateAlbum(account);
        
        // 3. Photo processing and upload
        UploadPhotos(selection, album);
    }
    
    protected virtual void UploadPhotos(IBrowsableCollection selection, IAlbum album)
    {
        var api = CreateAPI();
        
        foreach (var photo in selection.Items)
        {
            // Apply filters
            var processedPhoto = ApplyExportFilters(photo);
            
            // Upload with metadata
            var uploadResult = api.UploadPhoto(processedPhoto, album, 
                GetUploadMetadata(photo));
                
            // Handle result
            ProcessUploadResult(photo, uploadResult);
        }
    }
}
```

**Implemented Web Services**:
- **Flickr**: Popular photo sharing service
- **PicasaWeb/Google Photos**: Google's photo service
- **SmugMug**: Professional photo hosting
- **Facebook**: Social media photo sharing
- **Gallery**: Self-hosted Gallery software
- **23HQ**: Alternative photo sharing

### Filter Pipeline System

#### Filter Architecture (`src/Clients/FSpot.Gtk/FSpot.Filters/`)

**Filter Interface**:
```csharp
public interface IFilter : IDisposable
{
    SafeUri Process(SafeUri input, SafeUri output);
}

public class FilterSet
{
    List<IFilter> filters = new();
    
    public void Add(IFilter filter) => filters.Add(filter);
    
    public SafeUri Process(SafeUri input, SafeUri output)
    {
        var current = input;
        
        for (int i = 0; i < filters.Count; i++)
        {
            var nextOutput = (i == filters.Count - 1) ? output : 
                           GenerateTempFileName();
            current = filters[i].Process(current, nextOutput);
        }
        
        return current;
    }
}
```

#### Standard Filters

**Resize Filter**:
```csharp
public class ResizeFilter : IFilter
{
    uint maxSize;
    
    public ResizeFilter(uint size) => maxSize = size;
    
    public SafeUri Process(SafeUri input, SafeUri output)
    {
        using var original = new Pixbuf(input.LocalPath);
        
        // Calculate new dimensions maintaining aspect ratio
        var scale = Math.Min((double)maxSize / original.Width,
                           (double)maxSize / original.Height);
        
        if (scale >= 1.0)
        {
            // No resizing needed
            File.Copy(input.LocalPath, output.LocalPath);
            return output;
        }
        
        var newWidth = (int)(original.Width * scale);
        var newHeight = (int)(original.Height * scale);
        
        // Resize with high-quality interpolation
        using var resized = original.ScaleSimple(newWidth, newHeight, 
                                               InterpType.Bilinear);
        
        // Save with JPEG quality preservation
        SaveWithQuality(resized, output, GetJpegQuality(input));
        
        return output;
    }
}
```

**Sharpen Filter**:
```csharp
public class SharpFilter : IFilter
{
    public SafeUri Process(SafeUri input, SafeUri output)
    {
        using var original = new Pixbuf(input.LocalPath);
        
        // Apply unsharp mask
        var sharpened = ApplyUnsharpMask(original, 
            radius: 1.0, amount: 0.5, threshold: 0);
        
        sharpened.Save(output.LocalPath, "jpeg");
        
        return output;
    }
    
    Pixbuf ApplyUnsharpMask(Pixbuf source, double radius, 
                           double amount, int threshold)
    {
        // Gaussian blur for mask
        var blurred = ApplyGaussianBlur(source, radius);
        
        // Calculate difference and apply sharpening
        return ApplySharpening(source, blurred, amount, threshold);
    }
}
```

**JPEG Quality Filter**:
```csharp
public class JpegFilter : IFilter
{
    int quality = 95;
    
    public SafeUri Process(SafeUri input, SafeUri output)
    {
        using var pixbuf = new Pixbuf(input.LocalPath);
        
        // Preserve EXIF data
        var exifData = ExtractExifData(input);
        
        // Save with specified quality
        var options = new string[] { "quality", quality.ToString() };
        pixbuf.Save(output.LocalPath, "jpeg", options);
        
        // Restore EXIF data
        WriteExifData(output, exifData);
        
        return output;
    }
}
```

### Account Management System

#### Generic Account Framework

**Account Interface**:
```csharp
public interface IAccount
{
    string Name { get; set; }
    string Username { get; set; }
    bool IsAuthenticated { get; }
    
    void Authenticate();
    void Logout();
}

public abstract class AccountManager\<T\> where T : IAccount
{
    protected List\<T\> accounts = new();
    
    public abstract T CreateAccount();
    public abstract void SaveAccounts();
    public abstract void LoadAccounts();
    
    public T GetDefaultAccount() => 
        accounts.FirstOrDefault(a => a.IsAuthenticated);
        
    public void AddAccount(T account)
    {
        accounts.Add(account);
        SaveAccounts();
    }
}
```

**Flickr Account Implementation**:
```csharp
public class FlickrAccount : IAccount
{
    public string Username { get; set; }
    public string UserId { get; set; }
    public OAuthAccessToken Token { get; set; }
    
    public bool IsAuthenticated => Token != null && !Token.IsExpired;
    
    public void Authenticate()
    {
        var flickr = new FlickrNet.Flickr(ApiKey, SharedSecret);
        
        // OAuth flow implementation
        var requestToken = flickr.OAuthGetRequestToken(CallbackUrl);
        var authUrl = flickr.OAuthCalculateAuthorizationUrl(requestToken, 
                                                          AuthLevel.Write);
        
        // Open browser and wait for callback
        Process.Start(authUrl);
        
        // After user authorization
        Token = flickr.OAuthGetAccessToken(requestToken, verificationCode);
        
        // Get user information
        var user = flickr.TestLogin();
        Username = user.UserName;
        UserId = user.UserId;
    }
}
```

### Export Progress and Error Handling

#### Progress Reporting System

**Progress Dialog Integration**:
```csharp
public class ExportProgressManager
{
    ThreadProgressDialog progressDialog;
    ProgressItem progressItem;
    CancellationTokenSource cancellationSource;
    
    public void StartExport(string title, int totalItems)
    {
        cancellationSource = new CancellationTokenSource();
        
        progressItem = new ProgressItem(title, ProgressItemState.Running, 
                                       totalItems);
        
        progressDialog = new ThreadProgressDialog(progressItem, 
                                                 cancellationSource.Token);
        progressDialog.Show();
    }
    
    public void UpdateProgress(int completed, string currentItem)
    {
        progressItem.Current = completed;
        progressItem.Status = $"Processing: {currentItem}";
        
        ThreadAssist.ProxyToMain(() => {
            progressDialog.Update();
        });
    }
    
    public void HandleError(Exception error, string context)
    {
        var errorMessage = $"Error in {context}: {error.Message}";
        
        ThreadAssist.ProxyToMain(() => {
            var dialog = new MessageDialog(null, DialogFlags.Modal,
                MessageType.Error, ButtonsType.Ok, errorMessage);
            dialog.Run();
            dialog.Destroy();
        });
    }
}
```

#### Error Recovery Strategies

**Resilient Export Pattern**:
```csharp
public class ResilientExporter
{
    public ExportResult ExportWithRetry(IBrowsableCollection selection, 
                                       IExporter exporter, 
                                       ExportOptions options)
    {
        var result = new ExportResult();
        var retryPolicy = CreateRetryPolicy(options);
        
        foreach (var photo in selection.Items)
        {
            try
            {
                retryPolicy.Execute(() => {
                    ExportSinglePhoto(photo, exporter, options);
                });
                
                result.SuccessfulExports.Add(photo);
            }
            catch (Exception ex)
            {
                result.FailedExports.Add(new ExportFailure(photo, ex));
                
                if (options.StopOnError)
                    break;
            }
        }
        
        return result;
    }
    
    RetryPolicy CreateRetryPolicy(ExportOptions options)
    {
        return new RetryPolicy(
            maxAttempts: options.MaxRetries,
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(30),
            backoffMultiplier: 2.0
        );
    }
}
```

### Configuration and Preferences

#### Export Preferences System

**Preference Management**:
```csharp
public class ExportPreferences
{
    public const string ExportKey = "/apps/f-spot/export/";
    
    public static class Flickr
    {
        public const string Service = "flickr/";
        public const string ScaleKey = ExportKey + Service + "scale";
        public const string SizeKey = ExportKey + Service + "size";
        public const string PublicKey = ExportKey + Service + "public";
        public const string TagsKey = ExportKey + Service + "tags";
    }
    
    public static bool GetBool(string key, bool defaultValue = false)
    {
        return Preferences.Get<bool>(key) ?? defaultValue;
    }
    
    public static int GetInt(string key, int defaultValue = 0)
    {
        return Preferences.Get<int>(key) ?? defaultValue;
    }
    
    public static void Set(string key, object value)
    {
        Preferences.Set(key, value);
    }
}
```

### Performance Optimizations

#### Async Export Processing

**Background Export Threading**:
```csharp
public class AsyncExporter
{
    readonly SemaphoreSlim concurrencyLimiter;
    readonly IProgress<ExportProgress> progressReporter;
    
    public AsyncExporter(int maxConcurrency = 4)
    {
        concurrencyLimiter = new SemaphoreSlim(maxConcurrency);
    }
    
    public async Task<ExportResult> ExportAsync(
        IBrowsableCollection selection,
        IExporter exporter,
        CancellationToken cancellationToken = default)
    {
        var tasks = selection.Items.Select(photo => 
            ExportPhotoAsync(photo, exporter, cancellationToken));
            
        var results = await Task.WhenAll(tasks);
        
        return CombineResults(results);
    }
    
    async Task<ExportPhotoResult> ExportPhotoAsync(IPhoto photo, 
                                                  IExporter exporter,
                                                  CancellationToken cancellationToken)
    {
        await concurrencyLimiter.WaitAsync(cancellationToken);
        
        try
        {
            return await Task.Run(() => 
                ExportSinglePhoto(photo, exporter), cancellationToken);
        }
        finally
        {
            concurrencyLimiter.Release();
        }
    }
}
```

## Future Enhancement Opportunities

### Modern Authentication

**OAuth 2.0 and Modern Auth**:
```csharp
public interface IModernAuthProvider
{
    Task<AuthResult> AuthenticateAsync(string clientId, 
                                      string[] scopes,
                                      CancellationToken cancellationToken);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string accessToken);
}

public class OAuth2Provider : IModernAuthProvider
{
    public async Task<AuthResult> AuthenticateAsync(string clientId, 
                                                   string[] scopes,
                                                   CancellationToken cancellationToken)
    {
        // PKCE-enabled OAuth 2.0 flow
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        var authUrl = BuildAuthUrl(clientId, scopes, codeChallenge);
        
        // Open system browser
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        
        // Listen for callback
        var authCode = await ListenForCallback(cancellationToken);
        
        // Exchange code for tokens
        return await ExchangeCodeForTokens(clientId, authCode, codeVerifier);
    }
}
```

### Cloud Storage Integration

**Generic Cloud Storage Interface**:
```csharp
public interface ICloudStorageProvider
{
    string Name { get; }
    Task<bool> IsAvailableAsync();
    Task<ICloudFolder> GetRootFolderAsync();
    Task<ICloudFile> UploadFileAsync(Stream content, string name, 
                                    ICloudFolder folder);
}

public class GoogleDriveProvider : ICloudStorageProvider
{
    public async Task<ICloudFile> UploadFileAsync(Stream content, string name, 
                                                 ICloudFolder folder)
    {
        var driveService = await GetAuthenticatedService();
        
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = name,
            Parents = new[] { folder.Id }
        };
        
        var request = driveService.Files.Create(fileMetadata, content, 
                                               GetMimeType(name));
        
        var uploadedFile = await request.UploadAsync();
        
        return new GoogleDriveFile(uploadedFile.ResponseBody);
    }
}
```

## Conclusion

F-Spot's import/export system demonstrates sophisticated architecture for photo management workflows. Key strengths include:

**Import System Excellence**:
- Transactional import with rollback capabilities
- Comprehensive metadata extraction from EXIF/XMP
- Duplicate detection using MD5 hashing
- Flexible import source architecture
- Progress reporting and cancellation support

**Export System Flexibility**:
- Plugin-based export architecture
- Comprehensive web service integration
- Filter pipeline for image processing
- Account management with secure authentication
- Error recovery and retry mechanisms

**Modernization Opportunities**:
- OAuth 2.0 and modern authentication flows
- Async/await patterns for better responsiveness
- Cloud storage integration beyond traditional services
- Reactive UI patterns for real-time updates
- Dependency injection for better testability

The system provides a solid foundation for photo workflow management that could be enhanced with modern cloud services and authentication patterns while maintaining its flexible, extensible architecture.
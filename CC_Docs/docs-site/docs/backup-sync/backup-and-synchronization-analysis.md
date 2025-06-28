# Backup and Synchronization Analysis

## Overview

F-Spot implements a comprehensive backup and synchronization system that provides database merging capabilities, metadata synchronization with file systems, and background job processing for maintaining data consistency. The system ensures photo metadata is preserved across different storage locations and enables database consolidation from multiple F-Spot installations.

## Architecture Overview

### System Components

```
Backup and Synchronization Architecture
├── Database Merging System
│   ├── MergeDb Tool (Cross-database photo import)
│   ├── Tag Mapping (Hierarchical tag reconciliation)
│   ├── Roll Reconciliation (Import roll management)
│   └── Photo Import Pipeline (Version-aware copying)
├── Metadata Synchronization
│   ├── Sync Metadata Jobs (Background metadata writing)
│   ├── File System Sync (EXIF/XMP metadata preservation)
│   ├── Sidecar File Management (Non-destructive workflows)
│   └── Batch Processing (Catalog-wide synchronization)
├── Job Processing System
│   ├── Background Job Scheduler (Asynchronous processing)
│   ├── Persistent Job Queue (Database-backed scheduling)
│   ├── Job Priority Management (Resource allocation)
│   └── Error Recovery (Failed job retry mechanisms)
├── Database Backup and Repair
│   ├── Database Backup Creation (Corruption recovery)
│   ├── Schema Migration (Version compatibility)
│   ├── Database Integrity Checking (Validation routines)
│   └── Emergency Recovery (Repair mechanisms)
└── Integration Points
    ├── Import System (Automatic metadata sync)
    ├── Export Workflows (Metadata preservation)
    ├── Catalog Management (Multi-database operations)
    └── User Interface (Progress reporting and control)
```

## Database Merging System

### MergeDb Tool (`src/Extensions/Tools/FSpot.Tools.MergeDb/`)

**Cross-Database Photo Import**:
```csharp
public class MergeDb : ICommand
{
    Db from_db;       // Source database
    Db to_db;         // Target database (current F-Spot instance)
    List<Roll> new_rolls;
    
    Dictionary<uint, Tag> tag_map;    // Source-to-target tag mapping
    Dictionary<uint, uint> roll_map;  // Source-to-target roll mapping
    
    public void Run(object o, EventArgs e)
    {
        from_db = new Db(App.Instance.Container.Resolve<IImageFileFactory>(),
                        App.Instance.Container.Resolve<IThumbnailService>(),
                        new UpdaterUI());
        to_db = App.Instance.Database;
        
        mdd = new MergeDbDialog(this);
        mdd.FileChooser.FileSet += HandleFileSet;
        mdd.Dialog.Response += HandleResponse;
        mdd.ShowAll();
    }
    
    void HandleFileSet(object o, EventArgs e)
    {
        try
        {
            // Create temporary copy for safe processing
            string tempfilename = Path.GetTempFileName();
            File.Copy(mdd.FileChooser.Filename, tempfilename, true);
            
            from_db.Init(tempfilename, true);
            
            FillRolls();  // Identify new rolls to import
            mdd.Rolls = new_rolls;
            mdd.SetSensitive();
        }
        catch (Exception ex)
        {
            // Show user-friendly error dialog
            ShowErrorDialog("Error opening the selected file", ex.Message);
        }
    }
}
```

### Tag Mapping and Reconciliation

**Hierarchical Tag Merging**:
```csharp
void MergeTags(Tag tagToMerge)
{
    TagStore fromStore = from_db.Tags;
    TagStore to_store = to_db.Tags;
    
    if (tagToMerge != fromStore.RootCategory)  // Skip root category
    {
        Tag dest_tag = to_store.GetTagByName(tagToMerge.Name);
        
        if (dest_tag == null)
        {
            // Create new tag with proper parent hierarchy
            Category parent = (tagToMerge.Category == fromStore.RootCategory) ?
                            to_store.RootCategory :
                            to_store.GetTagByName(tagToMerge.Category.Name) as Category;
                            
            dest_tag = to_store.CreateTag(parent, tagToMerge.Name, false);
            // TODO: Copy tag icon and commit
        }
        
        // Map source tag ID to target tag
        tag_map[tagToMerge.Id] = dest_tag;
    }
    
    // Recursively process child tags
    if (tagToMerge is Category category)
    {
        foreach (var t in category.Children)
            MergeTags(t);
    }
}
```

### Roll Reconciliation

**Import Roll Management**:
```csharp
void FillRolls()
{
    var from_rolls = new List<Roll>(from_db.Rolls.GetRolls());
    var to_rolls = to_db.Rolls.GetRolls();
    
    // Remove rolls that already exist in target database
    foreach (Roll tr in to_rolls)
    {
        foreach (Roll fr in from_rolls.ToArray())
        {
            if (tr.Time == fr.Time)  // Match by timestamp
                from_rolls.Remove(fr);
        }
    }
    
    new_rolls = from_rolls;  // Only new rolls to import
}

void CreateRolls(List<Roll> rolls)
{
    if (rolls == null)
        rolls = from_db.Rolls.GetRolls();
        
    RollStore from_store = from_db.Rolls;
    RollStore to_store = to_db.Rolls;
    
    foreach (Roll roll in rolls)
    {
        if (from_store.PhotosInRoll(roll) == 0)
            continue;
            
        // Create corresponding roll in target database
        roll_map[roll.Id] = to_store.Create(roll.Time).Id;
    }
}
```

### Photo Import Pipeline

**Version-Aware Photo Copying**:
```csharp
void ImportPhoto(Photo photo, bool copy)
{
    Logger.Log.Warning($"Importing {photo.Name}");
    PhotoStore to_store = to_db.Photos;
    
    string photoPath = photo.VersionUri(Photo.OriginalVersionId).AbsolutePath;
    
    // Handle missing files with path mapping
    while (!File.Exists(photoPath))
    {
        // Try path mappings for relocated files
        foreach (string key in PathMap.Keys)
        {
            string path = photoPath.Replace(key, PathMap[key]);
            if (File.Exists(path))
            {
                photoPath = path;
                break;
            }
        }
        
        if (!File.Exists(photoPath))
        {
            // Interactive folder picker for missing files
            string[] parts = photoPath.Split('/');
            if (parts.Length > 6)
            {
                string folder = string.Join("/", parts, 0, parts.Length - 4);
                var pfd = new PickFolderDialog(mdd.Dialog, folder);
                string new_folder = pfd.Run();
                
                if (new_folder == null) return; // Skip this photo
                
                PathMap[folder] = new_folder;
            }
        }
    }
    
    string destination;
    Photo newp;
    
    // Determine destination path
    if (copy)
        destination = FindImportDestination(new SafeUri(photoPath), photo.Time).AbsolutePath;
    else
        destination = photoPath;
        
    // Copy file if needed
    if (photoPath != destination)
    {
        File.Copy(photoPath, destination);
        
        // Preserve file attributes and timestamps
        try
        {
            File.SetAttributes(destination, 
                File.GetAttributes(destination) & ~FileAttributes.ReadOnly);
            File.SetCreationTime(destination, File.GetCreationTime(photoPath));
            File.SetLastWriteTime(destination, File.GetLastWriteTime(photoPath));
        }
        catch (IOException)
        {
            // Non-fatal attribute setting errors
        }
    }
    
    // Create photo record in target database
    newp = to_store.CreateFrom(photo, true, roll_map[photo.RollId]);
    if (newp == null) return;
    
    // Import tags using mapping
    foreach (Tag t in photo.Tags)
    {
        Logger.Log.Warning($"Tagging with {t.Name}");
        newp.AddTag(tag_map[t.Id]);
    }
    
    // Import all photo versions
    foreach (uint version_id in photo.VersionIds)
    {
        if (version_id != Photo.OriginalVersionId)
        {
            var version = photo.GetVersion(version_id) as PhotoVersion;
            uint newv = newp.AddVersion(version.BaseUri, version.Filename, 
                                       version.Name, version.IsProtected);
            if (version_id == photo.DefaultVersionId)
                newp.DefaultVersionId = newv;
        }
    }
    
    // Import metadata
    newp.Time = photo.Time;
    newp.Description = photo.Description;
    newp.Rating = photo.Rating;
    
    to_store.Commit(newp);
}
```

## Metadata Synchronization System

### SyncMetadata Job (`src/Core/FSpot/Database/Jobs/SyncMetadataJob.cs`)

**Background Metadata Writing**:
```csharp
public class SyncMetadataJob : Job
{
    public static SyncMetadataJob Create(JobStore jobStore, Photo photo)
    {
        return (SyncMetadataJob)jobStore.CreatePersistent(JobName, photo.Id.ToString());
    }
    
    public static string JobName => "SyncMetadata";
    
    protected override bool Execute()
    {
        // Add reactivity delay
        System.Threading.Thread.Sleep(500);
        
        try
        {
            Photo photo = Db.Photos.Get(Convert.ToUInt32(JobOptions));
            if (photo == null) return false;
            
            Logger.Log.Debug($"Syncing metadata to file ({photo.DefaultVersion.Uri})...");
            
            WriteMetadataToImage(photo);
            return true;
        }
        catch (Exception e)
        {
            Logger.Log.Error(e, "Error syncing metadata to file");
        }
        return false;
    }
    
    void WriteMetadataToImage(Photo photo)
    {
        var tags = photo.Tags;
        var names = new List<string>(tags.Length);
        
        foreach (var t in tags)
            names.Add(t.Name);
            
        using var metadata = MetadataUtils.Parse(photo.DefaultVersion.Uri);
        
        metadata.EnsureAvailableTags();
        
        var tag = metadata.ImageTag;
        tag.DateTime = photo.Time;
        tag.Comment = photo.Description ?? string.Empty;
        tag.Keywords = names.ToArray();
        tag.Rating = photo.Rating;
        tag.Software = FSpotConfiguration.Package + " version " + 
                      FSpotConfiguration.Version;
        
        // Respect user preference for sidecar files
        var always_sidecar = Preferences.Get<bool>(Preferences.MetadataAlwaysUseSidecar);
        metadata.SaveSafely(photo.DefaultVersion.Uri, always_sidecar);
    }
}
```

### Catalog Synchronization Tool (`src/Extensions/Tools/FSpot.Tools.SyncCatalog/`)

**Batch Metadata Synchronization**:
```csharp
public class SyncCatalog : ICommand
{
    public void Run(object o, EventArgs e)
    {
        SyncCatalogDialog dialog = new SyncCatalogDialog();
        dialog.ShowDialog();
    }
}

public class SyncCatalogDialog : Dialog
{
    private RadioButton RadioSelectedPhotos;
    private RadioButton RadioEntireCatalog;
    
    void HandleResponse(object obj, ResponseArgs args)
    {
        switch(args.ResponseId)
        {
            case ResponseType.Cancel:
                this.Destroy();
                break;
                
            case ResponseType.Apply:
                Photo[] photos = new Photo[0];
                
                if (RadioEntireCatalog.Active)
                    photos = Core.Database.Photos.Query();  // Entire catalog
                else if (RadioSelectedPhotos.Active)
                    photos = App.Instance.Organizer.SelectedPhotos();  // Selection
                
                this.Hide();
                
                // Create sync jobs for all photos
                foreach (Photo photo in photos)
                {
                    SyncMetadataJob.Create(Core.Database.Jobs, photo);
                }
                
                this.Destroy();
                break;
        }
    }
}
```

## Job Processing System

### Job Store (`src/Core/FSpot/Database/JobStore.cs`)

**Persistent Job Queue Management**:
```csharp
public class JobStore : DbStore<Job>
{
    const string jobsTableName = "jobs";
    readonly TinyIoCContainer container;
    
    internal static void CreateTable(FSpotDatabaseConnection database)
    {
        if (database.TableExists(jobsTableName)) return;
        
        database.Execute(
            $"CREATE TABLE {jobsTableName} (\n" +
            "  id           INTEGER PRIMARY KEY NOT NULL, \n" +
            "  job_type     TEXT NOT NULL, \n" +
            "  job_options  TEXT NOT NULL, \n" +
            "  run_at       INTEGER, \n" +
            "  job_priority INTEGER NOT NULL\n" +
            ")");
    }
    
    Job CreateJob(string type, uint id, string options, DateTime runAt, JobPriority priority)
    {
        using (var childContainer = container.GetChildContainer())
        {
            childContainer.Register(new JobData
            {
                Id = id,
                JobOptions = options,
                JobPriority = priority,
                RunAt = runAt,
                Persistent = true
            });
            
            childContainer.TryResolve<Job>(type, out var job);
            if (job == null)
            {
                Logger.Log.Error($"Unknown job type {type} ignored.");
            }
            return job;
        }
    }
    
    void LoadAllItems()
    {
        var reader = Database.Query(
            $"SELECT id, job_type, job_options, run_at, job_priority FROM {jobsTableName}");
            
        Scheduler.Suspend();
        while (reader.Read())
        {
            Job job = LoadItem(reader);
            if (job != null)
            {
                AddToCache(job);
                job.Finished += HandleRemoveJob;
                Scheduler.Schedule(job, job.JobPriority);
                job.Status = JobStatus.Scheduled;
            }
        }
        
        reader.Dispose();
    }
    
    public Job CreatePersistent(string job_type, string job_options)
    {
        var run_at = DateTime.Now;
        var job_priority = JobPriority.Lowest;
        
        var id = Database.Execute(new HyenaSqliteCommand(
            $"INSERT INTO {jobsTableName} (job_type, job_options, run_at, job_priority) " +
            "VALUES (?, ?, ?, ?)",
            job_type, job_options,
            DateTimeUtil.FromDateTime(run_at),
            Convert.ToInt32(job_priority)));
            
        var job = CreateJob(job_type, (uint)id, job_options, run_at, job_priority);
        
        AddToCache(job);
        job.Finished += HandleRemoveJob;
        Scheduler.Schedule(job, job.JobPriority);
        job.Status = JobStatus.Scheduled;
        EmitAdded(job);
        
        return job;
    }
    
    public JobStore(IDb db, bool is_new) : base(db, true)
    {
        // Job type registration
        container = new TinyIoCContainer();
        container.Register(Db);
        container.Register<Job, SyncMetadataJob>(SyncMetadataJob.JobName).AsMultiInstance();
        container.Register<Job, CalculateHashJob>(CalculateHashJob.JobName).AsMultiInstance();
        
        // Legacy job type compatibility
        container.Register<Job, SyncMetadataJob>("FSpot.Database.Jobs.SyncMetadataJob").AsMultiInstance();
        container.Register<Job, SyncMetadataJob>("FSpot.Jobs.SyncMetadataJob").AsMultiInstance();
        container.Register<Job, CalculateHashJob>("FSpot.Database.Jobs.CalculateHashJob").AsMultiInstance();
        container.Register<Job, CalculateHashJob>("FSpot.Jobs.CalculateHashJob").AsMultiInstance();
        
        if (is_new || !Database.TableExists(jobsTableName))
        {
            CreateTable(Database);
        }
        else
        {
            LoadAllItems();  // Resume interrupted jobs
        }
    }
}
```

## Database Backup and Repair

### Database Repair Mechanism (`src/Core/FSpot/Database/Db.cs`)

**Corruption Recovery**:
```csharp
public class Db : IDb, IDisposable
{
    string path;
    
    public string Repair()
    {
        string backup_path = path;
        int i = 0;
        
        // Generate unique backup filename
        while (File.Exists(backup_path))
        {
            backup_path = $"{Path.GetFileNameWithoutExtension(path)}-" +
                         $"{DateTime.Now.ToString("yyyyMMdd")}-{i++}" +
                         $"{Path.GetExtension(path)}";
        }
        
        // Move corrupted database to backup location
        File.Move(path, backup_path);
        
        // Initialize new database
        Init(path, true);
        
        return backup_path;
    }
    
    public void Init(string path, bool createIfMissing)
    {
        using var op = Operation.Begin("Db Initialization");
        
        bool new_db = !File.Exists(path);
        this.path = path;
        
        if (new_db && !createIfMissing)
            throw new Exception($"{path}: File not found");
            
        Database = new FSpotDatabaseConnection(path);
        
        // Load or create the meta table
        Meta = new MetaStore(this, new_db);
        
        // Update the database schema if necessary
        Updater.Run(Database, updaterDialog);
        
        Database.BeginTransaction();
        
        // Initialize all stores
        Tags = new TagStore(this, new_db);
        Rolls = new RollStore(this, new_db);
        Exports = new ExportStore(this, new_db);
        Jobs = new JobStore(this, new_db);
        Photos = new PhotoStore(imageFileFactory, thumbnailService, this, new_db);
        
        Database.CommitTransaction();
        
        Empty = new_db;
        
        op.Complete();
    }
}
```

## Advanced Synchronization Features

### Path Mapping for Relocated Files

**Flexible File Location Handling**:
```csharp
Dictionary<string, string> path_map = null;
Dictionary<string, string> PathMap {
    get { return path_map ?? new Dictionary<string, string>(); }
}

SafeUri FindImportDestination(SafeUri uri, DateTime time)
{
    // Find a new unique location inside the photo folder
    string name = uri.GetFilename();
    
    var destinationUri = FSpotConfiguration.PhotoUri
        .Append(time.Year.ToString())
        .Append($"{time.Month:D2}")
        .Append($"{time.Day:D2}");
        
    EnsureDirectory(destinationUri);
    
    // If the destination we'd like to use is the file itself return that
    if (destinationUri.Append(name) == uri)
        return uri;
        
    // Find an unused name with collision detection
    int i = 1;
    var dest = destinationUri.Append(name);
    var fileInfo = new FileInfo(dest.AbsolutePath);
    
    while (fileInfo.Exists)
    {
        var filename = uri.GetFilenameWithoutExtension();
        var extension = uri.GetExtension();
        dest = destinationUri.Append($"{filename}-{i++}{extension}");
        fileInfo = new FileInfo(dest.AbsolutePath);
    }
    
    return dest;
}
```

### Synchronization Preferences

**User-Configurable Sync Behavior**:
```csharp
// Metadata synchronization preferences
var always_sidecar = Preferences.Get<bool>(Preferences.MetadataAlwaysUseSidecar);

// Database synchronization settings
public bool Sync {
    set {
        string query = "PRAGMA synchronous = " + (value ? "ON" : "OFF");
        Database.Execute(query);
    }
}
```

## Performance Considerations

### Optimization Strategies

**Efficient Synchronization Operations**:
1. **Background Processing**: Use job scheduler for non-blocking metadata sync
2. **Batch Operations**: Process multiple photos in single database transactions
3. **Smart Scheduling**: Prioritize user-visible operations over background sync
4. **Error Recovery**: Graceful handling of file system and database errors

**Example Batch Processing**:
```csharp
// Batch metadata sync for performance
Database.BeginTransaction();
try
{
    foreach (Photo photo in photos)
    {
        SyncMetadataJob.Create(Core.Database.Jobs, photo);
    }
    Database.CommitTransaction();
}
catch (Exception)
{
    Database.RollbackTransaction();
    throw;
}
```

## Usage Examples

### Merging Two F-Spot Databases

**Cross-Installation Database Consolidation**:
```csharp
// User selects source database file
var mergeDb = new MergeDb();
mergeDb.Run(this, EventArgs.Empty);

// System performs:
// 1. Tag hierarchy reconciliation
// 2. Roll deduplication
// 3. Photo import with version preservation
// 4. Metadata consistency verification
```

### Synchronizing Catalog with File System

**Metadata Consistency Maintenance**:
```csharp
// Sync entire catalog
var photos = Core.Database.Photos.Query();
foreach (Photo photo in photos)
{
    SyncMetadataJob.Create(Core.Database.Jobs, photo);
}

// Or sync selected photos only
var selectedPhotos = App.Instance.Organizer.SelectedPhotos();
foreach (Photo photo in selectedPhotos)
{
    SyncMetadataJob.Create(Core.Database.Jobs, photo);
}
```

## Limitations and Modernization Opportunities

### Current Limitations

1. **No Cloud Sync**: No synchronization with cloud storage services
2. **Limited Conflict Resolution**: Basic file collision handling
3. **No Incremental Sync**: Full metadata sync for all operations
4. **Single-Threaded Processing**: Jobs processed sequentially

### Modernization Recommendations

**Cloud Synchronization Support**:
```csharp
public interface ICloudSyncProvider
{
    Task<bool> UploadPhotoAsync(Photo photo, IProgress<double> progress);
    Task<bool> DownloadPhotoAsync(string photoId, SafeUri destination);
    Task<Photo[]> GetRemoteChangesAsync(DateTime since);
    Task<bool> SyncMetadataAsync(Photo photo);
}

public class GooglePhotosSyncProvider : ICloudSyncProvider
{
    // Implementation for Google Photos integration
}

public class OneDriveSyncProvider : ICloudSyncProvider
{
    // Implementation for Microsoft OneDrive integration
}
```

**Async Processing Framework**:
```csharp
public async Task<bool> SyncCatalogAsync(IEnumerable\<Photo\> photos, 
                                        IProgress<SyncProgress> progress,
                                        CancellationToken cancellationToken)
{
    var tasks = photos.Select(async photo =>
    {
        var result = await SyncPhotoMetadataAsync(photo, cancellationToken);
        progress?.Report(new SyncProgress { Photo = photo, Success = result });
        return result;
    });
    
    var results = await Task.WhenAll(tasks);
    return results.All(r => r);
}
```

**Conflict Resolution Framework**:
```csharp
public enum ConflictResolution
{
    SkipFile,
    OverwriteExisting,
    CreateVersion,
    MergeMetadata,
    PromptUser
}

public interface IConflictResolver
{
    ConflictResolution ResolveConflict(Photo existingPhoto, Photo incomingPhoto);
}
```

## Summary

F-Spot's backup and synchronization system provides comprehensive database merging, metadata synchronization, and background job processing capabilities that ensure data consistency and enable cross-installation consolidation. The system handles complex scenarios like tag hierarchy reconciliation, file relocation, and version preservation while maintaining data integrity through robust error handling and transaction management. Modern enhancements could include cloud synchronization, async processing, and improved conflict resolution to meet contemporary backup and sync expectations.
# Async Modernization Strategy

## Overview

F-Spot was built during the era of synchronous desktop applications and lacks modern async/await patterns. This creates numerous issues including UI blocking, poor responsiveness, and inability to cancel long-running operations. This document outlines a comprehensive strategy for modernizing F-Spot's threading and async patterns.

## Current State Analysis

### Legacy Threading Patterns

#### 1. Manual Thread Management
**File**: `src/Core/FSpot/Imaging/ImageLoaderThread.cs`
```csharp
public class ImageLoaderThread
{
    Thread worker_thread;
    
    public ImageLoaderThread()
    {
        worker_thread = new Thread(new ThreadStart(WorkerThread));
        worker_thread.Start(); // Manual thread creation
    }
    
    void WorkerThread()
    {
        while (true) {
            // Manual thread lifecycle management
            RequestItem request = GetNextRequest();
            if (request == null) {
                Thread.Sleep(200); // Busy waiting
                continue;
            }
            ProcessRequest(request);
        }
    }
}
```

**Problems**:
- Manual thread lifecycle management
- Busy waiting consuming CPU cycles
- No built-in cancellation support
- Thread-unsafe operations
- Difficult error handling

#### 2. Callback Hell Pattern
**File**: `src/Clients/FSpot.Gtk/FSpot.Loaders/GdkImageLoader.cs`
```csharp
void LoadFromStream()
{
    image_stream.BeginRead(buffer, 0, count, delegate (IAsyncResult r) {
        ThreadPool.QueueUserWorkItem(delegate {
            try {
                int bytes_read = image_stream.EndRead(r);
                if (bytes_read > 0) {
                    loader.Write(buffer, (ulong)bytes_read);
                    LoadFromStream(); // Recursive callback
                } else {
                    loader.Close();
                }
            } catch (Exception e) {
                // Error handling scattered across callbacks
                Console.WriteLine("Error in image loading: " + e.Message);
            }
        });
    }, null);
}
```

**Issues**:
- Nested callbacks create complex control flow
- Error handling fragmented across multiple callbacks
- Memory overhead from closure allocations
- Difficult to reason about execution order

#### 3. UI Thread Blocking
**File**: `src/Clients/FSpot.Gtk/FSpot.Widgets/PhotoImageView.cs`
```csharp
public Gdk.Pixbuf CompletePixbuf()
{
    //FIXME: this should be an async call
    if (loader != null)
        while (loader.Loading)
            Gtk.Application.RunIteration(); // BLOCKS UI THREAD
    return Pixbuf;
}
```

**Impact**:
- Application becomes unresponsive during image loading
- Poor user experience
- Cannot handle user input during operations

### Missing Modern Patterns

#### 1. No Async/Await Usage
The codebase contains zero usage of async/await patterns introduced in .NET 4.5/C# 5.0.

#### 2. No Cancellation Token Support
Long-running operations cannot be cancelled:
- Import operations run to completion
- Export operations cannot be interrupted
- Database queries cannot be cancelled

#### 3. No Progress Reporting
Operations provide no structured progress feedback:
- Import progress only via event callbacks
- No IProgress<T> implementation
- Manual progress tracking scattered throughout code

## Modernization Strategy

### Phase 1: Foundation Infrastructure

#### 1. Create Async Database Layer

**New File**: `src/Core/FSpot/Database/IAsyncDb.cs`
```csharp
public interface IAsyncDb : IDisposable
{
    Task<List<Photo>> GetPhotosAsync(PhotoQuery query, CancellationToken cancellationToken = default);
    Task<Photo> GetPhotoAsync(uint id, CancellationToken cancellationToken = default);
    Task SavePhotoAsync(Photo photo, CancellationToken cancellationToken = default);
    Task<List<Tag>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<Tag> GetTagAsync(uint id, CancellationToken cancellationToken = default);
}

public class AsyncDb : IAsyncDb
{
    private readonly SemaphoreSlim dbSemaphore = new(1, 1);
    private readonly IDb syncDb;
    
    public async Task<List<Photo>> GetPhotosAsync(PhotoQuery query, CancellationToken cancellationToken = default)
    {
        await dbSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => syncDb.Photos.Query(query), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            dbSemaphore.Release();
        }
    }
}
```

#### 2. Modernize Image Loading

**Updated File**: `src/Core/FSpot/Imaging/AsyncImageLoader.cs`
```csharp
public class AsyncImageLoader : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    
    public async Task<Gdk.Pixbuf> LoadImageAsync(SafeUri uri, int width = -1, int height = -1, 
        IProgress<double> progress = null, CancellationToken cancellationToken = default)
    {
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, cancellationTokenSource.Token);
            
        var token = combinedCts.Token;
        
        // Load image data asynchronously
        var imageData = await LoadImageDataAsync(uri, progress, token).ConfigureAwait(false);
        
        // Create pixbuf on background thread
        return await Task.Run(() => CreatePixbufFromData(imageData, width, height), token).ConfigureAwait(false);
    }
    
    private async Task<byte[]> LoadImageDataAsync(SafeUri uri, IProgress<double> progress, CancellationToken cancellationToken)
    {
        using var fileStream = await Task.Run(() => File.OpenRead(uri.LocalPath), cancellationToken).ConfigureAwait(false);
        using var memoryStream = new MemoryStream();
        
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        long totalLength = fileStream.Length;
        
        int bytesRead;
        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            
            progress?.Report((double)totalBytesRead / totalLength);
        }
        
        return memoryStream.ToArray();
    }
}
```

### Phase 2: Core Operations Modernization

#### 1. Async Import System

**Updated File**: `src/Core/FSpot/Import/AsyncImportController.cs`
```csharp
public class AsyncImportController
{
    public async Task<ImportResult> ImportPhotosAsync(
        IEnumerable<ImportablePhoto> photos,
        ImportOptions options,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        var importResult = new ImportResult();
        var photosList = photos.ToList();
        var totalPhotos = photosList.Count;
        var processedPhotos = 0;
        
        // Process photos in parallel with degree of parallelism
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = photosList.Select(async photo =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await ImportSinglePhotoAsync(photo, options, cancellationToken).ConfigureAwait(false);
                
                var completed = Interlocked.Increment(ref processedPhotos);
                progress?.Report(new ImportProgress 
                { 
                    Completed = completed, 
                    Total = totalPhotos, 
                    CurrentPhoto = photo.FileName 
                });
                
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        
        // Aggregate results
        importResult.ImportedPhotos.AddRange(results.Where(r => r.Success).Select(r => r.Photo));
        importResult.Errors.AddRange(results.Where(r => !r.Success).Select(r => r.Error));
        
        return importResult;
    }
    
    private async Task<SingleImportResult> ImportSinglePhotoAsync(
        ImportablePhoto photo, 
        ImportOptions options, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Check for duplicates asynchronously
            var isDuplicate = await CheckForDuplicateAsync(photo, options.DuplicateDetection, cancellationToken).ConfigureAwait(false);
            if (isDuplicate && !options.ImportDuplicates)
            {
                return new SingleImportResult { Success = false, Error = "Duplicate photo" };
            }
            
            // Copy file if needed
            if (options.CopyFiles)
            {
                await CopyPhotoFileAsync(photo, options.DestinationPath, cancellationToken).ConfigureAwait(false);
            }
            
            // Add to database
            var dbPhoto = await AddPhotoToDatabaseAsync(photo, cancellationToken).ConfigureAwait(false);
            
            return new SingleImportResult { Success = true, Photo = dbPhoto };
        }
        catch (Exception ex)
        {
            return new SingleImportResult { Success = false, Error = ex.Message };
        }
    }
}
```

#### 2. Async Export System

**Updated File**: `src/Extensions/Exporters/AsyncExportBase.cs`
```csharp
public abstract class AsyncExportBase
{
    protected async Task<ExportResult> ExportPhotosAsync(
        IEnumerable<Photo> photos,
        ExportOptions options,
        IProgress<ExportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        var photosList = photos.ToList();
        var totalPhotos = photosList.Count;
        var processedPhotos = 0;
        var result = new ExportResult();
        
        // Process exports with controlled parallelism
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4) // Limit for I/O bound operations
        };
        
        try
        {
            await Task.Run(() => 
            {
                Parallel.ForEach(photosList, parallelOptions, async photo =>
                {
                    try
                    {
                        await ExportSinglePhotoAsync(photo, options, cancellationToken).ConfigureAwait(false);
                        
                        var completed = Interlocked.Increment(ref processedPhotos);
                        progress?.Report(new ExportProgress
                        {
                            Completed = completed,
                            Total = totalPhotos,
                            CurrentPhoto = photo.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        lock (result.Errors)
                        {
                            result.Errors.Add($"Failed to export {photo.Name}: {ex.Message}");
                        }
                    }
                });
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result.WasCancelled = true;
        }
        
        return result;
    }
    
    protected abstract Task ExportSinglePhotoAsync(Photo photo, ExportOptions options, CancellationToken cancellationToken);
}
```

### Phase 3: UI Integration

#### 1. Async UI Operations

**Updated File**: `src/Clients/FSpot.Gtk/FSpot/AsyncMainWindow.cs`
```csharp
public partial class AsyncMainWindow : Window
{
    private CancellationTokenSource currentOperationCts;
    
    public async Task LoadPhotoAsync(Photo photo)
    {
        // Cancel any existing load operation
        currentOperationCts?.Cancel();
        currentOperationCts = new CancellationTokenSource();
        
        try
        {
            // Show loading indicator
            ShowLoadingIndicator(true);
            
            // Load image asynchronously
            var progress = new Progress<double>(p => UpdateLoadingProgress(p));
            var pixbuf = await imageLoader.LoadImageAsync(photo.DefaultVersion.Uri, -1, -1, progress, currentOperationCts.Token);
            
            // Update UI on main thread
            await Gtk.Application.InvokeOnMainThread(() =>
            {
                imageView.Pixbuf = pixbuf;
                ShowLoadingIndicator(false);
            });
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled - this is normal
        }
        catch (Exception ex)
        {
            // Handle errors
            await Gtk.Application.InvokeOnMainThread(() =>
            {
                ShowErrorMessage($"Failed to load image: {ex.Message}");
                ShowLoadingIndicator(false);
            });
        }
    }
    
    private async void OnImportPhotosClicked(object sender, EventArgs e)
    {
        var dialog = new ImportDialog();
        if (dialog.Run() != (int)ResponseType.Ok)
            return;
            
        var importOptions = dialog.GetImportOptions();
        var photos = dialog.SelectedPhotos;
        
        // Show progress dialog
        var progressDialog = new ProgressDialog("Importing Photos");
        progressDialog.Show();
        
        try
        {
            var progress = new Progress<ImportProgress>(p => 
            {
                Gtk.Application.InvokeOnMainThread(() =>
                {
                    progressDialog.UpdateProgress(p.Completed, p.Total, p.CurrentPhoto);
                });
            });
            
            var cts = new CancellationTokenSource();
            progressDialog.CancellationTokenSource = cts; // Allow user to cancel
            
            var result = await importController.ImportPhotosAsync(photos, importOptions, progress, cts.Token);
            
            // Update UI with results
            Gtk.Application.InvokeOnMainThread(() =>
            {
                progressDialog.Hide();
                ShowImportResults(result);
                RefreshPhotoView();
            });
        }
        catch (OperationCanceledException)
        {
            Gtk.Application.InvokeOnMainThread(() =>
            {
                progressDialog.Hide();
                ShowMessage("Import cancelled by user");
            });
        }
    }
}
```

### Phase 4: Advanced Patterns

#### 1. Reactive Extensions Integration

**New File**: `src/Core/FSpot/Reactive/ObservableExtensions.cs`
```csharp
public static class ObservableExtensions
{
    public static IObservable<T> FromAsync<T>(Func<CancellationToken, Task<T>> asyncFunction)
    {
        return Observable.Create<T>(async (observer, cancellationToken) =>
        {
            try
            {
                var result = await asyncFunction(cancellationToken).ConfigureAwait(false);
                observer.OnNext(result);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }
    
    public static IObservable<Photo> WatchPhotoChanges(this IAsyncDb database)
    {
        return Observable.Create<Photo>(observer =>
        {
            var timer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            return timer.SelectMany(_ => database.GetModifiedPhotosAsync().ToObservable())
                       .Subscribe(observer);
        });
    }
}
```

#### 2. Background Service Infrastructure

**New File**: `src/Core/FSpot/Services/BackgroundServiceHost.cs`
```csharp
public class BackgroundServiceHost : IDisposable
{
    private readonly List<IBackgroundService> services = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    
    public void AddService(IBackgroundService service)
    {
        services.Add(service);
    }
    
    public async Task StartAsync()
    {
        var tasks = services.Select(service => 
            service.StartAsync(cancellationTokenSource.Token)).ToArray();
            
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    
    public async Task StopAsync()
    {
        cancellationTokenSource.Cancel();
        
        var tasks = services.Select(service => service.StopAsync()).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}

public interface IBackgroundService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
}

public class ThumbnailGenerationService : IBackgroundService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var photosNeedingThumbnails = await GetPhotosNeedingThumbnailsAsync(cancellationToken).ConfigureAwait(false);
            
            await Task.WhenAll(photosNeedingThumbnails.Select(photo => 
                GenerateThumbnailAsync(photo, cancellationToken))).ConfigureAwait(false);
                
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
        }
    }
}
```

## Migration Strategy

### 1. Incremental Migration Approach

**Phase 1: Database Layer (2-3 weeks)**
- Create async wrapper for existing database operations
- Add cancellation token support to database queries
- Implement connection pooling for concurrent operations

**Phase 2: Core Operations (4-6 weeks)**
- Modernize import system with async/await
- Update export operations to use async patterns
- Add progress reporting infrastructure

**Phase 3: UI Integration (3-4 weeks)**
- Update MainWindow to use async operations
- Add proper cancellation support to UI operations
- Implement responsive progress dialogs

**Phase 4: Advanced Features (2-3 weeks)**
- Add background services for maintenance tasks
- Implement reactive patterns for real-time updates
- Optimize performance with advanced async patterns

### 2. Compatibility Strategy

**Maintain Backward Compatibility**:
```csharp
public class HybridPhotoStore : IPhotoStore
{
    private readonly IAsyncPhotoStore asyncStore;
    
    // Legacy synchronous methods
    public Photo Get(uint id) => GetAsync(id).GetAwaiter().GetResult();
    
    // New async methods
    public Task<Photo> GetAsync(uint id, CancellationToken cancellationToken = default)
        => asyncStore.GetAsync(id, cancellationToken);
}
```

### 3. Testing Strategy

**Async Unit Tests**:
```csharp
[Test]
public async Task ImportPhotosAsync_WithCancellation_ShouldCancelGracefully()
{
    var importController = new AsyncImportController();
    var photos = GenerateTestPhotos(1000);
    
    using var cts = new CancellationTokenSource();
    
    // Cancel after 100ms
    cts.CancelAfter(100);
    
    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
        await importController.ImportPhotosAsync(photos, new ImportOptions(), cancellationToken: cts.Token);
    });
}

[Test]
public async Task LoadImageAsync_WithProgress_ShouldReportProgress()
{
    var loader = new AsyncImageLoader();
    var progressReports = new List<double>();
    var progress = new Progress<double>(p => progressReports.Add(p));
    
    await loader.LoadImageAsync(testImageUri, progress: progress);
    
    Assert.That(progressReports.Count, Is.GreaterThan(0));
    Assert.That(progressReports.Last(), Is.EqualTo(1.0).Within(0.01));
}
```

## Performance Benefits

### Expected Improvements

1. **UI Responsiveness**: Elimination of UI thread blocking
2. **Throughput**: Parallel processing of I/O operations
3. **Resource Utilization**: Better CPU and I/O utilization
4. **User Experience**: Cancellable operations with progress feedback
5. **Memory Efficiency**: Streaming operations reduce memory pressure

### Benchmarking

**Before/After Comparison**:
```csharp
[Benchmark]
public void ImportPhotos_Sync() => syncImporter.ImportPhotos(testPhotos);

[Benchmark]
public Task ImportPhotos_Async() => asyncImporter.ImportPhotosAsync(testPhotos);

[Benchmark]
public void LoadImage_Sync() => syncLoader.LoadImage(testImageUri);

[Benchmark]
public Task LoadImage_Async() => asyncLoader.LoadImageAsync(testImageUri);
```

## File Locations for Async Work

### New Files to Create
- `src/Core/FSpot/Database/IAsyncDb.cs` - Async database interface
- `src/Core/FSpot/Imaging/AsyncImageLoader.cs` - Modern image loading
- `src/Core/FSpot/Import/AsyncImportController.cs` - Async import system
- `src/Core/FSpot/Services/BackgroundServiceHost.cs` - Background services
- `src/Core/FSpot/Reactive/ObservableExtensions.cs` - Reactive patterns

### Files to Modernize
- `src/Core/FSpot/Imaging/ImageLoaderThread.cs` - Replace with async version
- `src/Clients/FSpot.Gtk/FSpot.Loaders/GdkImageLoader.cs` - Async patterns
- `src/Clients/FSpot.Gtk/FSpot/MainWindow.cs` - UI async integration
- `src/Core/FSpot/Import/ImportController.cs` - Async import operations
- Export plugins in `src/Extensions/Exporters/` - Async export operations

## Conclusion

Modernizing F-Spot's async patterns is critical for creating a responsive, modern application. The phased approach allows for incremental improvement while maintaining compatibility. The key benefits include:

1. **Improved User Experience** through responsive UI
2. **Better Performance** via parallel processing
3. **Modern Architecture** following current .NET best practices
4. **Cancellation Support** for long-running operations
5. **Progress Reporting** for better user feedback

This modernization will transform F-Spot from a legacy synchronous application into a modern, responsive photo management tool.
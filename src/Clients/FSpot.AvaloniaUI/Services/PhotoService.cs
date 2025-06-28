using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FSpot.Core;
using FSpot.Database;

namespace FSpot.AvaloniaUI;

public class PhotoService : IPhotoService
{
    private readonly ILogger<PhotoService> _logger;
    private PhotoStore? _photoStore;

    public PhotoService(ILogger<PhotoService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<Photo>? PhotoAdded;
    public event EventHandler<Photo>? PhotoUpdated;
    public event EventHandler<uint>? PhotoDeleted;

    private PhotoStore PhotoStore
    {
        get
        {
            if (_photoStore == null)
            {
                // Initialize the photo store - this will create/open the database
                var db = new Db();
                _photoStore = db.Photos;
            }
            return _photoStore;
        }
    }

    public async Task<IEnumerable<Photo>> GetAllPhotosAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var photos = PhotoStore.Query();
                _logger.LogInformation("Retrieved {Count} photos from database", photos.Length);
                return photos.AsEnumerable();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all photos");
            return Enumerable.Empty<Photo>();
        }
    }

    public async Task<IEnumerable<Photo>> GetPhotosByTagAsync(Tag tag)
    {
        try
        {
            return await Task.Run(() =>
            {
                var photos = PhotoStore.Query(new Tag[] { tag });
                _logger.LogInformation("Retrieved {Count} photos for tag {TagName}", photos.Length, tag.Name);
                return photos.AsEnumerable();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get photos by tag {TagName}", tag.Name);
            return Enumerable.Empty<Photo>();
        }
    }

    public async Task<IEnumerable<Photo>> SearchPhotosAsync(string searchTerm)
    {
        try
        {
            return await Task.Run(() =>
            {
                // Simple search implementation - can be enhanced
                var allPhotos = PhotoStore.Query();
                var filteredPhotos = allPhotos.Where(p => 
                    (p.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    Path.GetFileName(p.DefaultVersion.Uri.LocalPath).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                );
                
                _logger.LogInformation("Found {Count} photos matching search term '{SearchTerm}'", 
                    filteredPhotos.Count(), searchTerm);
                
                return filteredPhotos;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search photos with term '{SearchTerm}'", searchTerm);
            return Enumerable.Empty<Photo>();
        }
    }

    public async Task<Photo?> GetPhotoAsync(uint photoId)
    {
        try
        {
            return await Task.Run(() =>
            {
                var photo = PhotoStore.Get(photoId);
                _logger.LogDebug("Retrieved photo {PhotoId}", photoId);
                return photo;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get photo {PhotoId}", photoId);
            return null;
        }
    }

    public async Task ImportPhotosAsync(IEnumerable<string> filePaths)
    {
        try
        {
            await Task.Run(() =>
            {
                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath) && IsImageFile(filePath))
                    {
                        try
                        {
                            var uri = new System.Uri(filePath);
                            var photo = PhotoStore.Create(uri, null);
                            
                            PhotoAdded?.Invoke(this, photo);
                            _logger.LogInformation("Imported photo: {FilePath}", filePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to import photo: {FilePath}", filePath);
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import photos");
            throw;
        }
    }

    public async Task<byte[]?> GetThumbnailAsync(Photo photo, int size = 256)
    {
        try
        {
            return await Task.Run(() =>
            {
                // TODO: Implement thumbnail generation using SkiaSharp
                // For now, return null to indicate no thumbnail available
                return (byte[]?)null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail for photo {PhotoId}", photo.Id);
            return null;
        }
    }

    public async Task<string?> GetFullSizeImagePathAsync(Photo photo)
    {
        try
        {
            return await Task.Run(() =>
            {
                var path = photo.DefaultVersion.Uri.LocalPath;
                return File.Exists(path) ? path : null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get full size image path for photo {PhotoId}", photo.Id);
            return null;
        }
    }

    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".webp" or ".raw" or ".nef" or ".cr2";
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FSpot.Core;
using Avalonia.Media.Imaging;
using SkiaSharp;
using System.Collections.Concurrent;

namespace FSpot.AvaloniaUI;

public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;
    private readonly ConcurrentDictionary<string, Bitmap> _thumbnailCache;

    public ThumbnailService(ILogger<ThumbnailService> logger)
    {
        _logger = logger;
        _thumbnailCache = new ConcurrentDictionary<string, Bitmap>();
    }

    public async Task<Bitmap?> GetThumbnailAsync(Photo photo, int size = 256)
    {
        try
        {
            var cacheKey = $"{photo.Id}_{size}";
            
            // Check cache first
            if (_thumbnailCache.TryGetValue(cacheKey, out var cachedThumbnail))
            {
                return cachedThumbnail;
            }

            var imagePath = photo.DefaultVersion.Uri.LocalPath;
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Image file not found: {ImagePath}", imagePath);
                return null;
            }

            var thumbnail = await CreateThumbnailAsync(imagePath, size);
            
            if (thumbnail != null)
            {
                // Cache the thumbnail
                _thumbnailCache.TryAdd(cacheKey, thumbnail);
            }

            return thumbnail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail for photo {PhotoId}", photo.Id);
            return null;
        }
    }

    public async Task<Bitmap?> CreateThumbnailAsync(string imagePath, int size = 256)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var inputStream = File.OpenRead(imagePath);
                using var skBitmap = SKBitmap.Decode(inputStream);
                
                if (skBitmap == null)
                {
                    _logger.LogWarning("Failed to decode image: {ImagePath}", imagePath);
                    return null;
                }

                // Calculate thumbnail dimensions maintaining aspect ratio
                var scale = Math.Min((float)size / skBitmap.Width, (float)size / skBitmap.Height);
                var newWidth = (int)(skBitmap.Width * scale);
                var newHeight = (int)(skBitmap.Height * scale);

                // Create resized bitmap
                using var resizedBitmap = skBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                
                if (resizedBitmap == null)
                {
                    _logger.LogWarning("Failed to resize image: {ImagePath}", imagePath);
                    return null;
                }

                // Convert to Avalonia Bitmap
                using var image = SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 90);
                using var stream = new MemoryStream(data.ToArray());
                
                return new Bitmap(stream);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create thumbnail for image: {ImagePath}", imagePath);
            return null;
        }
    }

    public async Task ClearCacheAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                foreach (var thumbnail in _thumbnailCache.Values)
                {
                    thumbnail?.Dispose();
                }
                _thumbnailCache.Clear();
                _logger.LogInformation("Thumbnail cache cleared");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear thumbnail cache");
        }
    }

    public async Task<long> GetCacheSizeAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                // Rough estimate - each bitmap size varies
                return _thumbnailCache.Count * 50000L; // Assume ~50KB per thumbnail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache size");
            return 0;
        }
    }
}
using System;
using System.Threading.Tasks;
using FSpot.Core;
using Avalonia.Media.Imaging;

namespace FSpot.AvaloniaUI;

public interface IThumbnailService
{
    Task<Bitmap?> GetThumbnailAsync(Photo photo, int size = 256);
    Task<Bitmap?> CreateThumbnailAsync(string imagePath, int size = 256);
    Task ClearCacheAsync();
    Task<long> GetCacheSizeAsync();
}
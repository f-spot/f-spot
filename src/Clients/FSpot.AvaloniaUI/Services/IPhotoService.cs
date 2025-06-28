using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSpot.Core;

namespace FSpot.AvaloniaUI;

public interface IPhotoService
{
    Task<IEnumerable<Photo>> GetAllPhotosAsync();
    Task<IEnumerable<Photo>> GetPhotosByTagAsync(Tag tag);
    Task<IEnumerable<Photo>> SearchPhotosAsync(string searchTerm);
    Task<Photo?> GetPhotoAsync(uint photoId);
    Task ImportPhotosAsync(IEnumerable<string> filePaths);
    Task<byte[]?> GetThumbnailAsync(Photo photo, int size = 256);
    Task<string?> GetFullSizeImagePathAsync(Photo photo);
    event EventHandler<Photo>? PhotoAdded;
    event EventHandler<Photo>? PhotoUpdated;
    event EventHandler<uint>? PhotoDeleted;
}
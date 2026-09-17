//
// IPhotoThumbnailLoader.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

namespace FSpot.Avalonia.Services;

public interface IPhotoThumbnailLoader
{
	Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken);
}

//
// IPhotoThumbnailLoader.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

namespace FSpot.Avalonia.Services;

public interface IPhotoThumbnailLoader
{
	Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken);

	// The histogram measures the file, so it must not pick up the display profile.
	Task<Bitmap?> LoadForMeasurementAsync (string path, int size, CancellationToken cancellationToken)
		=> LoadAsync (path, size, cancellationToken);
}

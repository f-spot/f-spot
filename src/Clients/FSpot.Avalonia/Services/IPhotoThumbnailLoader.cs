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

	// The grid paints this on the UI thread. It is a copy, and it is null when the
	// path has not been decoded at this size yet.
	Bitmap? TryCopyCached (string path, int size) => null;

	// Drop decoded grid bitmaps. A null path drops every path.
	void Invalidate (string? path = null)
	{
	}
}

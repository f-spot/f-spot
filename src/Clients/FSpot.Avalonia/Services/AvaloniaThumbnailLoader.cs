//
// AvaloniaThumbnailLoader.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Imaging;
using FSpot.Cms;
using FSpot.Imaging;
using FSpot.Thumbnail;

using Hyena;

namespace FSpot.Avalonia.Services;

interface IGridThumbnailCache
{
	ImageBuffer? TryLoad (string path, int pixelSize);

	ImageBuffer? Get (string path, int pixelSize);
}

sealed class XdgThumbnailCache : IGridThumbnailCache
{
	readonly IThumbnailService? service;

	public XdgThumbnailCache (IThumbnailService? service)
	{
		this.service = service;
	}

	public ImageBuffer? TryLoad (string path, int pixelSize)
		=> Read (path, pixelSize, create: false);

	public ImageBuffer? Get (string path, int pixelSize)
		=> Read (path, pixelSize, create: true);

	ImageBuffer? Read (string path, int pixelSize, bool create)
	{
		if (service is null || pixelSize < 1 || pixelSize > 256)
			return null;

		var uri = SafeUri.FromPath (path);
		var size = pixelSize <= 128 ? ThumbnailSize.Normal : ThumbnailSize.Large;
		return create
			? service.GetThumbnail (uri, size)
			: service.TryLoadThumbnail (uri, size);
	}
}

sealed class AvaloniaThumbnailLoader : IPhotoThumbnailLoader
{
	readonly ThumbnailMemory memory = new ();
	readonly Func<Profile?> profile;

	public AvaloniaThumbnailLoader ()
		: this (new XdgThumbnailCache (App.Services?.Thumbnails))
	{
	}

	internal AvaloniaThumbnailLoader (IGridThumbnailCache cache)
		: this (cache, DisplayColor.Selected)
	{
	}

	internal AvaloniaThumbnailLoader (IGridThumbnailCache cache, Func<Profile?> profile)
	{
		// The disk thumbnail is not painted. A stored hit kept the old blue pixels.
		_ = cache;
		this.profile = profile;
	}

	public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		=> LoadCoreAsync (path, size, colorManage: true, cancellationToken);

	public Task<Bitmap?> LoadForMeasurementAsync (string path, int size, CancellationToken cancellationToken)
		=> LoadCoreAsync (path, size, colorManage: false, cancellationToken);

	public Bitmap? TryCopyCached (string path, int size)
		=> memory.Copy (path, size, color: true);

	public void Invalidate (string? path = null)
		=> memory.Invalidate (path);

	async Task<Bitmap?> LoadCoreAsync (string path, int size, bool colorManage, CancellationToken cancellationToken)
	{
		// A cell that scrolls back paints the copy it already decoded, even when the
		// file has since been removed. The check is before the disk so a miss still
		// notices a missing photo.
		if (memory.Copy (path, size, colorManage) is Bitmap cached)
			return cached;

		if (!File.Exists (path))
			return null;

		// The on-disk cache stores 128 and 256. A larger request is the photo itself.
		// Both the cache read and the miss run inside the gate so a grid scroll cannot
		// start them on the UI thread beside a full-size decode.
		return await DecodeGate.RunAsync (() => {
			if (memory.Copy (path, size, colorManage) is Bitmap raced)
				return raced;

			// A measurement reads the stored pixels. The screen asks for the profile
			// and converts a copy, so the shared thumbnail cache stays unmanaged.
			var destination = colorManage ? profile () : null;
			// The on-disk thumbnail was written by the older reader and is blue.
			// A hit never replaced it, so the grid decodes the file at this size.
			var bitmap = AvaloniaPhotoDecoder.DecodeFile (path, size);
			Bitmap shown = DisplayColor.Present (bitmap, destination);

			memory.Remember (path, size, colorManage, shown);
			return shown;
		}, cancellationToken).ConfigureAwait (false);
	}

}

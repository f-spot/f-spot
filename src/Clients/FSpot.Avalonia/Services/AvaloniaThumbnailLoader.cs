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

using TinyIoC;

namespace FSpot.Avalonia.Services;

interface IGridThumbnailCache
{
	ImageBuffer? TryLoad (string path, int pixelSize);

	ImageBuffer? Get (string path, int pixelSize);
}

sealed class XdgThumbnailCache : IGridThumbnailCache
{
	public ImageBuffer? TryLoad (string path, int pixelSize)
		=> Read (path, pixelSize, create: false);

	public ImageBuffer? Get (string path, int pixelSize)
		=> Read (path, pixelSize, create: true);

	static ImageBuffer? Read (string path, int pixelSize, bool create)
	{
		if (pixelSize < 1 || pixelSize > 256)
			return null;
		IThumbnailService service;
		try {
			service = TinyIoCContainer.Current.Resolve<IThumbnailService> ();
		} catch (Exception) {
			return null;
		}

		var uri = SafeUri.FromPath (path);
		var size = pixelSize <= 128 ? ThumbnailSize.Normal : ThumbnailSize.Large;
		return create
			? service.GetThumbnail (uri, size)
			: service.TryLoadThumbnail (uri, size);
	}
}

sealed class AvaloniaThumbnailLoader : IPhotoThumbnailLoader
{
	readonly IGridThumbnailCache cache;
	readonly Func<Profile?> profile;

	public AvaloniaThumbnailLoader ()
		: this (new XdgThumbnailCache ())
	{
	}

	internal AvaloniaThumbnailLoader (IGridThumbnailCache cache)
		: this (cache, DisplayColor.Selected)
	{
	}

	internal AvaloniaThumbnailLoader (IGridThumbnailCache cache, Func<Profile?> profile)
	{
		this.cache = cache;
		this.profile = profile;
	}

	public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		=> LoadCoreAsync (path, size, colorManage: true, cancellationToken);

	public Task<Bitmap?> LoadForMeasurementAsync (string path, int size, CancellationToken cancellationToken)
		=> LoadCoreAsync (path, size, colorManage: false, cancellationToken);

	async Task<Bitmap?> LoadCoreAsync (string path, int size, bool colorManage, CancellationToken cancellationToken)
	{
		if (!File.Exists (path))
			return null;

		// The on-disk cache stores 128 and 256. A larger request is the photo itself.
		// Both the cache read and the miss run inside the gate so a grid scroll cannot
		// start them on the UI thread beside a full-size decode.
		return await DecodeGate.RunAsync (() => {
			// A measurement reads the stored pixels. The screen asks for the profile
			// and converts a copy, so the shared thumbnail cache stays unmanaged.
			var destination = colorManage ? profile () : null;
			if (size >= 1 && size <= 256) {
				if (cache.TryLoad (path, size) is ImageBuffer stored)
					return Show (stored, destination);
				if (cache.Get (path, size) is ImageBuffer created)
					return Show (created, destination);
			}

			var bitmap = AvaloniaPhotoDecoder.DecodeFile (path, size);
			return DisplayColor.Present (bitmap, destination);
		}, cancellationToken).ConfigureAwait (false);
	}

	static Bitmap Show (ImageBuffer buffer, Profile? destination)
	{
		buffer = DisplayColor.Apply (buffer, destination);
		return AvaloniaImagingBackend.ToBgraBitmap (buffer);
	}
}

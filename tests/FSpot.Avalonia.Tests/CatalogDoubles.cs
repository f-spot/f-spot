// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Services;
using FSpot.Core;

using Hyena;

namespace FSpot.Avalonia.Tests;

internal sealed class NullThumbnails : IPhotoThumbnailLoader
{
	public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		=> Task.FromResult<Bitmap?> (null);
}

internal sealed class StubVersion : IPhotoVersion
{
	public StubVersion (string path)
	{
		Uri = new SafeUri ("file://" + path, true);
		BaseUri = Uri;
		Filename = Path.GetFileName (path);
	}

	public string Name => "Original";
	public bool IsProtected => true;
	public SafeUri BaseUri { get; }
	public string Filename { get; }
	public string ImportMD5 => string.Empty;
	public SafeUri Uri { get; set; }
}

internal sealed class StubPhoto : IPhoto
{
	public StubPhoto (string path, DateTime? time = null, uint rating = 0, params Tag?[] tags)
	{
		Name = Path.GetFileName (path);
		DefaultVersion = new StubVersion (path);
		Versions = new[] { DefaultVersion };
		Time = time ?? DateTime.UnixEpoch;
		Rating = rating;
		// FormatTags skips null entries. IPhoto.Tags cannot say an element may be null.
		Tags = tags is null ? Array.Empty<Tag> () : Array.ConvertAll (tags, static tag => tag!);
	}

	public DateTime Time { get; }
	public Tag[] Tags { get; }
	public string Description => string.Empty;
	public string Name { get; }
	public uint Rating { get; }
	public IPhotoVersion DefaultVersion { get; }
	public IEnumerable<IPhotoVersion> Versions { get; }
}

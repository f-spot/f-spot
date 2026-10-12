// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Services;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ThumbnailCacheTests
{
	public TestContext TestContext { get; }

	public ThumbnailCacheTests (TestContext testContext) => TestContext = testContext;

	[UiTestMethod]
	public async Task AGridThumbnailComesFromTheStoredThumbnail ()
	{
		AvaloniaApp.Ensure ();
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".jpg");
		await File.WriteAllBytesAsync (path, [], TestContext.CancellationToken);
		var cache = new CountingCache (new ImageBuffer (2, 2, false, 6, new byte[12]));
		var loader = new AvaloniaThumbnailLoader (cache);

		try {
			Bitmap? bitmap = await loader.LoadAsync (path, 256, TestContext.CancellationToken);
			Assert.IsNotNull (bitmap);
			Assert.AreEqual (2, bitmap.PixelSize.Width);
			Assert.AreEqual (1, cache.Loads);
			Assert.AreEqual (0, cache.Creates);
			bitmap.Dispose ();
		} finally {
			File.Delete (path);
		}
	}

	[UiTestMethod]
	public async Task AnEditorSizedRequestDoesNotReadTheStoredThumbnail ()
	{
		AvaloniaApp.Ensure ();
		string path = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var cache = new CountingCache (new ImageBuffer (2, 2, false, 6, new byte[12]));
		var loader = new AvaloniaThumbnailLoader (cache);

		using var bitmap = await loader.LoadAsync (path, 1600, TestContext.CancellationToken);

		Assert.IsNotNull (bitmap);
		Assert.AreEqual (0, cache.Loads);
		Assert.AreEqual (0, cache.Creates);
		Assert.IsLessThan (1600, bitmap.PixelSize.Width);
	}

	sealed class CountingCache : IGridThumbnailCache
	{
		readonly ImageBuffer stored;

		public CountingCache (ImageBuffer stored) => this.stored = stored;

		public int Loads { get; private set; }

		public int Creates { get; private set; }

		public ImageBuffer? TryLoad (string path, int pixelSize)
		{
			Loads++;
			return stored;
		}

		public ImageBuffer? Get (string path, int pixelSize)
		{
			Creates++;
			return stored;
		}
	}
}
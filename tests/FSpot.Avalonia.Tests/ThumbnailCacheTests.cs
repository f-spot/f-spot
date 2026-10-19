// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading;

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Services;
using FSpot.Cms;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ThumbnailCacheTests
{
	public TestContext TestContext { get; }

	public ThumbnailCacheTests (TestContext testContext) => TestContext = testContext;

	[TestMethod]
	public void LoadForMeasurement_CompletesWhileTheCallingContextDoesNotPump ()
	{
		AvaloniaApp.Ensure ();
		string path = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var parked = new ParkedContext ();
		bool completed = false;
		Exception? failure = null;
		CancellationToken token = TestContext.CancellationToken;
		var thread = new Thread (() => {
			try {
				SynchronizationContext.SetSynchronizationContext (parked);
				var loader = new AvaloniaThumbnailLoader (new EmptyCache (), static () => null);
				Task<Bitmap?> task = loader.LoadForMeasurementAsync (path, 64, CancellationToken.None);
#pragma warning disable VSTHRD002 // This wait is the deadlock under test. The parked context is not pumped.
				completed = task.Wait (10_000, token);
				if (completed)
					task.Result?.Dispose ();
#pragma warning restore VSTHRD002
			} catch (Exception ex) {
				failure = ex;
			}
		});
		thread.Start ();
#pragma warning disable VSTHRD002 // Join the worker that is waiting on the decode.
		Assert.IsTrue (thread.Join (TimeSpan.FromSeconds (15)));
#pragma warning restore VSTHRD002
		Assert.IsNull (failure);
		Assert.IsTrue (completed);
		Assert.AreEqual (0, parked.Ran);
	}

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

	[UiTestMethod]
	public async Task ADisplayedThumbnailConvertsACopyAndAMeasurementDoesNot ()
	{
		if (!LcmsProbe.IsAvailable ())
			Assert.Inconclusive ("liblcms2 is not available on this machine");

		AvaloniaApp.Ensure ();
		string path = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N") + ".jpg");
		await File.WriteAllBytesAsync (path, [], TestContext.CancellationToken);
		var pixels = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120 };
		var stored = new ImageBuffer (2, 2, false, 6, pixels);
		int selects = 0;
		using var destination = Profile.CreateAlternateRgb ();
		var loader = new AvaloniaThumbnailLoader (new CountingCache (stored), () => {
			selects++;
			return destination;
		});

		try {
			using var shown = await loader.LoadAsync (path, 128, TestContext.CancellationToken);
			Assert.IsNotNull (shown);
			Assert.AreEqual (1, selects);
			Assert.AreEqual (10, stored.Pixels[0]);
			Assert.AreEqual (20, stored.Pixels[1]);
			Assert.AreEqual (30, stored.Pixels[2]);

			selects = 0;
			using var measured = await loader.LoadForMeasurementAsync (path, 128, TestContext.CancellationToken);
			Assert.IsNotNull (measured);
			Assert.AreEqual (0, selects);
			Assert.AreEqual (10, stored.Pixels[0]);
			Assert.AreEqual (20, stored.Pixels[1]);
			Assert.AreEqual (30, stored.Pixels[2]);
		} finally {
			File.Delete (path);
		}
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

	sealed class EmptyCache : IGridThumbnailCache
	{
		public ImageBuffer? TryLoad (string path, int pixelSize) => null;

		public ImageBuffer? Get (string path, int pixelSize) => null;
	}

	sealed class ParkedContext : SynchronizationContext
	{
		public int Ran { get; private set; }

		public override void Post (SendOrPostCallback d, object? state)
		{
		}

		public override void Send (SendOrPostCallback d, object? state)
			=> Ran++;
	}
}
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia;
using FSpot.Avalonia.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ThumbnailWindowTests
{
	[UiTestMethod]
	public void Range_ForTenThousandPhotos_IsTheViewportPlusOneOverscanRow ()
	{
		var slice = ThumbnailWindow.Range (
			itemCount: 10_000,
			columns: 5,
			scrollOffset: 10_000,
			viewport: 400,
			itemExtent: 100,
			overscanRows: 1);

		Assert.AreEqual (495, slice.First);
		Assert.AreEqual (30, slice.Count);
	}

	[UiTestMethod]
	public void Range_AtTheTop_DoesNotStartBeforeZero ()
	{
		var slice = ThumbnailWindow.Range (10_000, 5, scrollOffset: 0, viewport: 400, itemExtent: 100, overscanRows: 1);

		Assert.AreEqual (0, slice.First);
		Assert.AreEqual (25, slice.Count);
	}

	[UiTestMethod]
	public void Range_ClampsToTheLastPartialRow ()
	{
		var slice = ThumbnailWindow.Range (10, 4, scrollOffset: 0, viewport: 1000, itemExtent: 100, overscanRows: 1);

		Assert.AreEqual (0, slice.First);
		Assert.AreEqual (10, slice.Count);
	}

	[UiTestMethod]
	public void Range_EmptyViewportOrCatalog_DecodesNothing ()
	{
		Assert.AreEqual (0, ThumbnailWindow.Range (10_000, 5, 0, viewport: 0, 100, 1).Count);
		Assert.AreEqual (0, ThumbnailWindow.Range (0, 5, 0, viewport: 400, 100, 1).Count);
		Assert.AreEqual (0, ThumbnailWindow.Range (10_000, 5, 0, double.PositiveInfinity, 100, 1).Count);
	}

	[UiTestMethod]
	public void Range_TreatsAMissingColumnCountAsOne ()
	{
		var slice = ThumbnailWindow.Range (10, columns: 0, scrollOffset: 0, viewport: 100, itemExtent: 100, overscanRows: 0);

		Assert.AreEqual (0, slice.First);
		Assert.AreEqual (1, slice.Count);
	}

	[UiTestMethod]
	public void EditorPreview_CapsTheDecodeWidth ()
	{
		Assert.AreEqual (1600, EditorPreview.DecodeWidth (1));
		Assert.AreEqual (3200, EditorPreview.DecodeWidth (2));
		Assert.AreEqual (EditorPreview.MaxPixels, EditorPreview.DecodeWidth (3));
		Assert.AreEqual (1600, EditorPreview.DecodeWidth (0));
	}

	[UiTestMethod]
	public void Lease_KeepsAPhotoWhileEitherSurfaceShowsIt ()
	{
		var lease = new ThumbnailLease ();
		var photo = new object ();
		var grid = new object ();
		var filmstrip = new object ();

		lease.Realize (grid, photo);
		lease.Realize (filmstrip, photo);
		Assert.AreEqual (1, lease.HeldCount);
		Assert.IsNull (lease.Release (grid));
		Assert.IsTrue (lease.IsHeld (photo));

		Assert.AreSame (photo, lease.Release (filmstrip));
		Assert.AreEqual (0, lease.HeldCount);
		Assert.IsFalse (lease.IsHeld (photo));
	}

	[UiTestMethod]
	public void Lease_ReusingAContainer_ReleasesThePhotoThatScrolledAway ()
	{
		var lease = new ThumbnailLease ();
		var container = new object ();
		var first = new object ();
		var second = new object ();

		lease.Realize (container, first);
		lease.Realize (container, second);

		Assert.IsFalse (lease.IsHeld (first));
		Assert.IsTrue (lease.IsHeld (second));
		Assert.AreEqual (1, lease.HeldCount);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void DisposePrevious_ReleasesTheBitmapThatWasReplaced ()
	{
		AvaloniaApp.Ensure ();
		var previous = NewBitmap ();
		var current = NewBitmap ();

		ReleasedBitmap.DisposePrevious (previous, current);

		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = previous.PixelSize);
		Assert.AreEqual (1, current.PixelSize.Width);
		current.Dispose ();
	}

	static WriteableBitmap NewBitmap ()
		=> new (new PixelSize (1, 1), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
}

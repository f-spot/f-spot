// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ThumbnailMemoryTests
{
	[UiTestMethod]
	public void Remember_DisposesTheOldestMasterWhenTheCacheIsFull ()
	{
		var memory = new ThumbnailMemory ();
		using Bitmap first = Square ();
		memory.Remember ("first", 32, color: true, first);
		Bitmap? master = memory.Held ("first", 32, color: true);
		Assert.IsNotNull (master);
		Assert.AreNotSame (first, master);

		for (int i = 0; i < 512; i++) {
			using Bitmap later = Square ();
			memory.Remember ("later-" + i, 32, color: true, later);
		}

		try {
			Assert.IsNull (memory.Held ("first", 32, color: true));
			Assert.ThrowsExactly<ObjectDisposedException> (() => _ = master.PixelSize);
			Assert.AreEqual (new PixelSize (1, 1), first.PixelSize);

			using Bitmap? newest = memory.Copy ("later-511", 32, color: true);
			Assert.IsNotNull (newest);
			Assert.AreEqual (new PixelSize (1, 1), newest.PixelSize);
		} finally {
			memory.Invalidate (null);
		}
	}

	[UiTestMethod]
	public void Remember_DisposesThePreviousMasterWhenTheSamePhotoIsReplaced ()
	{
		var memory = new ThumbnailMemory ();
		using Bitmap first = Square ();
		using Bitmap second = Square ();
		memory.Remember ("photo", 32, color: true, first);
		Bitmap? previous = memory.Held ("photo", 32, color: true);
		Assert.IsNotNull (previous);

		memory.Remember ("photo", 32, color: true, second);

		try {
			Bitmap? current = memory.Held ("photo", 32, color: true);
			Assert.IsNotNull (current);
			Assert.AreNotSame (previous, current);
			Assert.ThrowsExactly<ObjectDisposedException> (() => _ = previous.PixelSize);
			Assert.AreEqual (new PixelSize (1, 1), current.PixelSize);
			Assert.AreEqual (new PixelSize (1, 1), first.PixelSize);
			Assert.AreEqual (new PixelSize (1, 1), second.PixelSize);
		} finally {
			memory.Invalidate (null);
		}
	}

	[UiTestMethod]
	public void Invalidate_DisposesThatPhotoAndLeavesTheRest ()
	{
		var memory = new ThumbnailMemory ();
		using Bitmap small = Square ();
		using Bitmap large = Square ();
		using Bitmap other = Square ();
		memory.Remember ("photo", 32, color: true, small);
		memory.Remember ("photo", 64, color: false, large);
		memory.Remember ("other", 32, color: true, other);
		Bitmap? smallMaster = memory.Held ("photo", 32, color: true);
		Bitmap? largeMaster = memory.Held ("photo", 64, color: false);
		Bitmap? otherMaster = memory.Held ("other", 32, color: true);
		Assert.IsNotNull (smallMaster);
		Assert.IsNotNull (largeMaster);
		Assert.IsNotNull (otherMaster);

		memory.Invalidate ("photo");

		try {
			Assert.IsNull (memory.Held ("photo", 32, color: true));
			Assert.IsNull (memory.Held ("photo", 64, color: false));
			Assert.ThrowsExactly<ObjectDisposedException> (() => _ = smallMaster.PixelSize);
			Assert.ThrowsExactly<ObjectDisposedException> (() => _ = largeMaster.PixelSize);
			Assert.AreSame (otherMaster, memory.Held ("other", 32, color: true));
			Assert.AreEqual (new PixelSize (1, 1), otherMaster.PixelSize);
		} finally {
			memory.Invalidate (null);
			Assert.ThrowsExactly<ObjectDisposedException> (() => _ = otherMaster.PixelSize);
		}
	}

	static WriteableBitmap Square ()
		=> new (new PixelSize (1, 1), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
}

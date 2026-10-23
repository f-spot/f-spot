// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Threading;

using FSpot.Imaging;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Imaging;

[TestClass]
public sealed class ImageLoaderThreadTests
{
	[TestMethod]
	public void ThumbnailWorkerDoesNotKeepTheProcessAlive ()
	{
		var loader = new ImageLoaderThread (new BrokenFiles ());
		try {
			loader.Request (new SafeUri ("file:///tmp/fspot-loader-missing.jpg", true), 1);
			var worker = typeof (ImageLoaderThread)
				.GetField ("worker_thread", BindingFlags.Instance | BindingFlags.NonPublic)
				?.GetValue (loader) as Thread;
			Assert.IsNotNull (worker);
			Assert.IsTrue (worker.IsBackground);
		} finally {
			loader.Cleanup ();
		}
	}

	sealed class BrokenFiles : IImageFileFactory
	{
		public IImageFile Create (SafeUri uri)
			=> throw new InvalidOperationException ("missing");

		public bool HasLoader (SafeUri uri) => false;

		public bool IsJpeg (SafeUri uri) => false;

		public bool IsRaw (SafeUri uri) => false;

		public bool IsJpegRawPair (SafeUri file1, SafeUri file2) => false;

		public void RegisterGenericLoader (string extension)
		{
		}
	}
}

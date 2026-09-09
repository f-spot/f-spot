//
// ImageThumbnailerTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Imaging;

using Hyena;

using Mocks;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Thumbnail.Tests
{
	[TestClass]
	public class ImageThumbnailerTests
	{
		static readonly SafeUri fileUri = new SafeUri ("file:///path/to/file.jpg");
		static readonly SafeUri thumbnailUri = new SafeUri ("file:///path/to/.cache/thumbnails/large/file.png");
		static readonly ImageBuffer dummyImage = new ImageBuffer (1, 1, false, 4, new byte[4]);

		CapturingImagingBackend backend;
		IImagingBackend previousBackend;

		[TestInitialize]
		public void SetUp ()
		{
			previousBackend = ImagingServices.Backend;
			backend = new CapturingImagingBackend ();
			ImagingServices.Backend = backend;
		}

		[TestCleanup]
		public void TearDown ()
		{
			ImagingServices.Backend = previousBackend;
		}

		[TestMethod]
		public void TryCreateThumbnail_PassesOneValueForEveryPngOptionKey ()
		{
			CreateThumbnail ();

			Assert.AreSequenceEqual (
				new[] { ThumbnailPng.UriOptionKey, ThumbnailPng.MTimeOptionKey },
				backend.OptionKeys);
			Assert.HasCount (backend.OptionKeys.Length, backend.OptionValues);
		}

		[TestMethod]
		public void TryCreateThumbnail_DoesNotLeakACArrayTerminatorIntoPngOptions ()
		{
			CreateThumbnail ();

			Assert.DoesNotContain ((string)null, backend.OptionKeys);
			Assert.DoesNotContain ((string)null, backend.OptionValues);
		}

		static void CreateThumbnail ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, DateTime.MinValue);

			var imageFileMock = new Mock<IImageFile> ();
			imageFileMock.Setup (file => file.Uri).Returns (fileUri);
			imageFileMock.Setup (file => file.Load ()).Returns (dummyImage);

			var factoryMock = new Mock<IImageFileFactory> ();
			factoryMock.Setup (factory => factory.Create (fileUri)).Returns (imageFileMock.Object);

			var thumbnailer = new ImageThumbnailer (fileUri, factoryMock.Object, fileSystem);

			Assert.IsTrue (thumbnailer.TryCreateThumbnail (thumbnailUri, ThumbnailSize.Large),
				"thumbnailing failed before the PNG options could be captured");
		}

		class CapturingImagingBackend : IImagingBackend
		{
			public string[] OptionKeys { get; private set; }
			public string[] OptionValues { get; private set; }

			public void SavePng (ImageBuffer image, string path, string[] optionKeys, string[] optionValues)
			{
				OptionKeys = optionKeys;
				OptionValues = optionValues;
			}

			public ImageBuffer Decode (Stream stream, TagLib.Image.ImageOrientation orientation) => dummyImage;
			public ImageBuffer ScaleToMaxSize (ImageBuffer source, int maxWidth, int maxHeight, bool upscale = true) => source;
			public ImageBuffer Scale (ImageBuffer source, int width, int height) => source;
			public void SaveDerivedVersion (ImageBuffer buffer, SafeUri source, SafeUri dest, uint jpegQuality) { }
			public ImageBuffer LoadThemeIcon (string name, int size) => dummyImage;
			public byte[] SerializeIcon (ImageBuffer icon) => Array.Empty<byte> ();
			public ImageBuffer DeserializeIcon (byte[] data) => dummyImage;
			public ThumbnailPng LoadPng (Stream stream) => null;
		}
	}
}

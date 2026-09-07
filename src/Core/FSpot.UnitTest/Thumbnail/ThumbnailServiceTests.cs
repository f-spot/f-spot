//
// ThumbnailServiceTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Imaging;

using Hyena;

using Mocks;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Thumbnail.UnitTest
{
	[TestClass]
	public class ThumbnailServiceTests
	{
		static readonly SafeUri fileUri = new SafeUri ("file:///path/to/file");
		const string fileHash = "d005f819e9ddaad2d09a23915aa68afe";
		static DateTime fileMTime = DateTime.MinValue;

		const string largeThumbnailPath = "/path/to/.cache/thumbnails/large";
		const string normalThumbnailPath = "/path/to/.cache/thumbnails/normal";
		static readonly SafeUri largeThumbnailUri = new SafeUri (largeThumbnailPath + "/" + fileHash + ".png");
		static readonly SafeUri normalThumbnailUri = new SafeUri (normalThumbnailPath + "/" + fileHash + ".png");

		static readonly ImageBuffer dummyImage = new ImageBuffer (1, 1, false, 4, new byte[4]);

		#region Test setup

		IXdgDirectoryService xdgDirectoryService;
		IThumbnailerFactory thumbnailerFactory;
		Mock<IThumbnailer> thumbnailerMock = new Mock<IThumbnailer> ();
		FakeImagingBackend backend;
		IImagingBackend previousBackend;

		[TestInitialize]
		public void OneTimeSetUp ()
		{
			var xdgDirectoryServiceMock = new Mock<IXdgDirectoryService> ();
			xdgDirectoryServiceMock.Setup (xdg => xdg.GetThumbnailsDir (ThumbnailSize.Large)).Returns (largeThumbnailPath);
			xdgDirectoryServiceMock.Setup (xdg => xdg.GetThumbnailsDir (ThumbnailSize.Normal)).Returns (normalThumbnailPath);
			xdgDirectoryService = xdgDirectoryServiceMock.Object;

			var thumbnailerFactoryMock = new Mock<IThumbnailerFactory> ();
			thumbnailerFactoryMock.Setup (factory => factory.GetThumbnailerForUri (fileUri)).Returns (thumbnailerMock.Object);
			thumbnailerFactory = thumbnailerFactoryMock.Object;

			previousBackend = ImagingServices.Backend;
			backend = new FakeImagingBackend ();
			ImagingServices.Backend = backend;
		}

		[TestCleanup]
		public void TearDown ()
		{
			ImagingServices.Backend = previousBackend;
		}

		#endregion

		#region Tests

		#region Constructor tests

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void Constructor_CreatesThumbnailFolders_IfTheyDontExist ()
		{
			var fileSystem = new FileSystemMock ();

			// Analysis disable once ObjectCreationAsStatement
			new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			fileSystem.DirectoryMock.Verify (m => m.CreateDirectory (new SafeUri (largeThumbnailPath)), Times.Once ());
			fileSystem.DirectoryMock.Verify (m => m.CreateDirectory (new SafeUri (normalThumbnailPath)), Times.Once ());
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void Constructor_DoesNotCreateThumbnailFolders_IfTheyExist ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetDirectory (new SafeUri (largeThumbnailPath));
			fileSystem.SetDirectory (new SafeUri (normalThumbnailPath));

			// Analysis disable once ObjectCreationAsStatement
			new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			fileSystem.DirectoryMock.Verify (m => m.CreateDirectory (It.IsAny<SafeUri> ()), Times.Never ());
		}

		#endregion

		#region GetThumbnail tests

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void GetThumbnail_ReturnsPixbuf_IfValidPngExists ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			fileSystem.SetFile (largeThumbnailUri, DateTime.MinValue, new byte[] { 1 });
			backend.Png = ValidPng ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.GetThumbnail (fileUri, ThumbnailSize.Large);

			Assert.IsNotNull (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void GetThumbnail_CreatesPngAndReturnsPixbuf_IfPngIsMissing ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			thumbnailerMock.Setup (thumb => thumb.TryCreateThumbnail (largeThumbnailUri, ThumbnailSize.Large))
				.Returns (true)
				.Callback (() => {
					fileSystem.SetFile (largeThumbnailUri, DateTime.MinValue, new byte[] { 1 });
					backend.Png = ValidPng ();
				});
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.GetThumbnail (fileUri, ThumbnailSize.Large);

			Assert.IsNotNull (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void GetThumbnail_ReturnsNull_IfNoThumbnailerFound ()
		{
			var fileSystem = new FileSystemMock ();
			var thumbnailerFactoryMock = new Mock<IThumbnailerFactory> ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactoryMock.Object, fileSystem);

			var result = thumbnailService.GetThumbnail (fileUri, ThumbnailSize.Large);

			Assert.IsNull (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void GetThumbnail_ReturnsNull_IfThumbnailCreationFails ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			thumbnailerMock.Setup (thumb => thumb.TryCreateThumbnail (largeThumbnailUri, ThumbnailSize.Large)).Returns (false);
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.GetThumbnail (fileUri, ThumbnailSize.Large);

			Assert.IsNull (result);
		}

		#endregion

		#region DeleteThumbnail tests

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void DeleteThumbnail_DeletesLargeAndNormalThumbnails_IfTheyExist ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (largeThumbnailUri);
			fileSystem.SetFile (normalThumbnailUri);
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			thumbnailService.DeleteThumbnails (fileUri);

			fileSystem.FileMock.Verify (m => m.Delete (largeThumbnailUri), Times.Once ());
			fileSystem.FileMock.Verify (m => m.Delete (normalThumbnailUri), Times.Once ());
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void DeleteThumbnail_DoesNotDeleteThumbnails_IfNotExist ()
		{
			var fileSystem = new FileSystemMock ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			thumbnailService.DeleteThumbnails (fileUri);

			fileSystem.FileMock.Verify (m => m.Delete (largeThumbnailUri), Times.Never ());
			fileSystem.FileMock.Verify (m => m.Delete (normalThumbnailUri), Times.Never ());
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void DeleteThumbnail_IgnoresExceptionsOnDeletingThumbnails ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (largeThumbnailUri);
			fileSystem.SetFile (normalThumbnailUri);
			fileSystem.FileMock.Setup (m => m.Delete (largeThumbnailUri)).Throws<Exception> ();
			fileSystem.FileMock.Setup (m => m.Delete (normalThumbnailUri)).Throws<Exception> ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			thumbnailService.DeleteThumbnails (fileUri);

			fileSystem.FileMock.Verify (m => m.Delete (largeThumbnailUri), Times.Once ());
			fileSystem.FileMock.Verify (m => m.Delete (normalThumbnailUri), Times.Once ());
			// test ends without the exceptions thrown by File.Delete beeing unhandled
		}

		#endregion

		#region LoadThumbnail tests

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void LoadThumbnail_ReturnsNull_IfThumbnailDoesNotExist ()
		{
			var fileSystem = new FileSystemMock ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.LoadThumbnail (largeThumbnailUri);

			Assert.IsNull (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void LoadThumbnail_ReturnsPixbuf_IfThumbnailExists ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (largeThumbnailUri, DateTime.MinValue, new byte[] { 1 });
			backend.Png = ValidPng ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.LoadThumbnail (largeThumbnailUri);

			Assert.IsNotNull (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void LoadThumbnail_DeletesThumbnail_IfLoadFails ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (largeThumbnailUri, DateTime.MinValue, new byte[] { 1 });
			fileSystem.FileMock.Setup (m => m.Read (largeThumbnailUri)).Throws<Exception> ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.LoadThumbnail (largeThumbnailUri);

			Assert.IsNull (result);
			fileSystem.FileMock.Verify (file => file.Delete (largeThumbnailUri), Times.Once ());
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void LoadThumbnail_IgnoresExceptionsOnDeletingThumbnails ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (largeThumbnailUri, DateTime.MinValue, new byte[] { 1 });
			fileSystem.FileMock.Setup (m => m.Read (largeThumbnailUri)).Throws<Exception> ();
			fileSystem.FileMock.Setup (m => m.Delete (largeThumbnailUri)).Throws<Exception> ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.LoadThumbnail (largeThumbnailUri);

			Assert.IsNull (result);
			fileSystem.FileMock.Verify (file => file.Delete (largeThumbnailUri), Times.Once ());
		}

		#endregion

		#region GetThumbnailPath tests

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void GetThumbnailPath_ReturnsPathForLargeThumbnail ()
		{
			var fileSystem = new FileSystemMock ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.GetThumbnailPath (fileUri, ThumbnailSize.Large);

			Assert.AreEqual (largeThumbnailUri, result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void GetThumbnailPath_ReturnsPathForNormalThumbnail ()
		{
			var fileSystem = new FileSystemMock ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.GetThumbnailPath (fileUri, ThumbnailSize.Normal);

			Assert.AreEqual (normalThumbnailUri, result);
		}

		#endregion

		#region IsValid tests

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void IsValid_ReturnsFalse_IfPixbufIsNull ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.IsValid (fileUri, null);

			Assert.IsFalse (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void IsValid_ReturnsFalse_IfFileUriIsDifferent ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			var png = new ThumbnailPng { Image = dummyImage, UriOption = "file:///some-uri", MTimeOption = fileMTime.ToString () };
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.IsValid (fileUri, png);

			Assert.IsFalse (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void IsValid_ReturnsFalse_IfMTimeIsDifferent ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			var png = new ThumbnailPng { Image = dummyImage, UriOption = fileUri.ToString (), MTimeOption = fileMTime.AddSeconds (1.0).ToString () };
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.IsValid (fileUri, png);

			Assert.IsFalse (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void IsValid_ReturnsFalse_IfFileDoesNotExist ()
		{
			var fileSystem = new FileSystemMock ();
			var png = new ThumbnailPng { Image = dummyImage, UriOption = fileUri.ToString (), MTimeOption = fileMTime.AddSeconds (1.0).ToString () };
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.IsValid (fileUri, png);

			Assert.IsFalse (result);
		}

		[TestMethod]
		// Analysis disable once InconsistentNaming
		public void IsValid_ReturnsTrue_IfPixbufIsValid ()
		{
			var fileSystem = new FileSystemMock ();
			fileSystem.SetFile (fileUri, fileMTime);
			var png = ValidPng ();
			var thumbnailService = new ThumbnailService (xdgDirectoryService, thumbnailerFactory, fileSystem);

			var result = thumbnailService.IsValid (fileUri, png);

			Assert.IsTrue (result);
		}

		#endregion

		#endregion

		ThumbnailPng ValidPng ()
		{
			return new ThumbnailPng {
				Image = dummyImage,
				UriOption = fileUri.ToString (),
				MTimeOption = fileMTime.ToString ()
			};
		}

		class FakeImagingBackend : IImagingBackend
		{
			public ThumbnailPng Png { get; set; }

			public ImageBuffer Decode (Stream stream, TagLib.Image.ImageOrientation orientation) => dummyImage;
			public ImageBuffer ScaleToMaxSize (ImageBuffer source, int maxWidth, int maxHeight, bool upscale = true) => source;
			public ImageBuffer Scale (ImageBuffer source, int width, int height) => source;
			public void SaveDerivedVersion (ImageBuffer buffer, SafeUri source, SafeUri dest, uint jpegQuality) { }
			public ImageBuffer LoadThemeIcon (string name, int size) => dummyImage;
			public byte[] SerializeIcon (ImageBuffer icon) => Array.Empty<byte> ();
			public ImageBuffer DeserializeIcon (byte[] data) => dummyImage;
			public ThumbnailPng LoadPng (Stream stream) => Png;
			public void SavePng (ImageBuffer image, string path, string[] optionKeys, string[] optionValues) { }
		}
	}
}

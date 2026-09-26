// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Settings;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public class PhotoEmptyDirectoryTests
	{
		string root;
		SafeUri previousLibrary;

		[TestInitialize]
		public void CreateRoot ()
		{
			root = Path.Combine (Path.GetTempPath (), "fspot-empty-" + Guid.NewGuid ().ToString ("n"));
			Directory.CreateDirectory (root);
			previousLibrary = FSpotConfiguration.PhotoUri;
		}

		[TestCleanup]
		public void RemoveRoot ()
		{
			FSpotConfiguration.PhotoUri = previousLibrary;
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}

		[TestMethod]
		public void DeletingAVersionDoesNotRemoveTheLibraryRootOrASiblingFolder ()
		{
			string library = Path.Combine (root, "Photos");
			string sibling = Path.Combine (root, "Photos-old");
			Directory.CreateDirectory (library);
			Directory.CreateDirectory (sibling);
			FSpotConfiguration.PhotoUri = new SafeUri (new DirectoryInfo (library).FullName);

			var photo = new Photo (null, null, 1, 0);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (library), "keep.jpg", string.Empty, "Original", true);
			photo.AddVersionUnsafely (2, new SafeUri (sibling), "pic.jpg", string.Empty, "Edit", false);
			string file = photo.VersionUri (2).AbsolutePath;
			File.WriteAllBytes (file, new byte[] { 1 });

			photo.DeleteVersion (2);

			Assert.IsFalse (File.Exists (file));
			Assert.IsTrue (Directory.Exists (sibling), "a folder whose name starts with the library path is not the library");
			Assert.IsTrue (Directory.Exists (library));
		}

		[TestMethod]
		public void DeletingTheOnlyFileInAnAlbumRemovesThatAlbumAndKeepsTheLibrary ()
		{
			string library = Path.Combine (root, "Photos");
			string album = Path.Combine (library, "album");
			Directory.CreateDirectory (album);
			FSpotConfiguration.PhotoUri = new SafeUri (new DirectoryInfo (library).FullName);

			var photo = new Photo (null, null, 1, 0);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (library), "keep.jpg", string.Empty, "Original", true);
			File.WriteAllBytes (Path.Combine (library, "keep.jpg"), new byte[] { 1 });
			photo.AddVersionUnsafely (2, new SafeUri (album), "pic.jpg", string.Empty, "Edit", false);
			File.WriteAllBytes (photo.VersionUri (2).AbsolutePath, new byte[] { 1 });

			photo.DeleteVersion (2);

			Assert.IsFalse (Directory.Exists (album));
			Assert.IsTrue (Directory.Exists (library));
			Assert.IsTrue (File.Exists (Path.Combine (library, "keep.jpg")));
		}

		[TestMethod]
		public void DeletingAVersionWhoseFolderIsAlreadyGoneDoesNotThrow ()
		{
			string library = Path.Combine (root, "Photos");
			string missing = Path.Combine (library, "gone");
			Directory.CreateDirectory (library);
			FSpotConfiguration.PhotoUri = new SafeUri (new DirectoryInfo (library).FullName);

			var photo = new Photo (null, null, 1, 0);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (library), "keep.jpg", string.Empty, "Original", true);
			photo.AddVersionUnsafely (2, new SafeUri (missing), "pic.jpg", string.Empty, "Edit", false);

			photo.DeleteVersion (2);

			Assert.IsTrue (Directory.Exists (library));
		}

		[TestMethod]
		public void DeletingAVersionWhoseFolderCannotBeListedDoesNotThrowOrRemoveIt ()
		{
			string library = Path.Combine (root, "Photos");
			string album = Path.Combine (library, "locked");
			Directory.CreateDirectory (album);
			FSpotConfiguration.PhotoUri = new SafeUri (new DirectoryInfo (library).FullName);

			var photo = new Photo (null, null, 1, 0);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (library), "keep.jpg", string.Empty, "Original", true);
			File.WriteAllBytes (Path.Combine (library, "keep.jpg"), new byte[] { 1 });
			// The version file is absent, so deletion reaches the empty-folder check.
			photo.AddVersionUnsafely (2, new SafeUri (album), "missing.jpg", string.Empty, "Edit", false);

			bool windows = OperatingSystem.IsWindows ();
			var locked = new DirectoryInfo (album);
			UnixFileMode previousMode = default;
			if (windows) {
				Directory.Delete (album);
				File.WriteAllBytes (album, new byte[] { 1 });
			} else {
				previousMode = locked.UnixFileMode;
				locked.UnixFileMode = UnixFileMode.None;
			}

			try {
				photo.DeleteVersion (2);

				Assert.IsNull (photo.VersionUri (2));
				Assert.IsNotNull (photo.VersionUri (Photo.OriginalVersionId));
				Assert.IsTrue (Directory.Exists (library));
				if (windows)
					Assert.IsTrue (File.Exists (album));
				else
					Assert.IsTrue (Directory.Exists (album));
			} finally {
				if (!windows)
					locked.UnixFileMode = previousMode;
			}
		}

		[TestMethod]
		public void ASiblingPathIsNotALibraryChild ()
		{
			string library = Path.Combine (root, "Photos");
			string sibling = Path.Combine (root, "Photos-old");
			Assert.IsFalse (Photo.IsStrictLibraryChild (sibling, library));
			Assert.IsFalse (Photo.IsStrictLibraryChild (library, library));
			Assert.IsTrue (Photo.IsStrictLibraryChild (Path.Combine (library, "album"), library));
		}
	}
}

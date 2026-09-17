// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Settings;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public class PhotoDriveDeleteTests
	{
		string root;
		SafeUri previousLibrary;

		[TestInitialize]
		public void CreateRoot ()
		{
			root = Path.Combine (Path.GetTempPath (), "fspot-drive-" + Guid.NewGuid ().ToString ("n"));
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
		public void TryDeleteAllVersions_KeepsTheOriginalWhenTheDerivedFileIsLocked ()
		{
			string library = Path.Combine (root, "Photos");
			string album = Path.Combine (library, "derived");
			Directory.CreateDirectory (album);
			FSpotConfiguration.PhotoUri = new SafeUri (new DirectoryInfo (library).FullName);

			string original = Path.Combine (library, "keep.jpg");
			string derived = Path.Combine (album, "edited.jpg");
			File.WriteAllBytes (original, new byte[] { 1 });
			File.WriteAllBytes (derived, new byte[] { 1 });

			var photo = new Photo (null, null, 1, 0);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (library), "keep.jpg", string.Empty, "Original", true);
			photo.AddVersionUnsafely (2, new SafeUri (album), "edited.jpg", string.Empty, "Edited", false);

			bool windows = OperatingSystem.IsWindows ();
			FileAttributes previousAttributes = default;
			UnixFileMode previousMode = default;
			var locked = new DirectoryInfo (album);
			if (windows) {
				previousAttributes = File.GetAttributes (derived);
				File.SetAttributes (derived, previousAttributes | FileAttributes.ReadOnly);
			} else {
				previousMode = locked.UnixFileMode;
				locked.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserExecute;
			}

			try {
				var failures = PhotoDriveDelete.TryDeleteAllVersions (photo);

				Assert.HasCount (1, failures);
				Assert.Contains (derived, failures[0].Path);
				Assert.DoesNotContain ("file://", failures[0].Path);
				Assert.IsTrue (File.Exists (original));
				Assert.IsTrue (File.Exists (derived));
				Assert.IsNotNull (photo.VersionUri (Photo.OriginalVersionId));
				Assert.IsNotNull (photo.VersionUri (2));
			} finally {
				if (windows)
					File.SetAttributes (derived, previousAttributes);
				else
					locked.UnixFileMode = previousMode;
			}
		}

		[TestMethod]
		public void TryDeleteAllVersions_RemovesEveryFileWhenNoneAreLocked ()
		{
			string library = Path.Combine (root, "Photos");
			string album = Path.Combine (library, "derived");
			Directory.CreateDirectory (album);
			FSpotConfiguration.PhotoUri = new SafeUri (new DirectoryInfo (library).FullName);

			string original = Path.Combine (library, "keep.jpg");
			string derived = Path.Combine (album, "edited.jpg");
			File.WriteAllBytes (original, new byte[] { 1 });
			File.WriteAllBytes (derived, new byte[] { 1 });

			var photo = new Photo (null, null, 1, 0);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (library), "keep.jpg", string.Empty, "Original", true);
			photo.AddVersionUnsafely (2, new SafeUri (album), "edited.jpg", string.Empty, "Edited", false);

			var failures = PhotoDriveDelete.TryDeleteAllVersions (photo);

			Assert.IsEmpty (failures);
			Assert.IsFalse (File.Exists (original));
			Assert.IsFalse (File.Exists (derived));
			Assert.IsNull (photo.VersionUri (Photo.OriginalVersionId));
			Assert.IsNull (photo.VersionUri (2));
		}
	}
}

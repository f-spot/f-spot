// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class UfrawDevelopTests
	{
		[TestMethod]
		public void QuoteForWindows_LeavesASeparatorAsOneBackslash ()
		{
			Assert.AreEqual ("\"C:\\shot.cr2\"", UfrawDevelop.QuoteForWindows (@"C:\shot.cr2"));
			Assert.AreEqual ("\"C:\\a\\\"b\\ \"", UfrawDevelop.QuoteForWindows ("C:\\a\"b\\ "));
		}

		[TestMethod]
		public void Develop_KeepsAHashInTheDevelopedFileName ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-ufraw-" + Guid.NewGuid ().ToString ("N"));
			string config = Path.Combine (root, "config");
			Directory.CreateDirectory (config);
			string rawPath = Path.Combine (root, "a#b.cr2");
			File.WriteAllText (rawPath, "raw");
			UfrawLaunch seen = null;
			try {
				Photo photo = PhotoAt (rawPath);
				bool developed = UfrawDevelop.Develop (photo, batch: false, quality: 98, extraArguments: "--wb=camera",
					config, IsRaw, launch => {
						seen = launch;
						File.WriteAllText (launch.DevelopedPath, "jpeg");
						File.WriteAllText (Path.ChangeExtension (launch.DevelopedPath, ".ufraw"), "sidecar");
						return 0;
					});

				Assert.IsTrue (developed);
				Assert.AreEqual ("ufraw", seen.Executable);
				Assert.Contains ("--compression=98", seen.Arguments);
				Assert.Contains ("--wb=camera", seen.Arguments);
				Assert.Contains ("--overwrite", seen.Arguments);
				Assert.Contains ("--out-type=jpeg", seen.Arguments);
				string developedName = "a#b (" + Strings.DevelopedInUFRaw + ").jpg";
				Assert.AreEqual (developedName, Path.GetFileName (seen.DevelopedPath));
				Assert.Contains ("#", seen.DevelopedPath);
				Assert.Contains (rawPath, seen.Arguments);
				PhotoVersion version = photo.GetVersion (photo.DefaultVersionId);
				Assert.AreEqual (Strings.DevelopedInUFRaw, version.Name);
				Assert.AreEqual (developedName, Path.GetFileName (version.Uri.AbsolutePath));
				Assert.AreEqual ("sidecar", File.ReadAllText (Path.ChangeExtension (rawPath, ".ufraw")));
				Assert.AreEqual ("sidecar", File.ReadAllText (Path.Combine (config, "batch.ufraw")));
				Assert.IsFalse (File.Exists (Path.ChangeExtension (seen.DevelopedPath, ".ufraw")));
				Assert.IsTrue (photo.Changes.DataChanged);
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Develop_UsesQuality98WhenTheRequestedQualityIsOutsideTheRange ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-ufraw-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string rawPath = Path.Combine (root, "shot.cr2");
			File.WriteAllText (rawPath, "raw");
			UfrawLaunch seen = null;
			try {
				UfrawDevelop.Develop (PhotoAt (rawPath), false, 0, null, root, IsRaw, launch => {
					seen = launch;
					File.WriteAllText (launch.DevelopedPath, "jpeg");
					return 0;
				});

				Assert.Contains ("--compression=98", seen.Arguments);
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Develop_LeavesAJpegAlone ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-ufraw-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string path = Path.Combine (root, "shot.jpg");
			File.WriteAllText (path, "jpeg");
			int launches = 0;
			try {
				Photo photo = PhotoAt (path);
				bool developed = UfrawDevelop.Develop (photo, false, 98, null, root, IsRaw, launch => {
					launches++;
					return 0;
				});

				Assert.IsFalse (developed);
				Assert.AreEqual (0, launches);
				Assert.AreEqual ((uint)Photo.OriginalVersionId, photo.DefaultVersionId);
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Develop_NamesTheNextVersionWhenTheFirstNameIsTaken ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-ufraw-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string rawPath = Path.Combine (root, "shot.cr2");
			File.WriteAllText (rawPath, "raw");
			try {
				Photo photo = PhotoAt (rawPath);
				SafeUri existing = SafeUri.FromPath (Path.Combine (root, "already.jpg"));
				photo.AddVersion (existing.GetBaseUri (), existing.GetFilename (), Strings.DevelopedInUFRaw, true);
				string seen = null;
				UfrawDevelop.Develop (photo, false, 98, null, root, IsRaw, launch => {
					seen = launch.DevelopedPath;
					File.WriteAllText (launch.DevelopedPath, "jpeg");
					return 0;
				});

				string expected = string.Format (Strings.DevelopedInUFRawX, 2);
				Assert.AreEqual (expected, photo.GetVersion (photo.DefaultVersionId).Name);
				Assert.AreEqual ("shot (" + expected + ").jpg", Path.GetFileName (seen));
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Develop_BatchUsesTheSavedSidecarAndUfrawBatch ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-ufraw-" + Guid.NewGuid ().ToString ("N"));
			string config = Path.Combine (root, "config");
			Directory.CreateDirectory (config);
			File.WriteAllText (Path.Combine (config, "batch.ufraw"), "saved");
			string rawPath = Path.Combine (root, "shot.cr2");
			File.WriteAllText (rawPath, "raw");
			File.WriteAllText (Path.ChangeExtension (rawPath, ".ufraw"), "beside-raw");
			UfrawLaunch seen = null;
			try {
				UfrawDevelop.Develop (PhotoAt (rawPath), batch: true, quality: 90, extraArguments: null,
					config, IsRaw, launch => {
						seen = launch;
						File.WriteAllText (launch.DevelopedPath, "jpeg");
						return 0;
					});

				Assert.AreEqual ("ufraw-batch", seen.Executable);
				Assert.Contains ("--compression=90", seen.Arguments);
				Assert.Contains (Path.Combine (config, "batch.ufraw"), seen.Arguments);
				Assert.DoesNotContain ("beside-raw", seen.Arguments);
				Assert.DoesNotContain (Path.ChangeExtension (rawPath, ".ufraw"), seen.Arguments);
			} finally {
				Directory.Delete (root, true);
			}
		}

		static bool IsRaw (SafeUri uri)
			=> string.Equals (uri.GetExtension (), ".cr2", StringComparison.OrdinalIgnoreCase);

		static Photo PhotoAt (string path)
		{
			SafeUri uri = SafeUri.FromPath (path);
			var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
			return photo;
		}
	}
}

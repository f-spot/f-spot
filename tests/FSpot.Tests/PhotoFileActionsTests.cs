// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Hyena;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoFileActionsTests
	{
		[TestMethod]
		public void SelectedPaths_AreTheDefaultVersionPathsInSelectionOrder ()
		{
			var photos = new[] {
				Photo ("/photos/one.jpg"),
				Photo ("/photos/two.jpg")
			};

			var paths = PhotoFileActions.SelectedPaths (photos);

			Assert.HasCount (2, paths);
			Assert.AreEqual ("/photos/one.jpg", paths[0]);
			Assert.AreEqual ("/photos/two.jpg", paths[1]);
		}

		[TestMethod]
		public void CopyToFolder_WritesEachSelectedFile ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-export-src-" + Guid.NewGuid ().ToString ("N"));
			string destDir = Path.Combine (Path.GetTempPath (), "fspot-export-dst-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			string two = Path.Combine (sourceDir, "two.jpg");
			File.WriteAllText (one, "a");
			File.WriteAllText (two, "b");
			try {
				var written = PhotoFileActions.CopyToFolder (new[] { one, two }, destDir);

				Assert.HasCount (2, written);
				Assert.IsTrue (File.Exists (Path.Combine (destDir, "one.jpg")));
				Assert.IsTrue (File.Exists (Path.Combine (destDir, "two.jpg")));
				Assert.AreEqual ("a", File.ReadAllText (Path.Combine (destDir, "one.jpg")));
			} finally {
				Directory.Delete (sourceDir, true);
				if (Directory.Exists (destDir))
					Directory.Delete (destDir, true);
			}
		}

		[TestMethod]
		public void WriteZip_StoresEachFileUnderItsOwnName ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string otherDir = Path.Combine (Path.GetTempPath (), "fspot-zip-other-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			Directory.CreateDirectory (sourceDir);
			Directory.CreateDirectory (otherDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			string two = Path.Combine (otherDir, "one.jpg");
			File.WriteAllText (one, "aaa");
			File.WriteAllText (two, "bbb");
			try {
				PhotoFileActions.WriteZip (new[] { one, two, Path.Combine (sourceDir, "missing.jpg") }, zipPath);

				using var archive = System.IO.Compression.ZipFile.OpenRead (zipPath);
				Assert.HasCount (2, archive.Entries);
				Assert.AreEqual ("one.jpg", archive.Entries[0].FullName);
				Assert.AreEqual ("one.jpg", archive.Entries[1].FullName);
				Assert.AreEqual ("aaa", ReadEntry (archive.Entries[0]));
				Assert.AreEqual ("bbb", ReadEntry (archive.Entries[1]));
			} finally {
				Directory.Delete (sourceDir, true);
				Directory.Delete (otherDir, true);
				if (File.Exists (zipPath))
					File.Delete (zipPath);
			}
		}

		static string ReadEntry (System.IO.Compression.ZipArchiveEntry entry)
		{
			using var stream = entry.Open ();
			using var reader = new StreamReader (stream);
			return reader.ReadToEnd ();
		}

		static IPhoto Photo (string path)
		{
			var version = new Mock<IPhotoVersion> ();
			version.Setup (v => v.Uri).Returns (new SafeUri ("file://" + path, true));
			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			photo.Setup (p => p.Name).Returns (Path.GetFileName (path));
			return photo.Object;
		}
	}
}

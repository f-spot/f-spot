// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

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

		[TestMethod]
		public void WriteZip_KeepsAParentSegmentInsideTheArchive ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			string extract = Path.Combine (Path.GetTempPath (), "fspot-zip-out-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (sourceDir);
			// '\' is a legal file name on Unix and a parent segment on Windows.
			string tricky = Path.Combine (sourceDir, "..\\..\\evil.txt");
			File.WriteAllText (tricky, "secret");
			try {
				PhotoFileActions.WriteZip (new[] { tricky }, zipPath);

				using (var archive = System.IO.Compression.ZipFile.OpenRead (zipPath)) {
					Assert.HasCount (1, archive.Entries);
					string entry = archive.Entries[0].FullName;
					Assert.AreEqual (Path.GetFileName (entry), entry);
					Assert.DoesNotContain ("\\", entry);
					Assert.DoesNotContain ("/", entry);
					Assert.AreEqual ("secret", ReadEntry (archive.Entries[0]));
				}

				System.IO.Compression.ZipFile.ExtractToDirectory (zipPath, extract);
				string[] files = Directory.GetFiles (extract, "*", SearchOption.AllDirectories);
				Assert.HasCount (1, files);
				Assert.AreEqual (extract, Path.GetDirectoryName (files[0]));
				Assert.AreEqual ("secret", File.ReadAllText (files[0]));
			} finally {
				Directory.Delete (sourceDir, true);
				if (Directory.Exists (extract))
					Directory.Delete (extract, true);
				if (File.Exists (zipPath))
					File.Delete (zipPath);
			}
		}

		[TestMethod]
		public void WriteZip_ReplacesAnExistingArchive ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			File.WriteAllText (one, "aaa");
			try {
				PhotoFileActions.WriteZip (new[] { one }, zipPath);
				File.WriteAllText (one, "bbb");
				PhotoFileActions.WriteZip (new[] { one }, zipPath);

				using var archive = System.IO.Compression.ZipFile.OpenRead (zipPath);
				Assert.HasCount (1, archive.Entries);
				Assert.AreEqual ("bbb", ReadEntry (archive.Entries[0]));
			} finally {
				Directory.Delete (sourceDir, true);
				if (File.Exists (zipPath))
					File.Delete (zipPath);
			}
		}

		[TestMethod]
		public void WriteZip_StoresScaledBytesAndKeepsTheOriginalName ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.png");
			File.WriteAllText (one, "original");
			int calls = 0;
			try {
				PhotoFileActions.WriteZip (new[] { one, Path.Combine (sourceDir, "missing.jpg") }, zipPath, 32,
					(path, edge) => {
						calls++;
						Assert.AreEqual (one, path);
						Assert.AreEqual (32, edge);
						return Encoding.UTF8.GetBytes ("jpeg");
					},
					null);

				Assert.AreEqual (1, calls);
				Assert.AreEqual ("original", File.ReadAllText (one));
				using var archive = System.IO.Compression.ZipFile.OpenRead (zipPath);
				Assert.HasCount (1, archive.Entries);
				Assert.AreEqual ("one.png", archive.Entries[0].FullName);
				Assert.AreEqual ("jpeg", ReadEntry (archive.Entries[0]));
			} finally {
				Directory.Delete (sourceDir, true);
				if (File.Exists (zipPath))
					File.Delete (zipPath);
			}
		}

		[TestMethod]
		public void WriteZip_KeepsTheOriginalBytesWhenThePhotoCannotBeScaled ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			File.WriteAllText (one, "original");
			try {
				PhotoFileActions.WriteZip (new[] { one }, zipPath, 1024, (path, edge) => null, null);

				using var archive = System.IO.Compression.ZipFile.OpenRead (zipPath);
				Assert.AreEqual ("original", ReadEntry (archive.Entries[0]));
			} finally {
				Directory.Delete (sourceDir, true);
				if (File.Exists (zipPath))
					File.Delete (zipPath);
			}
		}

		[TestMethod]
		public void WriteZip_StopsBeforeThePhotoThePersonCancelled ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			string two = Path.Combine (sourceDir, "two.jpg");
			File.WriteAllText (one, "aaa");
			File.WriteAllText (two, "bbb");
			int scaled = 0;
			try {
				PhotoFileActions.WriteZip (new[] { one, two }, zipPath, 64,
					(path, edge) => {
						scaled++;
						return Encoding.UTF8.GetBytes ("jpeg");
					},
					(index, total, name) => index == 1);

				Assert.AreEqual (1, scaled);
				using var archive = System.IO.Compression.ZipFile.OpenRead (zipPath);
				Assert.HasCount (1, archive.Entries);
				Assert.AreEqual ("one.jpg", archive.Entries[0].FullName);
			} finally {
				Directory.Delete (sourceDir, true);
				if (File.Exists (zipPath))
					File.Delete (zipPath);
			}
		}

		[TestMethod]
		public void WriteZip_LeavesNoArchiveWhenTheFirstPhotoIsCancelled ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-zip-src-" + Guid.NewGuid ().ToString ("N"));
			string zipPath = Path.Combine (Path.GetTempPath (), "fspot-zip-" + Guid.NewGuid ().ToString ("N") + ".zip");
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			File.WriteAllText (one, "aaa");
			try {
				PhotoFileActions.WriteZip (new[] { one }, zipPath, 64, (path, edge) => Encoding.UTF8.GetBytes ("jpeg"), (index, total, name) => true);

				Assert.IsFalse (File.Exists (zipPath));
			} finally {
				Directory.Delete (sourceDir, true);
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

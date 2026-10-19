// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoPathChangeTests
	{
		[TestMethod]
		public void Rewrite_MovesAYearFolderAndKeepsAHashInTheFileName ()
		{
			string rewritten = PhotoPathChange.Rewrite ("/old/2020/a#b.jpg", "/old/", "/new/");

			SafeUri uri = SafeUri.FromPath (rewritten);
			Assert.AreEqual ("/new/2020/a#b.jpg", WithSlashes (uri.AbsolutePath));
			Assert.AreEqual ("a#b.jpg", System.IO.Path.GetFileName (uri.AbsolutePath));
		}

		[TestMethod]
		public void Rewrite_LeavesAFileThatSitsInTheOldFolder ()
		{
			Assert.IsNull (PhotoPathChange.Rewrite ("/old/file.jpg", "/old/", "/new/"));
		}

		[TestMethod]
		public void Rewrite_KeepsTheExtraCharactersWhenTheOldBaseHasNoSlash ()
		{
			Assert.AreEqual ("/new-other/a.jpg", PhotoPathChange.Rewrite ("/old-other/a.jpg", "/old", "/new"));
		}

		[TestMethod]
		public void Rewrite_MovesAWindowsYearFolderWithoutAskingTheRuntimeToRewriteSeparators ()
		{
			Assert.AreEqual (@"C:\new\2020\a#b.jpg", PhotoPathChange.Rewrite (@"C:\old\2020\a#b.jpg", @"C:\old\", @"C:\new\"));
		}

		[TestMethod]
		public void GuessOldBase_StopsAtTheFirstYearFolder ()
		{
			Assert.AreEqual ("/photos/", PhotoPathChange.GuessOldBase (new[] {
				"/photos/2020/a.jpg",
				"/other/2019/b.jpg"
			}));
		}

		[TestMethod]
		public void GuessOldBase_StopsAtAWindowsYearFolder ()
		{
			Assert.AreEqual (@"C:\photos\", PhotoPathChange.GuessOldBase (new[] { @"C:\photos\2020\a.jpg" }));
		}

		[TestMethod]
		public void GuessOldBase_IsJustASlashWhenNoYearFolderExists ()
		{
			Assert.AreEqual ("/", PhotoPathChange.GuessOldBase (new[] { "/2020/a.jpg", "/photos/a.jpg" }));
		}

		[TestMethod]
		public void SameBase_IgnoresARepeatedTrailingSlash ()
		{
			Assert.IsTrue (PhotoPathChange.SameBase ("/photos/", "/photos//"));
			Assert.IsFalse (PhotoPathChange.SameBase ("/photos", "/pictures"));
		}

		[TestMethod]
		public void Apply_LeavesPathsAloneWhenTheOldBaseIsOnlyASlash ()
		{
			Photo photo = PhotoAt ("/Users/photos/img.jpg");

			int changed = PhotoPathChange.Apply (new[] { photo }, "/", "/library/");

			Assert.AreEqual (0, changed);
			Assert.AreEqual ("/Users/photos/img.jpg", WithSlashes (photo.VersionUri (Photo.OriginalVersionId).AbsolutePath));
			Assert.IsFalse (photo.Changes.UriChanged);
			Assert.AreEqual (0, PhotoPathChange.Apply (new[] { PhotoAt (@"C:\Users\photos\img.jpg") }, @"\", @"D:\library\"));
		}

		[TestMethod]
		public void Apply_RewritesAYearFolderAndLeavesAFileInTheOldFolder ()
		{
			Photo moved = PhotoAt ("/old/2020/a#b.jpg");
			Photo stayed = PhotoAt ("/old/file.jpg");

			int changed = PhotoPathChange.Apply (new[] { moved, stayed }, "/old/", "/new/");

			Assert.AreEqual (1, changed);
			Assert.AreEqual ("/new/2020/a#b.jpg", WithSlashes (moved.VersionUri (Photo.OriginalVersionId).AbsolutePath));
			Assert.IsTrue (moved.Changes.UriChanged);
			Assert.AreEqual ("/old/file.jpg", WithSlashes (stayed.VersionUri (Photo.OriginalVersionId).AbsolutePath));
			Assert.IsFalse (stayed.Changes.UriChanged);
		}

		[TestMethod]
		public void Commit_RollsBackEveryVersionWhenSavingThrows ()
		{
			using var library = new Library ();
			Photo first = library.Add ("a.jpg");
			SafeUri edited = SafeUri.FromPath (Path.Combine (library.Year, "a-edit.jpg"));
			first.AddVersion (edited.GetBaseUri (), edited.GetFilename (), "Edited", true);
			library.Database.Photos.Commit (first);
			Photo second = library.Add ("b.jpg");
			string[] before = library.StoredBases ();
			string firstPath = first.VersionUri (Photo.OriginalVersionId).AbsolutePath;
			string editPath = first.VersionUri (first.VersionIds.Last ()).AbsolutePath;
			string secondPath = second.VersionUri (Photo.OriginalVersionId).AbsolutePath;

			Assert.ThrowsExactly<IOException> (() => PhotoPathChange.Commit (library.Database, new[] { first, second }, library.OldBase, library.NewBase, items => {
				library.Database.Photos.Commit (new[] { items[0] });
				throw new IOException ("disk");
			}, null));

			Assert.AreEqual (firstPath, first.VersionUri (Photo.OriginalVersionId).AbsolutePath);
			Assert.AreEqual (editPath, first.VersionUri (first.VersionIds.Last ()).AbsolutePath);
			Assert.AreEqual (secondPath, second.VersionUri (Photo.OriginalVersionId).AbsolutePath);
			Assert.IsFalse (first.Changes.UriChanged);
			Assert.IsFalse (second.Changes.UriChanged);
			Assert.AreSequenceEqual (before, library.StoredBases ());
		}

		[TestMethod]
		public void Commit_LeavesTheCatalogAloneWhenTheChangeIsCancelled ()
		{
			using var library = new Library ();
			Photo photo = library.Add ("a.jpg");
			string[] before = library.StoredBases ();
			string path = photo.VersionUri (Photo.OriginalVersionId).AbsolutePath;

			int changed = PhotoPathChange.Commit (library.Database, new[] { photo }, library.OldBase, library.NewBase, null, () => true);

			Assert.AreEqual (-1, changed);
			Assert.AreEqual (path, photo.VersionUri (Photo.OriginalVersionId).AbsolutePath);
			Assert.IsFalse (photo.Changes.UriChanged);
			Assert.AreSequenceEqual (before, library.StoredBases ());
		}

		[TestMethod]
		public void Commit_StoresTheNewBaseForEveryVersion ()
		{
			using var library = new Library ();
			Photo photo = library.Add ("a.jpg");
			SafeUri edited = SafeUri.FromPath (Path.Combine (library.Year, "a-edit.jpg"));
			photo.AddVersion (edited.GetBaseUri (), edited.GetFilename (), "Edited", true);
			library.Database.Photos.Commit (photo);

			int changed = PhotoPathChange.Commit (library.Database, new[] { photo }, library.OldBase, library.NewBase, null, null);

			Assert.AreEqual (1, changed);
			string[] stored = library.StoredBases ();
			Assert.HasCount (2, stored);
			Assert.IsTrue (stored.All (uri => uri.Contains ("/new/2020") || uri.Contains ("\\new\\2020")));
			Assert.DoesNotContain ("/old/", stored[0]);
		}

		sealed class Library : IDisposable
		{
			public Db Database { get; }
			public string OldBase { get; }
			public string NewBase { get; }
			public string Year { get; }
			readonly string root;

			public Library ()
			{
				root = Path.Combine (Path.GetTempPath (), "fspot-path-" + Guid.NewGuid ().ToString ("N"));
				Year = Path.Combine (root, "old", "2020");
				Directory.CreateDirectory (Year);
				Directory.CreateDirectory (Path.Combine (root, "new"));
				OldBase = PhotoPathChange.WithTrailingSlash (Path.Combine (root, "old"));
				NewBase = PhotoPathChange.WithTrailingSlash (Path.Combine (root, "new"));
				Database = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
				Database.Init (Path.Combine (root, "photos.db"), true);
			}

			public Photo Add (string name)
			{
				string path = Path.Combine (Year, name);
				File.WriteAllText (path, "photo");
				uint roll = Database.Rolls.Create (new DateTime (2020, 1, 2, 0, 0, 0, DateTimeKind.Utc)).Id;
				return Database.Photos.CreateFrom (new FilePhoto (SafeUri.FromPath (path)), true, roll);
			}

			public string[] StoredBases ()
			{
				var found = new List<string> ();
				using var reader = Database.Database.Query ("SELECT base_uri FROM photo_versions ORDER BY photo_id, version_id");
				while (reader.Read ())
					found.Add (reader[0].ToString ());
				return found.ToArray ();
			}

			public void Dispose ()
			{
				Database.Dispose ();
				if (Directory.Exists (root))
					Directory.Delete (root, true);
			}
		}

		static string WithSlashes (string path)
			=> (path ?? string.Empty).Replace ('\\', '/');

		static Photo PhotoAt (string absolutePath)
		{
			SafeUri uri = SafeUri.FromPath (absolutePath);
			var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
			return photo;
		}
	}
}

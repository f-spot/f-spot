// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class CatalogMergeTests
	{
		static readonly DateTime January = new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		static readonly DateTime June = new DateTime (2020, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		static readonly DateTime Shot = new DateTime (2020, 1, 2, 15, 0, 0, DateTimeKind.Utc);

		[TestMethod]
		public void Merge_CopiesAHashNameWithoutEncodingItTwice ()
		{
			using var libraries = new Libraries ();
			string sourceFile = Path.Combine (libraries.Root, "a#b.jpg");
			string editedFile = Path.Combine (libraries.Root, "a#b (Edited).jpg");
			Db source = libraries.Open (libraries.SourcePath);
			try {
				uint roll = source.Rolls.Create (June).Id;
				Photo photo = AddPhoto (source, sourceFile, roll, Shot);
				SafeUri edited = SafeUri.FromPath (editedFile);
				photo.AddVersion (edited.GetBaseUri (), edited.GetFilename (), "Edited", false);
				Category trips = source.Tags.CreateCategory (source.Tags.RootCategory, "Trips", false);
				photo.AddTag (source.Tags.CreateTag (trips, "Beach", false));
				photo.AddTag (source.Tags.CreateTag ((Category)source.Tags.GetTagByName (Strings.Places), "Paris", false));
				source.Photos.Commit (photo);
			} finally {
				source.Dispose ();
			}
			File.SetAttributes (libraries.SourcePath, FileAttributes.ReadOnly);
			try {
				CatalogMergeResult result = libraries.Merge (copyFiles: true, CatalogRollChoice.All, 0);

				Assert.AreEqual (1, result.Imported);
				Assert.IsEmpty (result.Missing.ToList ());
				Assert.AreNotEqual ((FileAttributes)0, File.GetAttributes (libraries.SourcePath) & FileAttributes.ReadOnly);
				Photo imported = libraries.Destination.Photos.Query ("SELECT * FROM photos").Single ();
				SafeUri original = imported.VersionUri (Photo.OriginalVersionId);
				Assert.AreEqual ("a%23b.jpg", original.GetFilename ());
				Assert.Contains ("#", original.AbsolutePath);
				Assert.AreEqual ("photo", File.ReadAllText (original.AbsolutePath));
				Assert.AreEqual (Strings.Original, imported.DefaultVersion.Name);
				Assert.AreEqual (4u, imported.Rating);
				Assert.AreEqual ("Sun & shore", imported.Description);
				PhotoVersion editedVersion = imported.Versions.OfType<PhotoVersion> ().Single (version => version.Name == "Edited");
				Assert.AreEqual ("a%23b (Edited).jpg", editedVersion.Uri.GetFilename ());
				Assert.AreEqual (editedFile, editedVersion.Uri.AbsolutePath);
				Assert.IsFalse (File.Exists (Path.Combine (libraries.Library.AbsolutePath, "a#b (Edited).jpg")));
				Tag beach = libraries.Destination.Tags.GetTagByName ("Beach");
				Assert.IsInstanceOfType<Category> (libraries.Destination.Tags.GetTagByName ("Trips"));
				Assert.AreEqual ("Trips", beach.Category.Name);
				Tag paris = libraries.Destination.Tags.GetTagByName ("Paris");
				Assert.AreEqual (libraries.Destination.Tags.GetTagByName (Strings.Places).Id, paris.Category.Id);
				Assert.HasCount (2, imported.Tags);
			} finally {
				File.SetAttributes (libraries.SourcePath, FileAttributes.Normal);
			}
		}

		[TestMethod]
		public void Merge_KeepsTheSourcePathWhenCopyIsOff ()
		{
			using var libraries = new Libraries ();
			string sourceFile = Path.Combine (libraries.Root, "keep.jpg");
			Db source = libraries.Open (libraries.SourcePath);
			try {
				AddPhoto (source, sourceFile, source.Rolls.Create (June).Id, Shot);
			} finally {
				source.Dispose ();
			}

			CatalogMergeResult result = libraries.Merge (copyFiles: false, CatalogRollChoice.All, 0);

			Photo imported = libraries.Destination.Photos.Query ("SELECT * FROM photos").Single ();
			Assert.AreEqual (1, result.Imported);
			Assert.AreEqual (sourceFile, imported.VersionUri (Photo.OriginalVersionId).AbsolutePath);
			Assert.IsFalse (File.Exists (Path.Combine (libraries.Library.AbsolutePath, "2020", "01", "02", "keep.jpg")));
		}

		[TestMethod]
		public void Merge_SkipsAMissingFileInsteadOfAskingForever ()
		{
			using var libraries = new Libraries ();
			Db source = libraries.Open (libraries.SourcePath);
			try {
				uint roll = source.Rolls.Create (June).Id;
				Photo photo = AddPhoto (source, Path.Combine (libraries.Root, "present.jpg"), roll, Shot);
				photo.GetVersion (Photo.OriginalVersionId).Uri = SafeUri.FromPath ("/gone.jpg");
				photo.Changes.ChangeVersion (Photo.OriginalVersionId);
				photo.Changes.UriChanged = true;
				source.Photos.Commit (photo);
			} finally {
				source.Dispose ();
			}

			CatalogMergeResult result = libraries.Merge (copyFiles: false, CatalogRollChoice.All, 0);

			Assert.AreEqual (0, result.Imported);
			string missing = result.Missing.Single ();
			Assert.IsLessThan (7, missing.Split ('/').Length);
			Assert.IsFalse (File.Exists (missing));
			Assert.IsEmpty (libraries.Destination.Photos.Query ("SELECT * FROM photos").ToList ());
		}

		[TestMethod]
		public void Merge_ImportsOnlyRollsWhoseTimeIsNew ()
		{
			using var libraries = new Libraries ();
			AddPhoto (libraries.Destination, Path.Combine (libraries.Root, "stay.jpg"), libraries.Destination.Rolls.Create (January).Id, Shot);
			Db source = libraries.Open (libraries.SourcePath);
			try {
				AddPhoto (source, Path.Combine (libraries.Root, "old.jpg"), source.Rolls.Create (January).Id, Shot);
				AddPhoto (source, Path.Combine (libraries.Root, "new.jpg"), source.Rolls.Create (June).Id, Shot);
			} finally {
				source.Dispose ();
			}

			CatalogMergeResult result = libraries.Merge (copyFiles: false, CatalogRollChoice.New, 0);

			string[] names = libraries.Destination.Photos.Query ("SELECT * FROM photos").Select (photo => photo.Name).OrderBy (name => name).ToArray ();
			Assert.AreEqual (1, result.Imported);
			Assert.AreSequenceEqual (new[] { "new.jpg", "stay.jpg" }, names);
		}

		[TestMethod]
		public void Merge_ImportsOnlyTheChosenRoll ()
		{
			using var libraries = new Libraries ();
			uint oldRoll;
			Db source = libraries.Open (libraries.SourcePath);
			try {
				oldRoll = source.Rolls.Create (January).Id;
				AddPhoto (source, Path.Combine (libraries.Root, "old.jpg"), oldRoll, Shot);
				AddPhoto (source, Path.Combine (libraries.Root, "new.jpg"), source.Rolls.Create (June).Id, Shot);
			} finally {
				source.Dispose ();
			}

			CatalogMergeResult result = libraries.Merge (copyFiles: false, CatalogRollChoice.One, oldRoll);

			string[] names = libraries.Destination.Photos.Query ("SELECT * FROM photos").Select (photo => photo.Name).ToArray ();
			Assert.AreEqual (1, result.Imported);
			Assert.AreSequenceEqual (new[] { "old.jpg" }, names);
		}

		[TestMethod]
		[OSCondition (ConditionMode.Exclude, OperatingSystems.Windows)]
		public void Merge_RefusesTheOpenCatalogWhenReachedThroughALink ()
		{
			using var libraries = new Libraries ();
			AddPhoto (libraries.Destination, Path.Combine (libraries.Root, "stay.jpg"), libraries.Destination.Rolls.Create (January).Id, Shot);
			string link = Path.Combine (libraries.Root, "via-link.db");
			File.CreateSymbolicLink (link, libraries.DestinationPath);

			CatalogMergeResult result = libraries.MergeInto (link, libraries.DestinationPath, copyFiles: true, CatalogRollChoice.All, 0);

			Assert.AreEqual (0, result.Imported);
			Assert.AreEqual ("stay.jpg", libraries.Destination.Photos.Query ("SELECT * FROM photos").Single ().Name);
		}

		[TestMethod]
		public void Merge_RecordsAPhotoWhenTheLibraryFolderRejectsTheCopy ()
		{
			using var libraries = new Libraries ();
			string sourceFile = Path.Combine (libraries.Root, "present.jpg");
			Db source = libraries.Open (libraries.SourcePath);
			try {
				AddPhoto (source, sourceFile, source.Rolls.Create (June).Id, Shot);
			} finally {
				source.Dispose ();
			}
			string month = Path.Combine (libraries.Library.AbsolutePath, "2020", "01");
			Directory.CreateDirectory (Path.GetDirectoryName (month));
			File.WriteAllText (month, "not-a-directory");

			CatalogMergeResult result = libraries.Merge (copyFiles: true, CatalogRollChoice.All, 0);

			Assert.AreEqual (0, result.Imported);
			Assert.AreEqual (sourceFile, result.Missing.Single ());
			Assert.IsEmpty (libraries.Destination.Photos.Query ("SELECT * FROM photos").ToList ());
		}

		[TestMethod]
		public void Merge_KeepsAnIncomingCategoryWhenThatNameIsAlreadyATag ()
		{
			using var libraries = new Libraries ();
			libraries.Destination.Tags.CreateTag (libraries.Destination.Tags.RootCategory, "Trips", false);
			Db source = libraries.Open (libraries.SourcePath);
			try {
				Category trips = source.Tags.CreateCategory (source.Tags.RootCategory, "Trips", false);
				Photo photo = AddPhoto (source, Path.Combine (libraries.Root, "beach.jpg"), source.Rolls.Create (June).Id, Shot);
				photo.AddTag (source.Tags.CreateTag (trips, "Beach", false));
				source.Photos.Commit (photo);
			} finally {
				source.Dispose ();
			}

			libraries.Merge (copyFiles: false, CatalogRollChoice.All, 0);

			Tag beach = libraries.Destination.Tags.GetTagByName ("Beach");
			Assert.AreNotEqual (libraries.Destination.Tags.RootCategory.Id, beach.Category.Id);
			Assert.AreEqual ("Trips 2", beach.Category.Name);
			Assert.IsInstanceOfType<Category> (beach.Category);
			Assert.IsNotInstanceOfType<Category> (libraries.Destination.Tags.GetTagByName ("Trips"));
		}

		[TestMethod]
		public void Merge_RefusesTheOpenCatalog ()
		{
			using var libraries = new Libraries ();
			AddPhoto (libraries.Destination, Path.Combine (libraries.Root, "stay.jpg"), libraries.Destination.Rolls.Create (January).Id, Shot);

			CatalogMergeResult result = libraries.MergeInto (libraries.DestinationPath, libraries.DestinationPath, copyFiles: true, CatalogRollChoice.All, 0);

			Assert.AreEqual (0, result.Imported);
			Assert.AreEqual ("stay.jpg", libraries.Destination.Photos.Query ("SELECT * FROM photos").Single ().Name);
		}

		[TestMethod]
		public void Merge_UniquifiesACopiedNameThatIsAlreadyThere ()
		{
			using var libraries = new Libraries ();
			string sourceFile = Path.Combine (libraries.Root, "a#b.jpg");
			Db source = libraries.Open (libraries.SourcePath);
			try {
				AddPhoto (source, sourceFile, source.Rolls.Create (June).Id, Shot);
			} finally {
				source.Dispose ();
			}
			string occupied = Path.Combine (libraries.Library.AbsolutePath, "2020", "01", "02", "a#b.jpg");
			Directory.CreateDirectory (Path.GetDirectoryName (occupied));
			File.WriteAllText (occupied, "already");

			libraries.Merge (copyFiles: true, CatalogRollChoice.All, 0);

			Photo imported = libraries.Destination.Photos.Query ("SELECT * FROM photos").Single ();
			SafeUri original = imported.VersionUri (Photo.OriginalVersionId);
			Assert.AreEqual ("a%23b-1.jpg", original.GetFilename ());
			Assert.AreEqual ("photo", File.ReadAllText (original.AbsolutePath));
			Assert.AreEqual ("already", File.ReadAllText (occupied));
		}

		static Photo AddPhoto (Db database, string path, uint rollId, DateTime time)
		{
			Directory.CreateDirectory (Path.GetDirectoryName (path));
			if (!File.Exists (path))
				File.WriteAllText (path, "photo");
			Photo photo = database.Photos.CreateFrom (new FilePhoto (SafeUri.FromPath (path)), true, rollId);
			photo.Time = time;
			photo.Description = "Sun & shore";
			photo.Rating = 4;
			database.Photos.Commit (photo);
			return photo;
		}

		sealed class Libraries : IDisposable
		{
			public string Root { get; }
			public SafeUri Library { get; }
			public Db Destination { get; }
			public string DestinationPath { get; }
			public string SourcePath { get; }
			readonly IImageFileFactory images;
			readonly IThumbnailService thumbnails;
			readonly IUpdaterUI updater;

			public Libraries ()
			{
				Root = Path.Combine (Path.GetTempPath (), "fspot-merge-" + Guid.NewGuid ().ToString ("N"));
				Directory.CreateDirectory (Root);
				Library = new SafeUri (Path.Combine (Root, "library"));
				Directory.CreateDirectory (Library.AbsolutePath);
				images = new Mock<IImageFileFactory> ().Object;
				thumbnails = new Mock<IThumbnailService> ().Object;
				updater = new Mock<IUpdaterUI> ().Object;
				DestinationPath = Path.Combine (Root, "dest.db");
				SourcePath = Path.Combine (Root, "source.db");
				Destination = Open (DestinationPath);
			}

			public Db Open (string path)
			{
				var database = new Db (images, thumbnails, updater);
				database.Init (path, true);
				return database;
			}

			public CatalogMergeResult Merge (bool copyFiles, CatalogRollChoice rolls, uint rollId)
				=> MergeInto (SourcePath, DestinationPath, copyFiles, rolls, rollId);

			public CatalogMergeResult MergeInto (string sourcePath, string destinationPath, bool copyFiles, CatalogRollChoice rolls, uint rollId)
				=> CatalogMerge.Merge (Destination, sourcePath, destinationPath, copyFiles, rolls, rollId, Library, images, thumbnails, updater);

			public void Dispose ()
			{
				Destination.Dispose ();
				if (Directory.Exists (Root))
					Directory.Delete (Root, true);
			}
		}
	}
}

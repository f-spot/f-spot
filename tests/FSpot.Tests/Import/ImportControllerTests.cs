// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using FSpot.Core;
using FSpot.Core.Tests.Mocks;
using FSpot.Database;
using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Tests;
using FSpot.Thumbnail;
using FSpot.Utils.Tests;

using Hyena;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Import
{
	[TestClass]
	public class ImportControllerTests
	{
		TemporaryDirectories directories;
		SafeUri previousPhotoUri;

		[TestInitialize]
		public void SetUp ()
		{
			directories = new TemporaryDirectories ();
			previousPhotoUri = FSpotConfiguration.PhotoUri;
		}

		[TestCleanup]
		public void TearDown ()
		{
			FSpotConfiguration.PhotoUri = previousPhotoUri;
			directories.DeleteAll ();
		}

		[TestMethod]
		[DataRow (2016, 2, 6, "/photo/store/2016/02/06")]
		[DataRow (1999, 12, 31, "/photo/store/1999/12/31")]
		[DataRow (2026, 1, 1, "/photo/store/2026/01/01")]
		public void FindImportDestination_UsesYearMonthDayUnderTheLibrary (int year, int month, int day, string expected)
		{
			var source = Mocks.PhotoMock.Create (new SafeUri ("/path/to/photo.jpg"), new DateTime (year, month, day));

			var result = ImportController.FindImportDestination (source, new SafeUri ("/photo/store"));

			Assert.AreEqual (new SafeUri (expected), result);
		}

		[TestMethod]
		public void DoImport_ImportsAValidPhotoIntoTheLibrary ()
		{
			using var session = OpenSession ();
			var photo = ValidPhoto ();

			session.Controller.DoImport (session.Db, Photos (photo), Array.Empty<Tag> (), CopyingPreferences (), NullProgress (), CancellationToken.None);

			Assert.AreEqual (1, session.Controller.PhotosImported);
			Assert.IsEmpty (session.Controller.FailedImports.ToList ());
			Assert.AreEqual (1, session.Db.Photos.TotalPhotos);
			Assert.IsTrue (File.Exists (photo.DefaultVersion.Uri.AbsolutePath),
				"CopyFiles copies into the library and updates the version URI");
			Assert.StartsWith (session.Library.AbsolutePath, photo.DefaultVersion.Uri.AbsolutePath);
		}

		[TestMethod]
		public void DoImport_RecordsAnInvalidPhotoAsFailedAndKeepsImporting ()
		{
			using var session = OpenSession ();
			var invalid = new FilePhoto (new SafeUri (Path.Combine (session.Root, "missing.jpg")));
			var valid = ValidPhoto ();

			session.Controller.DoImport (session.Db, Photos (invalid, valid), Array.Empty<Tag> (), CopyingPreferences (), NullProgress (), CancellationToken.None);

			Assert.AreEqual (1, session.Controller.PhotosImported);
			Assert.AreSequenceEqual (new[] { invalid.DefaultVersion.Uri }, session.Controller.FailedImports.ToList ());
		}

		[TestMethod]
		public void DoImport_SkipsDuplicatesWithoutCountingThemAsFailures ()
		{
			using var session = OpenSession ();
			var first = ValidPhoto ();
			session.Controller.DoImport (session.Db, Photos (first), Array.Empty<Tag> (), CopyingPreferences (), NullProgress (), CancellationToken.None);

			var again = new FilePhoto (new SafeUri (ImageTestHelper.CreateTempFile ("taglib-sample.jpg")));
			session.Controller.DoImport (session.Db, Photos (again), Array.Empty<Tag> (), CopyingPreferences (), NullProgress (), CancellationToken.None);

			Assert.AreEqual (0, session.Controller.PhotosImported);
			Assert.IsEmpty (session.Controller.FailedImports.ToList ());
			Assert.AreEqual (1, session.Db.Photos.TotalPhotos);
		}

		[TestMethod]
		public void DoImport_ImportsTheSameFileTwiceWhenDuplicateDetectionIsOff ()
		{
			using var session = OpenSession ();
			var first = ValidPhoto ();
			var prefs = CopyingPreferences ();
			prefs.DuplicateDetect = false;
			session.Controller.DoImport (session.Db, Photos (first), Array.Empty<Tag> (), prefs, NullProgress (), CancellationToken.None);

			var again = new FilePhoto (new SafeUri (ImageTestHelper.CreateTempFile ("taglib-sample.jpg")));
			session.Controller.DoImport (session.Db, Photos (again), Array.Empty<Tag> (), prefs, NullProgress (), CancellationToken.None);

			Assert.AreEqual (1, session.Controller.PhotosImported);
			Assert.AreEqual (2, session.Db.Photos.TotalPhotos);
		}

		[TestMethod]
		public void DoImport_LeavesTheSourceInPlaceWhenCopyFilesIsOff ()
		{
			using var session = OpenSession ();
			var photo = ValidPhoto ();
			var sourcePath = photo.DefaultVersion.Uri.AbsolutePath;
			var prefs = CopyingPreferences ();
			prefs.CopyFiles = false;

			session.Controller.DoImport (session.Db, Photos (photo), Array.Empty<Tag> (), prefs, NullProgress (), CancellationToken.None);

			Assert.AreEqual (1, session.Controller.PhotosImported);
			Assert.AreEqual (sourcePath, photo.DefaultVersion.Uri.AbsolutePath);
		}

		[TestMethod]
		public void DoImport_AttachesTheGivenTags ()
		{
			using var session = OpenSession ();
			var photo = ValidPhoto ();
			var tag = session.Db.Tags.CreateCategory (session.Db.Tags.RootCategory, "Holiday", false);

			session.Controller.DoImport (session.Db, Photos (photo), new[] { tag }, CopyingPreferences (), NullProgress (), CancellationToken.None);

			Assert.AreEqual (1, session.Db.Photos.TotalPhotos);
			using (var reader = session.Db.Database.Query ("SELECT id FROM photos")) {
				Assert.IsTrue (reader.Read ());
				var imported = session.Db.Photos.Get (Convert.ToUInt32 (reader["id"]));
				Assert.Contains (tag, imported.Tags);
			}
		}

		[TestMethod]
		public void DoImport_CancelBeforeAnyPhoto_LeavesTheCatalogEmpty ()
		{
			using var session = OpenSession ();
			using var cancel = new CancellationTokenSource ();
			cancel.Cancel ();

			session.Controller.DoImport (session.Db, Photos (ValidPhoto ()), Array.Empty<Tag> (), CopyingPreferences (), NullProgress (), cancel.Token);

			Assert.AreEqual (0, session.Controller.PhotosImported);
			Assert.AreEqual (0, session.Db.Photos.TotalPhotos);
		}

		[TestMethod]
		public void DoImport_CancelAfterTheFirstPhoto_RollsTheCatalogBack ()
		{
			using var session = OpenSession ();
			using var cancel = new CancellationTokenSource ();
			var prefs = CopyingPreferences ();
			prefs.DuplicateDetect = false;
			var photos = new YieldThenCancelCollection (ValidPhoto (), ValidPhoto (), cancel);

			session.Controller.DoImport (session.Db, photos, Array.Empty<Tag> (), prefs, NullProgress (), cancel.Token);

			Assert.AreEqual (0, session.Controller.PhotosImported);
			Assert.AreEqual (0, session.Db.Photos.TotalPhotos);
		}

		[TestMethod]
		public void DoImport_RemoveOriginals_DeletesTheSourceFile ()
		{
			using var session = OpenSession ();
			var photo = ValidPhoto ();
			var sourcePath = photo.DefaultVersion.Uri.AbsolutePath;
			var prefs = CopyingPreferences ();
			prefs.RemoveOriginals = true;

			session.Controller.DoImport (session.Db, Photos (photo), Array.Empty<Tag> (), prefs, NullProgress (), CancellationToken.None);

			Assert.AreEqual (1, session.Controller.PhotosImported);
			Assert.IsFalse (File.Exists (sourcePath));
		}

		[TestMethod]
		public void DoImport_EmptyCollection_ImportsNothing ()
		{
			using var session = OpenSession ();

			session.Controller.DoImport (session.Db, Photos (), Array.Empty<Tag> (), CopyingPreferences (), NullProgress (), CancellationToken.None);

			Assert.AreEqual (0, session.Controller.PhotosImported);
			Assert.IsEmpty (session.Controller.FailedImports.ToList ());
			Assert.AreEqual (0, session.Db.Photos.TotalPhotos);
		}

		ImportSession OpenSession ()
		{
			string root = directories.Create ("import");
			return new ImportSession (root);
		}

		static FilePhoto ValidPhoto ()
			=> new FilePhoto (new SafeUri (ImageTestHelper.CreateTempFile ("taglib-sample.jpg")), Strings.Original);

		static BrowsableCollectionMock Photos (params IPhoto[] items)
			=> new BrowsableCollectionMock (items);

		static ImportPreferences CopyingPreferences ()
		{
			return new ImportPreferences (false) {
				CopyFiles = true,
				DuplicateDetect = true,
				RemoveOriginals = false,
				RecurseSubdirectories = true,
				MergeRawAndJpeg = true
			};
		}

		static IProgress<int> NullProgress () => new ImmediateProgress (_ => { });

		sealed class ImmediateProgress : IProgress<int>
		{
			readonly Action<int> handler;

			public ImmediateProgress (Action<int> handler)
			{
				this.handler = handler;
			}

			public void Report (int value) => handler (value);
		}

		// Items is walked once. Yielding the second photo after Cancel means the
		// next loop iteration sees the token and rolls the first import back.
		sealed class YieldThenCancelCollection : IBrowsableCollection
		{
			readonly IPhoto first;
			readonly IPhoto second;
			readonly CancellationTokenSource cancel;

			public YieldThenCancelCollection (IPhoto first, IPhoto second, CancellationTokenSource cancel)
			{
				this.first = first;
				this.second = second;
				this.cancel = cancel;
			}

			public event IBrowsableCollectionChangedHandler Changed { add { } remove { } }
			public event IBrowsableCollectionItemsChangedHandler ItemsChanged { add { } remove { } }
			public int IndexOf (IPhoto item) => throw new NotImplementedException ();
			public bool Contains (IPhoto item) => throw new NotImplementedException ();
			public void MarkChanged (int index, IBrowsableItemChanges changes) => throw new NotImplementedException ();
			public IPhoto this[int index] => throw new NotImplementedException ();
			public int Count => 2;

			public IEnumerable<IPhoto> Items {
				get {
					yield return first;
					cancel.Cancel ();
					yield return second;
				}
			}
		}

		sealed class ImportSession : IDisposable
		{
			public string Root { get; }
			public SafeUri Library { get; }
			public Db Db { get; }
			public ImportController Controller { get; }

			public ImportSession (string root)
			{
				Root = root;
				Library = new SafeUri (Path.Combine (root, "library"));
				Directory.CreateDirectory (Library.AbsolutePath);
				FSpotConfiguration.PhotoUri = Library;

				string dbPath = Path.Combine (root, "photos.db");
				Db = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
				Db.Init (dbPath, true);

				Controller = new ImportController (new DotNetFileSystem (), new Mock<IThumbnailLoader> ().Object);
			}

			public void Dispose ()
			{
				Db.Dispose ();
			}
		}
	}
}

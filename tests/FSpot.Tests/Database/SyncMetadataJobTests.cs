// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;
using FSpot.Database;
using FSpot.Database.Jobs;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;
using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Tests.Database
{
	[TestClass]
	public sealed class SyncMetadataJobTests
	{
		[TestMethod]
		public void Execute_WritesTheDescriptionAndLeavesTheFileClockAlone ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-sync-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string savedBase = FSpotConfiguration.BaseDirectory;
			bool savedSidecar = LibraryPreferences.MetadataAlwaysUseSidecar;
			Db database = null;
			SafeUri uri = null;
			try {
				FSpotConfiguration.BaseDirectory = root;
				LibraryPreferences.MetadataAlwaysUseSidecar = false;

				uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
				DateTime fileClock;
				using (var before = MetadataUtils.Parse (uri)) {
					Assert.IsNotNull (before);
					Assert.IsTrue (before.ImageTag.DateTime.HasValue);
					fileClock = before.ImageTag.DateTime.Value;
				}

				database = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
				database.Init (Path.Combine (root, "photos.db"), true);
				Photo photo = database.Photos.CreateFrom (new FilePhoto (uri), true, database.Rolls.Create ().Id);
				photo.Time = fileClock.AddHours (8);
				photo.Description = "from the catalog";
				database.Photos.Commit (photo);

				var job = new SyncMetadataJob (database, new JobData {
					Id = 1,
					JobOptions = photo.Id.ToString (),
					RunAt = DateTime.UtcNow,
				});
				job.Run ();
				Assert.AreEqual (FSpot.Jobs.JobStatus.Finished, job.Status);

				using var after = MetadataUtils.Parse (uri);
				Assert.IsNotNull (after);
				Assert.AreEqual (fileClock.ToString ("yyyy-MM-dd HH:mm:ss"), after.ImageTag.DateTime.Value.ToString ("yyyy-MM-dd HH:mm:ss"));
				Assert.AreEqual ("from the catalog", after.ImageTag.Comment);
			} finally {
				database?.Dispose ();
				LibraryPreferences.MetadataAlwaysUseSidecar = savedSidecar;
				FSpotConfiguration.BaseDirectory = savedBase;
				if (uri != null)
					ImageTestHelper.DeleteTempFile (uri);
				if (Directory.Exists (root))
					Directory.Delete (root, true);
			}
		}

	}
}

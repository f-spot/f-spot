// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot;
using FSpot.Core;
using FSpot.Thumbnail;
using FSpot.Utils;
using FSpot.Utils.Tests;

using Hyena;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoVersionWorkflowTests
	{
		string path;
		readonly List<Photo> commits = new List<Photo> ();

		[TestInitialize]
		public void Setup ()
		{
			path = ImageTestHelper.CreateTempFile ("taglib-sample.jpg").AbsolutePath;
			FSpot.Settings.FSpotConfiguration.PhotoUri = new SafeUri (Path.GetTempPath ());
		}

		[TestCleanup]
		public void TearDown ()
		{
			string dir = Path.GetDirectoryName (path);
			if (dir != null && Directory.Exists (dir)) {
				foreach (string leftover in Directory.GetFiles (dir, Path.GetFileNameWithoutExtension (path) + "*"))
					File.Delete (leftover);
			}
		}

		[TestMethod]
		public void Create_AddsANamedVersionAndCommits ()
		{
			var photo = CatalogPhoto ();
			uint id = PhotoVersionWorkflow.Create (photo, "Edited", commits.Add);

			Assert.HasCount (2, photo.Versions);
			Assert.AreEqual ("Edited", photo.DefaultVersion.Name);
			Assert.AreEqual (id, photo.DefaultVersionId);
			Assert.HasCount (1, commits);
			Assert.IsTrue (File.Exists (photo.DefaultVersion.Uri.AbsolutePath));
		}

		[TestMethod]
		public void Rename_ChangesTheDefaultVersionNameAndCommits ()
		{
			var photo = CatalogPhoto ();
			PhotoVersionWorkflow.Create (photo, "Edited", commits.Add);
			commits.Clear ();
			PhotoVersionWorkflow.Rename (photo, "Final", commits.Add);

			Assert.AreEqual ("Final", photo.DefaultVersion.Name);
			Assert.HasCount (1, commits);
		}

		[TestMethod]
		public void Delete_RemovesTheDefaultVersionAndCommits ()
		{
			var photo = CatalogPhoto ();
			PhotoVersionWorkflow.Create (photo, "Edited", commits.Add);
			commits.Clear ();
			PhotoVersionWorkflow.Delete (photo, commits.Add);

			Assert.HasCount (1, photo.Versions);
			Assert.AreEqual ((uint)Photo.OriginalVersionId, photo.DefaultVersionId);
			Assert.HasCount (1, commits);
		}

		[TestMethod]
		public void Detach_YieldsANewPhotoAndKeepsTheFile ()
		{
			var photo = CatalogPhoto ();
			PhotoVersionWorkflow.Create (photo, "Edited", commits.Add);
			var detached = new Photo (null, Thumbs (), 2, DateTimeUtil.FromDateTime (DateTime.UtcNow));
			var batch = new List<Photo[]> ();
			var result = PhotoVersionWorkflow.Detach (photo, _ => detached, photos => batch.Add (photos.ToArray ()));

			Assert.AreSame (detached, result);
			Assert.HasCount (1, photo.Versions);
			Assert.HasCount (1, batch);
			Assert.HasCount (2, batch[0]);
		}

		[TestMethod]
		public void Reparent_MovesVersionsRatingDescriptionAndTagsOntoTheParent ()
		{
			var parent = CatalogPhoto ();
			var child = CatalogPhoto ();
			child.Rating = 4;
			child.Description = "Beach";
			var tag = new Tag (null, 9, "Trip");
			child.AddTag (tag);
			var removed = new List<Photo> ();

			PhotoVersionWorkflow.Reparent (parent, new[] { child }, commits.Add, removed.AddRange);

			Assert.HasCount (2, parent.Versions);
			Assert.AreEqual (4u, parent.Rating);
			Assert.AreEqual ("Beach", parent.Description);
			Assert.Contains (tag, parent.Tags);
			Assert.HasCount (1, removed);
			Assert.AreSame (child, removed[0]);
			Assert.IsTrue (File.Exists (path));
		}

		[TestMethod]
		public void Reparent_NamesRepeatedVersionsUniquely ()
		{
			var parent = CatalogPhoto ();
			var first = CatalogPhoto ();
			var second = CatalogPhoto ();

			PhotoVersionWorkflow.Reparent (parent, new[] { first, second }, commits.Add, _ => { });

			var names = parent.Versions.Select (v => v.Name).ToList ();
			Assert.HasCount (3, names);
			Assert.HasCount (3, names.Distinct ());
			Assert.DoesNotContain (n => n.StartsWith ("rep", StringComparison.Ordinal), names);
		}

		[TestMethod]
		public void ShiftTime_OffsetsThePhotoAndCommits ()
		{
			var photo = CatalogPhoto ();
			DateTime original = photo.Time;
			PhotoTimeAdjust.Shift (photo, TimeSpan.FromHours (3), commits.Add);

			Assert.AreEqual (original + TimeSpan.FromHours (3), photo.Time);
			Assert.HasCount (1, commits);
		}

		[TestMethod]
		public void RefreshThumbnail_DeletesCachedThumbsForTheDefaultVersion ()
		{
			var thumbs = new Mock<IThumbnailService> ();
			var photo = CatalogPhoto (thumbs.Object);
			ThumbnailRefresh.Invalidate (thumbs.Object, photo);
			thumbs.Verify (t => t.DeleteThumbnails (photo.DefaultVersion.Uri), Times.Once);
		}

		Photo CatalogPhoto (IThumbnailService thumbs = null)
		{
			var photo = new Photo (null, thumbs ?? Thumbs (), 1, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
			var uri = new SafeUri (path);
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
			photo.AllVersionsLoaded = true;
			return photo;
		}

		static IThumbnailService Thumbs ()
			=> new Mock<IThumbnailService> ().Object;
	}
}

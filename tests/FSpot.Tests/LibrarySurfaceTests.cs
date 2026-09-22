// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Platform;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class LibrarySurfaceTests
	{
		string prefsFile;

		[TestInitialize]
		public void Setup ()
		{
			prefsFile = Path.ChangeExtension (Path.GetTempFileName (), "json");
			PreferenceJsonBackend.PreferenceLocationOverride = prefsFile;
		}

		[TestCleanup]
		public void TearDown ()
		{
			PreferenceJsonBackend.PreferenceLocationOverride = null;
			if (File.Exists (prefsFile))
				File.Delete (prefsFile);
		}

		[TestMethod]
		public void LibraryPreferences_RoundTripSidebarTimelineFilmstripAndImportDefaults ()
		{
			LibraryPreferences.ShowSidebar = false;
			LibraryPreferences.ShowTimeline = false;
			LibraryPreferences.ShowFilmstrip = true;
			LibraryPreferences.ImportCopyFiles = false;
			LibraryPreferences.StoragePath = "/tmp/f-spot-photos";
			LibraryPreferences.ViewerInterpolation = false;

			Assert.IsFalse (LibraryPreferences.ShowSidebar);
			Assert.IsFalse (LibraryPreferences.ShowTimeline);
			Assert.IsTrue (LibraryPreferences.ShowFilmstrip);
			Assert.IsFalse (LibraryPreferences.ImportCopyFiles);
			Assert.AreEqual ("/tmp/f-spot-photos", LibraryPreferences.StoragePath);
			Assert.IsFalse (LibraryPreferences.ViewerInterpolation);
		}

		[TestMethod]
		public void HelpContents_UsesGhelpOnLinuxAndTheProjectSiteOtherwise ()
		{
			Assert.AreEqual ("ghelp:f-spot", HelpContents.Uri (linux: true));
			Assert.AreEqual ("https://f-spot.app", HelpContents.Uri (linux: false));
		}

		[TestMethod]
		public void DeviceImport_EmptyCameraListIsNoCamerasDetected ()
		{
			Assert.IsEmpty (DeviceImport.Cameras ());
			Assert.AreEqual (Strings.NoCamerasDetected, DeviceImport.EmptyMessage);
		}

		[TestMethod]
		public void AddinCatalog_CanEnableAndDisableALoadedAddin ()
		{
			var addins = new List<AddinStatus> {
				new AddinStatus ("FSpot.Editors.Flip", "Flip", true),
				new AddinStatus ("FSpot.Tools.Merge", "Merge", false)
			};

			AddinCatalog.SetEnabled (addins, "FSpot.Tools.Merge", true);

			Assert.IsTrue (addins.Single (a => a.Id == "FSpot.Tools.Merge").Enabled);
			Assert.IsTrue (global::FSpot.Settings.Preferences.Get<bool> (
				global::FSpot.Settings.Preferences.ExtensionKey + "FSpot.Tools.Merge"));
		}

		[TestMethod]
		public void TagReparent_MovesATagToANewCategory ()
		{
			var people = new Category (null, 1, "People");
			var places = new Category (null, 2, "Places");
			var friends = new Tag (people, 3, "Friends");

			TagReparent.Move (friends, places);

			Assert.AreSame (places, friends.Category);
			Assert.DoesNotContain (friends, people.Children);
			Assert.Contains (friends, places.Children);
		}

		[TestMethod]
		public void TagReparent_RefusesACycle ()
		{
			var people = new Category (null, 1, "People");
			var friends = new Category (people, 2, "Friends");

			bool moved = TagReparent.Move (people, friends);

			Assert.IsFalse (moved);
			Assert.IsNull (people.Category);
			Assert.AreSame (people, friends.Category);
			Assert.DoesNotContain (people, friends.Children);
		}

		[TestMethod]
		public void AddinCatalog_MissingPreferenceStaysEnabled ()
		{
			string id = "FSpot.Test.NeverSaved." + Guid.NewGuid ().ToString ("N");

			Assert.IsTrue (AddinCatalog.IsEnabled (id));

			string key = global::FSpot.Settings.Preferences.ExtensionKey + id;
			global::FSpot.Settings.Preferences.Set (key, false);
			try {
				Assert.IsFalse (AddinCatalog.IsEnabled (id));
			} finally {
				global::FSpot.Settings.Preferences.Set (key, true);
			}
		}

		[TestMethod]
		public void FolderShare_IsCountOverLibraryTotal ()
		{
			Assert.AreEqual (0.25, FolderShare.Fraction (10, 40), 0.0001);
			Assert.AreEqual (0, FolderShare.Fraction (3, 0));
		}

		[TestMethod]
		public void InfoFieldVisibility_HidesAFieldAndRoundTrips ()
		{
			var visibility = new InfoFieldVisibility ();
			Assert.IsTrue (visibility.Date);
			visibility.Date = false;
			visibility.Save ();
			var loaded = InfoFieldVisibility.Load ();
			Assert.IsFalse (loaded.Date);
			Assert.IsTrue (loaded.Rating);
		}

		[TestMethod]
		public void StarRating_MapsZeroToFiveOntoFiveStars ()
		{
			Assert.AreSequenceEqual (new[] { false, false, false, false, false }, StarRating.Stars (0));
			Assert.AreSequenceEqual (new[] { true, true, true, false, false }, StarRating.Stars (3));
			Assert.AreSequenceEqual (new[] { true, true, true, true, true }, StarRating.Stars (5));
		}

		[TestMethod]
		public void PhotoViewTransform_PanAndZoomMapAViewPointIntoBufferPixels ()
		{
			var identity = PhotoViewTransform.MapPoint (200, 100, 200, 100, zoom: 1, panX: 0, panY: 0, viewX: 50, viewY: 25);
			Assert.AreEqual (50, identity.X);
			Assert.AreEqual (25, identity.Y);

			var zoomed = PhotoViewTransform.MapPoint (200, 100, 200, 100, zoom: 2, panX: 0, panY: 0, viewX: 100, viewY: 50);
			Assert.AreEqual (100, zoomed.X);
			Assert.AreEqual (50, zoomed.Y);

			var panned = PhotoViewTransform.MapPoint (200, 100, 200, 100, zoom: 1, panX: 10, panY: 5, viewX: 60, viewY: 30);
			Assert.AreEqual (50, panned.X);
			Assert.AreEqual (25, panned.Y);

			var rect = PhotoViewTransform.MapRect (200, 100, 200, 100, zoom: 2, panX: 0, panY: 0, 0, 0, 200, 100);
			Assert.AreEqual (100, rect.Width);
			Assert.AreEqual (50, rect.Height);
		}
	}
}

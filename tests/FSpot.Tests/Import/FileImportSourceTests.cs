// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Tests;
using FSpot.Utils;
using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Import
{
	[TestClass]
	public class FileImportSourceTests
	{
		readonly TemporaryDirectories directories = new TemporaryDirectories ();

		[TestCleanup]
		public void TearDown () => directories.DeleteAll ();

		[TestMethod]
		public void ScanPhotos_SkipsNonImageFiles ()
		{
			string root = directories.Create ("scan");
			CopySampleJpg (root, "photo.jpg");
			File.WriteAllText (Path.Combine (root, "notes.txt"), "not a photo");

			var infos = Scan (root, Recursing (true)).ToList ();

			Assert.HasCount (1, infos);
			Assert.AreEqual ("photo.jpg", infos[0].DefaultVersion.Uri.GetFilename ());
		}

		[TestMethod]
		public void ScanPhotos_RecurseIncludesNestedImages ()
		{
			string root = directories.Create ("scan");
			CopySampleJpg (root, "top.jpg");
			string nested = Path.Combine (root, "album");
			Directory.CreateDirectory (nested);
			CopySampleJpg (nested, "nested.jpg");

			var names = Scan (root, Recursing (true)).Select (i => i.DefaultVersion.Uri.GetFilename ()).ToList ();

			Assert.AreSequenceEqual (new[] { "nested.jpg", "top.jpg" }, names, SequenceOrder.InAnyOrder);
		}

		[TestMethod]
		public void ScanPhotos_WithoutRecurse_DoesNotEnterSubdirectories ()
		{
			string root = directories.Create ("scan");
			CopySampleJpg (root, "top.jpg");
			string nested = Path.Combine (root, "album");
			Directory.CreateDirectory (nested);
			CopySampleJpg (nested, "nested.jpg");

			var infos = Scan (root, Recursing (false)).ToList ();

			Assert.HasCount (1, infos);
			Assert.AreEqual ("top.jpg", infos[0].DefaultVersion.Uri.GetFilename ());
		}

		[TestMethod]
		public void ScanPhotos_MergeRawAndJpeg_YieldsOnePhotoWithTwoVersions ()
		{
			string root = directories.Create ("scan");
			CopySampleJpg (root, "photo.jpg");
			File.WriteAllBytes (Path.Combine (root, "photo.nef"), new byte[] { 1, 2, 3, 4 });

			var prefs = Recursing (false);
			prefs.MergeRawAndJpeg = true;
			var infos = Scan (root, prefs).ToList ();

			Assert.HasCount (1, infos);
			Assert.AreEqual (Strings.OriginalRaw, infos[0].DefaultVersion.Name);
			Assert.AreEqual ("photo.nef", infos[0].DefaultVersion.Uri.GetFilename ());
			Assert.HasCount (2, infos[0].Versions.ToList ());
			Assert.AreEqual (Strings.OriginalJpeg, infos[0].Versions.ElementAt (1).Name);
		}

		[TestMethod]
		public void ScanPhotos_WithoutMerge_YieldsJpegAndRawSeparately ()
		{
			string root = directories.Create ("scan");
			CopySampleJpg (root, "photo.jpg");
			File.WriteAllBytes (Path.Combine (root, "photo.nef"), new byte[] { 1, 2, 3, 4 });

			var prefs = Recursing (false);
			prefs.MergeRawAndJpeg = false;
			var infos = Scan (root, prefs).ToList ();

			Assert.HasCount (2, infos);
			Assert.AreEqual (Strings.Original, infos[0].DefaultVersion.Name);
			Assert.AreEqual (Strings.Original, infos[1].DefaultVersion.Name);
		}

		[TestMethod]
		public void ScanPhotos_SkipsEmptyFiles ()
		{
			string root = directories.Create ("scan");
			File.Create (Path.Combine (root, "empty.jpg")).Dispose ();
			CopySampleJpg (root, "photo.jpg");

			var infos = Scan (root, Recursing (false)).ToList ();

			Assert.HasCount (1, infos);
			Assert.AreEqual ("photo.jpg", infos[0].DefaultVersion.Uri.GetFilename ());
		}

		[TestMethod]
		public void ScanPhotos_YieldsSvgFiles ()
		{
			string root = directories.Create ("scan");
			File.WriteAllText (Path.Combine (root, "icon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"8\" height=\"8\"><rect width=\"8\" height=\"8\" fill=\"#00ff00\"/></svg>");
			CopySampleJpg (root, "photo.jpg");

			var infos = Scan (root, Recursing (false)).ToList ();

			var names = infos.Select (i => i.DefaultVersion.Uri.GetFilename ()).ToList ();
			Assert.HasCount (2, names);
			Assert.Contains ("icon.svg", names);
			Assert.Contains ("photo.jpg", names);
		}

		[TestMethod]
		public void ScanPhotos_YieldsACorruptJpegWhoseTimeAndDescriptionDoNotThrow ()
		{
			string root = directories.Create ("scan");
			File.Copy (ImageTestHelper.CreateTempFile ("taglib-sample-broken.jpg").AbsolutePath, Path.Combine (root, "broken.jpg"), true);
			CopySampleJpg (root, "photo.jpg");

			var infos = Scan (root, Recursing (false)).ToList ();

			Assert.HasCount (2, infos);
			var broken = infos.Single (i => i.DefaultVersion.Uri.GetFilename () == "broken.jpg");
			DateTime time = broken.Time;
			Assert.IsTrue (broken.IsInvalid);
			Assert.IsNull (broken.Description);
			Assert.AreNotEqual (default (DateTime), time);

			var good = infos.Single (i => i.DefaultVersion.Uri.GetFilename () == "photo.jpg");
			Assert.IsFalse (good.IsInvalid);
			Assert.AreNotEqual (default (DateTime), good.Time);
		}

		static ImportPreferences Recursing (bool recurse)
		{
			return new ImportPreferences (false) {
				RecurseSubdirectories = recurse,
				MergeRawAndJpeg = true
			};
		}

		static void CopySampleJpg (string directory, string name)
		{
			File.Copy (ImageTestHelper.CreateTempFile ("taglib-sample.jpg").AbsolutePath, Path.Combine (directory, name), true);
		}

		static System.Collections.Generic.IEnumerable<FileImportInfo> Scan (string root, ImportPreferences preferences)
		{
			var fileSystem = new DotNetFileSystem ();
			var factory = new ImageFileFactory (fileSystem);
			var source = new FileImportSource (new SafeUri (root), factory, fileSystem);
			return source.ScanPhotos (preferences);
		}
	}
}

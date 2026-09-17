// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Filters.Tests
{
	[TestClass]
	public sealed class FilterRequestTests
	{
		TemporaryDirectories directories;

		[TestInitialize]
		public void Setup ()
			=> directories = new TemporaryDirectories ();

		[TestCleanup]
		public void Cleanup ()
			=> directories.DeleteAll ();

		[TestMethod]
		public void Current_StartsAsTheSourceAndDisposeLeavesTheSource ()
		{
			string source = Path.Combine (directories.Create ("filter-src"), "photo.jpg");
			File.WriteAllText (source, "keep");

			using (var request = new FilterRequest (new SafeUri (source))) {
				Assert.AreEqual (source, request.Source.LocalPath);
				Assert.AreEqual (source, request.Current.LocalPath);
			}

			Assert.IsTrue (File.Exists (source));
		}

		[TestMethod]
		public void TempUri_CreatesAFileThatDisposeDeletes ()
		{
			string source = Path.Combine (directories.Create ("filter-src"), "photo.jpg");
			File.WriteAllText (source, "keep");
			string tempPath;

			using (var request = new FilterRequest (new SafeUri (source))) {
				tempPath = request.TempUri ().LocalPath;
				Assert.IsTrue (File.Exists (tempPath));
				Assert.AreNotEqual (source, tempPath);
			}

			Assert.IsFalse (File.Exists (tempPath));
		}

		[TestMethod]
		public void TempUri_WithExtension_UsesThatExtension ()
		{
			string source = Path.Combine (directories.Create ("filter-src"), "photo.png");
			File.WriteAllText (source, "keep");

			string tempPath;
			using (var request = new FilterRequest (new SafeUri (source))) {
				tempPath = request.TempUri ("jpg").LocalPath;
				Assert.AreEqual (".jpg", Path.GetExtension (tempPath));
				Assert.IsTrue (File.Exists (tempPath));
			}

			Assert.IsFalse (File.Exists (tempPath));
		}

		[TestMethod]
		public void Preserve_StopsDisposeFromDeletingTheTempFile ()
		{
			string source = Path.Combine (directories.Create ("filter-src"), "photo.jpg");
			File.WriteAllText (source, "keep");
			string tempPath;

			using (var request = new FilterRequest (new SafeUri (source))) {
				var temp = request.TempUri ();
				tempPath = temp.LocalPath;
				request.Preserve (temp);
			}

			Assert.IsTrue (File.Exists (tempPath));
			File.Delete (tempPath);
		}

		[TestMethod]
		public void SettingCurrent_ToANewUri_DeletesThatFileOnDispose ()
		{
			string dir = directories.Create ("filter-src");
			string source = Path.Combine (dir, "photo.jpg");
			string derived = Path.Combine (dir, "derived.jpg");
			File.WriteAllText (source, "keep");
			File.WriteAllText (derived, "temp");

			using (var request = new FilterRequest (new SafeUri (source)))
				request.Current = new SafeUri (derived);

			Assert.IsTrue (File.Exists (source));
			Assert.IsFalse (File.Exists (derived));
		}
	}
}

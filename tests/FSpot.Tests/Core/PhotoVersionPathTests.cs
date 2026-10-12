// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Utils;
using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoVersionPathTests
	{
		string directory;

		[TestInitialize]
		public void CreateDirectory ()
		{
			directory = Path.Combine (Path.GetTempPath (), "fspot-version-" + Guid.NewGuid ().ToString ("n"));
			Directory.CreateDirectory (directory);
		}

		[TestCleanup]
		public void DeleteDirectory ()
		{
			if (directory != null && Directory.Exists (directory))
				Directory.Delete (directory, true);
		}

		[TestMethod]
		public void ALiteralPercentInTheFileNameIsThePhotoName ()
		{
			Photo photo = CatalogPhoto ("file:///lib/", "a%2520b.jpg");

			Assert.AreEqual ("a%20b.jpg", photo.Name);
		}

		[TestMethod]
		public void AVersionOfAHashNamedPhotoMatchesTheImportedFile ()
		{
			Photo photo = PhotoOnDisk ("IMG #1.jpg");

			uint id = photo.CreateVersion ("Modified", Photo.OriginalVersionId, true);
			SafeUri version = photo.VersionUri (id);

			Assert.IsTrue (File.Exists (version.AbsolutePath));
			Assert.AreEqual (SafeUri.FromPath (version.AbsolutePath), version);
			Assert.Contains ("IMG #1 (Modified).jpg", version.AbsolutePath.Replace ('\\', '/'));
		}

		[TestMethod]
		public void AVersionLabelWithASpaceMatchesTheImportedFile ()
		{
			Photo photo = PhotoOnDisk ("sample.jpg");

			uint id = photo.CreateVersion ("My Edit", Photo.OriginalVersionId, true);
			SafeUri version = photo.VersionUri (id);

			Assert.AreEqual (SafeUri.FromPath (version.AbsolutePath), version);
			Assert.Contains ("sample (My Edit).jpg", version.AbsolutePath.Replace ('\\', '/'));
		}

		[TestMethod]
		public void AModifiedVersionOfAPercentNamedPhotoDoesNotUseTheDecodedTwin ()
		{
			Photo photo = PhotoOnDisk ("a%20b.jpg");
			string decoy = Path.Combine (directory, "a b (Modified).jpg");
			File.WriteAllBytes (decoy, new byte[] { 1 });

			uint id = photo.CreateDefaultModifiedVersion (Photo.OriginalVersionId, true);
			SafeUri version = photo.VersionUri (id);

			Assert.AreEqual ("a%20b.jpg", photo.Name);
			Assert.AreEqual (SafeUri.FromPath (version.AbsolutePath), version);
			Assert.AreEqual (Path.Combine (directory, "a%20b (Modified).jpg").Replace ('\\', '/'), version.AbsolutePath.Replace ('\\', '/'));
			Assert.IsTrue (File.Exists (decoy));
		}

		[TestMethod]
		public void ReparentKeepsALiteralPercentInTheVersionLabel ()
		{
			Photo parent = PhotoOnDisk ("photo.jpg");
			SafeUri edited = SafeUri.FromPath (Path.Combine (directory, "photo (take%20two).jpg"));
			var version = new PhotoVersion (parent, 2, edited.GetBaseUri (), edited.GetFilename (), string.Empty, "Original", true);

			uint id = parent.CreateReparentedVersion (version);

			Assert.AreEqual ("take%20two", parent.GetVersion (id).Name);
		}

		Photo PhotoOnDisk (string fileName)
		{
			string source = ImageTestHelper.CreateTempFile ("taglib-sample.jpg").AbsolutePath;
			string path = Path.Combine (directory, fileName);
			File.Copy (source, path, true);
			SafeUri uri = SafeUri.FromPath (path);
			Photo photo = CatalogPhoto (uri.GetBaseUri ().AbsoluteUri, uri.GetFilename ());
			photo.AllVersionsLoaded = true;
			return photo;
		}

		static Photo CatalogPhoto (string baseUri, string filename)
		{
			var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri (baseUri, true), filename, string.Empty, "Original", true);
			return photo;
		}
	}
}

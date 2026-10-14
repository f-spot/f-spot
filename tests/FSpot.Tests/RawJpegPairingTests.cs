// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class RawJpegPairingTests
	{
		[TestMethod]
		public void Merge_KeepsAHashInTheJpegPathAndNamesTheVersionJpeg ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-pair-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
			string rawPath = Path.Combine (root, "a#b.cr2");
			string jpegPath = Path.Combine (root, "a#b.jpg");
			string editedPath = Path.Combine (root, "a#b (Edited).jpg");
			File.WriteAllText (rawPath, "raw");
			File.WriteAllText (jpegPath, "jpeg");
			File.WriteAllText (editedPath, "edited");
			try {
				Photo raw = PhotoAt (rawPath, 1);
				Photo jpeg = PhotoAt (jpegPath, 2);
				SafeUri edited = SafeUri.FromPath (editedPath);
				jpeg.AddVersion (edited.GetBaseUri (), edited.GetFilename (), "Edited", false);
				jpeg.AddTagUnsafely (new Tag (null, 4, "Beach"));

				var pairs = RawJpegPairing.Find (new[] { jpeg, raw }, IsRaw, IsJpeg);

				Assert.HasCount (1, pairs);
				RawJpegPairing.Merge (pairs[0].Raw, pairs[0].Jpeg);

				PhotoVersion jpegVersion = raw.Versions.Cast<PhotoVersion> ().Single (version => version.Name == "Jpeg");
				Assert.AreEqual ("a#b.jpg", Path.GetFileName (jpegVersion.Uri.AbsolutePath));
				Assert.Contains ("#", jpegVersion.Uri.AbsolutePath);
				Assert.AreEqual ("a%23b.jpg", jpegVersion.Uri.GetFilename ());
				Assert.IsNotNull (raw.Versions.Cast<PhotoVersion> ().Single (version => version.Name == "Edited"));
				Assert.AreEqual ("Beach", raw.Tags.Single ().Name);
				Assert.IsEmpty (jpeg.VersionIds);
				Assert.IsTrue (raw.Changes.DataChanged);
				Assert.IsTrue (File.Exists (jpegPath));
				Assert.IsTrue (File.Exists (editedPath));
				Assert.IsTrue (File.Exists (rawPath));
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Find_DoesNotPairARawWithAJpegInAnotherDirectory ()
		{
			var pairs = RawJpegPairing.Find (new[] {
				PhotoAt ("/a/shot.cr2", 1),
				PhotoAt ("/b/shot.jpg", 2)
			}, IsRaw, IsJpeg);

			Assert.IsEmpty (pairs);
		}

		[TestMethod]
		public void Find_KeepsTheLaterJpegWhenAnEarlierOneSharedTheStem ()
		{
			Photo early = PhotoAt ("/a/shot.jpg", 1);
			Photo raw = PhotoAt ("/b/shot.cr2", 2);
			Photo later = PhotoAt ("/b/shot.jpg", 3);

			var pairs = RawJpegPairing.Find (new[] { later, early, raw }, IsRaw, IsJpeg);

			Assert.HasCount (1, pairs);
			Assert.AreSame (raw, pairs[0].Raw);
			Assert.AreSame (later, pairs[0].Jpeg);
		}

		[TestMethod]
		public void Find_ClassifiesByTheDefaultVersion ()
		{
			Photo developed = PhotoAt ("/dir/a.jpg", 1);
			SafeUri rawUri = SafeUri.FromPath ("/dir/a.cr2");
			uint rawVersion = developed.AddVersion (rawUri.GetBaseUri (), rawUri.GetFilename (), "Raw", false);
			developed.DefaultVersionId = rawVersion;
			Photo jpeg = PhotoAt ("/dir/a.jpg", 2);

			var pairs = RawJpegPairing.Find (new[] { jpeg, developed }, IsRaw, IsJpeg);

			Assert.HasCount (1, pairs);
			Assert.AreSame (developed, pairs[0].Raw);
			Assert.AreSame (jpeg, pairs[0].Jpeg);
		}

		static bool IsRaw (SafeUri uri)
			=> string.Equals (uri.GetExtension (), ".cr2", StringComparison.OrdinalIgnoreCase);

		static bool IsJpeg (SafeUri uri)
			=> string.Equals (uri.GetExtension (), ".jpg", StringComparison.OrdinalIgnoreCase);

		static Photo PhotoAt (string path, uint id)
		{
			SafeUri uri = SafeUri.FromPath (path);
			var photo = new Photo (null, null, id, DateTimeUtil.FromDateTime (new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
			return photo;
		}
	}
}

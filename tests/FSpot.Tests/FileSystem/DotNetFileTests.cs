using FSpot.FileSystem;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.FileSystem
{
	[TestClass]
	public class DotNetFileTests
	{
		readonly DotNetFile file = new DotNetFile ();

		[TestMethod]
		[DataRow ("file:///photos/album.xml", "text/xml")]
		[DataRow ("file:///photos/album.XML", "text/xml")]
		[DataRow ("file:///photos/feed.rss", "application/rss+xml")]
		[DataRow ("file:///photos/notes.txt", "text/plain")]
		public void GetMimeType_FeedExtensions_MatchesUriCollectionRssPath (string uri, string expectedMime)
		{
			var mime = file.GetMimeType (new SafeUri (uri));

			Assert.AreEqual (expectedMime, mime);
		}

		[TestMethod]
		[DataRow ("file:///photos/photo.jpg", "image/jpeg")]
		[DataRow ("file:///photos/photo.JPG", "image/jpeg")]
		[DataRow ("file:///photos/photo.png", "image/png")]
		[DataRow ("file:///photos/photo.xmp", "application/rdf+xml")]
		public void GetMimeType_KnownImageExtensions_ReturnsRegisteredImageType (string uri, string expectedMime)
		{
			var mime = file.GetMimeType (new SafeUri (uri));

			Assert.AreEqual (expectedMime, mime);
		}

		[TestMethod]
		[DataRow ("file:///photos/album")]
		[DataRow ("file:///photos/album.bin")]
		public void GetMimeType_UnknownOrMissingExtension_ReturnsOctetStream (string uri)
		{
			var mime = file.GetMimeType (new SafeUri (uri));

			Assert.AreEqual ("application/octet-stream", mime);
		}
	}
}

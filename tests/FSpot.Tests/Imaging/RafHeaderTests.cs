// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public class RafHeaderTests
	{
		[TestMethod]
		public void OpenImageStream_ReadsACompleteEmbeddedJpeg ()
		{
			string path = Path.Combine (Path.GetTempPath (), "fspot-raf-" + Path.GetRandomFileName ());
			try {
				var bytes = new byte[120];
				bytes[0x54] = 0;
				bytes[0x55] = 0;
				bytes[0x56] = 0;
				bytes[0x57] = 100;
				bytes[0x58] = 0;
				bytes[0x59] = 0;
				bytes[0x5A] = 0;
				bytes[0x5B] = 4;
				bytes[100] = 1;
				bytes[101] = 2;
				bytes[102] = 3;
				bytes[103] = 4;
				File.WriteAllBytes (path, bytes);

				using Stream image = new RafImageFile (SafeUri.FromPath (path)).OpenImageStream ();
				var read = new byte[4];
				Assert.AreEqual (4, image.Read (read, 0, 4));
				Assert.AreSequenceEqual (new byte[] { 1, 2, 3, 4 }, read);
			} finally {
				if (File.Exists (path))
					File.Delete (path);
			}
		}

		[TestMethod]
		public void OpenImageStream_ThrowsWhenTheHeaderIsTruncated ()
		{
			string path = Path.Combine (Path.GetTempPath (), "fspot-raf-short-" + Path.GetRandomFileName ());
			try {
				File.WriteAllBytes (path, new byte[10]);
				var file = new RafImageFile (SafeUri.FromPath (path));
				Assert.ThrowsExactly<EndOfStreamException> (() => file.OpenImageStream ());
			} finally {
				if (File.Exists (path))
					File.Delete (path);
			}
		}
	}
}

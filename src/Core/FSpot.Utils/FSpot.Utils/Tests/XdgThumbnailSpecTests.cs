#if ENABLE_TESTS
using NUnit.Framework;
using System;
using Hyena;
using FSpot;

namespace FSpot.Utils.Tests
{
    [TestFixture]
    public class XdgThumbnailSpecTests
	{
		[SetUp]
        public void Initialize () {
            GLib.GType.Init ();
        }

		[Test]
		public void TestMissingFile ()
		{
			XdgThumbnailSpec.DefaultLoader = (u) => {
				throw new Exception ("not found!");
			};

			var uri = new SafeUri ("file:///invalid");
			var pixbuf = XdgThumbnailSpec.LoadThumbnail (uri, ThumbnailSize.Large);
			Assert.IsNull (pixbuf);
		}
	}
}
#endif

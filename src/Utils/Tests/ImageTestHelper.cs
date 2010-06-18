#if ENABLE_TESTS
using NUnit.Framework;
using System;
using Hyena;
using TagLib;

namespace FSpot.Utils.Tests
{
	public static class ImageTestHelper
	{
        public static SafeUri CreateTempFile (string name)
        {
            var uri = new SafeUri (Environment.CurrentDirectory + "/../tests/data/" + name);
            var file = GLib.FileFactory.NewForUri (uri);

            var tmp = System.IO.Path.GetTempFileName ()+".jpg"; // hack!
            var uri2 = new SafeUri (tmp);
            var file2 = GLib.FileFactory.NewForUri (uri2);
            file.Copy (file2, GLib.FileCopyFlags.Overwrite, null, null);
            return uri2;
        }

        public static void DeleteTempFile (SafeUri uri)
        {
            var file = GLib.FileFactory.NewForUri (uri);
            file.Delete ();
        }
	}
}
#endif

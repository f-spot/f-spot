//
// ImageTestHelper.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Hyena;
using NUnit.Framework;

namespace FSpot.Utils.Tests
{
	public static class ImageTestHelper
	{
		public static string TestDir = TestContext.CurrentContext.TestDirectory;
		public static string TestDataLocation = "TestData";

        public static SafeUri CreateTempFile (string name)
        {
            var uri = new SafeUri (Paths.Combine (TestDir, TestDataLocation, name));
            var file = GLib.FileFactory.NewForUri (uri);

            var tmp = System.IO.Path.GetTempFileName ()+".jpg"; // hack!
            var uri2 = new SafeUri (tmp);
            var file2 = GLib.FileFactory.NewForUri (uri2);
            file.Copy (file2, GLib.FileCopyFlags.Overwrite, null, null);
            return uri2;
        }

        public static SafeUri CopySidecarToTest (SafeUri uri, string filename)
        {
            var target = uri.ReplaceExtension (".xmp");

            var orig_uri = new SafeUri (Paths.Combine (TestDir, TestDataLocation, filename));
            var file = GLib.FileFactory.NewForUri (orig_uri);
            var file2 = GLib.FileFactory.NewForUri (target);
            file.Copy (file2, GLib.FileCopyFlags.Overwrite, null, null);
            return target;
        }

        public static void DeleteTempFile (SafeUri uri)
        {
            var file = GLib.FileFactory.NewForUri (uri);
            file.Delete ();
        }
	}
}
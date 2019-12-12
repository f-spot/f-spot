//
// ImageTestHelper.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2019 Stephen Shaw
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

using System.IO;

using Hyena;

using NUnit.Framework;

namespace FSpot.Utils.Tests
{
	public static class ImageTestHelper
	{
		static readonly string TestDir = TestContext.CurrentContext.TestDirectory;
		static readonly string TestDataLocation = "TestData";

		public static SafeUri CreateTempFile (string name)
		{
			var uri = new SafeUri (Paths.Combine (TestDir, TestDataLocation, name));

			var tmp = Path.GetTempFileName () + ".jpg"; // hack!
			var uri2 = new SafeUri (tmp);

			File.Copy (uri.AbsolutePath, uri2.AbsolutePath, true);
			return uri2;
		}

		public static SafeUri CopySidecarToTest (SafeUri uri, string filename)
		{
			var source = new SafeUri (Paths.Combine (TestDir, TestDataLocation, filename)).AbsolutePath;
			var destination = uri.ReplaceExtension (".xmp");

			File.Copy (source, destination.AbsolutePath, true);

			return destination;
		}

		public static void DeleteTempFile (SafeUri uri)
		{
			if (File.Exists (uri.AbsolutePath))
				File.Delete (uri.AbsolutePath);
		}
	}
}

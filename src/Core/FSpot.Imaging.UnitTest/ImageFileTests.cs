//
// ImageFileTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
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

using Hyena;
using NUnit.Framework;

namespace FSpot.Imaging.UnitTest
{
	[TestFixture]
	public class ImageFileTests
	{
		[Test]
		public void TestIsJpegRawPair ()
		{
			var jpeg = new SafeUri ("file:///a/photo.jpeg");
			var jpg = new SafeUri ("file:///a/photo.jpg");

			var nef = new SafeUri ("file:///a/photo.nef");
			var nef2 = new SafeUri ("file:///b/photo.nef");
			var crw = new SafeUri ("file:///a/photo.crw");
			var crw2 = new SafeUri ("file:///a/photo2.jpeg");

			var factory = new ImageFileFactory (null);

			// both jpegs
			Assert.IsFalse (factory.IsJpegRawPair (jpeg, jpg));
			// both raw
			Assert.IsFalse (factory.IsJpegRawPair (nef, crw));
			// different filename
			Assert.IsFalse (factory.IsJpegRawPair (jpeg, crw2));
			// different basedir
			Assert.IsFalse (factory.IsJpegRawPair (jpeg, nef2));

			Assert.IsTrue (factory.IsJpegRawPair (jpeg, nef));
			Assert.IsTrue (factory.IsJpegRawPair (jpeg, crw));
			Assert.IsTrue (factory.IsJpegRawPair (jpg, nef));
		}
	}
}

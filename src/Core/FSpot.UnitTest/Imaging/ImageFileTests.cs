//
// ImageFileTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

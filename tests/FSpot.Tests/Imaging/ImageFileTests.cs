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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public class ImageFileTests
	{
		[TestMethod]
		[DataRow ("file.arw")]
		[DataRow ("file.crw")]
		[DataRow ("file.cr2")]
		[DataRow ("file.dng")]
		[DataRow ("file.mrw")]
		[DataRow ("file.nef")]
		[DataRow ("file.orf")]
		[DataRow ("file.pef")]
		[DataRow ("file.raw")]
		[DataRow ("file.raf")]
		[DataRow ("file.rw2")]
		public void IsRaw_RegisteredRawExtension_ReturnsTrue (string uri)
		{
			var factory = new ImageFileFactory (null);

			Assert.IsTrue (factory.IsRaw (new SafeUri (uri, true)));
		}

		[TestMethod]
		[DataRow ("file.jpg")]
		[DataRow ("file.jpeg")]
		[DataRow ("file.jpe")]
		[DataRow ("file.jfi")]
		[DataRow ("file.jfif")]
		[DataRow ("file.jif")]
		public void IsJpeg_RegisteredJpegExtension_ReturnsTrue (string uri)
		{
			var factory = new ImageFileFactory (null);

			Assert.IsTrue (factory.IsJpeg (new SafeUri (uri, true)));
		}

		[TestMethod]
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

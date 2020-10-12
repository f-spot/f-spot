//
// TestOf_ImageFile.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (c) 2012 Stephen Shaw
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using Hyena;

namespace FSpot.Imaging
{
	[TestFixture]
	public class TestOf_ImageFile
	{
		List<string> _imageTypes;
		[SetUp]
		public void Initialize () {
			GLib.GType.Init ();
			var factory = new ImageFileFactory (null);
			_imageTypes = factory.UnitTestImageFileTypes();
		}

		[TestCase("file.arw")]
		[TestCase("file.crw")]
		[TestCase("file.cr2")]
		[TestCase("file.dng")]
		[TestCase("file.mrw")]
		[TestCase("file.nef")]
		[TestCase("file.orf")]
		[TestCase("file.pef")]
		[TestCase("file.raw")]
		[TestCase("file.raf")]
		[TestCase("file.rw2")]
		public void TestIfUriIsRaw (string uri)
		{
			var factory = new ImageFileFactory (null);
			var result = factory.IsRaw(new SafeUri(uri, true));
			Assert.That(result);
		}

		[TestCase("file.jpg")]
		[TestCase("file.jpeg")]
		[TestCase("file.jpe")]
		[TestCase("file.jfi")]
		[TestCase("file.jfif")]
		[TestCase("file.jif")]
		public void TestIfUriJpeg (string uri)
		{
			var factory = new ImageFileFactory (null);
			var result = factory.IsJpeg(new SafeUri(uri, true));
			Assert.That(result);
		}

		[Test]
		public void CheckLoadableTypes ()
		{
			bool missing = false;

			// Test that we have loaders defined for all Taglib# parseable types.
			foreach (var key in TagLib.FileTypes.AvailableTypes.Keys)
			{
				Type type = TagLib.FileTypes.AvailableTypes [key];
				if (!type.IsSubclassOf (typeof (TagLib.Image.File))) {
					continue;
				}

				var test_key = key;
				if (key.StartsWith ("taglib/")) {
					test_key = "." + key.Substring (7);
				}

				if (!_imageTypes.Contains (test_key)) {
					Log.Information ($"Missing key for {test_key}");
					missing = true;
				}
			}

			Assert.IsFalse (missing, "No missing loaders for Taglib# parseable files.");
		}

		[Test]
		public void CheckTaglibSupport ()
		{
			bool missing = false;
			var missingTypes = new List<string> ();

			foreach (var key in _imageTypes) {
				string type = key;
				if (type.StartsWith ("."))
					type = $"taglib/{type.Substring (1)}";

				if (!TagLib.FileTypes.AvailableTypes.ContainsKey (type)) {
					Log.Information ($"Missing type support in Taglib# for {type}");
					missingTypes.Add (type);
					missing = true;
				}
			}

			Assert.That (missingTypes.Count == 6, string.Join (",", missingTypes));

			Assert.IsTrue (missing, $"There are {missingTypes.Count} missing type support in Taglib#.");
		}
	}
}

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

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FSpot.Imaging
{
	[TestClass]
	public class TestOf_ImageFile
	{
		List<string> _imageTypes;
		[TestInitialize]
		public void Initialize ()
		{
			var factory = new ImageFileFactory (null);
			_imageTypes = factory.UnitTestImageFileTypes ();
		}

		[TestMethod]
		public void CheckLoadableTypes ()
		{
			bool missing = false;

			// Test that we have loaders defined for all Taglib# parseable types.
			foreach (var key in TagLib.FileTypes.AvailableTypes.Keys) {
				Type type = TagLib.FileTypes.AvailableTypes[key];
				if (!type.IsSubclassOf (typeof (TagLib.Image.File))) {
					continue;
				}

				var test_key = key;
				if (key.StartsWith ("taglib/")) {
					test_key = "." + key.Substring (7);
				}

				if (!_imageTypes.Contains (test_key)) {
					Logger.Log.Information ($"Missing key for {test_key}");
					missing = true;
				}
			}

			Assert.IsFalse (missing, "No missing loaders for Taglib# parseable files.");
		}

		[TestMethod]
		public void CheckTaglibSupport ()
		{
			bool missing = false;
			var missingTypes = new List<string> ();

			foreach (var key in _imageTypes) {
				string type = key;
				if (type.StartsWith ("."))
					type = $"taglib/{type.Substring (1)}";

				if (!TagLib.FileTypes.AvailableTypes.ContainsKey (type)) {
					Logger.Log.Information ($"Missing type support in Taglib# for {type}");
					missingTypes.Add (type);
					missing = true;
				}
			}

			Assert.HasCount (6, missingTypes, string.Join (",", missingTypes));

			Assert.IsTrue (missing, $"There are {missingTypes.Count} missing type support in Taglib#.");
		}
	}
}

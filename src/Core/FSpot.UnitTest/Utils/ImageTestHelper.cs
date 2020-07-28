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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

//
// RecursiveFileEnumeratorTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using FSpot.FileSystem;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FSpot.Tests.FileSystem
{
	[TestClass]
	public class RecursiveFileEnumeratorTests
	{
		static readonly string TestDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		static readonly string TestDataLocation = "TestData";

		public static SafeUri GetTestDataDir ()
		{
			var uri = new SafeUri (Path.Combine (TestDir, TestDataLocation));
			return uri;
		}

		[TestMethod]
		public void FourFilesAreFound ()
		{
			var result = new RecursiveFileEnumerator (GetTestDataDir (), new DotNetFileSystem ()).ToArray ();

			Assert.HasCount (4, result);
		}
	}
}

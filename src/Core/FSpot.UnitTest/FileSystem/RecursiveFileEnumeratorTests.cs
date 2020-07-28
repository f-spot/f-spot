//
// RecursiveFileEnumeratorTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using FSpot.FileSystem;

using Hyena;

using NUnit.Framework;
using Shouldly;

namespace FSpot.UnitTest.FileSystem
{
	[TestFixture]
	public class RecursiveFileEnumeratorTests
	{
		static readonly string TestDir = TestContext.CurrentContext.TestDirectory;
		static readonly string TestDataLocation = "TestData";

		public static SafeUri GetTestDataDir ()
		{
			var uri = new SafeUri (Paths.Combine (TestDir, TestDataLocation));
			return uri;
		}

		[Test]
		public void FourFilesAreFound ()
		{
			var result = new RecursiveFileEnumerator (GetTestDataDir (), new DotNetFileSystem ()).ToArray ();

			result.Length.ShouldBe (4);
			result.ShouldNotBeNull();
		}
	}
}

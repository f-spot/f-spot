// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	[TestClass]
	public sealed class LcmsNativeLibraryTests
	{
		[TestMethod]
		public void Candidates_IncludeTheAppDirectoryAndUsualInstallPaths ()
		{
			var candidates = LcmsNativeLibrary.Candidates ();

			Assert.Contains (Path.Combine (AppContext.BaseDirectory, "liblcms2.2.dylib"), candidates);
			Assert.Contains ("/opt/homebrew/lib/liblcms2.2.dylib", candidates);
			Assert.Contains ("/usr/local/lib/liblcms2.2.dylib", candidates);
			Assert.Contains ("liblcms2.so.2", candidates);
			Assert.Contains ("lcms2.dll", candidates);
		}

		[TestMethod]
		public void Register_CanBeCalledMoreThanOnce ()
		{
			LcmsNativeLibrary.Register ();
			LcmsNativeLibrary.Register ();
		}
	}
}

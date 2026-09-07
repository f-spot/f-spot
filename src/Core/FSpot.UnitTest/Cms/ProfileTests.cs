//
// ProfileTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.UnitTest
{
	[TestClass]
	public class ProfileTests
	{
		// ColorManagement.AddProfilesByExtension walks a whole directory and catches
		// CmsException per file. Anything else escapes and abandons the remaining
		// profiles, so the exception type here is a contract, not a detail. These cases
		// fail before reaching lcms, which is why they need no native library.

		[TestMethod]
		public void Profile_ThrowsCmsExceptionWhenTheFileIsMissing ()
		{
			Assert.ThrowsExactly<CmsException> (() => new Profile (MissingPath ()));
		}

		[TestMethod]
		public void Profile_ThrowsCmsExceptionWhenThePathIsADirectory ()
		{
			Assert.ThrowsExactly<CmsException> (() => new Profile (Path.GetTempPath ()));
		}

		[TestMethod]
		public void Profile_KeepsTheUnderlyingIoFailureAsInnerException ()
		{
			var exception = Assert.ThrowsExactly<CmsException> (() => new Profile (MissingPath ()));

			Assert.IsNotNull (exception.InnerException, "the IO failure should not be discarded");
		}

		[TestMethod]
		public void Profile_NamesTheOffendingPath ()
		{
			string path = MissingPath ();

			var exception = Assert.ThrowsExactly<CmsException> (() => new Profile (path));

			Assert.Contains (Path.GetFileName (path), exception.Message);
		}

		[TestMethod]
		public void Profile_ThrowsCmsExceptionWhenTheBufferIsEmpty ()
		{
			Assert.ThrowsExactly<CmsException> (() => new Profile (Array.Empty<byte> ()));
		}

		[TestMethod]
		public void Profile_ThrowsCmsExceptionWhenTheFileIsEmpty ()
		{
			string path = MissingPath ();
			File.WriteAllBytes (path, Array.Empty<byte> ());

			try {
				Assert.ThrowsExactly<CmsException> (() => new Profile (path));
			} finally {
				File.Delete (path);
			}
		}

		static string MissingPath () =>
			Path.Combine (Path.GetTempPath (), $"f-spot-no-such-profile-{Guid.NewGuid ()}.icc");
	}
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// ColorManagement.Profiles is hit from draw handlers and from the preference
	// dialog's constructor. An escaping DllNotFoundException there is either an
	// abort (Import) or a menu click that does nothing (Preferences). This class
	// does not call LcmsNative.Require: the whole point is the missing-library path.
	[TestClass]
	public class ProfileCatalogBuildTests
	{
		[TestMethod]
		[DynamicData (nameof (MissingLibraryFailures))]
		public void Build_ReturnsAnEmptyCatalogWhenLittleCmsCannotLoad (Exception failure, string because)
		{
			var catalog = ProfileCatalog.Build (_ => throw failure);

			Assert.IsEmpty (catalog.Profiles, because);
		}

		[TestMethod]
		public void Build_StillThrowsWhenPopulateFailsForAnyOtherReason ()
		{
			Assert.ThrowsExactly<InvalidOperationException> (
				() => ProfileCatalog.Build (_ => throw new InvalidOperationException ("not a load failure")));
		}

		public static IEnumerable<(Exception failure, string because)> MissingLibraryFailures =>
		[
			(new DllNotFoundException ("liblcms2.2.dylib"), "no library at all"),
			(new EntryPointNotFoundException ("cmsGetEncodedCMMversion"),
				"a liblcms2 too old for the entry point the probe calls"),
			(new BadImageFormatException ("liblcms2.2.dylib"),
				"an architecture mismatch, such as arm64 Homebrew under x86_64 Mono"),
			(new TypeInitializationException ("lcmsNET.Cms", new DllNotFoundException ("lcms2")),
				"the same failure raised from a static constructor"),
		];
	}
}

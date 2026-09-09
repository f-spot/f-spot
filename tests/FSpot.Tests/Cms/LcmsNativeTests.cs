//
// LcmsNativeTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// LcmsNative decides whether a machine simply has no liblcms2 -- in which case the
	// Cms tests report Inconclusive -- or whether something genuinely broke. Getting
	// that wrong in the second direction is the harmless one; getting it wrong in the
	// first turns "no library here" into a wall of failures with no explanation.
	[TestClass]
	public class LcmsNativeTests
	{
		[TestMethod]
		[DynamicData (nameof (MissingLibraryFailures))]
		public void IsMissingLibrary_RecognisesEveryShapeOfLoadFailure (Exception failure, string because)
		{
			Assert.IsTrue (LcmsNative.IsMissingLibrary (failure), because);
		}

		public static IEnumerable<(Exception failure, string because)> MissingLibraryFailures =>
		[
			(new DllNotFoundException ("lcms2"), "no library at all"),
			(new EntryPointNotFoundException ("cmsGetEncodedCMMversion"),
				"a liblcms2 too old for the entry point the probe calls"),
			(new BadImageFormatException ("liblcms2.2.dylib"),
				"an architecture mismatch, such as an x64 Homebrew dylib under arm64"),
			(new TypeInitializationException ("lcmsNET.Cms", new DllNotFoundException ("lcms2")),
				"the same failure raised from a static constructor"),
		];

		[TestMethod]
		public void IsMissingLibrary_DoesNotSwallowAnUnrelatedFailure ()
		{
			Assert.IsFalse (LcmsNative.IsMissingLibrary (new InvalidOperationException ()));
			Assert.IsFalse (LcmsNative.IsMissingLibrary (
				new TypeInitializationException ("lcmsNET.Cms", new InvalidOperationException ())));
		}
	}
}

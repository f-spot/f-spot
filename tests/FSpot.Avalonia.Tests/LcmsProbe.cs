// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Cms;
using FSpot.Editors;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// Same policy as FSpot.Tests LcmsNative.Require: a machine without liblcms2 skips
// colour tools, and FSPOT_REQUIRE_LCMS2 turns that into a failure.
static class LcmsProbe
{
	static readonly object gate = new ();
	static bool? available;

	public static bool TryUse (string editorId)
	{
		if (!NeedsLibrary (editorId))
			return true;
		return IsAvailable ();
	}

	public static bool IsAvailable ()
	{
		lock (gate) {
			if (available is bool known)
				return known || FailIfRequired ();

			LcmsNativeLibrary.Register ();
			try {
				_ = lcmsNET.Cms.EncodedCMMVersion;
				available = true;
			} catch (Exception e) when (ProfileCatalog.IsNativeLibraryMissing (e)) {
				available = false;
			}
		}

		return available == true || FailIfRequired ();
	}

	static bool NeedsLibrary (string editorId)
		=> editorId == PhotoEditorCatalog.Desaturate.Id
			|| editorId == PhotoEditorCatalog.Sepia.Id
			|| editorId == PhotoEditorCatalog.AutoStretch.Id
			|| editorId == PhotoEditorCatalog.Color.Id;

	static bool FailIfRequired ()
	{
		if (string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("FSPOT_REQUIRE_LCMS2")))
			return false;

		Assert.Fail ("FSPOT_REQUIRE_LCMS2 is set but liblcms2 did not load");
		return false;
	}
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using Avalonia.Controls;
using Avalonia.Media;

using FSpot.Avalonia;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// Strings.Culture stays unset. The UI culture selects the satellite, a missing
// translation stays English, and a right-to-left culture lays the window out
// from the right.
[TestClass]
[DoNotParallelize]
public sealed class AvaloniaCultureTests
{
	[UiTestMethod]
	public void CurrentLanguage_LoadsTheSatelliteAndLaysOutFromTheRight ()
	{
		AvaloniaApp.Ensure ();
		var ui = CultureInfo.CurrentUICulture;
		var culture = CultureInfo.CurrentCulture;
		var forced = Strings.Culture;
		try {
			Strings.Culture = null;
			Show ("de", "Importieren", FlowDirection.LeftToRight);
			Show ("he", "ייבוא", FlowDirection.RightToLeft);
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo ("fr-CA");
			Assert.AreEqual ("Importer", Strings.Import);
			Assert.AreEqual ("About F-Spot", Strings.AboutFSpot);
		} finally {
			CultureInfo.CurrentUICulture = ui;
			CultureInfo.CurrentCulture = culture;
			Strings.Culture = forced;
		}
	}

	static void Show (string name, string import, FlowDirection direction)
	{
		var culture = CultureInfo.GetCultureInfo (name);
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.CurrentCulture = culture;
		Assert.AreEqual (import, Strings.Import);
		Assert.AreEqual ("About F-Spot", Strings.AboutFSpot);

		var window = new ImportWindow ();
		window.Show ();
		try {
			Assert.AreEqual (import, window.Title);
			Assert.AreEqual (direction, window.FlowDirection);
		} finally {
			window.Close ();
		}
	}
}

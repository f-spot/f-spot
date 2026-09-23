// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Styling;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AppearanceThemeTests
{
	[TestMethod]
	public void Normalize_KeepsLightDarkAndSystem ()
	{
		Assert.AreEqual (AppearanceTheme.Light, AppearanceTheme.Normalize ("Light"));
		Assert.AreEqual (AppearanceTheme.Dark, AppearanceTheme.Normalize ("Dark"));
		Assert.AreEqual (AppearanceTheme.System, AppearanceTheme.Normalize ("System"));
		Assert.AreEqual (AppearanceTheme.System, AppearanceTheme.Normalize (null));
		Assert.AreEqual (AppearanceTheme.System, AppearanceTheme.Normalize ("sepia"));
	}

	[TestMethod]
	public void ToVariant_MapsTheThreeChoices ()
	{
		Assert.AreEqual (ThemeVariant.Light, AppearanceTheme.ToVariant (AppearanceTheme.Light));
		Assert.AreEqual (ThemeVariant.Dark, AppearanceTheme.ToVariant (AppearanceTheme.Dark));
		Assert.AreEqual (ThemeVariant.Default, AppearanceTheme.ToVariant (AppearanceTheme.System));
		Assert.AreEqual (ThemeVariant.Default, AppearanceTheme.ToVariant ("nope"));
	}
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class PrintPageSetupTests
{
	[TestMethod]
	public void FromPrompt_CancelKeepsThePreviousSetup ()
	{
		Assert.IsNull (PrintPageSetup.FromPrompt (null, "Landscape"));
		Assert.IsNull (PrintPageSetup.FromPrompt ("   ", "Landscape"));
		Assert.IsNull (PrintPageSetup.FromPrompt ("Letter", null));
		Assert.IsNull (PrintPageSetup.FromPrompt ("Letter", "  "));
	}

	[TestMethod]
	public void FromPrompt_PortraitIsNotLandscapeAndA4UsesA4 ()
	{
		var portrait = PrintPageSetup.FromPrompt ("Letter", "Portrait");
		Assert.IsNotNull (portrait);
		Assert.IsFalse (portrait.Landscape);
		Assert.AreEqual (215.9, portrait.PaperWidthMm, 0.01);

		var a4 = PrintPageSetup.FromPrompt ("A4", "Landscape");
		Assert.IsNotNull (a4);
		Assert.IsTrue (a4.Landscape);
		Assert.AreEqual (210, a4.PaperWidthMm, 0.01);
		Assert.AreEqual (297, a4.PaperHeightMm, 0.01);
	}
}

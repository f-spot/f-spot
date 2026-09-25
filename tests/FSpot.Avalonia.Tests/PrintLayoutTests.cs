// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Printing;
using FSpot.Avalonia.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class PrintLayoutTests
{
	[UiTestMethod]
	[DataRow (1, 1, 1)]
	[DataRow (2, 1, 2)]
	[DataRow (4, 2, 2)]
	[DataRow (9, 3, 3)]
	[DataRow (20, 4, 5)]
	[DataRow (30, 5, 6)]
	public void Grid_MatchesTheGtkPhotosPerPageChoices (int perPage, int columns, int rows)
		=> Assert.AreEqual ((columns, rows), PrintLayout.Grid (perPage));

	[UiTestMethod]
	public void PhotosPerPageChoices_AreTheSixGtkRadioButtons ()
		=> Assert.AreSequenceEqual (new[] { 1, 2, 4, 9, 20, 30 }, PrintOptions.PhotosPerPageChoices.ToArray ());

	[UiTestMethod]
	public void GridCaption_MatchesTheGtkLabels ()
	{
		Assert.AreEqual ("1", PrintLayout.GridCaption (1));
		Assert.AreEqual ("2", PrintLayout.GridCaption (2));
		Assert.AreEqual ("2 x 2", PrintLayout.GridCaption (4));
		Assert.AreEqual ("5 x 6", PrintLayout.GridCaption (30));
	}

	[UiTestMethod]
	public void PageCount_RepeatPrintsOnePagePerPhoto ()
	{
		Assert.AreEqual (3, PrintLayout.PageCount (5, new PrintOptions { PhotosPerPage = 2 }));
		Assert.AreEqual (5, PrintLayout.PageCount (5, new PrintOptions { PhotosPerPage = 4, Repeat = true }));
		Assert.AreEqual (0, PrintLayout.PageCount (0, new PrintOptions ()));
	}

	[UiTestMethod]
	public void PhotoIndex_FillsRowsThenPagesOrRepeatsOnePhoto ()
	{
		var grid = new PrintOptions { PhotosPerPage = 4 };
		Assert.AreEqual (0, PrintLayout.PhotoIndex (0, 0, 0, 6, grid));
		Assert.AreEqual (1, PrintLayout.PhotoIndex (0, 1, 0, 6, grid));
		Assert.AreEqual (2, PrintLayout.PhotoIndex (0, 0, 1, 6, grid));
		Assert.AreEqual (5, PrintLayout.PhotoIndex (1, 1, 0, 6, grid));
		Assert.IsNull (PrintLayout.PhotoIndex (1, 0, 1, 6, grid));

		var repeat = grid with { Repeat = true };
		Assert.AreEqual (3, PrintLayout.PhotoIndex (3, 1, 1, 6, repeat));
	}

	[UiTestMethod]
	public void LabelFormat_OrdersFieldsLikeCustomPrintWidget ()
	{
		Assert.AreEqual ("{0}", new PrintOptions ().LabelFormat);
		var all = new PrintOptions { PrintTags = true, PrintFileName = true, PrintDate = true, PrintTime = true, PrintComment = true };
		Assert.AreEqual ("{0}\t{4}\t{1}\t{2} {3}\t{5}", all.LabelFormat);
	}

	[UiTestMethod]
	public void Label_FillsTheChosenFields ()
	{
		var photo = new PrintPhoto ("/a.jpg", "a.jpg", "1/2/2020", "10:00", "Beach, Sun", "Nice");
		var options = new PrintOptions { CustomText = "Hi", PrintFileName = true, PrintComment = true };
		Assert.AreEqual ("Hi\ta.jpg\tNice", PrintLayout.Label (options, photo));
	}

	[UiTestMethod]
	public void Scale_ZoomCoversScaledFitsFillStretches ()
	{
		Assert.AreEqual ((4.0, 4.0), PrintLayout.Scale (PrintFitMode.Zoom, 100, 50, 200, 200));
		Assert.AreEqual ((2.0, 2.0), PrintLayout.Scale (PrintFitMode.Scaled, 100, 50, 200, 400));
		Assert.AreEqual ((2.0, 4.0), PrintLayout.Scale (PrintFitMode.Fill, 100, 50, 200, 200));
	}

	[UiTestMethod]
	public void ImageRect_CentresTheScaledImage ()
	{
		var rect = PrintLayout.ImageRect (PrintFitMode.Scaled, 100, 50, new PrintRect (0, 0, 200, 200));
		Assert.AreEqual (new PrintRect (0, 50, 200, 100), rect);
	}

	[UiTestMethod]
	public void ShouldRotate_WhenOrientationsDisagree ()
	{
		Assert.IsTrue (PrintLayout.ShouldRotate (300, 200, 100, 200));
		Assert.IsFalse (PrintLayout.ShouldRotate (200, 300, 100, 200));
	}

	[UiTestMethod]
	public void ImageArea_WhiteBordersInsetFivePercent ()
	{
		var area = PrintLayout.ImageArea (new PrintRect (0, 0, 100, 200), whiteBorders: true);
		Assert.AreEqual (2.5, area.X, 1e-9);
		Assert.AreEqual (5, area.Y, 1e-9);
		Assert.AreEqual (95, area.Width, 1e-9);
		Assert.AreEqual (190, area.Height, 1e-9);
		Assert.AreEqual (new PrintRect (0, 0, 100, 200), PrintLayout.ImageArea (new PrintRect (0, 0, 100, 200), false));
	}

	[UiTestMethod]
	public void Printable_FullPageDropsTheMargin ()
	{
		Assert.AreEqual (new PrintRect (0, 0, 600, 800), PrintLayout.Printable (600, 800, fullPage: true));
		var margins = PrintLayout.Printable (600, 800, fullPage: false);
		Assert.AreEqual (PrintLayout.MarginPoints, margins.X);
		Assert.AreEqual (600 - 2 * PrintLayout.MarginPoints, margins.Width);
	}

	[UiTestMethod]
	public void PageSetup_LandscapeTurnsThePageAndFindsThePaper ()
	{
		var a4 = PrintPageSetup.FromPaper (PrintPageSetup.Papers.First (p => p.Name == "A4"), landscape: true);
		Assert.AreEqual (297, a4.PageWidthMm, 0.01);
		Assert.AreEqual (210, a4.PageHeightMm, 0.01);
		Assert.AreEqual ("A4", a4.Paper?.Media);
		Assert.AreEqual (297 * 72 / 25.4, a4.PageWidthPoints, 0.01);
		Assert.AreEqual ("Letter", PrintPageSetup.Letter.Paper?.Media);
		Assert.IsNull (new PrintPageSetup { PaperWidthMm = 1, PaperHeightMm = 2 }.Paper);
	}
}

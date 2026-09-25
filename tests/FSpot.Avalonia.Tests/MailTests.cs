// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Mail;
using FSpot.Resources.Lang;
using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class MailSizingTests
{
	[TestMethod]
	public void Sizes_AreOriginalThenTinyToExtraLarge ()
		=> Assert.AreSequenceEqual (new[] { 0, 320, 480, 640, 800, 1024 }, MailSizing.Sizes.ToArray ());

	[TestMethod]
	public void Estimate_OriginalIsTheTotalAndSmallerSizesUseTheScale ()
	{
		var scales = MailSizing.ReferenceScales;
		Assert.AreEqual (1_000_000, MailSizing.Estimate (1_000_000, 0, scales));
		Assert.AreEqual (53_200, MailSizing.Estimate (1_000_000, 3, scales));
	}

	[TestMethod]
	public void Calibrate_ScalesTheTableByTheMediumResult ()
	{
		// The first photo shrank to twice the reference ratio, so every size doubles.
		var scales = MailSizing.Calibrate (1_000_000, 106_400);
		Assert.AreEqual (0.1064, scales[3], 1e-4);
		Assert.AreEqual (0.0372, scales[1], 1e-4);
		Assert.AreSequenceEqual (MailSizing.ReferenceScales.ToArray (), MailSizing.Calibrate (0, 5));
	}

	[TestMethod]
	public void IsJpeg_ByExtension ()
	{
		Assert.IsTrue (MailSizing.IsJpeg ("/x/a.JPG"));
		Assert.IsTrue (MailSizing.IsJpeg ("/x/a.jpeg"));
		Assert.IsFalse (MailSizing.IsJpeg ("/x/a.png"));
		Assert.IsFalse (MailSizing.IsJpeg ("/x/a.nef"));
	}

	[TestMethod]
	public void ClampIndex_FallsBackToMedium ()
	{
		Assert.AreEqual (5, MailSizing.ClampIndex (5));
		Assert.AreEqual (3, MailSizing.ClampIndex (9));
		Assert.AreEqual (3, MailSizing.ClampIndex (-1));
	}

	[TestMethod]
	public void FormatSize_UsesSiUnits ()
	{
		Assert.AreEqual ($"999 {Strings.Bytes}", MailSizing.FormatSize (999));
		Assert.AreEqual (1.5.ToString ("0.0") + " kB", MailSizing.FormatSize (1500));
		Assert.AreEqual (2.5.ToString ("0.0") + " MB", MailSizing.FormatSize (2_500_000));
	}
}

[TestClass]
public sealed class MailLauncherTests
{
	static readonly string[] Files = ["/tmp/m/a.jpg", "/tmp/m/b.jpg"];

	[TestMethod]
	public void Thunderbird_ComposesWithCommaSeparatedAttachments ()
	{
		var launch = MailLauncher.Build ("thunderbird %s", "My Photos", Files, isMac: false, isWindows: false);
		Assert.AreEqual ("thunderbird", launch.FileName);
		Assert.AreSequenceEqual (new[] { "-compose", "subject=My Photos,attachment='/tmp/m/a.jpg,/tmp/m/b.jpg'" }, launch.Arguments.ToArray ());
	}

	[TestMethod]
	public void Kmail_AttachesEachFile ()
	{
		var launch = MailLauncher.Build ("kmail %s", "S", Files, isMac: false, isWindows: false);
		Assert.AreEqual ("kmail", launch.FileName);
		Assert.AreSequenceEqual (new[] { "--composer", "--subject", "S", "--attach", "/tmp/m/a.jpg", "--attach", "/tmp/m/b.jpg" }, launch.Arguments.ToArray ());
	}

	[TestMethod]
	public void Default_UsesMailtoWithAttach ()
	{
		var launch = MailLauncher.Build ("", "My Photos", Files, isMac: false, isWindows: false);
		Assert.IsNull (launch.FileName);
		Assert.AreEqual ("mailto:?subject=My%20Photos&attach=file:///tmp/m/a.jpg&attach=file:///tmp/m/b.jpg", launch.Uri);
	}

	[TestMethod]
	public void Evolution_KeepsTheSubjectUnencoded ()
	{
		var launch = MailLauncher.Build ("evolution %s", "My Photos", Files, isMac: false, isWindows: false);
		Assert.StartsWith ("mailto:?subject=My Photos&attach=", launch.Uri);
	}

	[TestMethod]
	public void MacDefault_HandsTheFilesToMail ()
	{
		var launch = MailLauncher.Build (null, "S", Files, isMac: true, isWindows: false);
		Assert.AreEqual ("open", launch.FileName);
		Assert.AreSequenceEqual (new[] { "-a", "Mail", "/tmp/m/a.jpg", "/tmp/m/b.jpg" }, launch.Arguments.ToArray ());
	}

	[TestMethod]
	public void WindowsDefault_OpensTheMessageAndShowsTheFolder ()
	{
		var launch = MailLauncher.Build (null, "S", Files, "/tmp/m", isMac: false, isWindows: true);
		Assert.AreEqual ("mailto:?subject=S", launch.Uri);
		Assert.AreEqual ("/tmp/m", launch.RevealFolder);
	}
}

[TestClass]
public sealed class MailAttachmentsTests
{
	string directory = string.Empty;

	[TestInitialize]
	public void SetUp ()
	{
		directory = Path.Combine (Path.GetTempPath (), "fspot-mail-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (directory);
	}

	[TestCleanup]
	public void TearDown ()
		=> Directory.Delete (directory, recursive: true);

	[TestMethod]
	public void Prepare_ResizesIntoTheMailFolderUnderAUniqueName ()
	{
		string source = PrintRendererTests.WriteJpeg (directory, "shot.jpg", 1600, 800, SKColors.Green);
		string mail = Path.Combine (directory, "mail");
		Directory.CreateDirectory (mail);

		string first = MailAttachments.Prepare (source, 640, mail);
		string second = MailAttachments.Prepare (source, 640, mail);

		Assert.AreEqual (Path.Combine (mail, "shot.jpg"), first);
		Assert.AreEqual (Path.Combine (mail, "shot-1.jpg"), second);
		using var codec = SKCodec.Create (first);
		Assert.AreEqual (640, codec.Info.Width);
		Assert.AreEqual (320, codec.Info.Height);
	}

	[TestMethod]
	public void Prepare_KeepsTheOriginalForSizeZeroOrASmallPhoto ()
	{
		string source = PrintRendererTests.WriteJpeg (directory, "small.jpg", 200, 100, SKColors.Green);
		string mail = Path.Combine (directory, "mail");
		Directory.CreateDirectory (mail);

		string original = MailAttachments.Prepare (source, 0, mail);
		string small = MailAttachments.Prepare (source, 640, mail);

		Assert.AreSequenceEqual (File.ReadAllBytes (source), File.ReadAllBytes (original));
		Assert.AreSequenceEqual (File.ReadAllBytes (source), File.ReadAllBytes (small));
	}
}

[TestClass]
[DoNotParallelize]
public sealed class PrintAndMailWindowTests
{
	[TestMethod]
	public void MailWindow_NonJpegForcesOriginal ()
	{
		AvaloniaApp.Ensure ();
		var window = new MailWindow (["/tmp/a.png", "/tmp/b.jpg"], loadTray: false);
		Assert.IsTrue (window.OriginalSize.IsChecked);
		Assert.IsFalse (window.MediumSize.IsEnabled);
		Assert.AreEqual (0, window.SizeIndex);
		Assert.AreEqual ("2", window.NumberOfPictures.Text);
	}

	[TestMethod]
	public void MailWindow_RemembersTheLastSize ()
	{
		AvaloniaApp.Ensure ();
		int saved = Preferences.Get<int> (Preferences.ExportEmailSize);
		try {
			Preferences.Set (Preferences.ExportEmailSize, 5);
			var window = new MailWindow (["/tmp/a.jpg"], loadTray: false);
			Assert.IsTrue (window.XLargeSize.IsChecked);
			Assert.AreEqual (5, window.SizeIndex);
		} finally {
			Preferences.Set (Preferences.ExportEmailSize, saved);
		}
	}

	[TestMethod]
	public void PageSetupWindow_ReflectsPaperAndOrientation ()
	{
		AvaloniaApp.Ensure ();
		var a4 = FSpot.Avalonia.Services.PrintPageSetup.FromPaper (FSpot.Avalonia.Services.PrintPageSetup.Papers.First (p => p.Name == "A4"), true);
		var window = new PageSetupWindow (a4);
		Assert.IsTrue (window.Landscape.IsChecked);
		Assert.AreEqual ("A4", window.Current.Paper?.Name);
		Assert.AreEqual (string.Format (Strings.PaperSizeXxYmm, 297, 210), window.SizeCaption.Text);

		window.Portrait.IsChecked = true;
		Assert.IsFalse (window.Current.Landscape);
	}

	[TestMethod]
	public void PrintWindow_DefaultsMatchCustomPrintWidget ()
	{
		AvaloniaApp.Ensure ();
		var window = new PrintWindow ([], FSpot.Avalonia.Services.PrintPageSetup.Letter, listPrinters: false);
		var options = window.Options;
		Assert.AreEqual (1, options.PhotosPerPage);
		Assert.AreEqual (FSpot.Avalonia.Printing.PrintFitMode.Zoom, options.Fit);
		Assert.IsFalse (options.Repeat || options.CropMarks || options.FullPage || options.WhiteBorders);

		window.Ppp20.IsChecked = true;
		window.Scaled.IsChecked = true;
		window.PrintTags.IsChecked = true;
		options = window.Options;
		Assert.AreEqual (20, options.PhotosPerPage);
		Assert.AreEqual (FSpot.Avalonia.Printing.PrintFitMode.Scaled, options.Fit);
		Assert.AreEqual ("{0}\t{4}", options.LabelFormat);
	}
}

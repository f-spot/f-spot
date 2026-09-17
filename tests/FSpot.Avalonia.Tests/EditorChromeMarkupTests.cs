// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class EditorChromeMarkupTests
{
	[TestMethod]
	public void SelectionOverlay_BindsPreviewSoMapRectHasImageSize ()
	{
		var overlay = Descendants (MainWindow (), "ImageSelectionOverlay").Single ();
		string? preview = overlay.Attribute ("Preview")?.Value;

		Assert.IsFalse (string.IsNullOrWhiteSpace (preview),
			"ImageSelectionOverlay must bind Preview; MapRect with a 0×0 image never commits the rubber-band.");
		Assert.Contains ("Preview", preview, StringComparison.Ordinal);
	}

	[TestMethod]
	public void PhotoPreview_UsesLoupeOverlayForOffsetLensAndHandle ()
	{
		var window = MainWindow ();

		Assert.HasCount (1, Descendants (window, "LoupeOverlay").ToList ());
		Assert.IsEmpty (Descendants (window, "Border")
			.Where (border => border.Attribute ("Width")?.Value == "256"
				&& border.Attribute ("Height")?.Value == "256")
			.ToList ());
	}

	[TestMethod]
	public void Loupe_IsBoundToUnadornedV ()
	{
		var window = MainWindow ();
		var loupe = Descendants (window, "NativeMenuItem")
			.Single (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Loupe")
				|| (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("ToggleLoupe"));
		Assert.AreEqual ("V", loupe.Attribute ("Gesture")?.Value);

		var vBindings = Descendants (window, "KeyBinding")
			.Where (binding => binding.Attribute ("Gesture")?.Value == "V")
			.ToList ();
		Assert.IsEmpty (vBindings,
			"Avalonia KeyBindings run before KeyDown; unadorned V is handled in OnKeyDown so it does not steal from text inputs.");
	}

	[TestMethod]
	public void FindByRatingDateAndImportRoll_AreEnabledCommands ()
	{
		var find = Descendants (MainWindow (), "NativeMenuItem")
			.Where (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("By _Rating")
				|| (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("By _Date")
				|| (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("By _Import Roll"))
			.ToList ();
		Assert.HasCount (3, find);
		foreach (var item in find) {
			Assert.AreNotEqual ("False", item.Attribute ("IsEnabled")?.Value, item.Attribute ("Header")?.Value);
			Assert.IsFalse (string.IsNullOrWhiteSpace (item.Attribute ("Command")?.Value), item.Attribute ("Header")?.Value);
			Assert.DoesNotContain ("NotYet", item.Attribute ("Command")?.Value ?? string.Empty);
		}
	}

	[TestMethod]
	public void SlideshowPrintMailExport_AreNotNotYet ()
	{
		var window = MainWindow ();
		var items = Descendants (window, "NativeMenuItem")
			.Where (item => {
				string header = item.Attribute ("Header")?.Value ?? string.Empty;
				return header.Contains ("Slideshow") || header == "Print"
					|| header.Contains ("Send by") || header.Contains ("_Export");
			})
			.ToList ();
		Assert.IsGreaterThanOrEqualTo (4, items.Count);
		foreach (var item in items) {
			string command = item.Attribute ("Command")?.Value ?? string.Empty;
			Assert.DoesNotContain ("NotYet", command, item.Attribute ("Header")?.Value);
			Assert.IsFalse (string.IsNullOrWhiteSpace (command)
				&& item.Attribute ("IsEnabled")?.Value == "False", item.Attribute ("Header")?.Value);
		}

		var toolbarSlideshow = Descendants (window, "Button")
			.Single (button => (button.Attribute ("AutomationProperties.AutomationId")?.Value
				?? button.Attributes ().FirstOrDefault (a => a.Name.LocalName == "AutomationId")?.Value)
				== "Main.Toolbar.Slideshow"
				|| (button.Attribute ("Command")?.Value ?? string.Empty).Contains ("Slideshow")
				|| (button.Attribute ("Command")?.Value ?? string.Empty).Contains ("NotYet"));
		string toolbarCommand = toolbarSlideshow.Attribute ("Command")?.Value ?? string.Empty;
		Assert.DoesNotContain ("NotYet", toolbarCommand);
		Assert.Contains ("Slideshow", toolbarCommand);
	}

	[TestMethod]
	public void LoupeOverlayMarkup_HasChromePathAndLens ()
	{
		var overlay = XDocument.Load (Path.Combine (ClientRoot (), "Controls", "LoupeOverlay.axaml"));

		var names = overlay.Descendants ()
			.Select (element => element.Attribute ("Name")?.Value)
			.Where (name => name is not null)
			.ToList ();
		Assert.Contains ("Chrome", names);
		Assert.Contains ("Lens", names);
	}

	static XDocument MainWindow ()
		=> XDocument.Load (Path.Combine (ClientRoot (), "MainWindow.axaml"));

	static string ClientRoot ()
		=> Path.Combine (RepoRoot (), "src", "Clients", "FSpot.Avalonia");

	static IEnumerable<XElement> Descendants (XDocument document, string localName)
		=> document.Descendants ().Where (element => element.Name.LocalName == localName);

	static string RepoRoot ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return dir.FullName;
		}

		Assert.Fail ("F-Spot.slnx not found walking up from " + AppContext.BaseDirectory);
		return null!;
	}
}

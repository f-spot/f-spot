// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class EditorChromeMarkupTests
{
	[UiTestMethod]
	public void GridAndFilmstrip_BindTileSizeToZoom ()
	{
		string markup = File.ReadAllText (Path.Combine (ClientRoot (), "MainWindow.axaml"));
		Assert.Contains ("TileSize", markup);
		Assert.DoesNotContain ("Height=\"120\"", markup);
	}

	[UiTestMethod]
	public void PhotoGrid_IsExtendedSelectWithTabOrderAndFocusStyle ()
	{
		string main = File.ReadAllText (Path.Combine (ClientRoot (), "MainWindow.axaml"));
		Assert.Contains ("SelectionMode=\"Multiple\"", main);
		Assert.Contains ("TabIndex", main);
		string app = File.ReadAllText (Path.Combine (ClientRoot (), "App.axaml"));
		Assert.Contains ("ListBoxItem:focus", app);
		Assert.Contains ("timeline-month:focus", app);
	}

	[UiTestMethod]
	public void StatusBar_IsASingleTextLineUnderTheToolbar ()
	{
		string main = File.ReadAllText (Path.Combine (ClientRoot (), "MainWindow.axaml"));
		Assert.Contains ("AutomationIds.StatusBar", main);
		Assert.Contains ("Classes=\"zoom\"", main);
		Assert.Contains ("Padding=\"8,2\"", main);
		Assert.DoesNotContain ("Padding=\"8,6\"", main);
		string app = File.ReadAllText (Path.Combine (ClientRoot (), "App.axaml"));
		Assert.Contains ("Slider.zoom", app);
		Assert.Contains ("SliderHorizontalHeight\">16", app);
	}

	[UiTestMethod]
	public void Splitters_AreHairlinesRatherThanFilledBars ()
	{
		string app = File.ReadAllText (Path.Combine (ClientRoot (), "App.axaml"));
		Assert.Contains ("GridSplitter.hairline", app);
		Assert.Contains ("Width=\"1\"", app);
		string main = File.ReadAllText (Path.Combine (ClientRoot (), "MainWindow.axaml"));
		string import = File.ReadAllText (Path.Combine (ClientRoot (), "ImportWindow.axaml"));
		Assert.Contains ("Classes=\"hairline\"", main);
		Assert.Contains ("Classes=\"hairline\"", import);
		Assert.DoesNotContain ("Width=\"6\"", main);
		Assert.DoesNotContain ("Width=\"8\"", import);
	}

	[UiTestMethod]
	public void CopyPageSetupBackgroundAndTools_AreEnabledCommands ()
	{
		string markup = File.ReadAllText (Path.Combine (ClientRoot (), "MainWindow.axaml"));
		Assert.Contains ("CopyCommand", markup);
		Assert.Contains ("PageSetupCommand", markup);
		Assert.Contains ("SetAsBackgroundCommand", markup);
		Assert.Contains ("ShowExtensionsCommand", markup);
		Assert.DoesNotContain ("ShowToolsCommand", markup);
		Assert.DoesNotContain ("Strings.Copy}\" Gesture=\"Ctrl+C\" IsEnabled=\"False\"", markup);
		Assert.DoesNotContain ("Strings.PageSetup2}\" IsEnabled=\"False\"", markup);
		Assert.DoesNotContain ("Strings.SetAsBackground}\" IsEnabled=\"False\"", markup);
		Assert.DoesNotContain ("Strings.Tools}\" IsEnabled=\"False\"", markup);
	}

	[UiTestMethod]
	public void SelectionOverlay_BindsPreviewSoMapRectHasImageSize ()
	{
		var overlay = Descendants (MainWindow (), "ImageSelectionOverlay").Single ();
		string? preview = overlay.Attribute ("Preview")?.Value;

		Assert.IsFalse (string.IsNullOrWhiteSpace (preview),
			"ImageSelectionOverlay must bind Preview; MapRect with a 0×0 image never commits the rubber-band.");
		Assert.Contains ("Preview", preview, StringComparison.Ordinal);
	}

	[UiTestMethod]
	public void PhotoPreview_UsesLoupeOverlayForOffsetLensAndHandle ()
	{
		var window = MainWindow ();

		Assert.HasCount (1, Descendants (window, "LoupeOverlay").ToList ());
		Assert.IsEmpty (Descendants (window, "Border")
			.Where (border => border.Attribute ("Width")?.Value == "256"
				&& border.Attribute ("Height")?.Value == "256")
			.ToList ());
	}

	[UiTestMethod]
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

	[UiTestMethod]
	public void FindByRatingDateAndImportRoll_AreEnabledCommands ()
	{
		var find = Descendants (MainWindow (), "NativeMenuItem")
			.Where (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("FindByRating")
				|| (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("FindByDate")
				|| (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("FindByImportRoll"))
			.ToList ();
		Assert.HasCount (3, find);
		foreach (var item in find) {
			Assert.AreNotEqual ("False", item.Attribute ("IsEnabled")?.Value, item.Attribute ("Header")?.Value);
			Assert.IsFalse (string.IsNullOrWhiteSpace (item.Attribute ("Command")?.Value), item.Attribute ("Header")?.Value);
			Assert.DoesNotContain ("NotYet", item.Attribute ("Command")?.Value ?? string.Empty);
		}
	}

	[UiTestMethod]
	public void FindBar_DeclaresDragOverForFolderDrops ()
	{
		string markup = MainWindow ().ToString ();
		Assert.Contains ("CatalogDrop.Kind=\"FindBar\"", markup);
	}

	[UiTestMethod]
	public void MessageDialog_HasHeadingBodyAndButtons ()
	{
		var dialog = XDocument.Load (Path.Combine (ClientRoot (), "MessageDialog.axaml"));
		Assert.Contains ("HeadingBlock", dialog.ToString ());
		Assert.Contains ("BodyBlock", dialog.ToString ());
		Assert.Contains ("OkButton", dialog.ToString ());
		Assert.Contains ("CancelButton", dialog.ToString ());
		Assert.Contains ("SizeToContent", dialog.ToString ());
	}

	[UiTestMethod]
	public void TagTree_BindsPerTagIconsAndInPlaceRename ()
	{
		var window = MainWindow ();
		Assert.Contains ("IconPath", window.ToString ());
		Assert.Contains ("IsRenaming", window.ToString ());
		Assert.Contains ("BeginRenameTag", window.ToString ());
		var delete = Descendants (window, "MenuItem")
			.Single (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("DeleteTagCommand"));
		Assert.Contains ("Binding", delete.Attribute ("CommandParameter")?.Value ?? string.Empty);
	}

	[UiTestMethod]
	public void SelectionOverlay_BindsPanAndZoom ()
	{
		var overlay = Descendants (MainWindow (), "ImageSelectionOverlay").Single ();
		Assert.Contains ("PreviewZoom", overlay.Attribute ("Zoom")?.Value ?? string.Empty);
		Assert.Contains ("PreviewPanX", overlay.Attribute ("PanX")?.Value ?? string.Empty);
	}

	[UiTestMethod]
	public void HighGtkSurfaces_AreNotNotYet ()
	{
		var window = MainWindow ();
		var commands = Descendants (window, "NativeMenuItem")
			.Select (item => (Header: item.Attribute ("Header")?.Value ?? string.Empty, Command: item.Attribute ("Command")?.Value ?? string.Empty, Enabled: item.Attribute ("IsEnabled")?.Value))
			.ToList ();
		string[] high = [
			"CreateVersion", "DeleteVersion", "RenameVersion", "DetachVersion",
			"AdjustTime", "RefreshThumbnail", "ShowPreferences", "ShowExtensions",
			"HelpContents", "ImportFromCamera"
		];
		foreach (string name in high) {
			Assert.Contains (item => item.Command.Contains (name), commands, name);
		}

		foreach (string name in new[] { "CopyCommand", "PageSetupCommand", "SetAsBackgroundCommand", "ShowExtensionsCommand" })
			Assert.Contains (item => item.Command.Contains (name), commands, name);
	}

	[UiTestMethod]
	public void SlideshowPrintMailExport_AreNotNotYet ()
	{
		var window = MainWindow ();
		var items = Descendants (window, "NativeMenuItem")
			.Where (item => {
				string command = item.Attribute ("Command")?.Value ?? string.Empty;
				return command.Contains ("Slideshow") || command.Contains ("PrintCommand")
					|| command.Contains ("MailCommand") || command.Contains ("ExportCommand");
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

	[UiTestMethod]
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

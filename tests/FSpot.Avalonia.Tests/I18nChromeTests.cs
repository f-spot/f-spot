// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Settings;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class I18nChromeTests
{
	static readonly string[] UserVisibleAttributes = [
		"Header", "Content", "Title", "Text", "ToolTip.Tip", "PlaceholderText",
		"AutomationProperties.Name"
	];

	[UiTestMethod]
	public void RemainingChrome_BindsUserVisibleCopyToStrings ()
	{
		foreach (var (file, document) in ChromeDocuments ()) {
			var leaks = document.Descendants ()
				.SelectMany (element => UserVisibleAttributes
					.Select (name => (element, name, value: Attribute (element, name)))
					.Where (hit => IsHardcodedEnglish (hit.value)))
				.Select (hit => $"{Relative (file)} <{hit.element.Name.LocalName} {hit.name}=\"{hit.value}\">")
				.ToList ();

			Assert.IsEmpty (leaks,
				"User-visible chrome must come from FSpot.Strings: " + string.Join ("; ", leaks));
		}
	}

	[UiTestMethod]
	public void QueryBar_SurfacesNoMatchingPhotosFromStrings ()
	{
		var queryBar = Descendants (MainWindow (), "Border")
			.Single (border => Attribute (border, "AutomationProperties.AutomationId")?.Contains ("QueryBar") == true
				|| Attribute (border, "Classes") == "query-bar");
		string markup = queryBar.ToString ();
		Assert.Contains ("ShowQueryBarNoMatches", markup);
		Assert.Contains ("NoMatchingPhotosFound", markup);
		Assert.Contains ("lang:Strings.", markup);
	}

	[UiTestMethod]
	public void ChipLabels_DoNotConcatenateFragmentsInXaml ()
	{
		string markup = MainWindow ().ToString ();
		Assert.DoesNotContain ("FindColon}\" +", markup);
		Assert.Contains ("FindRatingLabel", markup);
		Assert.Contains ("FindDateLabel", markup);
	}

	[UiTestMethod]
	public void InfoDate_WrapsInsteadOfEllipsizing ()
	{
		var date = Descendants (MainWindow (), "TextBlock")
			.Single (block => Attribute (block, "AutomationProperties.AutomationId")?.Contains ("SidebarInfoDate") == true
				|| Attribute (block, "Text")?.Contains ("Info.Date") == true);
		string classes = Attribute (date, "Classes") ?? string.Empty;
		Assert.Contains ("info-date", classes);
		Assert.AreNotEqual ("NoWrap", Attribute (date, "TextWrapping"));
	}

	[UiTestMethod]
	public void ImportAttachTags_UsesALabelTarget ()
	{
		var import = XDocument.Load (Path.Combine (ClientRoot (), "ImportWindow.axaml"));
		var label = Descendants (import, "Label")
			.Single (element => (Attribute (element, "Content") ?? string.Empty).Contains ("AttachTags"));
		Assert.IsFalse (string.IsNullOrWhiteSpace (Attribute (label, "Target")));
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void SidebarAndFilmstrip_FollowWindowFlowDirection ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) {
			Width = 800,
			Height = 500
		};
		var viewModel = (MainWindowViewModel)window.DataContext!;
		viewModel.SeedCatalog (new FilePhoto (new SafeUri ("file:///tmp/a.jpg")));
		if (viewModel.Photos.Count > 0)
			viewModel.SelectPhoto (viewModel.Photos[0]);
		window.Show ();
		window.FlowDirection = FlowDirection.RightToLeft;
		window.UpdateLayout ();

		var sidebar = window.GetVisualDescendants ().OfType<Control> ()
			.First (control => AutomationProperties.GetAutomationId (control) == AutomationIds.Sidebar);
		var filmstrip = window.GetVisualDescendants ().OfType<Control> ()
			.First (control => AutomationProperties.GetAutomationId (control) == AutomationIds.Filmstrip);

		Assert.AreEqual (FlowDirection.RightToLeft, sidebar.FlowDirection);
		Assert.AreEqual (FlowDirection.RightToLeft, filmstrip.FlowDirection);

		var date = window.GetVisualDescendants ().OfType<TextBlock> ()
			.First (block => AutomationProperties.GetAutomationId (block) == AutomationIds.SidebarInfoDate);
		Assert.AreEqual (TextWrapping.Wrap, date.TextWrapping);
	}

	[UiTestMethod]
	public void AboutAndImport_UseLocalizedWindowTitles ()
	{
		var about = XDocument.Load (Path.Combine (ClientRoot (), "AboutWindow.axaml"));
		Assert.Contains ("Strings.", about.Root?.Attribute ("Title")?.Value ?? string.Empty);
		var import = XDocument.Load (Path.Combine (ClientRoot (), "ImportWindow.axaml"));
		Assert.Contains ("Strings.", import.Root?.Attribute ("Title")?.Value ?? string.Empty);
		Assert.IsFalse (string.IsNullOrEmpty (Strings.AboutFSpot));
		Assert.IsFalse (string.IsNullOrEmpty (Strings.Import));
	}

	[UiTestMethod]
	public void AboutViewModel_ServesVersionFromStrings ()
	{
		var about = new AboutViewModel ();
		Assert.AreEqual (string.Format (Strings.VersionX, FSpotConfiguration.Version), about.Version);
		Assert.Contains (FSpotConfiguration.Version, about.Version);
		Assert.DoesNotContain ("{0}", about.Version);
	}

	[UiTestMethod]
	public void DateAndRollPromptCopy_ComesFromStrings ()
	{
		Assert.AreEqual (Strings.DateRangePrompt, PromptCopy.DateRange);
		Assert.AreEqual (Strings.RollIdPrompt, PromptCopy.RollId);
		Assert.AreEqual (Strings.RollIdPrompt, PromptCopy.RollHint ([]));
		Assert.AreEqual ("3:June", PromptCopy.RollHint ([(3u, "June")]));
		Assert.DoesNotContain ("{0}", PromptCopy.DateRange);
	}

	[UiTestMethod]
	public void TagDeleteCopy_ComesFromStrings ()
	{
		var root = new Category (null, 1, "Root");
		Assert.AreEqual (Strings.TagIsNotEmpty, TagDelete.BlockedHeading);
		Assert.AreEqual (string.Format (Strings.CanNotDeleteTagsThatHaveTagsWihinThem_PleaseDeleteTagUnderXFirst, "Root"),
			TagDelete.BlockedMessage (root));
		Assert.AreEqual (string.Format (Strings.DeleteTagXQuestion, "Root"), TagDelete.ConfirmHeading (root));
		Assert.AreEqual (Strings.ThisRemovesTheTagFromTheCatalogAndFromAnyPhotosThatUseIt, TagDelete.ConfirmBody (0));
	}

	[UiTestMethod]
	public void ApplicationName_ComesFromStrings ()
	{
		var app = XDocument.Load (Path.Combine (ClientRoot (), "App.axaml"));
		string name = app.Root?.Attribute ("Name")?.Value ?? string.Empty;
		Assert.Contains ("Strings.FSpot", name);
	}

	static bool IsHardcodedEnglish (string? value)
	{
		if (string.IsNullOrWhiteSpace (value))
			return false;
		if (value.Contains ("{Binding", StringComparison.Ordinal)
			|| value.Contains ("{x:Static", StringComparison.Ordinal)
			|| value.Contains ("lang:Strings", StringComparison.Ordinal)
			|| value.StartsWith ('{'))
			return false;
		if (value is "×" or "▾" or "◀" or "▶" or "!" or "*")
			return false;
		return value.Any (char.IsLetter);
	}

	static string? Attribute (XElement element, string localName)
		=> element.Attribute (localName)?.Value
			?? element.Attributes ().FirstOrDefault (attribute => attribute.Name.LocalName == localName)?.Value;

	static XDocument MainWindow ()
		=> XDocument.Load (Path.Combine (ClientRoot (), "MainWindow.axaml"));

	static IEnumerable<(string file, XDocument document)> ChromeDocuments ()
	{
		string root = ClientRoot ();
		return Directory.EnumerateFiles (root, "*.axaml", SearchOption.AllDirectories)
			.Where (path => !path.Contains ($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.Select (path => (path, XDocument.Load (path)));
	}

	static IEnumerable<XElement> Descendants (XDocument document, string localName)
		=> document.Descendants ().Where (element => element.Name.LocalName == localName);

	static string ClientRoot ()
		=> Path.Combine (RepoRoot (), "src", "Clients", "FSpot.Avalonia");

	static string RepoRoot ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return dir.FullName;
		}
		Assert.Fail ("F-Spot.slnx not found");
		return null!;
	}

	static string Relative (string file) => Path.GetRelativePath (RepoRoot (), file);
}

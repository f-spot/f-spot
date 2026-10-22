// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

using FSpot;
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
	[DoNotParallelize]
	public void AboutAndImport_UseLocalizedWindowTitles ()
	{
		var about = new AboutWindow ();
		var import = new ImportWindow ();
		about.Show ();
		import.Show ();
		try {
			Assert.AreEqual (Strings.AboutFSpot, about.Title);
			Assert.AreEqual (Strings.Import, import.Title);
		} finally {
			about.Close ();
			import.Close ();
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void ImportAttachTags_LabelsTheTagBox ()
	{
		var window = new ImportWindow ();
		window.Show ();
		try {
			var box = window.FindControl<TextBox> ("AttachTagsBox");
			Assert.IsNotNull (box);
			var label = window.GetVisualDescendants ().OfType<Label> ()
				.Single (item => Equals (item.Content, Strings.AttachTags));
			Assert.AreSame (box, label.Target);
		} finally {
			window.Close ();
		}
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
	public void AboutViewModel_NamesTheLicenceAndTheRuntime ()
	{
		var about = new AboutViewModel ();
		var avalonia = typeof (global::Avalonia.Application).Assembly.GetName ().Version;

		Assert.AreEqual ("MIT License", about.Licence);
		Assert.Contains ($"Avalonia {avalonia!.Major}.{avalonia.Minor}", about.Runtime);
		Assert.Contains (System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, about.Runtime);
	}

	[TestMethod]
	public void AboutViewModel_SaysWhenCrashReportsAreOff ()
	{
		var about = new AboutViewModel ();
		Assert.AreEqual (FSpotSentry.ReportingDescription (), about.Telemetry);
		Assert.AreEqual (Strings.CrashReportsAreOff, about.Telemetry);
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
		var app = new App ();
		app.Initialize ();
		Assert.AreEqual (Strings.FSpot, app.Name);
	}
}

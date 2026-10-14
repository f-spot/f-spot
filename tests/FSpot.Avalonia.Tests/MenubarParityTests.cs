// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

using FSpot.Avalonia.Input;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Resources.Lang;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

/// <summary>
/// The menubar follows GTK main_window.ui and MainWindow.UpdateMenus: the same items,
/// the same parents, and the same enable and visibility rules. Close still closes the
/// window and Quit still exits. On macOS the Photo menu shows only Quit, because this
/// client has one window. Invert Selection stays as an Avalonia extra.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MenubarParityTests
{
	[UiTestMethod]
	public void PhotoMenu_VersionAndExportAreSubmenus_AndMailFollowsThePreference ()
	{
		var photo = OriginalPhoto ();
		bool mail = LibraryPreferences.MailToEnabled;
		LibraryPreferences.MailToEnabled = false;
		var window = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			viewModel.SeedCatalog (photo);
			Assert.IsFalse (viewModel.Photos[0].Selected, "The photo the library lands on is current without a multi-select flag.");
			window.Show ();

			var version = NativeItem (window, Strings.Version);
			Assert.IsNotNull (DynamicMenu.GetSource (version), "Version is a submenu of the photo's versions.");
			Assert.IsTrue (viewModel.CanOpenVersionMenu);
			Assert.IsTrue (version.IsEnabled);
			DynamicMenu.Fill (version);
			var versionHeaders = Headers (version.Menu);
			Assert.Contains (Strings.Original, versionHeaders);
			Assert.Contains (Strings.ParenNoEditsParen, versionHeaders);

			var export = NativeItem (window, Strings.ExportTo2);
			Assert.IsNotNull (DynamicMenu.GetSource (export));
			Assert.Contains (Strings.Folder3, Headers (export.Menu));

			var mailItem = NativeItem (window, Strings.SendByMail);
			Assert.IsFalse (mailItem.IsVisible);
			var mirrored = MirroredItem (window, Strings.SendByMail);
			Assert.IsNotNull (mirrored);
			Assert.IsFalse (mirrored.IsVisible);
		} finally {
			LibraryPreferences.MailToEnabled = mail;
			window.Close ();
		}
	}

	[UiTestMethod]
	public void PhotoMenu_OnMacShowsQuitWithoutClose_ElsewhereShowsBoth ()
	{
		var window = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		window.Show ();
		try {
			var close = NativeItem (window, Strings.Close);
			var quit = NativeItem (window, Strings.Quit);
			bool showClose = !OperatingSystem.IsMacOS ();
			Assert.AreEqual (showClose, close.IsVisible, "macOS shows one exit; other platforms keep Close and Quit.");
			Assert.IsTrue (quit.IsVisible);
			Assert.IsNotNull (close.Command);
			Assert.IsNotNull (quit.Command);

			var mirroredClose = MirroredItem (window, Strings.Close);
			var mirroredQuit = MirroredItem (window, Strings.Quit);
			Assert.IsNotNull (mirroredClose);
			Assert.IsNotNull (mirroredQuit);
			Assert.AreEqual (showClose, mirroredClose.IsVisible);
			Assert.IsTrue (mirroredQuit.IsVisible);

			var closeGesture = PlatformGestures.Retarget (new KeyGesture (Key.W, KeyModifiers.Control));
			var quitGesture = PlatformGestures.Retarget (new KeyGesture (Key.Q, KeyModifiers.Control));
			Assert.Contains (binding => Equals (binding.Gesture, closeGesture), window.KeyBindings);
			Assert.Contains (binding => Equals (binding.Gesture, quitGesture), window.KeyBindings);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void VersionCommands_FollowTheCurrentVersion_AndNeedOnePhoto ()
	{
		var viewModel = ViewModelWith (OriginalPhoto ());

		Assert.IsTrue (viewModel.CreateVersionCommand.CanExecute (null));
		Assert.IsFalse (viewModel.DeleteVersionCommand.CanExecute (null));
		Assert.IsFalse (viewModel.RenameVersionCommand.CanExecute (null));
		Assert.IsFalse (viewModel.DetachVersionCommand.CanExecute (null));

		var photo = (Photo)viewModel.Photos[0].Photo;
		uint edited = photo.AddVersion (photo.VersionUri (Photo.OriginalVersionId), "edited.jpg", "Edited");
		photo.DefaultVersionId = edited;

		Assert.IsTrue (viewModel.DeleteVersionCommand.CanExecute (null));
		Assert.IsTrue (viewModel.RenameVersionCommand.CanExecute (null));
		Assert.IsTrue (viewModel.DetachVersionCommand.CanExecute (null));

		var second = new PhotoItemViewModel (OriginalPhoto ());
		viewModel.Photos.Add (second);
		second.Selected = true;

		Assert.IsFalse (viewModel.CreateVersionCommand.CanExecute (null));
		Assert.IsFalse (viewModel.DeleteVersionCommand.CanExecute (null));
		Assert.IsFalse (viewModel.SetAsBackgroundCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void SelectionCommands_AreDisabledWithoutAPhoto_AndSharpenAndLoupeNeedEditMode ()
	{
		var viewModel = new MainWindowViewModel (null, new MainWindowViewModelTests.NullShell (), new MainWindowViewModelTests.NullThumbnails ());

		Assert.IsFalse (viewModel.PrintCommand.CanExecute (null));
		Assert.IsFalse (viewModel.MailCommand.CanExecute (null));
		Assert.IsFalse (viewModel.ExportCommand.CanExecute (null));
		Assert.IsFalse (viewModel.SelectNoneCommand.CanExecute (null));
		Assert.IsFalse (viewModel.SharpenCommand.CanExecute (null));
		Assert.IsFalse (viewModel.ToggleLoupeCommand.CanExecute (null));

		var item = new PhotoItemViewModel (OriginalPhoto ());
		viewModel.Photos.Add (item);
		item.Selected = true;
		viewModel.SelectedPhoto = item;

		Assert.IsTrue (viewModel.PrintCommand.CanExecute (null));
		Assert.IsTrue (viewModel.MailCommand.CanExecute (null));
		Assert.IsTrue (viewModel.ExportCommand.CanExecute (null));
		Assert.IsTrue (viewModel.SelectNoneCommand.CanExecute (null));
		Assert.IsFalse (viewModel.SharpenCommand.CanExecute (null));
		Assert.IsFalse (viewModel.ToggleLoupeCommand.CanExecute (null));

		viewModel.EditCommand.Execute (null);
		Assert.IsTrue (viewModel.SharpenCommand.CanExecute (null));
		Assert.IsTrue (viewModel.ToggleLoupeCommand.CanExecute (null));
		viewModel.ToggleLoupeCommand.Execute (null);
		Assert.IsTrue (viewModel.IsLoupeVisible);

		viewModel.BrowseCommand.Execute (null);
		Assert.IsFalse (viewModel.IsLoupeVisible);
		Assert.IsFalse (viewModel.SharpenCommand.CanExecute (null));
		Assert.IsFalse (viewModel.ToggleLoupeCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void ZoomAndViewComponents_UseTheGtkEndsAndTheCurrentMode ()
	{
		var window = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		try {
			window.Show ();
			var viewModel = (MainWindowViewModel)window.DataContext!;
			viewModel.Zoom = 256;
			Assert.IsFalse (viewModel.ZoomInCommand.CanExecute (null));
			Assert.IsTrue (viewModel.ZoomOutCommand.CanExecute (null));

			viewModel.Zoom = 64;
			Assert.IsTrue (viewModel.ZoomInCommand.CanExecute (null));
			Assert.IsFalse (viewModel.ZoomOutCommand.CanExecute (null));

			var timeline = NativeItem (window, Strings.Timeline);
			var filmstrip = NativeItem (window, Strings.Filmstrip);
			Assert.IsTrue (timeline.IsEnabled, "Timeline is available while browsing.");
			Assert.IsFalse (filmstrip.IsEnabled, "Filmstrip is available while editing.");
			viewModel.EditCommand.Execute (null);
			Assert.IsFalse (timeline.IsEnabled, "Timeline is unavailable while editing.");
			Assert.IsTrue (filmstrip.IsEnabled, "Filmstrip is available while editing.");
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void ToolsMenu_ShowsChangePathAndAppendsExtensions ()
	{
		const string id = "menubar-parity-tool";
		MenuExtensions.Unregister (MenuExtensionPoint.Tools, id);
		var window = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		try {
			window.Show ();
			var tools = NativeItem (window, Strings.Tools);
			Assert.IsTrue (tools.IsVisible);
			Assert.IsNull (tools.Command);
			Assert.Contains (Strings.ChangePathToPhotos, Headers (tools.Menu));
			Assert.Contains (Strings.MergeRaw, Headers (tools.Menu));
			Assert.Contains (Strings.DevelopInUFRaw2, Headers (tools.Menu));
			Assert.Contains (Strings.BatchDevelop, Headers (tools.Menu));
			Assert.Contains (Strings.MergeDb, Headers (tools.Menu));

			MenuExtensions.Register (MenuExtensionPoint.Tools, new MenuContribution (id, "Sample Tool", _ => Task.CompletedTask));
			DynamicMenu.Fill (tools);
			Assert.Contains ("Sample Tool", Headers (tools.Menu));
			Assert.Contains (Strings.ChangePathToPhotos, Headers (tools.Menu));
			Assert.Contains (Strings.MergeRaw, Headers (tools.Menu));
			Assert.Contains (Strings.DevelopInUFRaw2, Headers (tools.Menu));
			Assert.Contains (Strings.BatchDevelop, Headers (tools.Menu));
			Assert.Contains (Strings.MergeDb, Headers (tools.Menu));
		} finally {
			MenuExtensions.Unregister (MenuExtensionPoint.Tools, id);
			window.Close ();
		}
	}

	[UiTestMethod]
	public void MenubarShape_MatchesGtkPhotoEditViewAndFind ()
	{
		var window = XDocument.Load (Path.Combine (ClientRoot (), "MainWindow.axaml"));
		string markup = window.ToString ();

		Assert.DoesNotContain ("Header=\"16\"", markup);
		Assert.DoesNotContain ("Strings.DisplayFileNames", markup);
		Assert.DoesNotContain ("Strings.MatchAllTags", markup);
		Assert.DoesNotContain ("ShowToolsCommand", markup);

		var native = window.Descendants ().Where (element => element.Name.LocalName == "NativeMenuItem").ToList ();
		Assert.IsEmpty (native.Where (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("VerticalMnemonic")).ToList ());

		var version = native.Single (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Strings.Version"));
		Assert.Contains ("VersionMenu", version.ToString ());
		Assert.AreNotEqual ("False", version.Attribute ("IsEnabled")?.Value);

		var export = native.Single (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("ExportTo2"));
		Assert.Contains ("ExportMenu", export.ToString ());
		Assert.Contains ("ExportCommand", export.ToString ());
		Assert.Contains ("ExportZipCommand", export.ToString ());
		Assert.Contains ("ExportGalleryCommand", export.ToString ());

		var tools = native.Single (item => HeaderIs (item, "Tools"));
		Assert.Contains ("ChangePathCommand", tools.ToString ());
		Assert.Contains ("MergeRawJpegCommand", tools.ToString ());
		Assert.Contains ("DevelopUfrawCommand", tools.ToString ());
		Assert.Contains ("BatchDevelopCommand", tools.ToString ());
		Assert.Contains ("MergeCatalogCommand", tools.ToString ());
		Assert.IsNull (tools.Attribute ("IsVisible"));

		var attach = native.Single (item => HeaderIs (item, "AttachTag"));
		Assert.Contains ("AttachTagMenu", attach.ToString ());
		Assert.IsNull (attach.Attribute ("Command"));

		var remove = native.Single (item => HeaderIs (item, "RemoveTag"));
		Assert.Contains ("RemoveTagMenu", remove.ToString ());
		Assert.IsNull (remove.Attribute ("Command"));

		Assert.Contains (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("TagIcons"), native);
		Assert.Contains (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Hidden2"), native);
		Assert.Contains (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Small2"), native);
		Assert.Contains (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Medium2"), native);
		Assert.Contains (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Large2"), native);

		Assert.Contains (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("IncludeHighlightedTags"), native);
		Assert.Contains (item => (item.ToString ()).Contains ("FindWithMenu"), native);

		var timeline = native.Single (item => (item.Attribute ("Header")?.Value ?? string.Empty).Contains ("Strings.Timeline"));
		Assert.Contains ("CanUseTimeline", timeline.Attribute ("IsEnabled")?.Value ?? string.Empty);
		var filmstrip = native.Single (item => HeaderIs (item, "Filmstrip"));
		Assert.Contains ("CanUseFilmstrip", filmstrip.Attribute ("IsEnabled")?.Value ?? string.Empty);

		var loupe = native.Single (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("ToggleLoupe"));
		Assert.AreEqual ("V", loupe.Attribute ("Gesture")?.Value);

		Assert.Contains (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("CloseWindowCommand"), native);
		Assert.Contains (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("QuitCommand"), native);
		Assert.Contains (item => (item.Attribute ("Command")?.Value ?? string.Empty).Contains ("AttachTagCommand"), native);
	}

	[UiTestMethod]
	public void ExtensionManager_ReplicatesTheAddinDialogWithoutInstalling ()
	{
		var addin = new AddinStatus ("tools/sample", "Sample", true);
		var window = new ExtensionsWindow ([addin]);
		window.Show ();
		try {
			Assert.AreEqual ("Extension Manager", window.Title);
			Assert.AreEqual (700, window.Width);
			Assert.AreEqual (550, window.Height);
			var tabs = window.GetLogicalDescendants ().OfType<TabItem> ().ToList ();
			Assert.IsNotNull (window.GetLogicalDescendants ().OfType<TabControl> ().FirstOrDefault ());
			Assert.IsNotNull (tabs.FirstOrDefault (tab => tab.Header is string header && header.Contains ("Installed")));
			Assert.IsNotNull (tabs.FirstOrDefault (tab => tab.Header is string header && header.Contains ("Updates")));
			Assert.IsNotNull (tabs.FirstOrDefault (tab => tab.Header is string header && header.Contains ("Gallery")));

			var install = window.GetLogicalDescendants ().OfType<Button> ().FirstOrDefault (button => button.Name == "InstallFromFile");
			Assert.IsNotNull (install);
			Assert.IsFalse (install.IsEnabled);
			var updateAll = window.GetLogicalDescendants ().OfType<Button> ().FirstOrDefault (button => button.Name == "UpdateAll");
			Assert.IsNotNull (updateAll);
			Assert.IsFalse (updateAll.IsEnabled);

			var boxes = window.GetVisualDescendants ().OfType<CheckBox> ()
				.Where (check => ReferenceEquals (check.DataContext, addin))
				.Distinct ()
				.ToList ();
			Assert.HasCount (1, boxes);
			boxes[0].IsChecked = false;
			window.Close ();
			Assert.IsFalse (AddinCatalog.IsEnabled ("tools/sample"));
		} finally {
			if (window.IsVisible)
				window.Close ();
			AddinCatalog.SetEnabled ([addin], addin.Id, true);
		}
	}

	static MainWindowViewModel ViewModelWith (Photo photo)
	{
		var viewModel = new MainWindowViewModel (null, new MainWindowViewModelTests.NullShell (), new MainWindowViewModelTests.NullThumbnails ());
		viewModel.SeedCatalog (photo);
		viewModel.Photos[0].Selected = true;
		viewModel.SelectedPhoto = viewModel.Photos[0];
		return viewModel;
	}

	static Photo OriginalPhoto ()
	{
		var photo = new Photo (null, null, 7, DateTimeUtil.FromDateTime (new DateTime (2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
		photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri ("file:///photos/a.jpg"), "a.jpg", string.Empty, Strings.Original, true);
		photo.AllVersionsLoaded = true;
		return photo;
	}

	static NativeMenuItem NativeItem (Window window, string header)
	{
		var menu = NativeMenu.GetMenu (window);
		return Walk (menu).Single (item => item.Header as string == header);
	}

	static MenuItem? MirroredItem (Window window, string header)
	{
		var bar = window.GetVisualDescendants ().OfType<Menu> ()
			.Single (menu => AutomationProperties.GetAutomationId (menu) == AutomationIds.MainMenu);
		return bar.GetLogicalDescendants ().OfType<MenuItem> ().FirstOrDefault (item => item.Header as string == header);
	}

	static List<string> Headers (NativeMenu? menu)
		=> Walk (menu).Select (item => item.Header as string ?? string.Empty).ToList ();

	static IEnumerable<NativeMenuItem> Walk (NativeMenu? menu)
	{
		if (menu is null)
			yield break;
		foreach (object entry in menu.Items) {
			if (entry is not NativeMenuItem item)
				continue;
			yield return item;
			foreach (var child in Walk (item.Menu))
				yield return child;
		}
	}

	static bool HeaderIs (XElement item, string name)
	{
		string header = item.Attribute ("Header")?.Value ?? string.Empty;
		return header.Contains ("Strings." + name + "}", StringComparison.Ordinal)
			|| header.Contains ("Strings." + name + "\"", StringComparison.Ordinal);
	}

	static string ClientRoot ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return Path.Combine (dir.FullName, "src", "Clients", "FSpot.Avalonia");
		}

		Assert.Fail ("F-Spot.slnx not found");
		return null!;
	}
}

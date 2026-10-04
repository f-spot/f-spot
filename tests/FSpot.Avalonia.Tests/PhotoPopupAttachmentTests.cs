// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Input;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

/// <summary>
/// GTK opens PhotoPopup on the browse grid, the photo view, and FullScreenView. The filmstrip
/// menu is horizontal or vertical. The standalone viewer keeps rotate plus set-as-background.
/// Version commands stay on the Photo menu.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PhotoPopupAttachmentTests
{
	static readonly string[] GtkPhotoPopup =
	[
		Strings.CopyPhoto,
		Strings.RotateLeftMnemonic,
		Strings.RotateRightMnemonic,
		Strings.OpenWith,
		Strings.RemoveFromCatalog,
		Strings.DeleteFromDrive,
		Strings.RemoveTag,
		Strings.Rating,
	];

	[UiTestMethod]
	public void ViewedPhoto_OpensGtkPhotoPopupOnEditAndFullscreen ()
	{
		var main = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		SlideshowWindow? fullscreen = null;
		try {
			var viewModel = (MainWindowViewModel)main.DataContext!;
			var surface = main.GetVisualDescendants ().OfType<Control> ().Single (ViewerInput.GetSurface);
			AssertPopup (surface, viewModel);

			fullscreen = new SlideshowWindow (["/missing/a.jpg"], 0, viewModel, play: false);
			fullscreen.Show ();
			var stage = fullscreen.FindControl<Panel> ("Stage");
			Assert.IsNotNull (stage);
			AssertPopup (stage, viewModel);
		} finally {
			fullscreen?.Close ();
			main.Close ();
		}
	}

	[UiTestMethod]
	public void BrowseGrid_OpensGtkPhotoPopup ()
	{
		var template = PhotoGridMarkup ().Elements ().Single (element => element.Name.LocalName == "ListBox.ItemTemplate");
		Assert.IsEmpty (template.Descendants ().Where (element => element.Name.LocalName.Contains ("ContextMenu")).ToList ());

		var main = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		try {
			var grid = main.GetVisualDescendants ().OfType<ListBox> ()
				.Single (list => AutomationProperties.GetAutomationId (list) == AutomationIds.PhotoGrid);
			AssertPopup (grid, (MainWindowViewModel)main.DataContext!);
		} finally {
			main.Close ();
		}
	}

	[UiTestMethod]
	public void Filmstrip_OpensTheGtkOrientationMenu ()
	{
		var main = new MainWindow (null, new MainWindowViewModelTests.NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		try {
			var viewModel = (MainWindowViewModel)main.DataContext!;
			var strip = main.GetVisualDescendants ().OfType<Border> ()
				.Single (border => AutomationProperties.GetAutomationId (border) == AutomationIds.Filmstrip);
			var menu = strip.ContextMenu;
			Assert.IsNotNull (menu);
			Assert.IsTrue (new[] { Strings.HorizontalMnemonic, Strings.VerticalMnemonic }.SequenceEqual (Labels (menu)),
				string.Join (" | ", Labels (menu)));

			int saved = FSpot.Settings.Preferences.Get<int> (FSpot.Settings.Preferences.FilmstripOrientation);
			menu.Open (strip);
			try {
				var horizontal = menu.Items.OfType<MenuItem> ().Single (item => Equals (item.Header, Strings.HorizontalMnemonic));
				var vertical = menu.Items.OfType<MenuItem> ().Single (item => Equals (item.Header, Strings.VerticalMnemonic));
				horizontal.Command!.Execute (horizontal.CommandParameter);
				Assert.IsFalse (viewModel.FilmstripOnSide);
				Assert.IsTrue (horizontal.IsChecked);
				vertical.Command!.Execute (vertical.CommandParameter);
				Assert.IsTrue (viewModel.FilmstripOnSide);
				Assert.IsTrue (vertical.IsChecked);
				Assert.IsFalse (horizontal.IsChecked);
			} finally {
				menu.Close ();
				FSpot.Settings.Preferences.Set (FSpot.Settings.Preferences.FilmstripOrientation, saved);
			}
		} finally {
			main.Close ();
		}
	}

	static XElement PhotoGridMarkup ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			var root = Path.Combine (dir.FullName, "F-Spot.slnx");
			if (!File.Exists (root))
				continue;
			return XDocument.Load (Path.Combine (dir.FullName, "src", "Clients", "FSpot.Avalonia", "MainWindow.axaml"))
				.Descendants ()
				.Single (element => element.Name.LocalName == "ListBox"
					&& (element.Attribute ("AutomationProperties.AutomationId")?.Value ?? string.Empty).Contains ("PhotoGrid"));
		}
		throw new InvalidOperationException ("F-Spot.slnx was not found above the test output.");
	}

	static void AssertPopup (Control owner, MainWindowViewModel viewModel)
	{
		var popup = owner.ContextMenu as PhotoPopup;
		Assert.IsNotNull (popup, owner.Name + " has no GTK photo context menu.");
		Assert.IsTrue (GtkPhotoPopup.SequenceEqual (Labels (popup)), string.Join (" | ", Labels (popup)));
		foreach (var version in new[] { Strings.CreateNewVersion, Strings.DeleteVersion, Strings.RenameVersion, Strings.DetachVersion })
			Assert.DoesNotContain (version, Labels (popup));

		popup.Open (owner);
		try {
			Assert.AreSame (viewModel, popup.DataContext);
			var copy = popup.Items.OfType<MenuItem> ().First ();
			Assert.AreSame (viewModel.CopyCommand, copy.Command);
		} finally {
			popup.Close ();
		}
	}

	static List<string> Labels (ContextMenu menu)
	{
		var labels = new List<string> ();
		foreach (var item in menu.Items.OfType<MenuItem> ()) {
			labels.Add (item.Header as string
				?? AutomationProperties.GetName (item)
				?? string.Empty);
		}
		return labels;
	}
}

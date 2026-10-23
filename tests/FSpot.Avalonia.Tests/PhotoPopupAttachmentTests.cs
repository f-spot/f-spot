// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot;
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
		var main = new MainWindow (null, new NullThumbnails ()) {
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
		var main = new MainWindow (null, new NullThumbnails ()) {
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

	[TestMethod]
	public void FilmstripWheel_MovesTheHorizontalStripLeftToRight ()
	{
		var forward = FilmstripPick.WheelOffset (new Vector (0, 0), new Size (1000, 72), new Size (200, 72), new Vector (0, -1));
		var back = FilmstripPick.WheelOffset (forward, new Size (1000, 72), new Size (200, 72), new Vector (1, 0));

		Assert.AreEqual (MainWindowViewModel.FilmstripItem, forward.X);
		Assert.AreEqual (0, forward.Y);
		Assert.AreEqual (0, back.X);
		Assert.AreEqual (0, back.Y);
	}

	[UiTestMethod]
	public void HorizontalFilmstrip_ScrollsLeftToRight ()
	{
		int saved = FSpot.Settings.Preferences.Get<int> (FSpot.Settings.Preferences.FilmstripOrientation);
		var photos = Enumerable.Range (0, 24).Select (i => new StubPhoto ($"/{i}.jpg")).ToArray ();
		var main = new MainWindow (null, new NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		try {
			var viewModel = (MainWindowViewModel)main.DataContext!;
			viewModel.SeedCatalog (photos);
			viewModel.DisplayFilmstrip = true;
			viewModel.ViewMode = FSpot.Core.LibraryViewMode.PhotoView;
			viewModel.SetFilmstripOrientationCommand.Execute ("Horizontal");
			main.UpdateLayout ();
			Dispatcher.UIThread.RunJobs ();

			var list = main.GetVisualDescendants ().OfType<ListBox> ()
				.Single (box => AutomationProperties.GetName (box) == Strings.Filmstrip);
			var panel = list.ItemsPanelRoot as VirtualizingStackPanel;
			var scroller = list.GetVisualDescendants ().OfType<ScrollViewer> ().Single ();

			Assert.IsNotNull (panel);
			Assert.AreEqual (Orientation.Horizontal, panel.Orientation);
			Assert.IsGreaterThan (scroller.Viewport.Width, scroller.Extent.Width);
			Assert.IsGreaterThan (photos.Length, scroller.Extent.Width);
			Assert.IsLessThan (scroller.Viewport.Height + 1, scroller.Extent.Height);

			var before = scroller.Offset;
			panel.RaiseEvent (Wheel (panel, main, new Vector (0, -1)));
			Dispatcher.UIThread.RunJobs ();

			Assert.AreEqual (before.X + MainWindowViewModel.FilmstripItem, scroller.Offset.X, 1);
			Assert.AreEqual (before.Y, scroller.Offset.Y);
		} finally {
			main.Close ();
			FSpot.Settings.Preferences.Set (FSpot.Settings.Preferences.FilmstripOrientation, saved);
		}
	}

	[UiTestMethod]
	public void VerticalFilmstrip_KeepsTheWheelOnItsOwnAxis ()
	{
		int saved = FSpot.Settings.Preferences.Get<int> (FSpot.Settings.Preferences.FilmstripOrientation);
		var photos = Enumerable.Range (0, 24).Select (i => new StubPhoto ($"/{i}.jpg")).ToArray ();
		var main = new MainWindow (null, new NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		try {
			var viewModel = (MainWindowViewModel)main.DataContext!;
			viewModel.SeedCatalog (photos);
			viewModel.DisplayFilmstrip = true;
			viewModel.ViewMode = FSpot.Core.LibraryViewMode.PhotoView;
			viewModel.SetFilmstripOrientationCommand.Execute ("Vertical");
			main.UpdateLayout ();
			Dispatcher.UIThread.RunJobs ();

			var list = main.GetVisualDescendants ().OfType<ListBox> ()
				.Single (box => AutomationProperties.GetName (box) == Strings.Filmstrip);
			var panel = list.ItemsPanelRoot as VirtualizingStackPanel;
			var scroller = list.GetVisualDescendants ().OfType<ScrollViewer> ().Single ();

			Assert.IsNotNull (panel);
			Assert.AreEqual (Orientation.Vertical, panel.Orientation);
			Assert.IsGreaterThan (scroller.Viewport.Height, scroller.Extent.Height);
			Assert.IsLessThan (scroller.Viewport.Width + 1, scroller.Extent.Width);

			var before = scroller.Offset;
			panel.RaiseEvent (Wheel (panel, main, new Vector (0, -1)));
			Dispatcher.UIThread.RunJobs ();

			Assert.AreEqual (before.X, scroller.Offset.X);
			Assert.IsGreaterThan (before.Y, scroller.Offset.Y);
		} finally {
			main.Close ();
			FSpot.Settings.Preferences.Set (FSpot.Settings.Preferences.FilmstripOrientation, saved);
		}
	}

	static PointerWheelEventArgs Wheel (Control source, Visual root, Vector delta)
		=> new (source, new Pointer (1, PointerType.Mouse, true), root, new Point (8, 8), 0,
			new PointerPointProperties (RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None, delta);

	[UiTestMethod]
	public void HorizontalFilmstrip_KeepsItsHeightWhenTheSelectionBorderAppears ()
	{
		int saved = FSpot.Settings.Preferences.Get<int> (FSpot.Settings.Preferences.FilmstripOrientation);
		var photos = Enumerable.Range (0, 24).Select (i => new StubPhoto ($"/{i}.jpg")).ToArray ();
		var main = new MainWindow (null, new NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		try {
			var viewModel = (MainWindowViewModel)main.DataContext!;
			viewModel.SeedCatalog (photos);
			viewModel.DisplayFilmstrip = true;
			viewModel.ViewMode = FSpot.Core.LibraryViewMode.PhotoView;
			viewModel.SetFilmstripOrientationCommand.Execute ("Horizontal");
			viewModel.SelectedPhoto = viewModel.Photos[0];
			main.UpdateLayout ();
			Dispatcher.UIThread.RunJobs ();

			var list = main.GetVisualDescendants ().OfType<ListBox> ()
				.Single (box => AutomationProperties.GetName (box) == Strings.Filmstrip);
			var scroller = list.GetVisualDescendants ().OfType<ScrollViewer> ().Single ();
			var selected = list.ContainerFromIndex (0) as ListBoxItem;
			Assert.IsNotNull (selected);
			// App.axaml adds this border when the item is focused. It has to be in the row from the start.
			selected.BorderThickness = new Thickness (2);
			selected.Focus ();
			main.UpdateLayout ();
			Dispatcher.UIThread.RunJobs ();

			var free = new Size (double.PositiveInfinity, double.PositiveInfinity);
			double tallest = 0;
			double shortest = double.MaxValue;
			int realized = 0;
			for (int i = 0; i < viewModel.Photos.Count; i++) {
				if (list.ContainerFromIndex (i) is not ListBoxItem cell)
					continue;
				cell.Measure (free);
				realized++;
				tallest = Math.Max (tallest, cell.DesiredSize.Height);
				shortest = Math.Min (shortest, cell.DesiredSize.Height);
			}

			var strip = list.GetVisualParent ();
			Assert.IsNotNull (strip);
			Assert.IsGreaterThan (1, realized);
			Assert.AreEqual (shortest, tallest, 0.5);
			Assert.IsGreaterThan (selected.DesiredSize.Height - 0.5, strip.Bounds.Height);
			Assert.IsLessThan (scroller.Viewport.Height + 1, scroller.Extent.Height);

			var before = scroller.Offset;
			var panel = (VirtualizingStackPanel)list.ItemsPanelRoot!;
			panel.RaiseEvent (Wheel (panel, main, new Vector (0, -1)));
			Dispatcher.UIThread.RunJobs ();

			Assert.AreEqual (before.Y, scroller.Offset.Y);
			Assert.IsGreaterThan (before.X, scroller.Offset.X);
		} finally {
			main.Close ();
			FSpot.Settings.Preferences.Set (FSpot.Settings.Preferences.FilmstripOrientation, saved);
		}
	}

	[UiTestMethod]
	public void HorizontalFilmstrip_LaysPhotosLeftToRight ()
	{
		int saved = FSpot.Settings.Preferences.Get<int> (FSpot.Settings.Preferences.FilmstripOrientation);
		var main = new MainWindow (null, new NullThumbnails ()) {
			Width = 800,
			Height = 500
		};
		main.Show ();
		try {
			var viewModel = (MainWindowViewModel)main.DataContext!;
			viewModel.SeedCatalog (new StubPhoto ("/a.jpg"), new StubPhoto ("/b.jpg"), new StubPhoto ("/c.jpg"));
			viewModel.DisplayFilmstrip = true;
			viewModel.ViewMode = FSpot.Core.LibraryViewMode.PhotoView;
			viewModel.SetFilmstripOrientationCommand.Execute ("Horizontal");
			Dispatcher.UIThread.RunJobs ();
			var list = main.GetVisualDescendants ().OfType<ListBox> ()
				.Single (box => AutomationProperties.GetName (box) == Strings.Filmstrip);
			var panel = list.ItemsPanelRoot as VirtualizingStackPanel;

			Assert.IsNotNull (panel);
			Assert.AreEqual (Orientation.Horizontal, panel.Orientation);
		} finally {
			main.Close ();
			FSpot.Settings.Preferences.Set (FSpot.Settings.Preferences.FilmstripOrientation, saved);
		}
	}

	[UiTestMethod]
	public void Filmstrip_OpensTheGtkOrientationMenu ()
	{
		var main = new MainWindow (null, new NullThumbnails ()) {
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

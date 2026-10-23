// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GridSelectionChromeTests
{
	[UiTestMethod]
	public void SelectedPhoto_UsesAQuietOutlineInsteadOfABrightFill ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null, new NullThumbnails ()) {
			Width = 900,
			Height = 600
		};
		window.Show ();
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			viewModel.SeedCatalog (new StubPhoto ("/one.jpg"), new StubPhoto ("/two.jpg"));
			viewModel.Photos[0].Selected = true;
			window.UpdateLayout ();
			Dispatcher.UIThread.RunJobs ();

			var grid = window.GetVisualDescendants ().OfType<ListBox> ()
				.Single (list => AutomationProperties.GetAutomationId (list) == AutomationIds.PhotoGrid);
			var tile = grid.GetVisualDescendants ().OfType<Border> ()
				.First (border => border.Classes.Contains ("selected"));
			Assert.IsTrue (IsQuietFill (tile.Background), tile.Background?.ToString ());
			Assert.AreEqual (PhotoTileChrome.SelectionFrame.Left, tile.BorderThickness.Left);
			var stroke = tile.BorderBrush as ISolidColorBrush;
			Assert.IsNotNull (stroke);
			Assert.IsGreaterThan (0, stroke.Opacity);
			Assert.IsLessThan (0.5, stroke.Opacity, stroke.ToString ());

			var item = (ListBoxItem)grid.ContainerFromIndex (0)!;
			var presenter = item.GetVisualDescendants ().OfType<ContentPresenter> ().First ();
			Assert.IsTrue (IsQuietFill (presenter.Background), presenter.Background?.ToString ());
		} finally {
			window.Close ();
		}
	}

	static bool IsQuietFill (IBrush? brush)
	{
		if (brush is null)
			return true;
		if (brush is ISolidColorBrush solid)
			return solid.Opacity < 0.2 || solid.Color.A < 40;
		return false;
	}
}

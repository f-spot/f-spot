// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Editors;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LayoutChromeTests
{
	[UiTestMethod]
	public void FindBar_SitsAboveThePhotoGridAndTheEditingPreview ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null, new NullThumbnails ()) { Width = 1100, Height = 720 };
		var viewModel = (MainWindowViewModel)window.DataContext!;
		viewModel.DisplayTimeline = true;
		viewModel.SeedCatalog (
			new StubPhoto ("/tmp/20261008_152224.jpg"),
			new StubPhoto ("/tmp/b.jpg"),
			new StubPhoto ("/tmp/c.jpg"));
		window.Show ();
		try {
			viewModel.ShowFindCommand.Execute (null);
			window.UpdateLayout ();

			var find = ById (window, AutomationIds.FindBar);
			var grid = ById (window, AutomationIds.PhotoGrid);
			Assert.IsTrue (find.IsVisible);
			Assert.IsNotNull (((Panel)find).Background);
			AssertBelow (window, find, grid);

			var item = grid.GetVisualDescendants ().OfType<ListBoxItem> ().First ();
			AssertBelow (window, find, item);

			viewModel.ViewMode = LibraryViewMode.PhotoView;
			window.UpdateLayout ();
			var preview = ById (window, AutomationIds.PhotoPreview);
			Assert.IsTrue (preview.IsVisible);
			AssertBelow (window, find, (Control)preview.Parent!);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void ImportButton_KeepsTheLabelClearOfTheButtonEdge ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null, new NullThumbnails ()) { Width = 1100, Height = 720 };
		window.Show ();
		try {
			window.UpdateLayout ();
			var import = (Button)ById (window, AutomationIds.ToolbarImport);
			var access = import.GetVisualDescendants ().OfType<AccessText> ().Single ();
			Assert.AreEqual (Strings.Import2, access.Text);
			Point end = access.TranslatePoint (new Point (access.Bounds.Width, 0), import) ?? default;
			double spare = import.Bounds.Width - end.X;
			Assert.IsGreaterThanOrEqualTo (16, spare);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void EditorLabels_WrapInsteadOfClippingInTheSidebar ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null, new NullThumbnails ()) { Width = 1100, Height = 720 };
		var viewModel = (MainWindowViewModel)window.DataContext!;
		var sidebar = viewModel.SidebarLength;
		viewModel.SeedCatalog (new StubPhoto ("/tmp/a.jpg"));
		window.Show ();
		try {
			viewModel.ViewMode = LibraryViewMode.PhotoView;
			viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Color);
			ById (window, AutomationIds.Sidebar).MinWidth = 0;
			viewModel.SidebarLength = new GridLength (100);
			window.UpdateLayout ();

			var brightness = Wrapped (window, Strings.Brightness);
			Assert.IsGreaterThan (20, brightness.Bounds.Height);
			Wrapped (window, Strings.Exposure);
			Wrapped (window, Strings.Saturation);
			Wrapped (window, Strings.Hue);
			Wrapped (window, Strings.Contrast);
			Wrapped (window, Strings.Temp);
			Wrapped (window, Strings.Tint);
			Wrapped (window, Strings.Red);
			Wrapped (window, Strings.Green);
			Wrapped (window, Strings.Blue);
			Wrapped (window, Strings.AdjustTheAngleOfTheImageToStraightenTheHorizon);
			Wrapped (window, Strings.RadiusColon);
			Wrapped (window, Strings.AmountColon);
			Wrapped (window, Strings.ThresholdColon);
			Wrapped (window, Strings.Resize);
		} finally {
			viewModel.SidebarLength = sidebar;
			window.Close ();
		}
	}

	[UiTestMethod]
	public void RightToLeft_KeepsFilenamesExposureAndThePhotoIndexLeftToRight ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null, new NullThumbnails ()) { Width = 1100, Height = 720 };
		var viewModel = (MainWindowViewModel)window.DataContext!;
		bool names = viewModel.DisplayFilenames;
		var original = new StubPhoto ("/tmp/a.jpg");
		var named = new StubPhoto ("/tmp/20261008_152224.jpg");
		((StubVersion)named.DefaultVersion).Name = "20261008_152224.jpg";
		viewModel.DisplayFilenames = true;
		viewModel.SeedCatalog (original, named);
		window.Show ();
		try {
			window.FlowDirection = FlowDirection.RightToLeft;
			window.UpdateLayout ();

			var sidebar = ById (window, AutomationIds.Sidebar);
			Assert.AreEqual (FlowDirection.RightToLeft, sidebar.FlowDirection);
			Assert.AreEqual (FlowDirection.RightToLeft, ById (window, AutomationIds.SidebarInfoCamera).FlowDirection);
			Assert.AreEqual (FlowDirection.LeftToRight, ById (window, AutomationIds.SidebarInfoName).FlowDirection);
			Assert.AreEqual (FlowDirection.LeftToRight, ById (window, AutomationIds.SidebarInfoExposure).FlowDirection);
			Assert.AreEqual (FlowDirection.LeftToRight, ById (window, AutomationIds.SidebarInfoSize).FlowDirection);
			Assert.AreEqual (FlowDirection.LeftToRight, ById (window, AutomationIds.SidebarInfoFocalLength).FlowDirection);
			Assert.AreEqual (FlowDirection.LeftToRight, ById (window, AutomationIds.ToolbarPhotoIndex).FlowDirection);

			var version = (ComboBox)ById (window, AutomationIds.SidebarInfoVersion);
			Assert.AreEqual (FlowDirection.RightToLeft, version.FlowDirection);

			viewModel.SelectPhoto (viewModel.Photos[1]);
			window.UpdateLayout ();
			Assert.AreEqual (FlowDirection.LeftToRight, version.FlowDirection);
			var caption = window.GetVisualDescendants ().OfType<TextBlock> ()
				.First (block => block.Text == "20261008_152224.jpg");
			Assert.AreEqual (FlowDirection.LeftToRight, caption.FlowDirection);
		} finally {
			viewModel.DisplayFilenames = names;
			window.Close ();
		}
	}

	static void AssertBelow (Control window, Control above, Control below)
	{
		Point aboveBottom = above.TranslatePoint (new Point (0, above.Bounds.Height), window) ?? default;
		Point belowTop = below.TranslatePoint (default, window) ?? default;
		Assert.IsGreaterThanOrEqualTo (aboveBottom.Y - 0.5, belowTop.Y);
	}

	static AccessText Wrapped (Control window, string text)
	{
		var label = window.GetVisualDescendants ().OfType<AccessText> ()
			.First (block => block.Text == text);
		Assert.AreEqual (TextWrapping.Wrap, label.TextWrapping, text);
		return label;
	}

	static Control ById (Control root, string id)
		=> root.GetVisualDescendants ().OfType<Control> ()
			.First (control => AutomationProperties.GetAutomationId (control) == id);
}

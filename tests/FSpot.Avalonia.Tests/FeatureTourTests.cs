// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// Walks the main window the way a person would, and checks that decoded images
// from each step are released before the next step keeps them.
[TestClass]
public sealed class FeatureTourTests
{
	[UiTestMethod]
	public async Task MainWindow_ExercisesTheLibraryAndReleasesDecodedImages ()
	{
		AvaloniaApp.Ensure ();
		var loader = new LedgerThumbnails ();
		var window = new MainWindow (null, loader) { Width = 1100, Height = 720 };
		window.Show ();
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			var photos = Enumerable.Range (0, 80)
				.Select (i => (IPhoto)new TourPhoto ($"/tour/{i}.jpg", new DateTime (2024, 1, 1).AddMonths (i)))
				.ToArray ();
			viewModel.SeedCatalog (photos);
			window.UpdateLayout ();
			await Drain ();

			Assert.IsGreaterThan (0, loader.Created, "the visible grid never decoded a thumbnail");
			Assert.IsLessThan (photos.Length, loader.Created, "the grid decoded a thumbnail for every catalog photo");

			var grid = Find<ListBox> (window, AutomationIds.PhotoGrid);
			var cell = grid.GetVisualDescendants ().OfType<ListBoxItem> ().First ();
			Click (window, cell);
			await Drain ();

			Assert.IsNotNull (viewModel.Histogram);
			Bitmap histogram = viewModel.Histogram;

			window.KeyPress (Key.OemQuestion, RawInputModifiers.None, default, null);
			Assert.IsTrue (viewModel.IsFindBarVisible);
			Find<TextBox> (window, AutomationIds.FindBox);

			var chooser = Find<ComboBox> (window, AutomationIds.SidebarChooser);
			chooser.SelectedIndex = 1;
			Assert.IsTrue (viewModel.IsFoldersPage);
			chooser.SelectedIndex = 0;
			Assert.IsFalse (viewModel.IsFoldersPage);

			Click (window, Find<Button> (window, AutomationIds.ToolbarEdit));
			await Drain ();
			Assert.IsTrue (viewModel.IsEditing);
			Assert.IsNotNull (viewModel.Preview);
			Bitmap preview = viewModel.Preview;
			Assert.Contains (preview, loader.All);

			Click (window, Find<Button> (window, AutomationIds.ToolbarBrowse));
			await Drain ();
			Assert.IsFalse (viewModel.IsEditing);
			Assert.IsNull (viewModel.Preview);
			AssertDisposed (preview);

			var zoom = Find<Slider> (window, AutomationIds.ZoomSlider);
			double zoomed = zoom.Value > zoom.Minimum + 1 ? zoom.Minimum : zoom.Maximum;
			zoom.Value = zoomed;
			Assert.AreEqual (zoomed, viewModel.Zoom);

			window.Close ();
			AssertDisposed (histogram);
			Assert.AreEqual (0, loader.Live);
			Assert.IsNull (viewModel.Histogram);
			Assert.IsNull (viewModel.HoverThumbnail);
			Assert.IsNull (viewModel.Preview);
		} finally {
			if (window.IsVisible)
				window.Close ();
		}
	}

	static async Task Drain ()
	{
		await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background);
		await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background);
	}

	static T Find<T> (Window window, string automationId) where T : Control
		=> window.GetVisualDescendants ().OfType<T> ()
			.First (control => AutomationProperties.GetAutomationId (control) == automationId);

	static void Click (Window window, Control control)
	{
		Point point = control.TranslatePoint (new Point (8, 8), window)
			?? throw new InvalidOperationException ($"{control.GetType ().Name} is not on the window");
		window.MouseDown (point, MouseButton.Left);
		window.MouseUp (point, MouseButton.Left);
	}

	static void AssertDisposed (Bitmap bitmap)
		=> Assert.ThrowsExactly<ObjectDisposedException> (() => _ = bitmap.PixelSize);

	sealed class LedgerThumbnails : IPhotoThumbnailLoader
	{
		public List<Bitmap> All { get; } = [];

		public int Created => All.Count;

		public int Live => All.Count (bitmap => !IsDisposed (bitmap));

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			int edge = Math.Max (1, size);
			var bitmap = new WriteableBitmap (
				new PixelSize (edge, edge),
				new global::Avalonia.Vector (96, 96),
				PixelFormat.Bgra8888,
				AlphaFormat.Premul);
			All.Add (bitmap);
			return Task.FromResult<Bitmap?> (bitmap);
		}

		static bool IsDisposed (Bitmap bitmap)
		{
			try {
				_ = bitmap.PixelSize;
				return false;
			} catch (ObjectDisposedException) {
				return true;
			}
		}
	}

	sealed class TourPhoto : IPhoto
	{
		public TourPhoto (string path, DateTime time)
		{
			Name = System.IO.Path.GetFileName (path);
			Time = time;
			DefaultVersion = new MainWindowViewModelTests.StubVersion (path);
			Versions = new[] { DefaultVersion };
			Tags = [];
		}

		public DateTime Time { get; }
		public Tag[] Tags { get; }
		public string Description => string.Empty;
		public string Name { get; }
		public uint Rating => 0;
		public IPhotoVersion DefaultVersion { get; }
		public IEnumerable<IPhotoVersion> Versions { get; }
	}
}

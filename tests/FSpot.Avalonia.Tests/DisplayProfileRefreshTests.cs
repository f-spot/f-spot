// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot;
using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Imaging;
using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// Saving the display profile has to repaint the photos already on screen.
// These tests share the process-wide preference, so they run alone.
[TestClass]
[DoNotParallelize]
public sealed class DisplayProfileRefreshTests
{
	public TestContext TestContext { get; }

	public DisplayProfileRefreshTests (TestContext testContext) => TestContext = testContext;

	[TestInitialize]
	public void Start ()
		=> AvaloniaApp.Ensure ();

	[UiTestMethod]
	public async Task Slideshow_RedecodesTheCurrentSlideWhenTheDisplayProfileChanges ()
	{
		int decodes = 0;
		var window = new SlideshowWindow (new[] { "/photos/a.jpg" }, 0, null, play: false, decode: (_, _) => {
			decodes++;
			return Tiny ();
		});
		window.Show ();
		try {
			await WaitUntilAsync (() => decodes >= 1);
			int opened = decodes;

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => decodes > opened);
			Assert.AreEqual (opened + 1, decodes);

			LibraryPreferences.DisplayProfile = "studio.icc";
			await SettleAsync ();
			Assert.AreEqual (opened + 1, decodes);

			bool interpolation = ViewerSettings.Interpolation;
			ViewerSettings.Interpolation = !interpolation;
			try {
				await SettleAsync ();
				Assert.AreEqual (opened + 1, decodes);
			} finally {
				ViewerSettings.Interpolation = interpolation;
			}
		} finally {
			window.Close ();
			int closed = decodes;
			LibraryPreferences.DisplayProfile = "other.icc";
			await SettleAsync ();
			Assert.AreEqual (closed, decodes);
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	[UiTestMethod]
	public async Task MainWindow_ReloadsTheGridHoverAndEditorWithoutRemeasuringTheHistogram ()
	{
		var thumbs = new CountingBitmaps ();
		var window = new MainWindow (null, thumbs);
		window.Show ();
		try {
			var viewModel = (MainWindowViewModel)window.DataContext!;
			viewModel.SeedCatalog (new StubPhoto ("/photos/a.jpg"));
			await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[0]);
			await viewModel.ShowHoverAsync (viewModel.Photos[0], 1920, 1080);
			await WaitUntilAsync (() => viewModel.Histogram is not null);
			int loads = thumbs.Loads;
			int measured = thumbs.Measured;
			var hover = viewModel.HoverThumbnail;
			var histogram = viewModel.Histogram;

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => thumbs.Loads >= loads + 2);

			Assert.AreEqual (loads + 2, thumbs.Loads);
			Assert.AreEqual (measured, thumbs.Measured);
			Assert.AreNotSame (hover, viewModel.HoverThumbnail);
			Assert.IsTrue (viewModel.ShowHoverPreview);
			Assert.AreSame (histogram, viewModel.Histogram);

			viewModel.ViewMode = LibraryViewMode.PhotoView;
			await WaitUntilAsync (() => viewModel.Preview is not null);
			int edited = thumbs.Measured;
			var preview = viewModel.Preview;
			histogram = viewModel.Histogram;

			LibraryPreferences.DisplayProfile = "other.icc";
			// The measurement counts before the screen conversion, which leaves the UI thread.
			await WaitUntilAsync (() => thumbs.Measured > edited && !ReferenceEquals (preview, viewModel.Preview));

			Assert.AreEqual (edited + 1, thumbs.Measured);
			Assert.AreNotSame (preview, viewModel.Preview);
			Assert.AreSame (histogram, viewModel.Histogram);
		} finally {
			window.Close ();
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	[UiTestMethod]
	public void Import_CloseDropsAPreviewThatIsStillLoading ()
	{
		var thumbs = new HeldPreview ();
		var viewModel = new ImportViewModel (null!, new MainWindowViewModelTests.NullShell (), thumbs, static _ => { });
		viewModel.AddPendingPhoto (new PhotoItemViewModel (new StubPhoto ("/import/a.jpg")));
		viewModel.SelectedPhoto = viewModel.Photos[0];
		Assert.AreEqual (1, thumbs.Loads);

		var bitmap = Tiny ();
		viewModel.CancelInFlight ();
		thumbs.Gate.SetResult (bitmap);

		Assert.IsNull (viewModel.Preview);
		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = bitmap.PixelSize);
	}

	[UiTestMethod]
	public async Task Import_ReloadsTheTrayAndThePreview ()
	{
		var thumbs = new CountingBitmaps ();
		var viewModel = new ImportViewModel (null!, new MainWindowViewModelTests.NullShell (), thumbs, static _ => { });
		var window = new ImportWindow (viewModel);
		window.Show ();
		try {
			viewModel.AddPendingPhoto (new PhotoItemViewModel (new StubPhoto ("/import/a.jpg")));
			viewModel.SelectedPhoto = viewModel.Photos[0];
			await viewModel.RealizeThumbnailAsync (new object (), viewModel.Photos[0]);
			await WaitUntilAsync (() => viewModel.Preview is not null);
			int loads = thumbs.Loads;
			var preview = viewModel.Preview;
			var thumbnail = viewModel.Photos[0].Thumbnail;

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => thumbs.Loads >= loads + 2);

			Assert.AreEqual (loads + 2, thumbs.Loads);
			Assert.AreNotSame (preview, viewModel.Preview);
			Assert.AreNotSame (thumbnail, viewModel.Photos[0].Thumbnail);
		} finally {
			window.Close ();
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	[UiTestMethod]
	public async Task Viewer_ReloadsThePhotoAndTheFilmstrip ()
	{
		int decodes = 0;
		var thumbs = new CountingBitmaps ();
		string path = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var window = new ViewerWindow (new[] { path }, static _ => true, thumbs, (_, _) => {
			decodes++;
			return Tiny ();
		});
		window.Show ();
		try {
			await WaitUntilAsync (() => decodes >= 1);
			window.OnTrayPrepared (null, new ContainerPreparedEventArgs (new Border (), 0));
			await WaitUntilAsync (() => window.ViewModel.Items[0].Thumbnail is not null);
			int opened = decodes;
			int trays = thumbs.Loads;
			var tray = window.ViewModel.Items[0].Thumbnail;

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => decodes > opened && thumbs.Loads > trays);

			Assert.AreEqual (opened + 1, decodes);
			Assert.AreEqual (trays + 1, thumbs.Loads);
			Assert.AreNotSame (tray, window.ViewModel.Items[0].Thumbnail);
		} finally {
			window.Close ();
			int closed = decodes;
			LibraryPreferences.DisplayProfile = "other.icc";
			await SettleAsync ();
			Assert.AreEqual (closed, decodes);
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	[UiTestMethod]
	public async Task AdjustTime_ReloadsTheReferenceAndTheTray ()
	{
		var thumbs = new CountingBitmaps ();
		var photos = new[] {
			("a.jpg", DateTime.UtcNow, "/photos/a.jpg"),
			("b.jpg", DateTime.UtcNow, "/photos/b.jpg")
		};
		var window = new AdjustTimeWindow (photos, thumbs);
		window.Show ();
		try {
			await WaitUntilAsync (() => thumbs.Loads >= 3);
			int opened = thumbs.Loads;

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => thumbs.Loads >= opened + 3);
			Assert.AreEqual (opened + 3, thumbs.Loads);
		} finally {
			window.Close ();
			int closed = thumbs.Loads;
			LibraryPreferences.DisplayProfile = "other.icc";
			await SettleAsync ();
			Assert.AreEqual (closed, thumbs.Loads);
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	[UiTestMethod]
	public async Task ChromeIcon_RebuildsOnlyAColorManagedIconThatIsOnScreen ()
	{
		string png = EditorIcons.UriFor ("crop")!;
		string svg = EditorIcons.UriFor ("adjust-colors")!;
		var managed = new ChromeIcon { Width = 22, Height = 22, Uri = png, ColorManage = true };
		var toolbar = new ChromeIcon { Width = 22, Height = 22, Uri = svg };
		var window = new Window { Width = 80, Height = 40, Content = new StackPanel { Children = { managed, toolbar } } };
		window.Show ();
		managed.UpdateLayout ();
		toolbar.UpdateLayout ();
		try {
			var managedImage = Assert.IsInstanceOfType<Image> (managed.Children.Single ());
			var toolbarImage = toolbar.Children.Single ();

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => !ReferenceEquals (managedImage, managed.Children.Single ()));

			Assert.AreNotSame (managedImage, managed.Children.Single ());
			Assert.AreSame (toolbarImage, toolbar.Children.Single ());

			window.Content = null;
			var afterDetach = managed.Children.Single ();
			LibraryPreferences.DisplayProfile = "other.icc";
			await SettleAsync ();
			Assert.AreSame (afterDetach, managed.Children.Single ());
		} finally {
			window.Close ();
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	[UiTestMethod]
	public async Task TagIconDialogs_RepaintTheIconAndThePhoto ()
	{
		var root = new Category (null, 0, "(None)");
		var tag = new Tag (root, 2, "Beach");
		var edit = TagWindow.ForEdit (root, new TagIconSource (tag, []));
		edit.Show ();
		var pixels = new byte[] { 10, 20, 30 };
		edit.PendingIcon = new TagIconChoice (null, new ImageBuffer (1, 1, false, 3, pixels));

		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var icon = new EditTagIconWindow (new TagIconSource (tag, [sample]), new TagIconChoice (null, new ImageBuffer (1, 1, false, 3, pixels)));
		icon.Show ();
		try {
			await WaitUntilAsync (() => icon.PhotoImage.Source is not null);
			var photo = icon.PhotoImage.Source;
			var preview = icon.PreviewImage.Source;

			LibraryPreferences.DisplayProfile = "studio.icc";
			await WaitUntilAsync (() => edit.IconImage.Source is not null && !ReferenceEquals (photo, icon.PhotoImage.Source));

			Assert.IsNotNull (edit.IconImage.Source);
			Assert.AreNotSame (photo, icon.PhotoImage.Source);
			Assert.AreNotSame (preview, icon.PreviewImage.Source);
		} finally {
			edit.Close ();
			icon.Close ();
			LibraryPreferences.DisplayProfile = string.Empty;
		}
	}

	async Task WaitUntilAsync (Func<bool> done)
	{
		long started = Environment.TickCount64;
		while (!done () && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}
	}

	async Task SettleAsync ()
	{
		long started = Environment.TickCount64;
		while (Environment.TickCount64 - started < 150) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}
	}

	static WriteableBitmap Tiny ()
		=> new (new PixelSize (2, 2), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

	sealed class HeldPreview : IPhotoThumbnailLoader
	{
		public TaskCompletionSource<Bitmap?> Gate { get; } = new ();

		public int Loads { get; private set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Loads++;
			// The test completes Gate after the load has been requested.
#pragma warning disable VSTHRD003
			return Gate.Task;
#pragma warning restore VSTHRD003
		}

		public Task<Bitmap?> LoadForMeasurementAsync (string path, int size, CancellationToken cancellationToken)
			=> Task.FromResult<Bitmap?> (null);
	}

	sealed class CountingBitmaps : IPhotoThumbnailLoader
	{
		public int Loads { get; private set; }
		public int Measured { get; private set; }

		public Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
		{
			Loads++;
			return Task.FromResult<Bitmap?> (Tiny ());
		}

		public Task<Bitmap?> LoadForMeasurementAsync (string path, int size, CancellationToken cancellationToken)
		{
			Measured++;
			return Task.FromResult<Bitmap?> (Tiny ());
		}
	}
}

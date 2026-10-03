// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using FSpot.Avalonia.Controls;
using FSpot.Editors;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Prefs = FSpot.Settings.Preferences;

namespace FSpot.Avalonia.Tests;

public sealed partial class MainWindowViewModelTests
{
	[UiTestMethod]
	public void ApplyEditor_Crop_ListsTheGtkConstraintsWithNoConstraintSelected ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		Assert.IsTrue (viewModel.ShowCropSettings);
		Assert.AreEqual (Strings.NoConstraint, viewModel.CropConstraintOptions[0].Label);
		Assert.AreEqual (Strings.SameAsPhoto, viewModel.CropConstraintOptions[1].Label);
		Assert.AreEqual (CropConstraintKind.AddCustom, viewModel.CropConstraintOptions[^1].Kind);
		Assert.AreSame (viewModel.CropConstraintOptions[0], viewModel.SelectedCropConstraint);
		Assert.AreEqual (0, viewModel.CropSelectionRatio);
	}

	[UiTestMethod]
	public void SelectCropConstraint_SetsTheSelectionRatio ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		viewModel.SelectedCropConstraint = viewModel.CropConstraintOptions.First (c => c.Label == Strings.Square);

		Assert.AreEqual (1.0, viewModel.CropSelectionRatio);
	}

	[UiTestMethod]
	public void ApplyEditor_Tilt_HidesCropSettings ()
	{
		var viewModel = ViewModelWithFocusedPhoto ();

		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Tilt);

		Assert.IsFalse (viewModel.ShowCropSettings);
	}

	[UiTestMethod]
	public void ApplyEditor_RedEye_ReadsTheThresholdPreference ()
	{
		int previous = Prefs.Get<int> (Prefs.EditRedeyeThreshold);
		try {
			Prefs.Set (Prefs.EditRedeyeThreshold, -7);
			var viewModel = ViewModelWithFocusedPhoto ();

			viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.RedEye);

			Assert.AreEqual (-7, viewModel.EditorSession?.Context.RedEyeThreshold);
			Assert.AreEqual (-7, viewModel.EditorSession?.CreateContext ().RedEyeThreshold);
		} finally {
			Prefs.Set (Prefs.EditRedeyeThreshold, previous);
		}
	}

	// Headless Lock() hands out a fresh buffer and frees it, so pixel bytes
	// cannot be read back here. Channel weights are covered by
	// ConvertToBw_UsesChannelWeightsAndTheToneCurve. Color needs liblcms2,
	// which the app registers at startup and this host does not.
	[UiTestMethod]
	public async Task LivePreviewEditors_RenderOnOpenAndWhenThePhotoChanges ()
	{
		AvaloniaApp.Ensure ();
		FSpot.Cms.LcmsNativeLibrary.Register ();
		PhotoEditorDescriptor[] editors = [
			PhotoEditorCatalog.BW,
			PhotoEditorCatalog.Tilt,
			PhotoEditorCatalog.SoftFocus,
			PhotoEditorCatalog.Color
		];

		foreach (var editor in editors) {
			var viewModel = ViewModelWithFocusedPhoto ();
			viewModel.Preview = new WriteableBitmap (new PixelSize (8, 8), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			viewModel.ApplyEditorCommand.Execute (editor);
			await viewModel.LivePreviewTask;

			var opened = viewModel.EditedPreview;
			Assert.IsNotNull (opened, editor.Id);
			Assert.AreNotSame (viewModel.Preview, opened, editor.Id);

			viewModel.Preview = new WriteableBitmap (new PixelSize (8, 8), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
			await viewModel.LivePreviewTask;

			Assert.IsNotNull (viewModel.EditedPreview, editor.Id);
			Assert.AreNotSame (opened, viewModel.EditedPreview, editor.Id);
		}
	}

	[UiTestMethod]
	public async Task TiltSlider_RendersALivePreviewAndCancelRestoresTheOriginal ()
	{
		AvaloniaApp.Ensure ();
		var viewModel = ViewModelWithFocusedPhoto ();
		var original = new WriteableBitmap (new PixelSize (40, 20), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		viewModel.Preview = original;
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Tilt);
		Assert.AreSame (original, viewModel.DisplayedPreview);

		viewModel.EditorSession!.Context.TiltDegrees = 10;
		await viewModel.LivePreviewTask;

		Assert.IsNotNull (viewModel.EditedPreview);
		Assert.AreSame (viewModel.EditedPreview, viewModel.DisplayedPreview);
		Assert.AreEqual (40, viewModel.EditedPreview.PixelSize.Width);

		viewModel.CancelEditorCommand.Execute (null);

		Assert.IsNull (viewModel.EditedPreview);
		Assert.AreSame (original, viewModel.DisplayedPreview);
	}

	[UiTestMethod]
	public async Task BlackAndWhiteCurve_LiftsTheToneAndRendersAgain ()
	{
		var panel = XDocument.Load (Path.Combine (ClientRoot (), "MainWindow.axaml"))
			.Descendants ()
			.Single (element => (element.Attribute ("IsVisible")?.Value ?? string.Empty).Contains ("ShowBwSettings"));
		var markup = panel.Descendants ().Single (element => element.Name.LocalName == "SplineCurveEditor");
		Assert.Contains ("BwCurve", markup.Attribute ("Curve")?.Value ?? string.Empty);

		AvaloniaApp.Ensure ();
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.Preview = new WriteableBitmap (new PixelSize (8, 8), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.BW);
		await viewModel.LivePreviewTask;
		var straight = viewModel.EditedPreview;
		Assert.IsNotNull (straight);

		const double side = 256 + SplineCurve.Radius * 2;
		var editor = new SplineCurveEditor { Width = side, Height = side };
		editor.Bind (SplineCurveEditor.CurveProperty, new Binding (nameof (PhotoEditorContext.BwCurve)) {
			Source = viewModel.EditorSession!.Context,
			Mode = BindingMode.OneWayToSource
		});
		editor.Session = viewModel.EditorSession;
		editor.Measure (new Size (side, side));
		editor.Arrange (new Rect (0, 0, side, side));
		await viewModel.LivePreviewTask;

		editor.PressAt (new Point (131, SplineCurve.Radius));
		await viewModel.LivePreviewTask;

		Assert.IsGreaterThan (200, viewModel.EditorSession.Context.BwCurve[128]);
		Assert.IsNotNull (viewModel.EditedPreview);
		Assert.AreNotSame (straight, viewModel.EditedPreview);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void BlackAndWhiteCurve_DragOffTheGraphRestoresTheStraightLine ()
	{
		AvaloniaApp.Ensure ();
		var session = new PhotoEditorSession (PhotoEditorCatalog.BW);
		var editor = ShownCurve (session);
		var window = TopLevel.GetTopLevel (editor) as Window ?? throw new InvalidOperationException ("curve has no window");

		var press = editor.TranslatePoint (new Point (131, SplineCurve.Radius), window)
			?? throw new InvalidOperationException ("curve has no press point");
		var drop = editor.TranslatePoint (new Point (131, -SplineCurve.MinDistance - 1), window)
			?? throw new InvalidOperationException ("curve has no drop point");
		window.MouseDown (press, MouseButton.Left);
		Assert.IsGreaterThan (200, session.Context.BwCurve[128]);

		window.MouseMove (drop);
		window.MouseUp (drop, MouseButton.Left);

		Assert.AreEqual (128, session.Context.BwCurve[128]);
		window.Close ();
	}

	[UiTestMethod]
	public void CurvePress_LeavesAnotherEditorsToneTableUnset ()
	{
		AvaloniaApp.Ensure ();
		var session = new PhotoEditorSession (PhotoEditorCatalog.Crop);
		var editor = LaidOutCurve (session);

		editor.PressAt (new Point (131, SplineCurve.Radius));

		Assert.IsNull (session.Context.BwCurve);
	}

	[UiTestMethod]
	public void NewBlackAndWhiteSession_StartsFromTheStraightLine ()
	{
		AvaloniaApp.Ensure ();
		var holder = new CurveHolder { EditorSession = new PhotoEditorSession (PhotoEditorCatalog.BW) };
		var editor = LaidOutCurve (holder);
		editor.PressAt (new Point (131, SplineCurve.Radius));
		Assert.IsGreaterThan (200, holder.EditorSession.Context.BwCurve[128]);

		var next = new PhotoEditorSession (PhotoEditorCatalog.BW);
		holder.EditorSession = next;
		editor.PressAt (new Point (131, SplineCurve.Radius));

		Assert.IsGreaterThan (200, next.Context.BwCurve[128]);
		Assert.AreEqual (0, next.Context.BwCurve[0]);
		Assert.AreEqual (255, next.Context.BwCurve[255]);
	}

	static SplineCurveEditor ShownCurve (PhotoEditorSession session)
	{
		const double side = 256 + SplineCurve.Radius * 2;
		var editor = new SplineCurveEditor { Width = side, Height = side };
		editor.Bind (SplineCurveEditor.CurveProperty, new Binding (nameof (PhotoEditorContext.BwCurve)) {
			Source = session.Context,
			Mode = BindingMode.OneWayToSource
		});
		editor.Session = session;
		var canvas = new Canvas { Width = side, Height = side + 40 };
		Canvas.SetTop (editor, 40);
		canvas.Children.Add (editor);
		var window = new Window { Width = side, Height = side + 80, Content = canvas };
		window.Show ();
		editor.UpdateLayout ();
		return editor;
	}

	static SplineCurveEditor LaidOutCurve (PhotoEditorSession session)
	{
		const double side = 256 + SplineCurve.Radius * 2;
		var editor = new SplineCurveEditor { Width = side, Height = side };
		editor.Bind (SplineCurveEditor.CurveProperty, new Binding (nameof (PhotoEditorContext.BwCurve)) {
			Source = session.Context,
			Mode = BindingMode.OneWayToSource
		});
		editor.Session = session;
		editor.Measure (new Size (side, side));
		editor.Arrange (new Rect (0, 0, side, side));
		return editor;
	}

	static SplineCurveEditor LaidOutCurve (CurveHolder holder)
	{
		const double side = 256 + SplineCurve.Radius * 2;
		var editor = new SplineCurveEditor { Width = side, Height = side };
		editor.Bind (SplineCurveEditor.SessionProperty, new Binding (nameof (CurveHolder.EditorSession)) { Source = holder });
		editor.Bind (SplineCurveEditor.CurveProperty, new Binding ("EditorSession.Context.BwCurve") {
			Source = holder,
			Mode = BindingMode.OneWayToSource
		});
		editor.Measure (new Size (side, side));
		editor.Arrange (new Rect (0, 0, side, side));
		return editor;
	}

	sealed class CurveHolder : INotifyPropertyChanged
	{
		PhotoEditorSession? session;

		public PhotoEditorSession? EditorSession {
			get => session;
			set {
				session = value;
				PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (EditorSession)));
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}

	static string ClientRoot ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return Path.Combine (dir.FullName, "src", "Clients", "FSpot.Avalonia");
		}
		throw new InvalidOperationException ("F-Spot.slnx was not found above the test output.");
	}

	[UiTestMethod]
	public async Task CropSession_DoesNotRenderALivePreview ()
	{
		AvaloniaApp.Ensure ();
		var viewModel = ViewModelWithFocusedPhoto ();
		viewModel.Preview = new WriteableBitmap (new PixelSize (8, 8), new Vector (96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
		viewModel.ApplyEditorCommand.Execute (PhotoEditorCatalog.Crop);

		viewModel.EditorSession!.Context.TiltDegrees = 10;
		await viewModel.LivePreviewTask;

		Assert.IsNull (viewModel.EditedPreview);
	}
}

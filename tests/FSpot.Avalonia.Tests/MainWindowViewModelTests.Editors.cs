// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

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

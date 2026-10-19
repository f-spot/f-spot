//
// MainWindowViewModel.Editors.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Avalonia.Imaging;
using FSpot.Editors;
using FSpot.Imaging;
using FSpot.Settings;

namespace FSpot.Avalonia.ViewModels;

// Editor-page parity with GTK: crop ratio constraints, the live preview the
// Tilt/SoftFocus/Color/BW editors render when they open, when the photo
// changes, and on every slider change, and the red-eye threshold preference.
public partial class MainWindowViewModel
{
	// GTK Editor.UpdatePreview renders into a copy scaled to the view.
	internal const int LivePreviewMaxEdge = 1200;

	[ObservableProperty]
	CropConstraint? selectedCropConstraint;

	[ObservableProperty]
	double cropSelectionRatio;

	[ObservableProperty]
	[NotifyPropertyChangedFor (nameof (DisplayedPreview))]
	Bitmap? editedPreview;

	ImageBuffer? livePreviewSource;
	ImageBuffer? storedPreview;
	ImageBuffer? pendingEditorSource;
	Bitmap? uncorrectedPreview;
	int livePreviewGeneration;
	Task livePreviewTask = Task.CompletedTask;
	bool editingCustomRatios;

	public ObservableCollection<CropConstraint> CropConstraintOptions { get; } = new ();

	public bool ShowCropSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.Crop.Id;

	public Bitmap? DisplayedPreview => EditedPreview ?? Preview;

	partial void OnEditedPreviewChanged (Bitmap? value)
	{
		if (SelectedPhoto is not null)
			RequestHistogram (SelectedPhoto);
	}

	internal Task LivePreviewTask => livePreviewTask;

	// The buffer the open editor processes. The screen bitmap is a separate copy.
	internal ImageBuffer? EditorSource => livePreviewSource ?? storedPreview;

	partial void OnEditorSessionChanged (PhotoEditorSession? oldValue, PhotoEditorSession? newValue)
	{
		if (oldValue is not null)
			oldValue.Context.PropertyChanged -= OnEditorContextChanged;

		ClearLivePreview ();

		if (newValue is not null) {
			// GTK Editor.CreateContext reads Preferences.EditRedeyeThreshold.
			newValue.Context.RedEyeThreshold = Preferences.Get<int> (Preferences.EditRedeyeThreshold);
			if (PhotoEditorSession.HasLivePreview (newValue.Editor)) {
				newValue.Context.PropertyChanged += OnEditorContextChanged;
				RequestLivePreview ();
			}
			if (newValue.Editor.Id == PhotoEditorCatalog.Crop.Id)
				ReloadCropConstraints ();
		}

		OnPropertyChanged (nameof (ShowCropSettings));
	}

	partial void OnPreviewChanged (Bitmap? oldValue, Bitmap? newValue)
	{
		OnPropertyChanged (nameof (DisplayedPreview));
		ClearLivePreview ();
		// UpdatePreviewAsync hands off the stored pixels for this bitmap. A preview
		// assigned directly, as the tests do, has no handoff and is read as itself.
		storedPreview = newValue is null ? null : pendingEditorSource;
		pendingEditorSource = null;
		if (EditorSession is not null && PhotoEditorSession.HasLivePreview (EditorSession.Editor))
			RequestLivePreview ();
		// CropEditor.UpdateSelectionCombo: a new photo drops "Same as photo".
		if (SelectedCropConstraint?.Kind == CropConstraintKind.SameAsPhoto && CropConstraintOptions.Count > 0)
			SelectedCropConstraint = CropConstraintOptions[0];
	}

	void ReloadCropConstraints ()
	{
		CropConstraintOptions.Clear ();
		foreach (var constraint in CropConstraints.Build (CropConstraints.LoadCustom ()))
			CropConstraintOptions.Add (constraint);
		SelectedCropConstraint = CropConstraintOptions[0];
		CropSelectionRatio = 0;
	}

	partial void OnSelectedCropConstraintChanged (CropConstraint? value)
	{
		if (value is null || editingCustomRatios)
			return;
		if (value.Kind == CropConstraintKind.AddCustom) {
			_ = EditCustomCropRatiosAsync ();
			return;
		}
		CropSelectionRatio = CropConstraints.RatioFor (value, PreviewPixelWidth, PreviewPixelHeight);
	}

	async Task EditCustomCropRatiosAsync ()
	{
		editingCustomRatios = true;
		try {
			var custom = CropConstraints.LoadCustom ();
			var result = await shell.EditCustomCropRatiosAsync (custom);
			if (result is not null)
				CropConstraints.SaveCustom (result);
		} catch (Exception ex) {
			Logger.Log.Warning (ex, "Failed to edit custom crop ratios");
		} finally {
			editingCustomRatios = false;
		}

		// CropEditor.PopulateConstraints repopulates and selects "No Constraint".
		if (ShowCropSettings)
			ReloadCropConstraints ();
	}

	void OnEditorContextChanged (object? sender, PropertyChangedEventArgs e)
		=> RequestLivePreview ();

	void ClearLivePreview ()
	{
		livePreviewGeneration++;
		livePreviewSource = null;
		var previous = EditedPreview;
		var chart = uncorrectedPreview;
		// Drop the chart before the preview change asks for a histogram, or that
		// histogram counts a bitmap this method is about to dispose.
		uncorrectedPreview = null;
		EditedPreview = null;
		if (!ReferenceEquals (chart, previous))
			chart?.Dispose ();
		previous?.Dispose ();
	}

	void RequestLivePreview ()
	{
		var session = EditorSession;
		var source = Preview;
		if (session is null || source is null || !PhotoEditorSession.HasLivePreview (session.Editor))
			return;

		// storedPreview survives ClearLivePreview, so opening the editor does not
		// reread the screen bitmap. The working copy is separate from that store.
		livePreviewSource ??= storedPreview is ImageBuffer stored
			? ImageBuffers.Clone (stored)
			: LivePreviewSource (source);
		int generation = ++livePreviewGeneration;
		livePreviewTask = RenderLivePreviewAsync (session, livePreviewSource, generation);
	}

	static ImageBuffer LivePreviewSource (Bitmap source)
	{
		var buffer = AvaloniaImagingBackend.ToRgbBuffer (source);
		if (Math.Max (buffer.Width, buffer.Height) <= LivePreviewMaxEdge)
			return buffer;
		return new AvaloniaImagingBackend ().ScaleToMaxSize (buffer, LivePreviewMaxEdge, LivePreviewMaxEdge, upscale: false);
	}

	async Task RenderLivePreviewAsync (PhotoEditorSession session, ImageBuffer source, int generation)
	{
		var editor = session.Editor;
		var context = session.CreateContext ();
		ScreenAndChart pair = default;
		try {
			pair = await Task.Run (() => {
				if (generation != Volatile.Read (ref livePreviewGeneration))
					return default;
				var output = PhotoEditorOperations.Process (editor, source, null, context);
				// The histogram counts the editor output. The screen gets one conversion after it.
				var measured = (Bitmap)AvaloniaImagingBackend.ToBgraBitmap (output);
				var converted = DisplayColor.Apply (output, DisplayColor.Selected ());
				if (ReferenceEquals (converted, output))
					return new ScreenAndChart (measured, measured);
				var shown = (Bitmap)AvaloniaImagingBackend.ToBgraBitmap (converted);
				return new ScreenAndChart (shown, measured);
			}).ConfigureAwait (true);
		} catch (Exception ex) {
			pair.Release ();
			Logger.Log.Debug (ex, "Failed to render {Editor} preview", editor.Id);
			return;
		}

		if (pair.Screen is null)
			return;
		if (generation != livePreviewGeneration || !ReferenceEquals (session, EditorSession)) {
			pair.Release ();
			return;
		}

		var previous = EditedPreview;
		var previousChart = uncorrectedPreview;
		uncorrectedPreview = pair.Chart;
		EditedPreview = pair.Screen;
		if (!ReferenceEquals (previousChart, previous))
			previousChart?.Dispose ();
		if (!ReferenceEquals (previous, pair.Screen))
			previous?.Dispose ();
	}

	readonly record struct ScreenAndChart (Bitmap? Screen, Bitmap? Chart)
	{
		public void Release ()
		{
			if (Screen is null)
				return;
			if (!ReferenceEquals (Chart, Screen))
				Chart?.Dispose ();
			Screen.Dispose ();
		}
	}
}

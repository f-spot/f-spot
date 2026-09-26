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
	int livePreviewGeneration;
	Task livePreviewTask = Task.CompletedTask;
	bool editingCustomRatios;

	public ObservableCollection<CropConstraint> CropConstraintOptions { get; } = new ();

	public bool ShowCropSettings => EditorSession?.Editor.Id == PhotoEditorCatalog.Crop.Id;

	public Bitmap? DisplayedPreview => EditedPreview ?? Preview;

	internal Task LivePreviewTask => livePreviewTask;

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
		EditedPreview = null;
		previous?.Dispose ();
	}

	void RequestLivePreview ()
	{
		var session = EditorSession;
		var source = Preview;
		if (session is null || source is null || !PhotoEditorSession.HasLivePreview (session.Editor))
			return;

		livePreviewSource ??= LivePreviewSource (source);
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
		Bitmap? rendered = null;
		try {
			rendered = await Task.Run (() => {
				if (generation != Volatile.Read (ref livePreviewGeneration))
					return null;
				var output = PhotoEditorOperations.Process (editor, source, null, context);
				return (Bitmap)AvaloniaImagingBackend.ToBgraBitmap (output);
			}).ConfigureAwait (true);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to render {Editor} preview", editor.Id);
			return;
		}

		if (rendered is null)
			return;
		if (generation != livePreviewGeneration || !ReferenceEquals (session, EditorSession)) {
			rendered.Dispose ();
			return;
		}

		var previous = EditedPreview;
		EditedPreview = rendered;
		previous?.Dispose ();
	}
}

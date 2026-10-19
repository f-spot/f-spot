// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.Input;

using FSpot.Avalonia.Imaging;
using FSpot.Imaging;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Edit Tag Icon: design a tag icon from one of the tag's photos (pick it with the spinner,
/// drag a square to crop), from a photo file, or from a predefined icon, or choose No image.
/// </summary>
public partial class EditTagIconWindow : Window
{
	const int PhotoDecodeWidth = 1024;

	readonly TagIconSource source;
	readonly AvaloniaImagingBackend backend = new ();
	ImageBuffer? photoBuffer;
	TagIconChoice? preview;
	bool hasPreview;
	bool showingPhoto;
	int displayedPhoto = -1;
	int choiceGeneration;

	public EditTagIconWindow ()
		: this (new TagIconSource (new FSpot.Core.Tag (null, 0, string.Empty), []), null)
	{
	}

	public EditTagIconWindow (TagIconSource source, TagIconChoice? current)
	{
		this.source = source;
		InitializeComponent ();
		Title = string.Format (CultureInfo.CurrentCulture, Strings.EditIconForTagX, source.Tag.Name);
		AutomationProperties.SetName (this, Title);
		PreviewHeading.Text = DialogText.Plain (DialogStrings.BPreviewB);
		PhotoTab.Header = DialogText.Plain (DialogStrings.BFromPhotoB);
		FileTab.Header = DialogText.Plain (DialogStrings.BFromExternalPhotoB);
		PredefinedTab.Header = DialogText.Plain (DialogStrings.BPredefinedIconsB);
		NoImageButton.Content = DialogStrings.NoImage;
		AutomationProperties.SetName (NoImageButton, GtkMnemonic.Display (DialogStrings.NoImage));
		AutomationProperties.SetName (PhotoIndex, Strings.PhotoXOfY);
		AutomationProperties.SetName (PredefinedList, PredefinedTab.Header as string);

		ShowCurrent (current);

		int count = source.PhotoPaths.Count;
		if (count > 0) {
			PhotoIndex.Maximum = count;
			PhotoIndex.Value = 1;
			PhotoIndex.ValueChanged += (_, _) => OnPhotoIndexChanged ();
			PhotoSelection.SelectionCommand = new RelayCommand<PixelRect> (OnPhotoSelection);
			ShowPhoto (0, pending: false);
		} else {
			NoPhotosText.Text = DialogText.Plain (string.Format (CultureInfo.CurrentCulture, Strings.HowToAddImageToTagFromPhotoX, source.Tag.Name)).TrimEnd ('"');
			NoPhotosText.IsVisible = true;
			PhotoArea.IsVisible = false;
			PhotoControls.IsVisible = false;
		}

		PredefinedList.ItemsSource = PredefinedTagIcons.Names (PackedIconFiles.All);
		PredefinedList.SelectionChanged += (_, _) => {
			if (PredefinedList.SelectedItem is string name)
				SetPreview (new TagIconChoice (name, null));
		};
	}

	/// <summary>
	/// The chosen icon, <see cref="TagIconChoice.NoImage"/> for No image, or null on cancel.
	/// </summary>
	public TagIconChoice? Result { get; private set; }

	internal TagIconChoice? Preview => preview;
	internal int PhotoNumber => (int)(PhotoIndex.Value ?? 1);

	void ShowCurrent (TagIconChoice? current)
	{
		if (current is not null) {
			SetPreview (current, pending: false);
			return;
		}
		if (source.Tag.ThemeIconName is string theme) {
			SetPreview (new TagIconChoice (theme, null), pending: false);
			return;
		}
		ImageBuffer? icon = null;
		try {
			icon = source.Tag.Icon;
		} catch (InvalidOperationException) {
			icon = null;
		}
		SetPreview (new TagIconChoice (null, icon), pending: false);
	}

	void SetPreview (TagIconChoice choice, bool pending = true)
	{
		choiceGeneration++;
		preview = choice;
		hasPreview |= pending;
		PreviewSvg.Path = choice.ThemeIconName is null ? null : TagIcons.UriFor (choice.ThemeIconName);
		PreviewSvg.IsVisible = choice.ThemeIconName is not null;
		PreviewImage.Source = choice.Icon is null ? null : AvaloniaImagingBackend.ToBgraBitmap (DisplayColor.Apply (choice.Icon, DisplayColor.Selected ()));
		PreviewImage.IsVisible = choice.Icon is not null;
	}

	void OnPhotoIndexChanged ()
	{
		int count = source.PhotoPaths.Count;
		int number = PhotoNumber;
		// The GTK spinner wraps around.
		if (number > count) {
			PhotoIndex.Value = 1;
			return;
		}
		if (number < 1) {
			PhotoIndex.Value = count;
			return;
		}
		// The spinner reports the photo the dialog already opened. That is not a choice.
		if (number - 1 == displayedPhoto)
			return;
		ShowPhoto (number - 1);
	}

	internal void ShowPhoto (int index, bool pending = true)
	{
		displayedPhoto = index;
		int generation = ++choiceGeneration;
		_ = ShowPhotoAsync (index, pending, generation);
	}

	async Task ShowPhotoAsync (int index, bool pending, int generation)
	{
		PhotoLabel.Text = string.Format (CultureInfo.CurrentCulture, Strings.PhotoXOfY, index + 1, source.PhotoPaths.Count);
		Bitmap? bitmap = null;
		try {
			// The crop keeps the stored pixels. The photo on screen is converted after that copy.
			ImageBuffer? stored;
			(stored, bitmap) = await DecodeGate.RunAsync (() => {
				var decoded = AvaloniaPhotoDecoder.DecodeFile (source.PhotoPaths[index], PhotoDecodeWidth);
				var pixels = AvaloniaImagingBackend.ToRgbBuffer (decoded);
				return (pixels, DisplayColor.Present (decoded));
			}, CancellationToken.None);
			if (index != PhotoNumber - 1) {
				bitmap?.Dispose ();
				return;
			}
			photoBuffer = stored;
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to load {Path} for a tag icon", source.PhotoPaths[index]);
			if (index != PhotoNumber - 1) {
				bitmap?.Dispose ();
				return;
			}
			bitmap?.Dispose ();
			bitmap = null;
			photoBuffer = null;
		}
		var old = PhotoImage.Source as IDisposable;
		PhotoImage.Source = bitmap;
		// Replacing the bitmap clears the marquee and reports an empty selection.
		// That report is the image change, not a selection the user drew.
		showingPhoto = true;
		try {
			PhotoSelection.Preview = bitmap;
		} finally {
			showingPhoto = false;
		}
		old?.Dispose ();
		// A photo the user picked becomes the icon. The photo opened with the dialog does not.
		// A choice made while this decode was in flight keeps its generation.
		if (generation == choiceGeneration && photoBuffer is not null)
			SetPreview (new TagIconChoice (null, TagIconImage.Create (photoBuffer, null, backend.Scale)), pending);
	}

	internal void OnPhotoSelection (PixelRect selection)
	{
		if (showingPhoto || photoBuffer is null)
			return;
		SetPreview (new TagIconChoice (null, TagIconImage.Create (photoBuffer, selection, backend.Scale)));
	}

	void OnFromFile (object? sender, RoutedEventArgs e)
		=> _ = PickExternalAsync ();

	async Task PickExternalAsync ()
	{
		var files = await StorageProvider.OpenFilePickerAsync (new FilePickerOpenOptions {
			Title = Strings.SelectPhotoFromFile,
			AllowMultiple = false,
			FileTypeFilter = [FilePickerFileTypes.ImageAll]
		});
		if (files.Count == 0)
			return;
		string? path = files[0].TryGetLocalPath ();
		await LoadExternalAsync (path ?? files[0].Path.ToString ());
	}

	internal async Task LoadExternalAsync (string path)
	{
		try {
			using var bitmap = await DecodeGate.RunAsync (
				() => AvaloniaPhotoDecoder.DecodeFile (path, PhotoDecodeWidth),
				CancellationToken.None);
			var buffer = AvaloniaImagingBackend.ToRgbBuffer (bitmap);
			SetPreview (new TagIconChoice (null, TagIconImage.Create (buffer, null, backend.Scale)));
			FileName.Text = path;
		} catch (Exception) {
			await MessageDialog.ShowAsync (this, Strings.UnableToLoadImage,
				string.Format (CultureInfo.CurrentCulture, Strings.UnableToLoadXAsIconForTheTag, path),
				MessageDialogKind.Error);
		}
	}

	internal void Accept ()
	{
		if (hasPreview && preview is not null && !preview.Clears)
			Result = preview;
		Close ();
	}

	internal void ChooseNoImage ()
	{
		Result = TagIconChoice.NoImage;
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnNoImage (object? sender, RoutedEventArgs e)
		=> ChooseNoImage ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();

	protected override void OnClosed (EventArgs e)
	{
		(PhotoImage.Source as IDisposable)?.Dispose ();
		PhotoImage.Source = null;
		showingPhoto = true;
		try {
			PhotoSelection.Preview = null;
		} finally {
			showingPhoto = false;
		}
		base.OnClosed (e);
	}
}

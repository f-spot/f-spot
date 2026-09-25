// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using FSpot;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Resources.Lang;

using Hyena;

namespace FSpot.Avalonia;

public partial class AdjustTimeWindow : Window
{
	readonly IReadOnlyList<(string Name, DateTime Time, string Path)> photos;
	readonly IPhotoThumbnailLoader thumbnails;
	readonly List<AdjustTimePhotoItem> trayItems;

	// Guards against the DateEdit <-> difference-field round trip re-entering itself, and
	// against the tray selection and the N-of-M spinner fighting over who moved the reference.
	bool updatingFromCode;
	double uiScale = 1;
	int previewGeneration;

	public AdjustTimeWindow ()
		: this (new[] { ("", DateTime.Now, "") })
	{
	}

	public AdjustTimeWindow (IReadOnlyList<(string Name, DateTime Time, string Path)> photos, IPhotoThumbnailLoader? thumbnails = null)
	{
		this.photos = photos is { Count: > 0 } ? photos : new[] { ("", DateTime.Now, "") };
		this.thumbnails = thumbnails ?? new AvaloniaThumbnailLoader ();
		InitializeComponent ();
		DifferenceBox.PropertyChanged += (_, change) => {
			if (change.Property == TextBox.TextProperty)
				ApplyDifference ();
		};

		trayItems = this.photos.Select (photo => new AdjustTimePhotoItem (photo.Name, photo.Time, photo.Path)).ToList ();
		PhotoList.ItemsSource = trayItems;
		PhotoList.SelectionChanged += (_, _) => OnReferenceChanged ();

		bool multiple = this.photos.Count > 1;
		CounterRow.IsVisible = multiple;
		ActionSection.IsVisible = multiple;
		TraySection.IsVisible = multiple;

		PhotoSpin.Minimum = 1;
		PhotoSpin.Maximum = this.photos.Count;

		ShiftAll.IsChecked = true;
		SpacingBox.IsEnabled = false;

		Opened += (_, _) => {
			uiScale = RenderScaling;
			_ = LoadPreviewAsync ();
			_ = LoadTrayThumbnailsAsync ();
		};
		ScalingChanged += (_, _) => uiScale = RenderScaling;

		PhotoList.SelectedIndex = 0;
		OnReferenceChanged ();
	}

	public TimeAdjustChoice? Choice { get; private set; }

	int ReferenceIndex => Math.Clamp (PhotoList.SelectedIndex, 0, photos.Count - 1);

	DateTime AdjustedDateTime {
		get {
			DateTime original = photos[ReferenceIndex].Time;
			DateTime date = AdjustedDatePart.SelectedDate?.Date ?? original.Date;
			TimeSpan time = AdjustedTimePart.SelectedTime ?? original.TimeOfDay;
			return date + time;
		}
	}

	public static bool TryParseHours (string? text, out double hours)
	{
		hours = 0;
		if (string.IsNullOrWhiteSpace (text))
			return false;
		return double.TryParse (text, NumberStyles.Float, CultureInfo.CurrentCulture, out hours)
			|| double.TryParse (text, NumberStyles.Float, CultureInfo.InvariantCulture, out hours);
	}

	public static bool TryParseOffset (string? text, out TimeSpan offset)
	{
		offset = TimeSpan.Zero;
		if (string.IsNullOrWhiteSpace (text))
			return false;
		return TimeSpan.TryParse (text, CultureInfo.CurrentCulture, out offset)
			|| TimeSpan.TryParse (text, CultureInfo.InvariantCulture, out offset);
	}

	public static string FormatOffset (TimeSpan offset)
		=> offset.ToString ("c", CultureInfo.InvariantCulture);

	void OnReferenceChanged ()
	{
		int index = ReferenceIndex;
		var photo = photos[index];

		PhotoName.Text = photo.Name;
		OldTime.Text = photo.Time.ToString ("g", CultureInfo.CurrentCulture);
		CountLabel.Text = string.Format (CultureInfo.CurrentCulture, Strings.PhotoOutOfPhotos, index + 1, photos.Count);

		updatingFromCode = true;
		try {
			PhotoSpin.Value = index + 1;
			AdjustedDatePart.SelectedDate = new DateTimeOffset (photo.Time.Date, TimeSpan.Zero);
			AdjustedTimePart.SelectedTime = photo.Time.TimeOfDay;
			DifferenceBox.Text = FormatOffset (TimeSpan.Zero);
		} finally {
			updatingFromCode = false;
		}

		StartingAtLabel.Text = string.Format (CultureInfo.CurrentCulture, Strings.MinStartingAtX, photo.Name);
		RefreshShiftLabel (TimeSpan.Zero);
		_ = LoadPreviewAsync ();
	}

	void RefreshShiftLabel (TimeSpan offset)
		=> ShiftAll.Content = string.Format (CultureInfo.CurrentCulture, Strings.ShiftAllPhotosByX, FormatOffset (offset));

	void OnSpinChanged (object? sender, NumericUpDownValueChangedEventArgs e)
	{
		if (updatingFromCode || PhotoSpin.Value is not decimal value)
			return;
		PhotoList.SelectedIndex = Math.Clamp ((int)value - 1, 0, photos.Count - 1);
	}

	void OnAdjustedDateChanged (object? sender, DatePickerSelectedValueChangedEventArgs e)
		=> OnAdjustedDateTimeChanged ();

	void OnAdjustedTimeChanged (object? sender, TimePickerSelectedValueChangedEventArgs e)
		=> OnAdjustedDateTimeChanged ();

	void OnAdjustedDateTimeChanged ()
	{
		if (updatingFromCode)
			return;
		TimeSpan offset = AdjustedDateTime - photos[ReferenceIndex].Time;
		updatingFromCode = true;
		try {
			DifferenceBox.Text = FormatOffset (offset);
		} finally {
			updatingFromCode = false;
		}
		RefreshShiftLabel (offset);
	}

	void OnDifferenceChanged (object? sender, TextChangedEventArgs e)
		=> ApplyDifference ();

	void ApplyDifference ()
	{
		if (updatingFromCode)
			return;
		if (!TryParseOffset (DifferenceBox.Text, out TimeSpan offset))
			return;

		DateTime adjusted = photos[ReferenceIndex].Time + offset;
		// The pickers raise their change after the property set, and that change still
		// sees the previous time. Ignore it, or it writes 00:00:00 back over the offset.
		AdjustedDatePart.SelectedDateChanged -= OnAdjustedDateChanged;
		AdjustedTimePart.SelectedTimeChanged -= OnAdjustedTimeChanged;
		AdjustedDatePart.SelectedDate = new DateTimeOffset (adjusted.Date, TimeSpan.Zero);
		AdjustedTimePart.SelectedTime = adjusted.TimeOfDay;
		RefreshShiftLabel (offset);
		Dispatcher.UIThread.Post (() => {
			AdjustedDatePart.SelectedDateChanged += OnAdjustedDateChanged;
			AdjustedTimePart.SelectedTimeChanged += OnAdjustedTimeChanged;
		}, DispatcherPriority.Background);
	}

	void OnModeChanged (object? sender, RoutedEventArgs e)
	{
		SpacingBox.IsEnabled = SpaceAll.IsChecked == true;
		UpdateOkEnabled ();
	}

	void OnSpacingChanged (object? sender, TextChangedEventArgs e)
		=> UpdateOkEnabled ();

	void UpdateOkEnabled ()
		=> OkButton.IsEnabled = SpaceAll.IsChecked != true || TryParseHours (SpacingBox.Text, out _);

	async Task LoadPreviewAsync ()
	{
		int generation = ++previewGeneration;
		int index = ReferenceIndex;
		string path = photos[index].Path;
		if (string.IsNullOrEmpty (path)) {
			PreviewImage.Source = null;
			return;
		}
		try {
			var bitmap = await thumbnails.LoadAsync (path, HiDpi.PixelSize (HiDpi.AdjustTimePreviewDip, uiScale), CancellationToken.None);
			if (generation != previewGeneration)
				bitmap?.Dispose ();
			else
				PreviewImage.Source = bitmap;
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to preview the Adjust Time reference photo {Path}", path);
		}
	}

	async Task LoadTrayThumbnailsAsync ()
	{
		int size = HiDpi.PixelSize (HiDpi.AdjustTimeTrayDip, uiScale);
		foreach (var item in trayItems) {
			if (string.IsNullOrEmpty (item.Path))
				continue;
			try {
				item.Thumbnail = await thumbnails.LoadAsync (item.Path, size, CancellationToken.None);
			} catch (Exception ex) {
				Logger.Log.Debug (ex, "Failed to thumbnail the Adjust Time tray photo {Path}", item.Path);
			}
		}
	}

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();

	void OnOk (object? sender, RoutedEventArgs e)
	{
		int anchorIndex = ReferenceIndex;
		DateTime anchorTime = AdjustedDateTime;
		bool shift = ShiftAll.IsChecked == true;
		double spacingMinutes = 0;

		if (!shift && !TryParseHours (SpacingBox.Text, out spacingMinutes))
			return;

		Choice = shift
			? new TimeAdjustChoice {
				ShiftByOffset = true,
				Hours = (anchorTime - photos[anchorIndex].Time).TotalHours,
				AnchorIndex = anchorIndex,
				AnchorTime = anchorTime
			}
			: new TimeAdjustChoice {
				ShiftByOffset = false,
				SpacingMinutes = spacingMinutes,
				AnchorIndex = anchorIndex,
				AnchorTime = anchorTime
			};
		Close ();
	}
}

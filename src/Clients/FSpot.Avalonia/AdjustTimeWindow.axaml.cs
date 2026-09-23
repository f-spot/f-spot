// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

public partial class AdjustTimeWindow : Window
{
	readonly IReadOnlyList<(string Name, DateTime Time)> photos;

	public AdjustTimeWindow ()
		: this (new[] { ("", DateTime.Now) })
	{
	}

	public AdjustTimeWindow (IReadOnlyList<(string Name, DateTime Time)> photos)
	{
		this.photos = photos is { Count: > 0 } ? photos : new[] { ("", DateTime.Now) };
		InitializeComponent ();
		PhotoList.ItemsSource = this.photos.Select (photo => photo.Name).ToList ();
		PhotoList.SelectedIndex = 0;
		PhotoList.SelectionChanged += (_, _) => Refresh ();
		Refresh ();
	}

	public TimeAdjustChoice? Choice { get; private set; }

	public static bool TryParseHours (string? text, out double hours)
	{
		hours = 0;
		if (string.IsNullOrWhiteSpace (text))
			return false;
		return double.TryParse (text, NumberStyles.Float, CultureInfo.CurrentCulture, out hours)
			|| double.TryParse (text, NumberStyles.Float, CultureInfo.InvariantCulture, out hours);
	}

	void OnModeChanged (object? sender, RoutedEventArgs e)
		=> Refresh ();

	void OnHoursChanged (object? sender, TextChangedEventArgs e)
		=> Refresh ();

	void Refresh ()
	{
		if (OldTime is null || photos.Count == 0)
			return;
		int index = PhotoList.SelectedIndex;
		if (index < 0)
			index = 0;
		var photo = photos[index];
		PhotoName.Text = photo.Name;
		OldTime.Text = photo.Time.ToString ("g", CultureInfo.CurrentCulture);
		bool shift = ShiftAll.IsChecked == true;
		SpacingBox.IsEnabled = !shift;
		if (!TryParseHours (shift ? HoursBox.Text : SpacingBox.Text, out double amount)) {
			NewTime.Text = string.Empty;
			return;
		}
		DateTime next = shift
			? photo.Time.AddHours (amount)
			: photo.Time.AddMinutes (amount * (index - 0));
		NewTime.Text = next.ToString ("g", CultureInfo.CurrentCulture);
	}

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();

	void OnOk (object? sender, RoutedEventArgs e)
	{
		bool shift = ShiftAll.IsChecked == true;
		if (!TryParseHours (shift ? HoursBox.Text : SpacingBox.Text, out double amount))
			return;
		if (!shift && !TryParseHours (SpacingBox.Text, out amount))
			return;
		Choice = new TimeAdjustChoice {
			ShiftByOffset = shift,
			Hours = shift && TryParseHours (HoursBox.Text, out double hours) ? hours : 0,
			SpacingMinutes = shift ? 0 : amount,
			AnchorIndex = Math.Max (0, PhotoList.SelectedIndex)
		};
		Close ();
	}
}

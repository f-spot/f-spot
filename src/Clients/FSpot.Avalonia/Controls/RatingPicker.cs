// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace FSpot.Avalonia.Controls;

/// <summary>
/// A 0 to 5 star picker like the GTK RatingEntry. Clicking a star sets that rating;
/// clicking the highest lit star again steps down one. Left/Right, Home/End and the
/// digit keys also set it.
/// </summary>
public sealed class RatingPicker : StackPanel
{
	public const uint MaxRating = 5;
	const string Lit = "★";
	const string Unlit = "☆";

	public static readonly StyledProperty<uint> ValueProperty =
		AvaloniaProperty.Register<RatingPicker, uint> (nameof (Value), coerce: (_, value) => Math.Min (value, MaxRating));

	readonly Button[] stars = new Button[MaxRating];

	public RatingPicker ()
	{
		Orientation = Orientation.Horizontal;
		Spacing = 0;
		Focusable = true;
		for (uint i = 0; i < MaxRating; i++) {
			uint rating = i + 1;
			var star = new Button {
				Classes = { "star" },
				Padding = new Thickness (2, 0),
				FontSize = 20,
				Focusable = false,
				Tag = rating
			};
			star.Click += (_, _) => Value = Value == rating ? rating - 1 : rating;
			AutomationProperties.SetName (star, rating.ToString (System.Globalization.CultureInfo.CurrentCulture));
			stars[i] = star;
			Children.Add (star);
		}
		Refresh ();
	}

	public uint Value {
		get => GetValue (ValueProperty);
		set => SetValue (ValueProperty, value);
	}

	public event EventHandler? ValueChanged;

	protected override void OnPropertyChanged (AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged (change);
		if (change.Property != ValueProperty)
			return;
		Refresh ();
		ValueChanged?.Invoke (this, EventArgs.Empty);
	}

	protected override void OnKeyDown (KeyEventArgs e)
	{
		uint? next = e.Key switch {
			Key.Left or Key.Down => Value == 0 ? 0 : Value - 1,
			Key.Right or Key.Up => Math.Min (Value + 1, MaxRating),
			Key.Home => 0,
			Key.End => MaxRating,
			>= Key.D0 and <= Key.D5 => (uint)(e.Key - Key.D0),
			>= Key.NumPad0 and <= Key.NumPad5 => (uint)(e.Key - Key.NumPad0),
			_ => null
		};
		if (next is uint rating) {
			Value = rating;
			e.Handled = true;
			return;
		}
		base.OnKeyDown (e);
	}

	void Refresh ()
	{
		for (int i = 0; i < stars.Length; i++)
			stars[i].Content = i < Value ? Lit : Unlit;
		AutomationProperties.SetItemStatus (this, Value.ToString (System.Globalization.CultureInfo.CurrentCulture));
	}
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.RegularExpressions;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

// SingleView.PreferenceDialog (viewer_preferences.ui): every change is written straight
// to the Viewer* preferences, and only one dialog is open at a time.
public partial class ViewerPreferencesWindow : Window
{
	static ViewerPreferencesWindow? shown;
	bool loading;

	public ViewerPreferencesWindow ()
	{
		InitializeComponent ();
		InterpolationHeading.Text = PlainText (ViewerStrings.BImageInterpolationB);
		InterpolationDetail.Text = PlainText (ViewerStrings.InterpolationDetail);
		TransparencyHeading.Text = PlainText (ViewerStrings.BTransparentPartsB);
		TransparencyDetail.Text = PlainText (ViewerStrings.TransparencyDetail);

		Load ();
		Interpolation.IsCheckedChanged += (_, _) => {
			if (!loading)
				ViewerSettings.Interpolation = Interpolation.IsChecked == true;
		};
		AsBackground.IsCheckedChanged += OnTransparencyToggled;
		AsCheckPattern.IsCheckedChanged += OnTransparencyToggled;
		AsCustomColor.IsCheckedChanged += OnTransparencyToggled;
		CustomColor.ColorChanged += (_, e) => {
			if (!loading)
				ViewerSettings.TransparentColor = ViewerSettings.FormatColor (e.NewColor.R, e.NewColor.G, e.NewColor.B);
		};
	}

	public static ViewerPreferencesWindow ShowUp (Window? owner)
	{
		if (shown is null) {
			shown = new ViewerPreferencesWindow ();
			shown.Closed += (_, _) => shown = null;
			if (owner is not null)
				shown.Show (owner);
			else
				shown.Show ();
		}
		shown.Activate ();
		return shown;
	}

	void Load ()
	{
		loading = true;
		try {
			Interpolation.IsChecked = ViewerSettings.Interpolation;
			switch (ViewerSettings.Transparency) {
			case ViewerTransparency.CustomColor:
				AsCustomColor.IsChecked = true;
				break;
			case ViewerTransparency.CheckPattern:
				AsCheckPattern.IsChecked = true;
				break;
			default:
				AsBackground.IsChecked = true;
				break;
			}
			ViewerSettings.TryParseColor (ViewerSettings.TransparentColor, out byte r, out byte g, out byte b);
			CustomColor.Color = Color.FromRgb (r, g, b);
		} finally {
			loading = false;
		}
	}

	void OnTransparencyToggled (object? sender, RoutedEventArgs e)
	{
		if (loading || sender is not RadioButton { IsChecked: true } radio)
			return;
		ViewerSettings.Transparency = radio == AsCheckPattern
			? ViewerTransparency.CheckPattern
			: radio == AsCustomColor ? ViewerTransparency.CustomColor : ViewerTransparency.Background;
	}

	void OnClose (object? sender, RoutedEventArgs e)
		=> Close ();

	// The GTK labels carry Pango markup (<b>, <small>, <i>); the text alone is shown and
	// the styling comes from the TextBlock.
	public static string PlainText (string? markup)
		=> string.IsNullOrEmpty (markup)
			? string.Empty
			: WebUtility.HtmlDecode (Regex.Replace (markup, "<[^>]+>", string.Empty)).Trim ();
}

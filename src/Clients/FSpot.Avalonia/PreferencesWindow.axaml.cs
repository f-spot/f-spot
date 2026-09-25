using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using FSpot;
using FSpot.Cms;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

public partial class PreferencesWindow : Window
{
	bool saved;

	public PreferencesWindow ()
	{
		InitializeComponent ();
		Closing += (_, _) => Save ();
		StoragePath.Text = LibraryPreferences.StoragePath;
		FillMetadata ();
		FillProfiles (DisplayProfile, IccProfileClass.Display, LibraryPreferences.DisplayProfile, SystemColorProfile.IsAvailable);
		FillProfiles (PrintProfile, IccProfileClass.Output, LibraryPreferences.PrintProfile, offerSystemProfile: false);
		ColorScheme.ItemsSource = new[] {
			Strings.UseSystemTheme,
			Strings.Light,
			Strings.Dark
		};
		ColorScheme.SelectedIndex = AppearanceTheme.Normalize (LibraryPreferences.ColorScheme) switch {
			AppearanceTheme.Light => 1,
			AppearanceTheme.Dark => 2,
			_ => 0
		};
		ViewerInterpolation.IsChecked = LibraryPreferences.ViewerInterpolation;
		ShowHoverPreview.IsChecked = LibraryPreferences.ShowHoverPreview;
		EmbedMetadata.IsCheckedChanged += (_, _) => SyncSidecar ();
		SyncSidecar ();
		FillTransparency ();
		MailEnabled.IsChecked = LibraryPreferences.MailToEnabled;
		SendCrashReports.IsChecked = LibraryPreferences.SendCrashReports;
	}

	void FillMetadata ()
	{
		EmbedMetadata.Content = Caption (Strings.InsideTheImageFilesWhenPossibleSmallMakesThemAccessibleToOtherImageEditingProgramsSmall);
		AlwaysSidecar.Content = Caption (Strings.NeverModifyImageFilesSmallWriteXMPFilesNextToTheImagesInsteadSmall);
		AlwaysSidecar.IsChecked = LibraryPreferences.MetadataAlwaysUseSidecar;
		if (LibraryPreferences.MetadataEmbedInImage)
			EmbedMetadata.IsChecked = true;
		else
			SeparateMetadata.IsChecked = true;
	}

	static Control Caption (string markup)
	{
		var (title, detail) = GtkMarkupText.Split (markup);
		var panel = new StackPanel { Spacing = 2 };
		panel.Children.Add (new TextBlock { Text = title, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
		if (detail.Length > 0) {
			panel.Children.Add (new TextBlock {
				Text = detail,
				FontSize = 12,
				Opacity = 0.7,
				TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
			});
		}

		return panel;
	}

	void SyncSidecar ()
		=> AlwaysSidecar.IsEnabled = EmbedMetadata.IsChecked == true;

	static void FillProfiles (ComboBox box, IccProfileClass deviceClass, string selected, bool offerSystemProfile)
	{
		var items = new List<string> { Strings.None };
		if (offerSystemProfile)
			items.Add (Strings.SystemProfile);
		items.AddRange (InstalledProfileNames.For (deviceClass));
		box.ItemsSource = items;
		string resolved = offerSystemProfile && selected == SystemColorProfile.Sentinel
			? Strings.SystemProfile
			: selected;
		box.SelectedItem = items.Contains (resolved) ? resolved : Strings.None;
	}

	void FillTransparency ()
	{
		TransparencyBackground.Content = GtkMnemonic.Display (Strings.AsBackground);
		TransparencyCheckPattern.Content = GtkMnemonic.Display (Strings.AsCheckPattern);
		TransparencyCustomColor.Content = GtkMnemonic.Display (Strings.AsCustomColor);

		switch (LibraryPreferences.ViewerTransparency) {
		case "CHECK_PATTERN":
			TransparencyCheckPattern.IsChecked = true;
			break;
		case "COLOR":
			TransparencyCustomColor.IsChecked = true;
			break;
		default: // NONE
			TransparencyBackground.IsChecked = true;
			break;
		}

		TransparencyColorHex.Text = LibraryPreferences.ViewerTransColor;
		UpdateColorSwatch ();
		SyncTransparencyColorEnabled ();

		TransparencyColorHex.TextChanged += (_, _) => UpdateColorSwatch ();
		TransparencyBackground.IsCheckedChanged += (_, _) => SyncTransparencyColorEnabled ();
		TransparencyCheckPattern.IsCheckedChanged += (_, _) => SyncTransparencyColorEnabled ();
		TransparencyCustomColor.IsCheckedChanged += (_, _) => SyncTransparencyColorEnabled ();
	}

	void SyncTransparencyColorEnabled ()
		=> TransparencyColorHex.IsEnabled = TransparencyCustomColor.IsChecked == true;

	void UpdateColorSwatch ()
	{
		if (TryParseHexColor (TransparencyColorHex.Text, out var color))
			TransparencyColorSwatch.Background = new SolidColorBrush (color);
	}

	static bool TryParseHexColor (string? text, out Color color)
	{
		color = default;
		if (string.IsNullOrWhiteSpace (text))
			return false;
		try {
			color = Color.Parse (text.Trim ());
			return true;
		} catch (FormatException) {
			return false;
		}
	}

	static string FormatHexColor (Color color)
		=> $"#{color.R:x2}{color.G:x2}{color.B:x2}";

	void OnBrowse (object? sender, RoutedEventArgs e)
		=> _ = BrowseAsync ();

	async Task BrowseAsync ()
	{
		var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions {
			Title = Strings.SelectAFolder,
			AllowMultiple = false
		});
		if (folders.Count > 0)
			StoragePath.Text = folders[0].Path.LocalPath;
	}

	void OnClose (object? sender, RoutedEventArgs e)
		=> Close ();

	void Save ()
	{
		if (saved)
			return;
		saved = true;
		if (!string.IsNullOrWhiteSpace (StoragePath.Text))
			LibraryPreferences.StoragePath = StoragePath.Text;
		LibraryPreferences.MetadataEmbedInImage = EmbedMetadata.IsChecked == true;
		LibraryPreferences.MetadataAlwaysUseSidecar = AlwaysSidecar.IsChecked == true;
		LibraryPreferences.DisplayProfile = ChosenProfile (DisplayProfile);
		LibraryPreferences.PrintProfile = ChosenProfile (PrintProfile);
		LibraryPreferences.ColorScheme = ColorScheme.SelectedIndex switch {
			1 => AppearanceTheme.Light,
			2 => AppearanceTheme.Dark,
			_ => AppearanceTheme.System
		};
		LibraryPreferences.ViewerInterpolation = ViewerInterpolation.IsChecked == true;
		LibraryPreferences.ShowHoverPreview = ShowHoverPreview.IsChecked == true;
		LibraryPreferences.ViewerTransparency = TransparencyCheckPattern.IsChecked == true ? "CHECK_PATTERN"
			: TransparencyCustomColor.IsChecked == true ? "COLOR"
			: "NONE";
		if (TryParseHexColor (TransparencyColorHex.Text, out var transColor))
			LibraryPreferences.ViewerTransColor = FormatHexColor (transColor);
		LibraryPreferences.MailToEnabled = MailEnabled.IsChecked == true;
		LibraryPreferences.SendCrashReports = SendCrashReports.IsChecked == true;
		FSpotSentry.Apply (FSpotSentry.AvaloniaClient);
		AppearanceTheme.Apply (global::Avalonia.Application.Current);
	}

	static string ChosenProfile (ComboBox box)
	{
		if (box.SelectedItem is not string name || name == Strings.None)
			return string.Empty;
		return name == Strings.SystemProfile ? SystemColorProfile.Sentinel : name;
	}
}

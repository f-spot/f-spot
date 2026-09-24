using Avalonia.Controls;
using Avalonia.Interactivity;
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
		FillProfiles (DisplayProfile, IccProfileClass.Display, LibraryPreferences.DisplayProfile);
		FillProfiles (PrintProfile, IccProfileClass.Output, LibraryPreferences.PrintProfile);
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

	static void FillProfiles (ComboBox box, IccProfileClass deviceClass, string selected)
	{
		var items = new List<string> { Strings.None };
		items.AddRange (InstalledProfileNames.For (deviceClass));
		box.ItemsSource = items;
		box.SelectedItem = items.Contains (selected) ? selected : Strings.None;
	}

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
		AppearanceTheme.Apply (global::Avalonia.Application.Current);
	}

	static string ChosenProfile (ComboBox box)
		=> box.SelectedItem is string name && name != Strings.None ? name : string.Empty;
}

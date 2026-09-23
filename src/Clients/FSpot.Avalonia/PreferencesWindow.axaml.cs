using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

public partial class PreferencesWindow : Window
{
	public PreferencesWindow ()
	{
		InitializeComponent ();
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
		ShowSidebar.IsChecked = LibraryPreferences.ShowSidebar;
		ShowTimeline.IsChecked = LibraryPreferences.ShowTimeline;
		ShowFilmstrip.IsChecked = LibraryPreferences.ShowFilmstrip;
		ImportCopyFiles.IsChecked = LibraryPreferences.ImportCopyFiles;
		ViewerInterpolation.IsChecked = LibraryPreferences.ViewerInterpolation;
		StoragePath.Text = LibraryPreferences.StoragePath;
	}

	void OnClose (object? sender, RoutedEventArgs e)
	{
		LibraryPreferences.ColorScheme = ColorScheme.SelectedIndex switch {
			1 => AppearanceTheme.Light,
			2 => AppearanceTheme.Dark,
			_ => AppearanceTheme.System
		};
		LibraryPreferences.ShowSidebar = ShowSidebar.IsChecked == true;
		LibraryPreferences.ShowTimeline = ShowTimeline.IsChecked == true;
		LibraryPreferences.ShowFilmstrip = ShowFilmstrip.IsChecked == true;
		LibraryPreferences.ImportCopyFiles = ImportCopyFiles.IsChecked == true;
		LibraryPreferences.ViewerInterpolation = ViewerInterpolation.IsChecked == true;
		if (!string.IsNullOrWhiteSpace (StoragePath.Text))
			LibraryPreferences.StoragePath = StoragePath.Text;
		AppearanceTheme.Apply (global::Avalonia.Application.Current);
		Close ();
	}
}

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot;

namespace FSpot.Avalonia;

public partial class PreferencesWindow : Window
{
	public PreferencesWindow ()
	{
		InitializeComponent ();
		ShowSidebar.IsChecked = LibraryPreferences.ShowSidebar;
		ShowTimeline.IsChecked = LibraryPreferences.ShowTimeline;
		ShowFilmstrip.IsChecked = LibraryPreferences.ShowFilmstrip;
		ImportCopyFiles.IsChecked = LibraryPreferences.ImportCopyFiles;
		ViewerInterpolation.IsChecked = LibraryPreferences.ViewerInterpolation;
		StoragePath.Text = LibraryPreferences.StoragePath;
	}

	void OnClose (object? sender, RoutedEventArgs e)
	{
		LibraryPreferences.ShowSidebar = ShowSidebar.IsChecked == true;
		LibraryPreferences.ShowTimeline = ShowTimeline.IsChecked == true;
		LibraryPreferences.ShowFilmstrip = ShowFilmstrip.IsChecked == true;
		LibraryPreferences.ImportCopyFiles = ImportCopyFiles.IsChecked == true;
		LibraryPreferences.ViewerInterpolation = ViewerInterpolation.IsChecked == true;
		if (!string.IsNullOrWhiteSpace (StoragePath.Text))
			LibraryPreferences.StoragePath = StoragePath.Text;
		Close ();
	}
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Settings;

namespace FSpot
{
	public static class LibraryPreferences
	{
		public static bool ShowSidebar {
			get => Preferences.Get<bool> (Preferences.ShowSidebar);
			set => Preferences.Set (Preferences.ShowSidebar, value);
		}

		public static bool ShowTimeline {
			get => Preferences.Get<bool> (Preferences.ShowTimeline);
			set => Preferences.Set (Preferences.ShowTimeline, value);
		}

		public static bool ShowFilmstrip {
			get => Preferences.Get<bool> (Preferences.ShowFilmstrip);
			set => Preferences.Set (Preferences.ShowFilmstrip, value);
		}

		public static bool ShowToolbar {
			get => Preferences.Get<bool> (Preferences.ShowToolbar);
			set => Preferences.Set (Preferences.ShowToolbar, value);
		}

		public static bool ShowTags {
			get => Preferences.Get<bool> (Preferences.ShowTags);
			set => Preferences.Set (Preferences.ShowTags, value);
		}

		public static bool ShowDates {
			get => Preferences.Get<bool> (Preferences.ShowDates);
			set => Preferences.Set (Preferences.ShowDates, value);
		}

		public static bool ShowRatings {
			get => Preferences.Get<bool> (Preferences.ShowRatings);
			set => Preferences.Set (Preferences.ShowRatings, value);
		}

		public static bool GroupAdaptorOrderAsc {
			get => Preferences.Get<bool> (Preferences.GroupAdaptorOrderAsc);
			set => Preferences.Set (Preferences.GroupAdaptorOrderAsc, value);
		}

		public static double Zoom {
			get => Preferences.Get<double> (Preferences.Zoom);
			set => Preferences.Set (Preferences.Zoom, value);
		}

		public static int SidebarPosition {
			get => Preferences.Get<int> (Preferences.SidebarPosition);
			set => Preferences.Set (Preferences.SidebarPosition, value);
		}

		public static int SidebarPageLibrary {
			get => Preferences.Get<int> (Preferences.SidebarPageLibrary);
			set => Preferences.Set (Preferences.SidebarPageLibrary, value);
		}

		public static int MainWindowX {
			get => Preferences.Get<int> (Preferences.MainWindowX);
			set => Preferences.Set (Preferences.MainWindowX, value);
		}

		public static int MainWindowY {
			get => Preferences.Get<int> (Preferences.MainWindowY);
			set => Preferences.Set (Preferences.MainWindowY, value);
		}

		public static int MainWindowWidth {
			get => Preferences.Get<int> (Preferences.MainWindowWidth);
			set => Preferences.Set (Preferences.MainWindowWidth, value);
		}

		public static int MainWindowHeight {
			get => Preferences.Get<int> (Preferences.MainWindowHeight);
			set => Preferences.Set (Preferences.MainWindowHeight, value);
		}

		public static bool MainWindowMaximized {
			get => Preferences.Get<bool> (Preferences.MainWindowMaximized);
			set => Preferences.Set (Preferences.MainWindowMaximized, value);
		}

		public static bool ShowHoverPreview {
			get => Preferences.Get<bool> (Preferences.ShowHoverPreview);
			set => Preferences.Set (Preferences.ShowHoverPreview, value);
		}

		public static bool ImportCopyFiles {
			get => Preferences.Get<bool> (Preferences.ImportCopyFiles);
			set => Preferences.Set (Preferences.ImportCopyFiles, value);
		}

		public static string StoragePath {
			get => Preferences.Get<string> (Preferences.StoragePath);
			set => Preferences.Set (Preferences.StoragePath, value);
		}

		public static bool ViewerInterpolation {
			get => Preferences.Get<bool> (Preferences.ViewerInterpolation);
			set => Preferences.Set (Preferences.ViewerInterpolation, value);
		}

		public static string ColorScheme {
			get => Preferences.Get<string> (Preferences.ColorScheme);
			set => Preferences.Set (Preferences.ColorScheme, value);
		}

		public static bool MetadataEmbedInImage {
			get => Preferences.Get<bool> (Preferences.MetadataEmbedInImage);
			set => Preferences.Set (Preferences.MetadataEmbedInImage, value);
		}

		public static bool MetadataAlwaysUseSidecar {
			get => Preferences.Get<bool> (Preferences.MetadataAlwaysUseSidecar);
			set => Preferences.Set (Preferences.MetadataAlwaysUseSidecar, value);
		}

		public static string DisplayProfile {
			get => Preferences.Get<string> (Preferences.ColorManagementDisplayProfile);
			set => Preferences.Set (Preferences.ColorManagementDisplayProfile, value);
		}

		public static string PrintProfile {
			get => Preferences.Get<string> (Preferences.ColorManagementDisplayOutputProfile);
			set => Preferences.Set (Preferences.ColorManagementDisplayOutputProfile, value);
		}

		// "NONE" (as background), "CHECK_PATTERN" or "COLOR".
		public static string ViewerTransparency {
			get => Preferences.Get<string> (Preferences.ViewerTransparency);
			set => Preferences.Set (Preferences.ViewerTransparency, value);
		}

		// "#rrggbb", used only when ViewerTransparency is "COLOR".
		public static string ViewerTransColor {
			get => Preferences.Get<string> (Preferences.ViewerTransColor);
			set => Preferences.Set (Preferences.ViewerTransColor, value);
		}

		public static bool MailToEnabled {
			get => Preferences.Get<bool> (Preferences.MailToEnabled);
			set => Preferences.Set (Preferences.MailToEnabled, value);
		}

		// On while crash reporting is in active development. FSPOT_SENTRY=0 forces it off.
		public static bool SendCrashReports {
			get => Preferences.Get<bool> (Preferences.SendCrashReports);
			set => Preferences.Set (Preferences.SendCrashReports, value);
		}
	}
}

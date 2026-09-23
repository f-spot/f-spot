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
	}
}

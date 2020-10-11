//
// PreferencesKeys.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Settings
{
	public static partial class Preferences
	{
		public const string ExtensionKey = "Extension/";
		public const string ExportKey = "Export/";
		public const string ExportTokens = ExportKey + "Tokens/";

		public const string UIKey = "UI/";
		public const string GtkRc = UIKey + "gtkrc";

		public const string MainWindowMaximized = UIKey + "Maximized";
		public const string MainWindowX = UIKey + "MainWindowX";
		public const string MainWindowY = UIKey + "MainWindowY";
		public const string MainWindowWidth = UIKey + "MainWindowWidth";
		public const string MainWindowHeight = UIKey + "MainWindowHeight";

		public const string ImportWindowWidth = UIKey + "ImportWindowWidth";
		public const string ImportWindowHeight = UIKey + "ImportWindowHeight";
		public const string ImportWindowPanePosition = UIKey + "ImportWindowPanePosition";

		const string ImportKey = "Import";
		public const string ImportCopyFiles = ImportKey + "CopyFiles";
		public const string ImportIncludeSubfolders = ImportKey + "IncludeSubfolders";
		public const string ImportCheckDuplicates = ImportKey + "CheckDuplicates";
		public const string ImportRemoveOriginals = ImportKey + "RemoveOriginals";
		public const string ImportMergeRawAndJpeg = ImportKey + "MergeRawAndJpeg";

		public const string ViewerWidth = UIKey + "ViewerWidth";
		public const string ViewerHeight = UIKey + "ViewerHeight";
		public const string ViewerMaximized = UIKey + "ViewerMaximized";
		public const string ViewerShowToolbar = UIKey + "ViewerShowToolbar";
		public const string ViewerShawFilenames = UIKey + "ViewerShowFilenames";
		public const string ViewerInterpolation = "ViewerInterpolation";
		public const string ViewerTransColor = "ViewerTransColor";
		public const string ViewerTransparency = "ViewerTransparency";
		public const string CustomCropRatios = "ViewerCustomCropRatios";

		public const string ColorManagementDisplayProfile = UIKey + "ColorManagementDisplayProfile";
		public const string ColorManagementDisplayOutputProfile = UIKey + "ColorManagementOutputProfile";

		public const string ShowToolbar = UIKey + "ShowToolbar";
		public const string ShowSidebar = UIKey + "ShowSidebar";
		public const string ShowTimeline = UIKey + "ShowTimeline";
		public const string ShowFilmstrip = UIKey + "ShowFilmstrip";
		public const string FilmstripOrientation = UIKey + "FilmstripOrientation";
		public const string ShowTags = UIKey + "ShowTags";
		public const string ShowDates = UIKey + "ShowDates";
		public const string ExpandedTags = UIKey + "ExpandedTags";
		public const string ShowRatings = UIKey + "ShowRatings";
		public const string TagIconSize = UIKey + "TagIconSize";
		public const string TagIconAutomatic = UIKey + "TagIconAutomatic";

		public const string GlassPosition = UIKey + "GlassPosition";
		public const string GroupAdaptorOrderAsc = UIKey + "GroupAdaptorSortAsc";

		public const string SidebarPosition = UIKey + "SidebarSize";
		public const string Zoom = UIKey + "Zoom";

		public const string ExportEmailSize = ExportKey + "Email/Size";
		public const string ExportEmailRotate = ExportKey + "Email/AutoRotate";

		public const string ImportGuiRollHistory = "Import/GuiRollHistory";

		public const string ScreensaverTag = "screensaver/tag_id";
		public const string ScreensaverDelay = "screensaver/delay";

		public const string StoragePath = "Import/StoragePath";

		public const string MetadataEmbedInImage = "Metadata/EmbedInImage";
		public const string MetadataAlwaysUseSidecar = "Metadata/AlwaysUseSidecar";

		public const string EditRedeyeThreshold = "Edit/RedeyeThreshold";
		public const string EditCreateXcfVersion = "Edit/CreateXcf";

		// FIXME, These were originally GNOME settings. Add support
		public const string MailToCommand = "MailToCommand";
		public const string MailToEnabled = "MailToEenabled";

		// FIXME, These don't appear to used at all
		public const string ThumbsMaxAge = "ThumbnailCache/MaximumAge";
		public const string ThumbsMaxSize = "ThumbnailCache/MaximumSize";
	}
}

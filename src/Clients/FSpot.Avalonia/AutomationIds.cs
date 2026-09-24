// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

public static class AutomationIds
{
	public const string MainWindow = "MainWindow";
	public const string ImportWindow = "ImportWindow";
	public const string AboutWindow = "AboutWindow";
	public const string DialogWindow = "Dialog.Window";
	public const string DialogHeading = "Dialog.Heading";
	public const string DialogMessage = "Dialog.Message";
	public const string DialogPrompt = "Dialog.Prompt";
	public const string DialogInput = "Dialog.Input";
	public const string DialogOk = "Dialog.OK";
	public const string DialogCancel = "Dialog.Cancel";

	public const string ToolbarImport = "Main.Toolbar.Import";
	public const string ToolbarRotateLeft = "Main.Toolbar.RotateLeft";
	public const string ToolbarRotateRight = "Main.Toolbar.RotateRight";
	public const string ToolbarBrowse = "Main.Toolbar.Browse";
	public const string ToolbarEdit = "Main.Toolbar.Edit";
	public const string ToolbarFullscreen = "Main.Toolbar.Fullscreen";
	public const string ToolbarSlideshow = "Main.Toolbar.Slideshow";
	public const string ToolbarPrevious = "Main.Toolbar.Previous";
	public const string ToolbarNext = "Main.Toolbar.Next";
	public const string ToolbarPhotoIndex = "Main.Toolbar.PhotoIndex";

	public const string FindBar = "Main.FindBar";
	public const string FindBox = "Main.FindBox";
	public const string FindClose = "Main.FindClose";

	public const string StatusBar = "Main.StatusBar";
	public const string ZoomSlider = "Main.ZoomSlider";
	public const string StatusText = "Main.StatusText";
	public const string ZoomLabel = "Main.ZoomLabel";

	public const string Sidebar = "Main.Sidebar";
	public const string SidebarChooser = "Main.Sidebar.Chooser";
	public const string SidebarChooserTags = "Main.Sidebar.Chooser.Tags";
	public const string SidebarChooserFolders = "Main.Sidebar.Chooser.Folders";
	public const string SidebarChooserEdit = "Main.Sidebar.Chooser.Edit";
	public const string SidebarClose = "Main.Sidebar.Close";
	public const string SidebarTagsTree = "Main.Sidebar.TagsTree";
	public const string SidebarFoldersTree = "Main.Sidebar.FoldersTree";
	public const string SidebarEditPage = "Main.Sidebar.EditPage";
	public const string SidebarEditCrop = "Main.Sidebar.Edit.Crop";
	public const string SidebarEditRedEye = "Main.Sidebar.Edit.RedEye";
	public const string SidebarEditDesaturate = "Main.Sidebar.Edit.Desaturate";
	public const string SidebarEditSepia = "Main.Sidebar.Edit.Sepia";
	public const string SidebarEditTilt = "Main.Sidebar.Edit.Tilt";
	public const string SidebarEditSoftFocus = "Main.Sidebar.Edit.SoftFocus";
	public const string SidebarEditAutoStretch = "Main.Sidebar.Edit.AutoStretch";
	public const string SidebarEditColor = "Main.Sidebar.Edit.Color";
	public const string SidebarEditFlip = "Main.Sidebar.Edit.Flip";
	public const string SidebarEditPixelate = "Main.Sidebar.Edit.Pixelate";
	public const string SidebarEditBW = "Main.Sidebar.Edit.BW";
	public const string SidebarEditResize = "Main.Sidebar.Edit.Resize";
	public const string SidebarEditBlackout = "Main.Sidebar.Edit.Blackout";
	public const string SidebarEditSharpen = "Main.Sidebar.Edit.Sharpen";

	public static string SidebarEditEditor (string id) => $"Main.Sidebar.Edit.{id}";
	public const string SidebarHistogram = "Main.Sidebar.Histogram";
	public const string SidebarInfo = "Main.Sidebar.Info";
	public const string SidebarInfoName = "Main.Sidebar.Info.Name";
	public const string SidebarInfoVersion = "Main.Sidebar.Info.Version";
	public const string SidebarInfoDate = "Main.Sidebar.Info.Date";
	public const string SidebarInfoSize = "Main.Sidebar.Info.Size";
	public const string SidebarInfoExposure = "Main.Sidebar.Info.Exposure";
	public const string SidebarInfoFocalLength = "Main.Sidebar.Info.FocalLength";
	public const string SidebarInfoCamera = "Main.Sidebar.Info.Camera";
	public const string SidebarInfoCreator = "Main.Sidebar.Info.Creator";
	public const string SidebarInfoRating = "Main.Sidebar.Info.Rating";

	public const string QueryBar = "Main.QueryBar";
	public const string QueryUntagged = "Main.QueryBar.Untagged";
	public const string QueryTagChip = "Main.QueryBar.Tag";
	public const string QueryFolderChip = "Main.QueryBar.Folder";
	public const string QueryClearTag = "Main.QueryBar.ClearTag";
	public const string QueryClearFolder = "Main.QueryBar.ClearFolder";
	public const string QueryClearAll = "Main.QueryBar.ClearAll";
	public const string QueryRatingChip = "Main.QueryBar.Rating";
	public const string QueryDateChip = "Main.QueryBar.Date";
	public const string QueryRollChip = "Main.QueryBar.Roll";
	public const string QueryClearRating = "Main.QueryBar.ClearRating";
	public const string QueryClearDate = "Main.QueryBar.ClearDate";
	public const string QueryClearRoll = "Main.QueryBar.ClearRoll";
	public const string QueryNoMatches = "Main.QueryBar.NoMatches";
	public const string FindBarNoMatches = "Main.FindBar.NoMatches";
	public const string SlideshowWindow = "Slideshow.Window";
	public const string SlideshowImage = "Slideshow.Image";
	public const string SlideshowClose = "Slideshow.Close";

	public const string Timeline = "Main.Timeline";
	public const string Filmstrip = "Main.Filmstrip";
	public const string PhotoGrid = "Main.PhotoGrid";
	public const string PhotoGridEmpty = "Main.PhotoGrid.Empty";
	public const string PhotoGridNoMatches = "Main.PhotoGrid.NoMatches";
	public const string PhotoPreview = "Main.PhotoPreview";
	public const string PhotoPreviewOverlay = "Main.PhotoPreview.Overlay";
	public const string PhotoLoupe = "Main.PhotoLoupe";
	public const string EditorApply = "Main.Editor.Apply";
	public const string EditorCancel = "Main.Editor.Cancel";
	public const string EditorHint = "Main.Editor.Hint";
	public const string EditorColorExposure = "Main.Editor.Color.Exposure";
	public const string EditorColorSaturation = "Main.Editor.Color.Saturation";
	public const string EditorColorBrightness = "Main.Editor.Color.Brightness";
	public const string EditorColorHue = "Main.Editor.Color.Hue";
	public const string EditorColorContrast = "Main.Editor.Color.Contrast";
	public const string EditorColorTemperature = "Main.Editor.Color.Temperature";
	public const string EditorColorTint = "Main.Editor.Color.Tint";
	public const string EditorBwRed = "Main.Editor.BW.Red";
	public const string EditorBwGreen = "Main.Editor.BW.Green";
	public const string EditorBwBlue = "Main.Editor.BW.Blue";
	public const string EditorResizeMaxEdge = "Main.Editor.Resize.MaxEdge";
	public const string EditorTiltAngle = "Main.Editor.Tilt.Angle";
	public const string EditorSoftFocusRadius = "Main.Editor.SoftFocus.Radius";
	public const string EditorSharpenAmount = "Main.Editor.Sharpen.Amount";
	public const string EditorSharpenRadius = "Main.Editor.Sharpen.Radius";
	public const string EditorSharpenThreshold = "Main.Editor.Sharpen.Threshold";
	public const string TagBar = "Main.TagBar";
	public const string PhotoDescription = "Main.PhotoDescription";
	public const string PhotoRating = "Main.PhotoRating";

	public const string ImportSource = "Import.Source";
	public const string ImportPhotoList = "Import.PhotoList";
	public const string ImportPreview = "Import.Preview";
	public const string ImportAttachTags = "Import.AttachTags";
	public const string ImportDetectDuplicates = "Import.DetectDuplicates";
	public const string ImportCopyFiles = "Import.CopyFiles";
	public const string ImportIncludeSubfolders = "Import.IncludeSubfolders";
	public const string ImportRemoveOriginals = "Import.RemoveOriginals";
	public const string ImportRemoveOriginalsWarning = "Import.RemoveOriginalsWarning";
	public const string ImportMergeRawJpeg = "Import.MergeRawJpeg";
	public const string ImportProgress = "Import.Progress";
	public const string ImportStatus = "Import.Status";
	public const string ImportCancel = "Import.Cancel";
	public const string ImportConfirm = "Import.Import";

	public const string AboutLogo = "About.Logo";
	public const string AboutVersion = "About.Version";
	public const string AboutDescription = "About.Description";
	public const string AboutClose = "About.Close";

	public const string PreferencesWindow = "Preferences.Window";
	public const string PreferencesColorScheme = "Preferences.ColorScheme";
	public const string PreferencesSidebar = "Preferences.Sidebar";
	public const string PreferencesTimeline = "Preferences.Timeline";
	public const string PreferencesFilmstrip = "Preferences.Filmstrip";
	public const string PreferencesCopyFiles = "Preferences.CopyFiles";
	public const string PreferencesStoragePath = "Preferences.StoragePath";
	public const string PreferencesInterpolation = "Preferences.Interpolation";
	public const string PreferencesHoverPreview = "Preferences.HoverPreview";
	public const string PreferencesEmbedMetadata = "Preferences.EmbedMetadata";
	public const string PreferencesSeparateMetadata = "Preferences.SeparateMetadata";
	public const string PreferencesSidecar = "Preferences.Sidecar";
	public const string PreferencesDisplayProfile = "Preferences.DisplayProfile";
	public const string PreferencesPrintProfile = "Preferences.PrintProfile";
	public const string ExtensionsWindow = "Extensions.Window";
	public const string ExtensionsList = "Extensions.List";
	public const string InfoShowDate = "Main.Sidebar.Info.ShowDate";
	public const string InfoStars = "Main.Sidebar.Info.Stars";
	public const string FolderShare = "Main.Sidebar.Folders.Share";
	public const string MainMenu = "Main.Menu";
	public const string FolderPicker = "Dialog.FolderPicker";
	public const string FolderPickerPath = "Dialog.FolderPicker.Path";
	public const string FolderPickerBrowse = "Dialog.FolderPicker.Browse";
}

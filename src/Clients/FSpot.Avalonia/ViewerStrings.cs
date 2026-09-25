// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

// Strings.resx entries the GTK single_view.ui and viewer_preferences.ui used through the
// builder, so the checked-in Strings.Designer.cs has no accessor for them. They are read
// by name from the same translated resources.
public static class ViewerStrings
{
	public static string AsBackground => Get (nameof (AsBackground));
	public static string AsCheckPattern => Get (nameof (AsCheckPattern));
	public static string AsCustomColor => Get (nameof (AsCustomColor));
	public static string AsCustomColor2 => Get (nameof (AsCustomColor2));
	public static string DisplayFileNames => Get (nameof (DisplayFileNames));
	public static string Export2 => Get (nameof (Export2));
	public static string FSpotView => Get (nameof (FSpotView));
	public static string NewWindow => Get (nameof (NewWindow));
	public static string OpenFolder => Get (nameof (OpenFolder));
	public static string PhotoViewer => Get (nameof (PhotoViewer));
	public static string SetAsBackground2 => Get (nameof (SetAsBackground2));
	public static string SidePane => Get (nameof (SidePane));
	public static string Toolbar3 => Get (nameof (Toolbar3));
	public static string ZoomIn2 => Get (nameof (ZoomIn2));
	public static string ZoomOut2 => Get (nameof (ZoomOut2));
	public static string BImageInterpolationB => Get (nameof (BImageInterpolationB));
	public static string BTransparentPartsB => Get (nameof (BTransparentPartsB));
	public static string InterpolationDetail => Get ("SmallIEnableThisToAllowInterpolationOnZoomedImagesYouShouldnTDisableThisForViewingPhotosButDisablingTheInterpolationCouldBeUsefulInIconDesignISmall");
	public static string TransparencyDetail => Get ("SmallIYouCanChooseHowToDisplayTransparentPartsInImagesThisOptionHasNoEffectOnPhotosButSettingThisAsCheckPatternOrCustomColorCouldBeUsefulWhenViewingIconsOrOtherArtworksWithTransparentPartsISmall");

	public static string Get (string name)
		=> Strings.ResourceManager.GetString (name, Strings.Culture) ?? name;
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Strings.resx entries the GTK filter and tag dialogs use that the generated
/// <see cref="Strings"/> class does not expose yet. They read the same localized resources.
/// </summary>
public static class DialogStrings
{
	static string Get (string name)
		=> Strings.ResourceManager.GetString (name, Strings.Culture) ?? name;

	public static string At => Get ("At");

	public static string BEndDateB => Get ("BEndDateB");

	public static string Between => Get ("Between");

	public static string BFromExternalPhotoB => Get ("BFromExternalPhotoB");

	public static string BFromPhotoB => Get ("BFromPhotoB");

	public static string BMaxRatingB => Get ("BMaxRatingB");

	public static string BMinRatingB => Get ("BMinRatingB");

	public static string BPredefinedIconsB => Get ("BPredefinedIconsB");

	public static string BPreviewB => Get ("BPreviewB");

	public static string BSelectPeriodB => Get ("BSelectPeriodB");

	public static string BStartDateB => Get ("BStartDateB");

	public static string BViewAllPicturesImportedB => Get ("BViewAllPicturesImportedB");

	public static string CancelDoNotChangeTheCurrentTimeline => Get ("CancelDoNotChangeTheCurrentTimeline");

	public static string ClearRatingFilter => Get ("ClearRatingFilter");

	public static string ClearRollFilter => Get ("ClearRollFilter");

	public static string Create2 => Get ("Create2");

	public static string CreateIconForThisTagWhenFirstUsed => Get ("CreateIconForThisTagWhenFirstUsed");

	public static string DesignIconFrom => Get ("DesignIconFrom");

	public static string DisplayOnlyThosePhotosThatWereImportedInSpecifiedRolls => Get ("DisplayOnlyThosePhotosThatWereImportedInSpecifiedRolls");

	public static string EditIcon => Get ("EditIcon");

	public static string EditTagIcon => Get ("EditTagIcon");

	public static string FilterOnSelectedRolls => Get ("FilterOnSelectedRolls");

	public static string Icon => Get ("Icon");

	public static string LastImportRoll => Get ("LastImportRoll");

	public static string MergeTags2 => Get ("MergeTags2");

	public static string NoImage => Get ("NoImage");

	public static string NumberOfPhotosInSelectedRolls => Get ("NumberOfPhotosInSelectedRolls");

	public static string OnlyPhotosTakenWithinTheseDatesWillBeDisplayed => Get ("OnlyPhotosTakenWithinTheseDatesWillBeDisplayed");

	public static string ParentTag => Get ("ParentTag");

	public static string SelectATag => Get ("SelectATag");

	public static string SelectImportRolls => Get ("SelectImportRolls");

	public static string SetDateRange => Get ("SetDateRange");

	public static string SetDateRange2 => Get ("SetDateRange2");

	public static string SetRatingFilter => Get ("SetRatingFilter");

	public static string SetRatingFilter2 => Get ("SetRatingFilter2");

	public static string ShowAllPhotos => Get ("ShowAllPhotos");

	public static string TagName => Get ("TagName");

	public static string TagName2 => Get ("TagName2");
}

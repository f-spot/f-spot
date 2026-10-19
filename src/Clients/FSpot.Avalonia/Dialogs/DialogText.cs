// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.RegularExpressions;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Turns the GTK dialogs' Pango markup strings (such as "&lt;b&gt;Min Rating&lt;/b&gt;") into plain text.
/// </summary>
public static partial class DialogText
{
	public static string Plain (string? markup)
	{
		if (string.IsNullOrEmpty (markup))
			return string.Empty;
		string text = Tags ().Replace (markup, string.Empty);
		return WebUtility.HtmlDecode (text).Replace ("\\n", "\n", StringComparison.Ordinal).Trim ();
	}

	[GeneratedRegex ("<[^>]+>")]
	private static partial Regex Tags ();
}

public static class DialogAutomationIds
{
	public const string RatingFilterWindow = "RatingFilter.Window";
	public const string RatingFilterMin = "RatingFilter.Min";
	public const string RatingFilterMax = "RatingFilter.Max";

	public const string DateRangeWindow = "DateRange.Window";
	public const string DateRangePeriod = "DateRange.Period";
	public const string DateRangeStart = "DateRange.Start";
	public const string DateRangeEnd = "DateRange.End";

	public const string ImportRollsWindow = "ImportRolls.Window";
	public const string ImportRollsMode = "ImportRolls.Mode";
	public const string ImportRollsFirst = "ImportRolls.First";
	public const string ImportRollsSecond = "ImportRolls.Second";
	public const string ImportRollsCount = "ImportRolls.Count";

	public const string ZipScaleWindow = "ZipScale.Window";
	public const string ZipScaleToggle = "ZipScale.Toggle";
	public const string ZipScaleEdge = "ZipScale.Edge";

	public const string CatalogMergeWindow = "CatalogMerge.Window";
	public const string CatalogMergeNewRolls = "CatalogMerge.NewRolls";
	public const string CatalogMergeOneRoll = "CatalogMerge.OneRoll";
	public const string CatalogMergeRoll = "CatalogMerge.Roll";
	public const string CatalogMergeAllRolls = "CatalogMerge.AllRolls";
	public const string CatalogMergeCopy = "CatalogMerge.Copy";
	public const string CatalogMergeKeep = "CatalogMerge.Keep";

	public const string CreateTagWindow = "CreateTag.Window";
	public const string EditTagWindow = "EditTag.Window";
	public const string TagName = "TagDialog.Name";
	public const string TagParent = "TagDialog.Parent";
	public const string TagNameInUse = "TagDialog.NameInUse";
	public const string TagAutoIcon = "TagDialog.AutoIcon";
	public const string TagIconButton = "TagDialog.Icon";

	public const string EditTagIconWindow = "EditTagIcon.Window";
	public const string EditTagIconPreview = "EditTagIcon.Preview";
	public const string EditTagIconPhoto = "EditTagIcon.Photo";
	public const string EditTagIconPhotoIndex = "EditTagIcon.PhotoIndex";
	public const string EditTagIconFromFile = "EditTagIcon.FromFile";
	public const string EditTagIconPredefined = "EditTagIcon.Predefined";
	public const string EditTagIconNoImage = "EditTagIcon.NoImage";

	public const string VersionNameWindow = "VersionName.Window";
	public const string VersionName = "VersionName.Name";
	public const string VersionNameInUse = "VersionName.InUse";

	public const string TagSelectionWindow = "TagSelection.Window";
	public const string TagSelectionTree = "TagSelection.Tree";
}

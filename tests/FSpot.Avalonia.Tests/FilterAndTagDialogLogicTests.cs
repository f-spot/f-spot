// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Dialogs;
using FSpot.Core;
using FSpot.Imaging;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class FilterAndTagDialogLogicTests
{
	static readonly TimeSpan EndOfDay = new (23, 59, 59);

	[TestMethod]
	public void RatingFilter_DefaultsToFourToFiveStars ()
	{
		Assert.AreEqual ((4u, 5u), RatingFilterLogic.Initial (null, null));
		Assert.AreEqual ((2u, 3u), RatingFilterLogic.Initial (2, 3));
	}

	[TestMethod]
	public void RatingFilter_RaisingMinDragsMaxAndLoweringMaxDragsMin ()
	{
		Assert.AreEqual ((5u, 5u), RatingFilterLogic.WithMin (5, 3));
		Assert.AreEqual ((1u, 3u), RatingFilterLogic.WithMin (1, 3));
		Assert.AreEqual ((2u, 2u), RatingFilterLogic.WithMax (4, 2));
		Assert.AreEqual ((5u, 5u), RatingFilterLogic.WithMax (5, 9));
	}

	[TestMethod]
	public void DateRange_Last7DaysEndsAtTheEndOfToday ()
	{
		var today = new DateTime (2026, 9, 23);
		var range = DateRangePresets.Range (DateRangePreset.Last7Days, today, today, today);

		Assert.IsNotNull (range);
		Assert.AreEqual (new DateTime (2026, 9, 17), range.Value.Start);
		Assert.AreEqual (today + EndOfDay, range.Value.End);
	}

	[TestMethod]
	public void DateRange_WeeksStartOnMondayEvenOnASunday ()
	{
		var wednesday = new DateTime (2026, 9, 23);
		var sunday = new DateTime (2026, 9, 27);

		Assert.AreEqual (new DateTime (2026, 9, 21), DateRangePresets.Range (DateRangePreset.CurrentWeek, wednesday, wednesday, wednesday)!.Value.Start);
		Assert.AreEqual (new DateTime (2026, 9, 21), DateRangePresets.Range (DateRangePreset.CurrentWeek, sunday, sunday, sunday)!.Value.Start);

		var previous = DateRangePresets.Range (DateRangePreset.PreviousWeek, wednesday, wednesday, wednesday)!.Value;
		Assert.AreEqual (new DateTime (2026, 9, 14), previous.Start);
		Assert.AreEqual (new DateTime (2026, 9, 20) + EndOfDay, previous.End);
	}

	[TestMethod]
	public void DateRange_PreviousMonthAndYearCrossTheYearBoundary ()
	{
		var today = new DateTime (2026, 1, 10);

		var month = DateRangePresets.Range (DateRangePreset.PreviousMonth, today, today, today)!.Value;
		Assert.AreEqual (new DateTime (2025, 12, 1), month.Start);
		Assert.AreEqual (new DateTime (2025, 12, 31) + EndOfDay, month.End);

		var year = DateRangePresets.Range (DateRangePreset.PreviousYear, today, today, today)!.Value;
		Assert.AreEqual (new DateTime (2025, 1, 1), year.Start);
		Assert.AreEqual (new DateTime (2025, 12, 31) + EndOfDay, year.End);
	}

	[TestMethod]
	public void DateRange_AllDatesClearsAndCustomUsesThePickedDaysInOrder ()
	{
		var today = new DateTime (2026, 9, 23);
		Assert.IsNull (DateRangePresets.Range (DateRangePreset.AllDates, today, today, today));

		var custom = DateRangePresets.Range (DateRangePreset.CustomizedRange, today,
			new DateTime (2026, 3, 9, 14, 0, 0), new DateTime (2026, 3, 2))!.Value;
		Assert.AreEqual (new DateTime (2026, 3, 2), custom.Start);
		Assert.AreEqual (new DateTime (2026, 3, 9) + EndOfDay, custom.End);
	}

	[TestMethod]
	public void DateRange_LabelsReuseTheGtkStrings ()
	{
		var today = new DateTime (2026, 9, 23);
		Assert.AreEqual (Strings.LastSevenDays, DateRangePresets.Label (DateRangePreset.Last7Days, today));
		Assert.AreEqual (Strings.AllImages, DateRangePresets.Label (DateRangePreset.AllDates, today));
		Assert.AreEqual ("2025", DateRangePresets.Label (DateRangePreset.PreviousYear, today));
		Assert.HasCount (14, DateRangePresets.All);
	}

	static IReadOnlyList<ImportRollOption> Rolls ()
		=> [
			new ImportRollOption (30, new DateTime (2026, 9, 3, 10, 0, 0, DateTimeKind.Utc), 2),
			new ImportRollOption (20, new DateTime (2026, 9, 2, 10, 0, 0, DateTimeKind.Utc), 3),
			new ImportRollOption (10, new DateTime (2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), 5)
		];

	[TestMethod]
	public void ImportRolls_AtAfterAndBetween ()
	{
		var rolls = Rolls ();

		Assert.AreSequenceEqual (new uint[] { 20 }, ImportRollFilter.Select (rolls, ImportRollMode.At, 1, 0).Select (r => r.Id).ToArray ());
		Assert.AreSequenceEqual (new uint[] { 30, 20 }, ImportRollFilter.Select (rolls, ImportRollMode.After, 1, 0).Select (r => r.Id).ToArray ());
		Assert.AreSequenceEqual (new uint[] { 20, 10 }, ImportRollFilter.Select (rolls, ImportRollMode.Between, 2, 1).Select (r => r.Id).ToArray ());
		Assert.AreEqual (10u, ImportRollFilter.PhotoCount (ImportRollFilter.Select (rolls, ImportRollMode.Between, 0, 2)));
		Assert.IsEmpty (ImportRollFilter.Select (rolls, ImportRollMode.At, -1, 0));
		Assert.IsEmpty (ImportRollFilter.Select (rolls, ImportRollMode.Between, 0, 7));
	}

	[TestMethod]
	public void ImportRolls_HeaderShowsLocalTimeAndPhotoCount ()
	{
		var utc = new DateTime (2026, 9, 3, 10, 5, 0, DateTimeKind.Utc);
		string header = ImportRollFilter.Header (utc, 12);

		Assert.EndsWith (" (12)", header);
		Assert.Contains (utc.ToLocalTime ().ToString ("HH:mm", System.Globalization.CultureInfo.CurrentCulture), header);
	}

	[TestMethod]
	public void DialogText_StripsPangoMarkup ()
	{
		Assert.AreEqual ("Min Rating", DialogText.Plain (DialogStrings.BMinRatingB));
		Assert.AreEqual ("a & b", DialogText.Plain ("<b>a &amp; b</b>"));
		Assert.AreEqual (string.Empty, DialogText.Plain (null));
	}

	[TestMethod]
	public void DialogStrings_ReadTheSharedResources ()
	{
		Assert.AreEqual ("Set Rating Filter", DialogStrings.SetRatingFilter);
		Assert.AreEqual ("C_reate", DialogStrings.Create2);
		Assert.AreEqual ("P_arent Tag:", DialogStrings.ParentTag);
	}

	[TestMethod]
	public void VersionName_CreateRejectsAnExistingNameAndRenameMayKeepItsOwn ()
	{
		string[] names = ["Original", "Modified"];

		Assert.IsTrue (VersionNameRules.InUse (VersionNameRequest.Create, "Modified", "Modified", names));
		Assert.IsFalse (VersionNameRules.CanAccept (VersionNameRequest.Create, string.Empty, "Modified", names));
		Assert.IsTrue (VersionNameRules.CanAccept (VersionNameRequest.Create, "Crop", "Modified", names));

		Assert.IsFalse (VersionNameRules.InUse (VersionNameRequest.Rename, "Modified", "Modified", names));
		Assert.IsTrue (VersionNameRules.InUse (VersionNameRequest.Rename, "Original", "Modified", names));
		Assert.IsNull (VersionNameRules.Result (VersionNameRequest.Rename, "Modified", "Modified"));
		Assert.AreEqual ("Crop", VersionNameRules.Result (VersionNameRequest.Rename, "Crop", "Modified"));

		Assert.AreEqual (Strings.NameColon, VersionNameRules.Prompt (VersionNameRequest.Create));
		Assert.AreEqual (Strings.NewNameColon, VersionNameRules.Prompt (VersionNameRequest.Rename));
		Assert.AreEqual (Strings.RenameVersion, VersionNameRules.Title (VersionNameRequest.Rename));
	}

	sealed class Tree
	{
		public Category Root { get; } = new (null, 0, "(None)");
		public Category People { get; }
		public Category Family { get; }
		public Tag Mom { get; }
		public Category Places { get; }

		public Tree ()
		{
			People = new Category (Root, 1, "People");
			Family = new Category (People, 2, "Family");
			Mom = new Tag (Family, 3, "Mom");
			Places = new Category (Root, 4, "Places");
		}
	}

	[TestMethod]
	public void TagParents_CreateIndentsAndEditSkipsTheTagAndItsDescendants ()
	{
		var tree = new Tree ();

		var create = TagDialogRules.ParentChoices (tree.Root);
		Assert.AreSequenceEqual (new[] { "(None)", "People", "  Family", "Places" }, create.Select (option => option.Label).ToArray ());

		var edit = TagDialogRules.ParentChoices (tree.Root, tree.People);
		Assert.AreSequenceEqual (new[] { "(None)", "Places" }, edit.Select (option => option.Label).ToArray ());
		Assert.AreEqual (1, TagDialogRules.IndexOf (edit, tree.Places));
		Assert.AreEqual (0, TagDialogRules.IndexOf (edit, tree.Family));
	}

	[TestMethod]
	public void TagNames_AreUniqueAcrossTheTreeIgnoringCase ()
	{
		var tree = new Tree ();

		Assert.IsTrue (TagDialogRules.NameInUse (tree.Root, "mom"));
		Assert.IsFalse (TagDialogRules.NameInUse (tree.Root, "MOM", "Mom"));
		Assert.IsFalse (TagDialogRules.CanAccept (tree.Root, string.Empty));
		Assert.IsTrue (TagDialogRules.CanAccept (tree.Root, "Dad"));
		Assert.AreSame (tree.Family, TagDialogRules.DefaultParent (tree.Root, tree.Mom));
		Assert.AreSame (tree.People, TagDialogRules.DefaultParent (tree.Root, tree.People));
		Assert.AreSame (tree.Root, TagDialogRules.DefaultParent (tree.Root, null));
	}

	[TestMethod]
	public void MergePlan_BringsDescendantsAndKeepsAShallowSurvivor ()
	{
		var tree = new Tree ();

		var plan = TagMergePlan.For ([tree.People, tree.Places]);

		Assert.IsNotNull (plan);
		Assert.AreSame (tree.Places, plan.Survivor);
		Assert.HasCount (3, plan.Merged);
		Assert.DoesNotContain (plan.Survivor, plan.Merged);
		foreach (var merged in plan.Merged)
			Assert.IsFalse (merged.IsAncestorOf (plan.Survivor), merged.Name);
		Assert.IsLessThan (plan.Merged.ToList ().IndexOf (tree.People), plan.Merged.ToList ().IndexOf (tree.Mom));
		Assert.IsNull (TagMergePlan.For ([tree.Mom]));
	}

	[TestMethod]
	public void TagIcon_SquaresTheSelectionAndScalesToTheLargeIconSize ()
	{
		var square = TagIconImage.Square (new PixelRect (10, 10, 40, 20));
		Assert.AreEqual (20, square.X);
		Assert.AreEqual (20, square.Width);
		Assert.AreEqual (20, square.Height);

		var source = new ImageBuffer (100, 50, false, 300, new byte[300 * 50]);
		(int Width, int Height) scaledTo = default;
		var icon = TagIconImage.Create (source, null, (image, width, height) => {
			scaledTo = (image.Width, image.Height);
			return new ImageBuffer (width, height, false, width * 3, new byte[width * 3 * height]);
		});

		Assert.AreEqual ((50, 50), scaledTo);
		Assert.AreEqual (48, icon.Width);
		Assert.AreEqual (48, icon.Height);
	}

	[TestMethod]
	public void PredefinedIcons_AreTheDistinctPackedEmblems ()
	{
		var names = PredefinedTagIcons.Names (["emblem-event-16.svg", "emblem-event.svg", "camera-photo.svg", "emblem-people.svg", "emblem-favorite-22.png"]);

		Assert.AreSequenceEqual (new[] { "emblem-event", "emblem-favorite", "emblem-people" }, names.ToArray ());
	}

	[TestMethod]
	public void TagIconChoice_ThemeIconReplacesAnImageAndNoImageClears ()
	{
		var tag = new Tag (null, 9, "Dog") {
			Icon = new ImageBuffer (1, 1, false, 3, new byte[3])
		};

		new TagIconChoice ("emblem-favorite", null).ApplyTo (tag);
		Assert.AreEqual ("emblem-favorite", tag.ThemeIconName);

		TagIconChoice.NoImage.ApplyTo (tag);
		Assert.IsNull (tag.ThemeIconName);
		Assert.IsTrue (tag.IconWasCleared);
		Assert.IsTrue (TagIconChoice.NoImage.Clears);
	}
}

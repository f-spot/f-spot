// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Dialogs;
using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FilterAndTagDialogWindowTests
{
	[TestInitialize]
	public void Start ()
		=> AvaloniaApp.Ensure ();

	[TestMethod]
	public void RatingFilter_KeepsMinAtOrBelowMax ()
	{
		var window = new RatingFilterWindow (1, 3);

		window.SetMin (5);
		Assert.AreEqual (5u, window.Max);

		window.SetMax (2);
		Assert.AreEqual (2u, window.Min);

		window.Accept ();
		Assert.AreEqual ((2u, 2u), window.Result);
	}

	[TestMethod]
	public void DateRange_PresetFillsTheDatesAndEditingADateMakesItCustom ()
	{
		var today = new DateTime (2026, 9, 23);
		var window = new DateRangeWindow (today, null, null);

		Assert.AreEqual (DateRangePreset.Last7Days, window.Preset);

		window.SetDates (new DateTime (2026, 1, 1), new DateTime (2026, 1, 31));
		Assert.AreEqual (DateRangePreset.CustomizedRange, window.Preset);

		window.Accept ();
		Assert.IsNotNull (window.Result?.Range);
		Assert.AreEqual (new DateTime (2026, 1, 1), window.Result.Range.Value.Start);
	}

	[TestMethod]
	public void DateRange_AllImagesDisablesTheDatesAndClearsTheFilter ()
	{
		var window = new DateRangeWindow (new DateTime (2026, 9, 23), null, null);

		window.Preset = DateRangePreset.AllDates;
		Assert.IsFalse (window.DatesEnabled);

		window.Accept ();
		Assert.IsNotNull (window.Result);
		Assert.IsNull (window.Result.Range);
	}

	[TestMethod]
	public void ImportRolls_CountFollowsTheModeAndBetweenShowsTheSecondRoll ()
	{
		var window = new ImportRollsWindow ([
			new ImportRollOption (30, new DateTime (2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), 2),
			new ImportRollOption (20, new DateTime (2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), 3),
			new ImportRollOption (10, new DateTime (2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 5)
		]);

		Assert.AreEqual ("2", window.CountText);
		Assert.IsFalse (window.ShowsSecondRoll);

		window.Mode = ImportRollMode.After;
		window.Choose (1);
		Assert.AreEqual ("5", window.CountText);

		window.Mode = ImportRollMode.Between;
		window.Choose (2, 1);
		Assert.IsTrue (window.ShowsSecondRoll);
		Assert.AreEqual ("8", window.CountText);

		window.Accept ();
		Assert.AreSequenceEqual (new uint[] { 20, 10 }, window.Result!.ToArray ());
	}

	static (Category Root, Category People, Tag Mom) Tree ()
	{
		var root = new Category (null, 0, "(None)");
		var people = new Category (root, 1, "People");
		var mom = new Tag (people, 2, "Mom");
		return (root, people, mom);
	}

	[TestMethod]
	public void CreateTag_WarnsAboutANameInUseAndCreatesUnderTheParent ()
	{
		var (root, people, _) = Tree ();
		var window = TagWindow.ForCreate (root, people, autoIcon: true);

		Assert.IsFalse (window.CanAccept);
		window.TagName = "mom";
		Assert.IsTrue (window.ShowsNameInUse);
		Assert.IsFalse (window.CanAccept);

		window.TagName = "Dad";
		Assert.IsFalse (window.ShowsNameInUse);
		Assert.IsTrue (window.CanAccept);

		window.Accept ();
		Assert.IsNotNull (window.CreateResult);
		Assert.AreEqual ("Dad", window.CreateResult.Name);
		Assert.AreSame (people, window.CreateResult.Parent);
		Assert.IsTrue (window.CreateResult.AutoIcon);
	}

	[TestMethod]
	public void EditTag_KeepsItsOwnNameAndReturnsTheNewParentAndIcon ()
	{
		var (root, people, mom) = Tree ();
		var window = TagWindow.ForEdit (root, new TagIconSource (mom, []));

		Assert.AreEqual ("Mom", window.TagName);
		Assert.IsTrue (window.CanAccept);
		Assert.AreSame (people, window.SelectedParent);

		window.TagName = "people";
		Assert.IsTrue (window.ShowsNameInUse);

		window.TagName = "Mother";
		window.SelectedParent = root;
		window.PendingIcon = new TagIconChoice ("emblem-people", null);
		window.Accept ();

		Assert.IsNotNull (window.EditResult);
		Assert.AreEqual ("Mother", window.EditResult.Name);
		Assert.AreSame (root, window.EditResult.Parent);
		Assert.AreEqual ("emblem-people", window.EditResult.Icon?.ThemeIconName);
	}

	[TestMethod]
	public void EditTagIcon_NoImageAndPredefinedIcons ()
	{
		var (_, _, mom) = Tree ();
		var window = new EditTagIconWindow (new TagIconSource (mom, []), null);
		window.ChooseNoImage ();
		Assert.AreSame (TagIconChoice.NoImage, window.Result);

		var cancelled = new EditTagIconWindow (new TagIconSource (mom, []), null);
		cancelled.Accept ();
		Assert.IsNull (cancelled.Result);
	}

	[TestMethod]
	public void EditTagIcon_PhotoSelectionBecomesTheIcon ()
	{
		var (_, _, mom) = Tree ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var window = new EditTagIconWindow (new TagIconSource (mom, [sample, sample]), null);

		Assert.AreEqual (1, window.PhotoNumber);
		Assert.IsNotNull (window.Preview?.Icon);
		Assert.AreEqual (TagIconImage.Size, window.Preview.Icon.Width);

		window.OnPhotoSelection (new FSpot.Imaging.PixelRect (0, 0, 10, 10));
		window.Accept ();
		Assert.IsNotNull (window.Result?.Icon);
		Assert.AreEqual (TagIconImage.Size, window.Result.Icon.Height);
	}

	[TestMethod]
	public void VersionName_RenameStartsWithTheCurrentNameAndRejectsAnother ()
	{
		var window = new VersionNameWindow (VersionNameRequest.Rename, "Modified", ["Original", "Modified"]);

		Assert.AreEqual ("Modified", window.VersionName);
		Assert.IsTrue (window.CanAccept);

		window.VersionName = "Original";
		Assert.IsTrue (window.ShowsNameInUse);
		Assert.IsFalse (window.CanAccept);

		window.VersionName = "Cropped";
		window.Accept ();
		Assert.AreEqual ("Cropped", window.Result);
	}

	[TestMethod]
	public void TagSelection_ReturnsTheHighlightedTags ()
	{
		var (root, _, _) = Tree ();
		var window = new TagSelectionWindow (root);

		window.Select (window.Nodes[0], window.Nodes[0].Children[0]);
		window.Accept ();

		Assert.AreSequenceEqual (new[] { "People", "Mom" }, window.Result!.Select (tag => tag.Name).ToArray ());
	}
}

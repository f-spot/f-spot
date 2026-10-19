// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Services;

using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Imaging;
using FSpot;
using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class FilterAndTagDialogWindowTests
{
	public TestContext TestContext { get; }

	public FilterAndTagDialogWindowTests (TestContext testContext) => TestContext = testContext;

	[TestInitialize]
	public void Start ()
		=> AvaloniaApp.Ensure ();

	[UiTestMethod]
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

	[UiTestMethod]
	public void CatalogMerge_StartsOnNewRollsAndCopyAndReturnsTheChosenRoll ()
	{
		var window = new CatalogMergeWindow ([
			new ImportRollOption (8, new DateTime (2019, 6, 7, 0, 0, 0, DateTimeKind.Utc), 1),
			new ImportRollOption (4, new DateTime (2019, 1, 2, 0, 0, 0, DateTimeKind.Utc), 2)
		]);
		window.Show ();
		try {
			Assert.AreEqual (CatalogRollChoice.New, window.Rolls);
			Assert.IsTrue (window.CopyFiles);
			Assert.IsFalse (window.RollListEnabled);

			window.ChooseRolls (CatalogRollChoice.One);
			Assert.IsTrue (window.RollListEnabled);
			Assert.IsTrue (window.OkButton.IsEnabled);
			window.ChooseRoll (1);
			window.SetCopy (false);
			window.Accept ();

			Assert.IsNotNull (window.Result);
			Assert.AreEqual (CatalogRollChoice.One, window.Result.Value.Rolls);
			Assert.IsFalse (window.Result.Value.CopyFiles);
			Assert.AreEqual (4u, window.Result.Value.RollId);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void CatalogMerge_DisablesOkWhenASingleRollIsSelectedAndTheListIsEmpty ()
	{
		var window = new CatalogMergeWindow ([]);
		window.Show ();
		try {
			window.ChooseRolls (CatalogRollChoice.One);
			Assert.IsFalse (window.OkButton.IsEnabled);

			window.Accept ();
			Assert.IsNull (window.Result);

			window.ChooseRolls (CatalogRollChoice.New);
			Assert.IsTrue (window.OkButton.IsEnabled);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public async Task MainWindow_ShowsAnInformationDialogInsteadOfAWarning ()
	{
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			IAvaloniaShell shell = window;
			Task pending = shell.ShowInfoAsync ("Moved", "Moved 2 photos to a new import roll.");
			MessageDialog dialog = window.OwnedWindows.OfType<MessageDialog> ().Single ();
			Assert.AreEqual ("i", dialog.IconGlyph.Text);
			Assert.AreEqual ("Moved", dialog.HeadingBlock.Text);
			Assert.AreEqual ("Moved 2 photos to a new import roll.", dialog.BodyBlock.Text);
			dialog.OkButton.RaiseEvent (new RoutedEventArgs (Button.ClickEvent));
			await pending;
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
	public void ZipScale_StartsOffAt1024AndReturnsTheEdgeWhenChecked ()
	{
		var window = new ZipScaleWindow ();
		window.Show ();
		try {
			Assert.IsFalse (window.Scale);
			Assert.AreEqual (ZipScaleChoice.DefaultLongerSide, window.LongerSide);
			Assert.AreEqual (1024, window.LongerSide);
			window.SetScale (true);
			window.SetLongerSide (32);
			window.Accept ();
			Assert.IsNotNull (window.Result);
			Assert.IsTrue (window.Result.Value.Scale);
			Assert.AreEqual (32, window.Result.Value.LongerSide);
		} finally {
			window.Close ();
		}
	}

	[UiTestMethod]
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

	[UiTestMethod]
	public void DateRange_OkUsesTheDatesAlreadyOnScreen ()
	{
		var today = new DateTime (2026, 9, 23);
		var start = new DateTime (2020, 3, 1);
		var end = new DateTime (2020, 3, 15);
		var window = new DateRangeWindow (today, start, end);

		window.Accept ();

		Assert.IsNotNull (window.Result?.Range);
		Assert.AreEqual (start, window.Result.Range.Value.Start);
		Assert.AreEqual (end.Add (new TimeSpan (23, 59, 59)), window.Result.Range.Value.End);
	}

	[UiTestMethod]
	public void DateRange_AllImagesDisablesTheDatesAndClearsTheFilter ()
	{
		var window = new DateRangeWindow (new DateTime (2026, 9, 23), null, null);

		window.Preset = DateRangePreset.AllDates;
		Assert.IsFalse (window.DatesEnabled);

		window.Accept ();
		Assert.IsNotNull (window.Result);
		Assert.IsNull (window.Result.Range);
	}

	[UiTestMethod]
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

	[UiTestMethod]
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

	[UiTestMethod]
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

	[UiTestMethod]
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

	[UiTestMethod]
	public async Task EditTagIcon_PhotoSelectionBecomesTheIcon ()
	{
		var (_, _, mom) = Tree ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var window = new EditTagIconWindow (new TagIconSource (mom, [sample, sample]), null);

		Assert.AreEqual (1, window.PhotoNumber);
		long started = Environment.TickCount64;
		while (window.Preview?.Icon is null && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.IsNotNull (window.Preview?.Icon);
		Assert.AreEqual (TagIconImage.Size, window.Preview.Icon.Width);

		window.OnPhotoSelection (new FSpot.Imaging.PixelRect (0, 0, 10, 10));
		window.Accept ();
		Assert.IsNotNull (window.Result?.Icon);
		Assert.AreEqual (TagIconImage.Size, window.Result.Icon.Height);
	}

	[UiTestMethod]
	public async Task EditTagIcon_LeavesTheFirstPhotoUntouched ()
	{
		var (_, _, mom) = Tree ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var window = new EditTagIconWindow (new TagIconSource (mom, [sample]), null);

		long started = Environment.TickCount64;
		while (window.Preview?.Icon is null && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.IsNotNull (window.Preview?.Icon);
		window.Accept ();
		Assert.IsNull (window.Result);
	}

	[UiTestMethod]
	public async Task EditTagIcon_ChangingThePhotoAppliesThatPhoto ()
	{
		var (_, _, mom) = Tree ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		var window = new EditTagIconWindow (new TagIconSource (mom, [sample, sample]), null);
		window.Show ();

		long started = Environment.TickCount64;
		while (window.Preview?.Icon is null && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.IsNotNull (window.Preview?.Icon);
		var first = window.Preview.Icon;
		var spinner = window.GetVisualDescendants ().OfType<NumericUpDown> ().Single ();
		spinner.Value = 2;
		started = Environment.TickCount64;
		while (ReferenceEquals (window.Preview?.Icon, first) && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.AreEqual (2, window.PhotoNumber);
		window.Accept ();
		Assert.IsNotNull (window.Result?.Icon);
		Assert.AreEqual (TagIconImage.Size, window.Result.Icon.Width);
		if (window.IsVisible)
			window.Close ();
	}

	[UiTestMethod]
	public async Task EditTagIcon_StaleDecodeFailureLeavesTheCurrentPhoto ()
	{
		var (_, _, mom) = Tree ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		string missing = sample + ".missing.jpg";
		var window = new EditTagIconWindow (new TagIconSource (mom, [sample, missing]), null);

		long started = Environment.TickCount64;
		while (window.PhotoImage.Source is null && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		var first = window.PhotoImage.Source;
		Assert.IsNotNull (first);
		Assert.AreEqual (1, window.PhotoNumber);
		window.ShowPhoto (1);

		started = Environment.TickCount64;
		while (Environment.TickCount64 - started < 2_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.AreSame (first, window.PhotoImage.Source);
		Assert.AreEqual (1, window.PhotoNumber);
	}

	[UiTestMethod]
	public async Task EditTagIcon_CurrentDecodeFailureClearsThePhoto ()
	{
		var (_, _, mom) = Tree ();
		string missing = Path.Combine (Path.GetTempPath (), "fspot-missing-" + Guid.NewGuid ().ToString ("N") + ".jpg");
		var window = new EditTagIconWindow (new TagIconSource (mom, [missing]), null);

		long started = Environment.TickCount64;
		while (Environment.TickCount64 - started < 2_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.IsNull (window.PhotoImage.Source);
		Assert.AreEqual (1, window.PhotoNumber);
	}

	[UiTestMethod]
	public async Task EditTagIcon_AChoiceDuringTheFirstDecodeStaysTheIcon ()
	{
		var (_, _, mom) = Tree ();
		string sample = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		string icon = PredefinedTagIcons.Names (PackedIconFiles.All).First ();
		using var hold = DecodeGate.Enter ();
		var window = new EditTagIconWindow (new TagIconSource (mom, [sample]), null);
		var list = window.FindControl<ListBox> ("PredefinedList");
		Assert.IsNotNull (list);
		list.SelectedItem = icon;
		Assert.AreEqual (icon, window.Preview?.ThemeIconName);
		hold.Dispose ();

		long started = Environment.TickCount64;
		while (window.PhotoImage.Source is null && Environment.TickCount64 - started < 5_000) {
			await Dispatcher.UIThread.InvokeAsync (() => { }, DispatcherPriority.Background, TestContext.CancellationToken);
			await Task.Delay (10, TestContext.CancellationToken);
		}

		Assert.IsNotNull (window.PhotoImage.Source);
		window.Accept ();
		Assert.AreEqual (icon, window.Result?.ThemeIconName);
		Assert.IsNull (window.Result?.Icon);
	}

	[UiTestMethod]
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

	[UiTestMethod]
	public void TagSelection_ReturnsTheHighlightedTags ()
	{
		var (root, _, _) = Tree ();
		var window = new TagSelectionWindow (root);

		window.Select (window.Nodes[0], window.Nodes[0].Children[0]);
		window.Accept ();

		Assert.AreSequenceEqual (new[] { "People", "Mom" }, window.Result!.Select (tag => tag.Name).ToArray ());
	}
}

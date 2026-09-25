// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;
using FSpot.Core;
using FSpot.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class FindAndQueryBarTests
{
	static readonly Tag Beach = new (null, 1, "Beach");
	static readonly Tag Family = new (null, 2, "Family");
	static readonly Tag Work = new (null, 3, "Work");

	static MainWindowViewModel NewViewModel (out TaggedPhoto beach, out TaggedPhoto both, out TaggedPhoto work)
	{
		var viewModel = new MainWindowViewModel (null, new MainWindowViewModelTests.NullShell (), new MainWindowViewModelTests.NullThumbnails ());
		viewModel.Tags.Add (new TagNode (Beach));
		viewModel.Tags.Add (new TagNode (Family));
		viewModel.Tags.Add (new TagNode (Work));
		beach = new TaggedPhoto ("a.jpg", Beach);
		both = new TaggedPhoto ("b.jpg", Beach, Family);
		work = new TaggedPhoto ("c.jpg", Work);
		viewModel.SeedCatalog (beach, both, work);
		return viewModel;
	}

	static IPhoto[] Visible (MainWindowViewModel viewModel)
		=> [.. viewModel.Photos.Select (item => item.Photo)];

	[UiTestMethod]
	public void FindText_Or_ShowsEitherTag ()
	{
		var viewModel = NewViewModel (out var beach, out var both, out var work);

		viewModel.FindText = "family or work";

		Assert.AreSequenceEqual (new IPhoto[] { both, work }, Visible (viewModel));
	}

	[UiTestMethod]
	public void FindText_AndNot_ExcludesTheNegatedTag ()
	{
		var viewModel = NewViewModel (out var beach, out _, out _);

		viewModel.FindText = "beach and not (family)";

		Assert.AreSequenceEqual (new IPhoto[] { beach }, Visible (viewModel));
	}

	[UiTestMethod]
	public void FindText_Invalid_KeepsThePreviousQuery ()
	{
		var viewModel = NewViewModel (out _, out _, out var work);
		viewModel.FindText = "work";

		viewModel.FindText = "work and (beach";

		Assert.AreSequenceEqual (new IPhoto[] { work }, Visible (viewModel));
	}

	[UiTestMethod]
	public void HideFind_ClearsTheQuery ()
	{
		var viewModel = NewViewModel (out _, out _, out _);
		viewModel.FindText = "work";

		viewModel.HideFindCommand.Execute (null);

		Assert.HasCount (3, viewModel.Photos);
		Assert.IsFalse (viewModel.IsFindBarVisible);
	}

	[UiTestMethod]
	public void DropTagOnQuery_ShowsTheBarAndFilters ()
	{
		var viewModel = NewViewModel (out _, out _, out var work);

		viewModel.DropTagOnQuery (Work.Id.ToString (), null);

		Assert.IsTrue (viewModel.HasLibraryFilter);
		Assert.HasCount (1, viewModel.QueryGroups);
		Assert.AreSequenceEqual (new IPhoto[] { work }, Visible (viewModel));
	}

	[UiTestMethod]
	public void QueryGroups_SecondGroupShowsOr ()
	{
		var viewModel = NewViewModel (out _, out _, out _);

		viewModel.DropTagOnQuery (Work.Id.ToString (), null);
		viewModel.DropTagOnQuery (Beach.Id.ToString (), null);

		Assert.IsFalse (viewModel.QueryGroups[0].ShowOr);
		Assert.IsTrue (viewModel.QueryGroups[1].ShowOr);
		Assert.HasCount (3, viewModel.Photos);
	}

	[UiTestMethod]
	public void DropTagOnLiteral_AndsWithIt ()
	{
		var viewModel = NewViewModel (out _, out var both, out _);
		viewModel.DropTagOnQuery (Beach.Id.ToString (), null);

		viewModel.DropTagOnQuery (Family.Id.ToString (), viewModel.QueryGroups[0].Literals[0]);

		Assert.AreSequenceEqual (new IPhoto[] { both }, Visible (viewModel));
	}

	[UiTestMethod]
	public void ToggleNegated_UpdatesTooltipAndFilter ()
	{
		var viewModel = NewViewModel (out _, out _, out var work);
		viewModel.DropTagOnQuery (Beach.Id.ToString (), null);

		viewModel.ToggleQueryLiteralNegatedCommand.Execute (viewModel.QueryGroups[0].Literals[0]);

		var literal = viewModel.QueryGroups[0].Literals[0];
		Assert.IsTrue (literal.IsNegated);
		Assert.AreEqual (string.Format (FSpot.Resources.Lang.Strings.NotSpaceX, "Beach"), literal.ToolTip);
		Assert.AreSequenceEqual (new IPhoto[] { work }, Visible (viewModel));
	}

	[UiTestMethod]
	public void RemoveLiteral_EmptiesTheQuery ()
	{
		var viewModel = NewViewModel (out _, out _, out _);
		viewModel.DropTagOnQuery (Beach.Id.ToString (), null);

		viewModel.RemoveQueryLiteralCommand.Execute (viewModel.QueryGroups[0].Literals[0]);

		Assert.IsFalse (viewModel.HasTagQuery);
		Assert.IsFalse (viewModel.HasLibraryFilter);
		Assert.HasCount (3, viewModel.Photos);
	}

	[UiTestMethod]
	public void MoveLiteral_BetweenGroups ()
	{
		var viewModel = NewViewModel (out _, out var both, out _);
		viewModel.DropTagOnQuery (Beach.Id.ToString (), null);
		viewModel.DropTagOnQuery (Family.Id.ToString (), null);

		viewModel.MoveQueryLiteral (viewModel.QueryGroups[1].Literals[0], viewModel.QueryGroups[0].Literals[0]);

		Assert.HasCount (1, viewModel.QueryGroups);
		Assert.AreSequenceEqual (new IPhoto[] { both }, Visible (viewModel));
	}

	[UiTestMethod]
	public void ClearLibraryFilters_ClearsTheTagQuery ()
	{
		var viewModel = NewViewModel (out _, out _, out _);
		viewModel.DropTagOnQuery (Beach.Id.ToString (), null);

		viewModel.ClearLibraryFiltersCommand.Execute (null);

		Assert.IsFalse (viewModel.HasTagQuery);
		Assert.IsEmpty (viewModel.QueryGroups);
	}

	[UiTestMethod]
	public void RefreshSearch_KeepsTheFilter ()
	{
		var viewModel = NewViewModel (out _, out _, out var work);
		viewModel.DropTagOnQuery (Work.Id.ToString (), null);

		viewModel.RefreshSearchCommand.Execute (null);

		Assert.AreSequenceEqual (new IPhoto[] { work }, Visible (viewModel));
	}

	[UiTestMethod]
	public void TagNames_ListsNestedTags ()
	{
		var viewModel = NewViewModel (out _, out _, out _);

		Assert.AreSequenceEqual (new[] { "Beach", "Family", "Work" }, viewModel.TagNames.ToArray ());
	}

	[UiTestMethod]
	public void FindCompletion_CompletesTheWordUnderTheCaret ()
	{
		var logic = new CompletionLogic ();
		string text = "beach and fa";

		var matches = FindCompletion.Matches (["Beach", "Family", "Work"], text, text.Length, logic);

		Assert.AreSequenceEqual (new[] { "Family" }, matches.ToArray ());
		int pos = text.Length;
		Assert.AreEqual ("beach and Family", logic.ReplaceKey (text, "Family", ref pos));
		Assert.AreEqual (16, pos);
	}

	[UiTestMethod]
	public void FindCompletion_AfterOpenParen_OffersNothing ()
	{
		string text = "beach and (";

		Assert.IsEmpty (FindCompletion.Matches (["Beach"], text, text.Length, new CompletionLogic ()));
	}

	internal sealed class TaggedPhoto (string name, params Tag[] tags) : IPhoto
	{
		public DateTime Time => DateTime.UnixEpoch;
		public Tag[] Tags { get; } = tags;
		public string Description => string.Empty;
		public string Name { get; } = name;
		public uint Rating => 0;
		public IPhotoVersion DefaultVersion { get; } = new MainWindowViewModelTests.StubVersion ("/p/" + name);
		public IEnumerable<IPhotoVersion> Versions => [DefaultVersion];
	}
}

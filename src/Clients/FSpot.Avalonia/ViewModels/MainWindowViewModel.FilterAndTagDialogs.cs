// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using FSpot.Avalonia.Dialogs;
using FSpot.Avalonia.Models;
using FSpot.Core;
using FSpot.Database;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot.Avalonia.ViewModels;

// The GTK filter dialogs (rating, date range, import rolls) and tag dialogs (create,
// edit, icon, version name, tag selection, merge).
public partial class MainWindowViewModel
{
	/// <summary>
	/// Every tag highlighted in the sidebar tree, which may be more than <see cref="SelectedTag"/>.
	/// </summary>
	public ObservableCollection<TagNode> HighlightedTags { get; } = [];

	/// <summary>
	/// Set while a modified click (or a right-click on a highlighted tag) extends the tree
	/// selection, so tapping a tag does not collapse the highlight to that one tag.
	/// </summary>
	public bool ExtendingTagSelection { get; set; }

	bool KeepsTagHighlight (TagNode? node)
		=> node is not null && ExtendingTagSelection;

	public void SetHighlightedTags (IEnumerable<TagNode> nodes)
	{
		HighlightedTags.Clear ();
		foreach (var node in nodes.Distinct ())
			HighlightedTags.Add (node);
		MergeTagsCommand.NotifyCanExecuteChanged ();
	}

	void NotifyFilterDialogCommands ()
	{
		ClearRatingFilterCommand.NotifyCanExecuteChanged ();
		ClearDateRangeCommand.NotifyCanExecuteChanged ();
		ClearRollFilterCommand.NotifyCanExecuteChanged ();
	}

	[RelayCommand (CanExecute = nameof (HasFindRating))]
	void ClearRatingFilter ()
		=> DismissFindRating ();

	[RelayCommand (CanExecute = nameof (HasFindDate))]
	void ClearDateRange ()
		=> DismissFindDate ();

	[RelayCommand (CanExecute = nameof (HasFindRoll))]
	void ClearRollFilter ()
		=> DismissFindRoll ();

	[RelayCommand]
	void FindLastImportRoll ()
	{
		var last = database?.Rolls.GetRolls (1).FirstOrDefault ();
		if (last is null)
			return;
		findRollIds.Clear ();
		findRollIds.Add (last.Id);
		NotifyFindTerms ();
		ReloadPhotos ();
	}

	IReadOnlyList<ImportRollOption> ImportRollOptions ()
	{
		if (database?.Rolls is not RollStore rolls)
			return [];
		return rolls.GetRolls (Preferences.Get<int> (Preferences.ImportGuiRollHistory))
			.Select (roll => new ImportRollOption (roll.Id, roll.Time, rolls.PhotosInRoll (roll)))
			.ToList ();
	}

	async Task CreateTagWithDialogAsync (Db db)
	{
		var root = db.Tags.RootCategory;
		var parent = TagDialogRules.DefaultParent (root, (HighlightedTags.FirstOrDefault () ?? SelectedTag)?.Tag);
		var choice = await shell.PromptCreateTagAsync (root, parent, Preferences.Get<bool> (Preferences.TagIconAutomatic));
		if (choice is null || string.IsNullOrWhiteSpace (choice.Name))
			return;
		Preferences.Set (Preferences.TagIconAutomatic, choice.AutoIcon);
		try {
			db.Tags.CreateCategory (choice.Parent, choice.Name, choice.AutoIcon);
		} catch (Exception ex) {
			await shell.ShowWarningAsync (Strings.CreateNewTag, ex.Message);
			return;
		}
		ReloadTags ();
	}

	async Task EditTagWithDialogAsync (Db db, Tag tag)
	{
		var choice = await shell.PromptEditTagAsync (db.Tags.RootCategory, new TagIconSource (tag, TaggedPhotoPaths (db, tag)));
		if (choice is null)
			return;
		try {
			bool nameChanged = !string.IsNullOrWhiteSpace (choice.Name) && tag.Name != choice.Name;
			if (nameChanged)
				tag.Name = choice.Name;
			if (choice.Parent is not null && !ReferenceEquals (choice.Parent, tag) && !tag.IsAncestorOf (choice.Parent))
				tag.Category = choice.Parent;
			choice.Icon?.ApplyTo (tag);
			db.Tags.Commit (tag, nameChanged);
		} catch (Exception ex) {
			await shell.ShowWarningAsync (Strings.EditTag2, ex.Message);
		}
		ReloadTags ();
	}

	static IReadOnlyList<string> TaggedPhotoPaths (Db db, Tag tag)
	{
		try {
			return db.Photos.Query (new TagTerm (tag))
				.Select (photo => photo.DefaultVersion?.Uri?.LocalPath)
				.Where (path => !string.IsNullOrEmpty (path))
				.Select (path => path!)
				.ToList ();
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to query the photos tagged {Tag}", tag.Name);
			return [];
		}
	}

	Task<string?> PromptVersionNameAsync (VersionNameRequest request, Photo photo)
	{
		var names = photo.Versions.Select (version => version.Name).ToList ();
		return shell.PromptVersionNameAsync (request, photo.DefaultVersion.Name, names);
	}

	bool CanMergeTags ()
		=> database is not null;

	/// <summary>
	/// Merge Tags: fold the highlighted tags (two or more) into one. With fewer highlighted,
	/// the Tag Selection dialog picks the tags to merge.
	/// </summary>
	[RelayCommand (CanExecute = nameof (CanMergeTags))]
	async Task MergeTagsAsync ()
	{
		if (database is null)
			return;
		IReadOnlyList<Tag>? tags = HighlightedTags.Select (node => node.Tag).ToList ();
		if (tags.Count < 2)
			tags = await shell.PickTagsAsync (database.Tags.RootCategory);
		if (tags is null)
			return;
		var plan = TagMergePlan.For (tags);
		if (plan is null)
			return;

		if (!await shell.ConfirmAsync (
				string.Format (Strings.MergeTheSelectedTags, tags.Count),
				Strings.ThisWillMergeSelectedTagsAndSubTagsIntoSingleTag,
				Strings.MergeTagsMnemonic))
			return;

		bool inTransaction = false;
		try {
			var merged = plan.Merged.ToList ();
			var photos = database.Photos.Query (new OrOperator (merged.Select (tag => new TagTerm (tag)).ToArray ()));
			foreach (var photo in photos)
				photo.AddTag (plan.Survivor);
			database.BeginTransaction ();
			inTransaction = true;
			database.Photos.Remove (merged);
			database.CommitTransaction ();
		} catch (Exception ex) {
			if (inTransaction)
				database.RollbackTransaction ();
			await shell.ShowWarningAsync (Strings.MergeTags, ex.Message);
			ReloadTags ();
			return;
		}

		ReloadTags ();
		ReloadPhotos ();
		await EditTagWithDialogAsync (database, plan.Survivor);
	}
}

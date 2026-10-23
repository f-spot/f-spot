//
// MainWindowViewModel.Menus.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using FSpot;
using FSpot.Avalonia.Models;
using FSpot.Avalonia.Services;
using FSpot.Core;
using FSpot.Query;
using FSpot.Resources.Lang;

using Hyena;

namespace FSpot.Avalonia.ViewModels;

// The GTK menus whose rows depend on the selection (Version, Attach/Remove Tag, Find With,
// Open With, extension menus) and the context menus of the tag tree, timeline, filmstrip,
// info box and query chips.
public partial class MainWindowViewModel
{
	readonly List<TagNode> highlightedTags = [];
	readonly InfoFieldVisibility libraryInfoVisibility = InfoFieldVisibility.Load (InfoFieldVisibility.LibraryContext);
	readonly InfoFieldVisibility editInfoVisibility = InfoFieldVisibility.Load (InfoFieldVisibility.EditContext);
	MenuSource? versionMenu;
	MenuSource? attachTagMenu;
	MenuSource? removeTagMenu;
	MenuSource? findWithMenu;
	MenuSource? openWithMenu;
	MenuSource? exportMenu;
	MenuSource? toolsMenu;
	MenuExtensionWatcher? menuWatcher;

	public TagTermQuery TermQuery { get; } = new ();
	public ObservableCollection<QueryLiteralChip> QueryLiterals { get; } = [];
	public bool HasTermQuery => !TermQuery.IsEmpty;
	public IOpenWithService OpenWith { get; set; } = OpenWithApps.Current;

	public MenuSource VersionMenu => versionMenu ??= new MenuSource (BuildVersionMenu);
	public MenuSource AttachTagMenu => attachTagMenu ??= new MenuSource (BuildAttachTagMenu);
	public MenuSource RemoveTagMenu => removeTagMenu ??= new MenuSource (BuildRemoveTagMenu);
	public MenuSource FindWithMenu => findWithMenu ??= new MenuSource (BuildFindWithMenu);
	public MenuSource OpenWithMenu => openWithMenu ??= new MenuSource (BuildOpenWithMenu);
	public MenuSource ExportMenu => exportMenu ??= new MenuSource (BuildExportMenu);
	public MenuSource ToolsMenu => toolsMenu ??= new MenuSource (BuildToolsMenu);
	public bool ShowToolsMenu => MenuExtensions.For (MenuExtensionPoint.Tools).Count > 0;

	// This client has one window, so Close and Quit leave the same way. macOS shows Quit only.
	public bool ShowCloseInMenu => !OperatingSystem.IsMacOS ();

	void WatchMenuExtensions ()
		=> menuWatcher = new MenuExtensionWatcher (this);

	void OnMenuExtensionsChanged ()
		=> OnPropertyChanged (nameof (ShowToolsMenu));

	// The tags selected in the sidebar tree (GTK's TagHighlight); the focused tag when none are.
	public IReadOnlyList<TagNode> HighlightedTags
		=> highlightedTags.Count > 0 ? highlightedTags : SelectedTag is null ? [] : [SelectedTag];

	public bool HasHighlightedTags => HighlightedTags.Count > 0;
	public bool HasSingleHighlightedTag => HighlightedTags.Count == 1;
	public bool CanMergeHighlightedTags => TagMerge.CanMerge (HighlightedTags.Select (node => node.Tag).ToList ());
	public bool CanFindWith => HasHighlightedTags && HasTermQuery;

	public string FindSelectedTagsHeader
		=> string.Format (HighlightedTags.Count <= 1 ? Strings.FindSelectedTagMnemonic : Strings.FindSelectedTagsMnemonic, HighlightedTags.Count);

	public string FindSelectedTagsWithHeader
		=> string.Format (HighlightedTags.Count <= 1 ? Strings.FindSelectedTagWithMnemonic : Strings.FindSelectedTagsWithMnemonic, HighlightedTags.Count);

	public string FindWithHeader => string.Format (Strings.FindWithMnemonic, HighlightedTags.Count);

	public string ActiveTagExcludeHeader
		=> ActiveTagFilter is null ? string.Empty : string.Format (Strings.ExcludePhotosTaggedX, ActiveTagFilter.Name);

	public bool FilmstripHorizontal => !FilmstripOnSide;

	public IReadOnlyList<InfoStar> PopupStars {
		get {
			uint rating = 0;
			if (EntirePagedSelection (out PhotoQuery query)) {
				rating = query.UniformRating (picked) ?? 0;
			} else {
				var photos = MutationItems ().Select (item => item.Photo).ToList ();
				rating = photos.Count > 0 && photos.All (photo => photo.Rating == photos[0].Rating) ? photos[0].Rating : 0;
			}
			return Enumerable.Range (1, 5).Select (index => new InfoStar ((uint)index, index <= rating)).ToList ();
		}
	}

	public void SetHighlightedTags (IEnumerable<TagNode> nodes)
	{
		highlightedTags.Clear ();
		highlightedTags.AddRange (nodes.Distinct ());
		NotifyHighlightedTags ();
	}

	partial void OnSelectedTagChanged (TagNode? oldValue, TagNode? newValue)
		=> NotifyHighlightedTags ();

	partial void OnActiveTagFilterChanged (TagNode? oldValue, TagNode? newValue)
		=> OnPropertyChanged (nameof (ActiveTagExcludeHeader));

	partial void OnViewModeChanged (LibraryViewMode oldValue, LibraryViewMode newValue)
		=> OnPropertyChanged (nameof (InfoVisibility));

	void NotifyHighlightedTags ()
	{
		OnPropertyChanged (nameof (HighlightedTags));
		OnPropertyChanged (nameof (HasHighlightedTags));
		OnPropertyChanged (nameof (HasSingleHighlightedTag));
		OnPropertyChanged (nameof (CanMergeHighlightedTags));
		OnPropertyChanged (nameof (CanFindWith));
		OnPropertyChanged (nameof (FindSelectedTagsHeader));
		OnPropertyChanged (nameof (FindSelectedTagsWithHeader));
		OnPropertyChanged (nameof (FindWithHeader));
		IncludeHighlightedTagsCommand.NotifyCanExecuteChanged ();
		MergeTagsCommand.NotifyCanExecuteChanged ();
	}

	// Refreshes the rows a photo popup shows before it opens.
	public void RefreshPhotoPopup ()
		=> OnPropertyChanged (nameof (PopupStars));

	public IReadOnlyList<MenuContribution> PhotoPopupContributions ()
		=> MenuExtensions.For (MenuExtensionPoint.PhotoPopup);

	internal IReadOnlyList<MenuEntry> BuildVersionMenu ()
	{
		if (SelectedPhoto?.Photo is not { } photo)
			return [MenuEntry.Disabled (Strings.ParenNoEditsParen)];
		var entries = new List<MenuEntry> ();
		var current = photo.DefaultVersion;
		foreach (var version in photo.Versions)
			entries.Add (MenuEntry.Item (MenuEntry.Literal (version.Name), SwitchVersionCommand, version.Name,
				isChecked: ReferenceEquals (version, current)));
		if (entries.Count <= 1)
			entries.Add (MenuEntry.Disabled (Strings.ParenNoEditsParen));
		return entries;
	}

	internal IReadOnlyList<MenuEntry> BuildAttachTagMenu ()
	{
		var entries = TagEntries (Tags);
		entries.Add (MenuEntry.Separator);
		entries.Add (MenuEntry.Item (Strings.CreateNewTag, CreateTagAndAttachCommand));
		return entries;
	}

	// GTK TagMenu: a category with children opens a submenu that starts with the category itself.
	List<MenuEntry> TagEntries (IEnumerable<TagNode> nodes)
	{
		var entries = new List<MenuEntry> ();
		foreach (var node in nodes) {
			var item = MenuEntry.Item (MenuEntry.Literal (node.Tag.Name), AttachTagToSelectionCommand, node);
			if (node.Children.Count == 0) {
				entries.Add (item);
				continue;
			}
			var children = new List<MenuEntry> { item, MenuEntry.Separator };
			children.AddRange (TagEntries (node.Children));
			entries.Add (MenuEntry.Submenu (MenuEntry.Literal (node.Tag.Name), children));
		}
		return entries;
	}

	internal IReadOnlyList<MenuEntry> BuildRemoveTagMenu ()
	{
		var tags = new List<Tag> ();
		if (Photos.IsPaged && allSelected && libraryQuery is not null && database is not null) {
			foreach (uint id in libraryQuery.TagIds (picked)) {
				Tag tag = database.Tags.Get (id);
				if (tag is not null && tags.All (existing => existing.Id != tag.Id))
					tags.Add (tag);
			}
		} else {
			foreach (var photo in MutationItems ().Select (item => item.Photo)) {
				foreach (var tag in photo.Tags ?? []) {
					if (tag is not null && tags.All (existing => existing.Id != tag.Id))
						tags.Add (tag);
				}
			}
		}
		if (tags.Count == 0)
			return [MenuEntry.Disabled (Strings.ParenNoTagsParen)];
		return tags.Select (tag => MenuEntry.Item (MenuEntry.Literal (tag.Name), RemoveTagFromSelectionCommand, tag)).ToList ();
	}

	// GTK TermMenuItem: "All" requires the tags in every term; each term row ANDs them into that term.
	internal IReadOnlyList<MenuEntry> BuildFindWithMenu ()
	{
		var tags = HighlightedTags;
		var single = tags.Count == 1 ? tags[0].Tag : null;
		var terms = new List<MenuEntry> ();
		bool anyEnabled = false;
		for (int i = 0; i < TermQuery.Terms.Count; i++) {
			bool contains = single is not null && TermQuery.TermContains (i, single);
			anyEnabled |= !contains;
			terms.Add (MenuEntry.Item (MenuEntry.Literal (TermQuery.DescribeTerm (i)), AddHighlightedTagsToTermCommand, i,
				enabled: !contains && tags.Count > 0));
		}
		var entries = new List<MenuEntry> {
			MenuEntry.Item (Strings.All, RequireHighlightedTagsCommand, enabled: anyEnabled && tags.Count > 0)
		};
		if (terms.Count > 0) {
			entries.Add (MenuEntry.Separator);
			entries.AddRange (terms);
		}
		return entries;
	}

	internal IReadOnlyList<MenuEntry> BuildOpenWithMenu ()
	{
		var paths = SelectionPaths ();
		IReadOnlyList<OpenWithApp> apps = [];
		if (paths.Count > 0) {
			try {
				apps = OpenWith.ApplicationsFor (paths);
			} catch (Exception ex) {
				Logger.Log.Debug (ex, "Failed to list applications for the selection");
			}
		}
		if (apps.Count == 0)
			return [MenuEntry.Disabled (Strings.NoApplicationsAvailable)];
		return apps.Select (app => MenuEntry.Item (MenuEntry.Literal (app.Name), OpenWithApplicationCommand, app)).ToList ();
	}

	internal IReadOnlyList<MenuEntry> BuildExportMenu ()
		=> ContributionEntries (MenuExtensionPoint.Exports, separated: false);

	internal IReadOnlyList<MenuEntry> BuildToolsMenu ()
		=> ContributionEntries (MenuExtensionPoint.Tools, separated: false);

	List<MenuEntry> ContributionEntries (MenuExtensionPoint point, bool separated)
	{
		var entries = new List<MenuEntry> ();
		IReadOnlyList<IPhoto> photos = SelectionPhotos ();
		foreach (var contribution in MenuExtensions.For (point)) {
			if (separated && entries.Count == 0)
				entries.Add (MenuEntry.Separator);
			entries.Add (MenuEntry.Item (contribution.Label, RunMenuContributionCommand, contribution,
				enabled: contribution.IsEnabled?.Invoke (photos) ?? true));
		}
		return entries;
	}

	[RelayCommand]
	void SwitchVersion (string? name)
	{
		if (!string.IsNullOrEmpty (name))
			ApplyVersion (name);
	}

	[RelayCommand]
	void AttachTagToSelection (TagNode? node)
	{
		if (node is not null)
			AttachTagToCurrentSelection (node.Tag);
	}

	[RelayCommand]
	async Task CreateTagAndAttachAsync ()
	{
		if (database is null)
			return;
		string? name = await shell.PromptAsync (Strings.CreateNewTag, Strings.NameOfNewTagColon, string.Empty);
		if (string.IsNullOrWhiteSpace (name))
			return;
		var parent = SelectedTag?.Tag as Category ?? SelectedTag?.Tag.Category ?? database.Tags.RootCategory;
		var tag = database.Tags.GetTagByName (name.Trim ()) ?? database.Tags.CreateCategory (parent, name.Trim (), true);
		ReloadTags ();
		AttachTagToCurrentSelection (tag);
	}

	[RelayCommand]
	void RemoveTagFromSelection (Tag? tag)
	{
		if (tag is null)
			return;
		RemoveTagFromCurrentSelection (tag);
	}

	[RelayCommand]
	void PreserveOrSelectTag (TagNode? node)
	{
		// A right-click inside a multi-tag selection keeps it, so the popup can merge or find all of them.
		if (node is null || highlightedTags.Contains (node))
			return;
		SelectedTag = node;
	}

	bool CanIncludeHighlightedTags ()
		=> HasHighlightedTags;

	[RelayCommand (CanExecute = nameof (CanIncludeHighlightedTags))]
	void IncludeHighlightedTags ()
	{
		TermQuery.Include (HighlightedTags.Select (node => node.Tag));
		OnTermQueryChanged ();
	}

	[RelayCommand]
	void RequireHighlightedTags ()
	{
		TermQuery.Require (HighlightedTags.Select (node => node.Tag));
		OnTermQueryChanged ();
	}

	[RelayCommand]
	void AddHighlightedTagsToTerm (int index)
	{
		TermQuery.AddToTerm (index, HighlightedTags.Select (node => node.Tag));
		OnTermQueryChanged ();
	}

	[RelayCommand]
	void ToggleQueryLiteral (QueryLiteralChip? chip)
	{
		if (chip is null)
			return;
		TermQuery.ToggleNegated (chip.Literal);
		OnTermQueryChanged ();
	}

	[RelayCommand]
	void RemoveTermChip (QueryLiteralChip? chip)
	{
		if (chip is null)
			return;
		TermQuery.Remove (chip.Literal);
		OnTermQueryChanged ();
	}

	// Query chip popup for the tag chip: GTK's LiteralPopup "Exclude Photos Tagged" moves the tag
	// into the tag query as a negated term.
	[RelayCommand]
	void ExcludeActiveTag ()
	{
		if (ActiveTagFilter is not { } node)
			return;
		var tag = node.Tag;
		ActiveTagFilter = null;
		var found = FindTags.FirstOrDefault (existing => existing.Tag.Id == tag.Id);
		if (found is not null)
			FindTags.Remove (found);
		TermQuery.Include ([tag]);
		var literal = TermQuery.Literals.FirstOrDefault (existing => existing.Tag.Id == tag.Id && !existing.IsNegated);
		if (literal is not null)
			TermQuery.ToggleNegated (literal);
		OnTermQueryChanged ();
	}

	void OnTermQueryChanged ()
	{
		QueryLiterals.Clear ();
		for (int term = 0; term < TermQuery.Terms.Count; term++) {
			foreach (var literal in TermQuery.Terms[term])
				QueryLiterals.Add (new QueryLiteralChip (literal, term));
		}
		OnPropertyChanged (nameof (HasTermQuery));
		OnPropertyChanged (nameof (CanFindWith));
		ApplyLocalFilters ();
		NotifyFilterBar ();
		NotifyFindTerms ();
	}

	void ClearTermQuery ()
	{
		if (TermQuery.IsEmpty)
			return;
		TermQuery.Clear ();
		OnTermQueryChanged ();
	}

	[RelayCommand]
	void ClearTimelineRange ()
	{
		timelineRangeStart = null;
		timelineRangeEnd = null;
		if (SelectedTimeline is not null) {
			SelectedTimeline = null;
			return;
		}
		HighlightTimelineRange ();
		ApplyLocalFilters ();
	}

	[RelayCommand]
	void SetFilmstripOrientation (string? orientation)
	{
		bool vertical = orientation == "Vertical";
		if (vertical != FilmstripOnSide)
			ToggleFilmstripSide ();
	}

	[RelayCommand]
	void RateSelection (uint rating)
	{
		uint value = Math.Min (rating, 5);
		if (EntirePagedSelection (out PhotoQuery query)) {
			if (CountSelected () == 0)
				return;
			if (query.UniformRating (picked) == value)
				value = 0;
			query.SetRating (value, picked);
			if (database is null)
				return;
			database.Photos.ForgetCached ();
			query.DropCachedPages ();
			var shown = new List<Photo> ();
			foreach (var (index, item) in Photos.Indexed ()) {
				if (!IndexIsSelected (index) || item.Photo is not Photo photo)
					continue;
				photo.Rating = value;
				photo.Changes = null;
				shown.Add (photo);
			}
			RefreshSelectedCaptions (shown);
			RefreshPhotoPopup ();
			return;
		}

		var photos = SelectedCatalogPhotos ().ToArray ();
		if (photos.Length == 0)
			return;
		if (photos.All (photo => photo.Rating == value))
			value = 0;
		foreach (var photo in photos)
			photo.Rating = value;
		database?.Photos.Commit (photos);
		RefreshSelectedCaptions (photos);
		RefreshPhotoPopup ();
	}

	[RelayCommand]
	async Task OpenWithApplicationAsync (OpenWithApp? app)
	{
		if (app is null)
			return;
		var paths = SelectionPaths ();
		if (paths.Count == 0)
			return;
		try {
			OpenWith.Open (app, paths);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to open the selection with {App}", app.Name);
			await shell.ShowWarningAsync (app.Name, ex.Message);
		}
	}

	[RelayCommand]
	async Task RunMenuContributionAsync (MenuContribution? contribution)
	{
		if (contribution is null)
			return;
		IReadOnlyList<IPhoto> photos = SelectionPhotos ();
		try {
			await contribution.Run (photos);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Extension {Id} failed", contribution.Id);
			await shell.ShowWarningAsync (contribution.Label, ex.Message);
		}
	}

	// The static extension event must not root every view model tests construct.
	sealed class MenuExtensionWatcher : IDisposable
	{
		readonly WeakReference<MainWindowViewModel> owner;

		public MenuExtensionWatcher (MainWindowViewModel owner)
		{
			this.owner = new WeakReference<MainWindowViewModel> (owner);
			MenuExtensions.Changed += OnChanged;
		}

		public void Dispose ()
			=> MenuExtensions.Changed -= OnChanged;

		void OnChanged (object? sender, EventArgs e)
		{
			if (!owner.TryGetTarget (out var viewModel)) {
				Dispose ();
				return;
			}

			viewModel.OnMenuExtensionsChanged ();
		}
	}
}

/// <summary>
/// A tag in the find query bar, with the labels of its GTK LiteralPopup rows.
/// </summary>
public sealed class QueryLiteralChip
{
	public QueryLiteralChip (TagLiteral literal, int term)
	{
		Literal = literal;
		Term = term;
	}

	public TagLiteral Literal { get; }
	public int Term { get; }
	public string Label => Literal.Label;
	public string AutomationId => $"Main.Query.Term.{Term}.{Literal.Tag.Id}";

	public string ToggleHeader => Literal.IsNegated
		? string.Format (Strings.IncludePhotoTaggedX, Literal.Tag.Name)
		: string.Format (Strings.ExcludePhotosTaggedX, Literal.Tag.Name);
}

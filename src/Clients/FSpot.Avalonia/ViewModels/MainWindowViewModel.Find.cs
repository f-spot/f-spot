//
// MainWindowViewModel.Find.cs
//
// The find bar query language and the tag logic of the query bar.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using FSpot.Avalonia.Models;
using FSpot.Core;
using FSpot.Query;

namespace FSpot.Avalonia.ViewModels;

public partial class MainWindowViewModel
{
	TagQueryTree? tagQuery;
	FindExpression? findExpression;

	/// <summary>Tags dropped on the query bar: an OR of AND groups, like GTK's LogicWidget.</summary>
	public TagQueryTree TagQuery {
		get {
			if (tagQuery is null) {
				tagQuery = new TagQueryTree ();
				tagQuery.Changed += (_, _) => OnTagQueryChanged ();
			}
			return tagQuery;
		}
	}

	public ObservableCollection<QueryGroupViewModel> QueryGroups { get; } = [];

	public bool HasTagQuery => !TagQuery.IsEmpty;

	/// <summary>Every tag name, for completing words in the find bar.</summary>
	public IReadOnlyList<string> TagNames => [.. FlattenTags (Tags).Select (node => node.Tag.Name).Where (name => !string.IsNullOrEmpty (name))];

	/// <summary>The last query the find bar parsed; an invalid query keeps the previous one, as in GTK.</summary>
	internal FindExpression? FindExpression => findExpression;

	void ParseFindText ()
	{
		if (FindQueryParser.TryParse (FindText, TagByName, out var parsed))
			findExpression = parsed;
	}

	Tag? TagByName (string name)
	{
		string wanted = name.Trim ();
		return FlattenTags (Tags).Select (node => node.Tag)
			.FirstOrDefault (tag => string.Equals (tag.Name, wanted, StringComparison.OrdinalIgnoreCase));
	}

	IReadOnlyList<IPhotoMatcher> FindMatchers ()
	{
		var matchers = new List<IPhotoMatcher> ();
		if (findExpression is not null && !string.IsNullOrWhiteSpace (FindText))
			matchers.Add (findExpression);
		if (!TagQuery.IsEmpty)
			matchers.Add (TagQuery);
		return matchers;
	}

	void OnTagQueryChanged ()
	{
		RebuildQueryGroups ();
		OnPropertyChanged (nameof (HasTagQuery));
		ApplyLocalFilters ();
		NotifyFilterBar ();
	}

	void RebuildQueryGroups ()
	{
		QueryGroups.Clear ();
		for (int i = 0; i < TagQuery.Groups.Count; i++) {
			var group = new QueryGroupViewModel (i > 0);
			foreach (var literal in TagQuery.Groups[i].Literals)
				group.Literals.Add (new QueryLiteralViewModel (literal));
			QueryGroups.Add (group);
		}
	}

	/// <summary>Drops query literals whose tag was deleted and refreshes renamed ones.</summary>
	void SyncTagQueryWithTags ()
	{
		OnPropertyChanged (nameof (TagNames));
		if (tagQuery is null || tagQuery.IsEmpty)
			return;
		var ids = new HashSet<uint> (FlattenTags (Tags).Select (node => node.Tag.Id));
		foreach (var literal in tagQuery.Literals ().ToList ()) {
			if (!ids.Contains (literal.Tag.Id))
				tagQuery.Remove (literal);
		}
		RebuildQueryGroups ();
	}

	/// <summary>Tags dropped on the query bar (after == null) or on one of its tags.</summary>
	public void DropTagOnQuery (string? tagIdText, QueryLiteralViewModel? after)
	{
		if (!uint.TryParse (tagIdText, out uint id))
			return;
		var node = FindTagNodeById (Tags, id);
		if (node is null)
			return;
		TagQuery.Insert ([node.Tag], after?.Literal);
	}

	/// <summary>Moves a query tag onto the bar (new OR group) or next to another tag (same AND group).</summary>
	public void MoveQueryLiteral (QueryLiteralViewModel? literal, QueryLiteralViewModel? after)
	{
		if (literal is null)
			return;
		TagQuery.Move (literal.Literal, after?.Literal);
	}

	/// <summary>Adds tags to the query bar, skipping any already there (GTK's Include).</summary>
	public void IncludeInQuery (IEnumerable<Tag> tags)
		=> TagQuery.Include (tags);

	[RelayCommand]
	void ToggleQueryLiteralNegated (QueryLiteralViewModel? literal)
	{
		if (literal is not null)
			TagQuery.ToggleNegated (literal.Literal);
	}

	[RelayCommand]
	void RemoveQueryLiteral (QueryLiteralViewModel? literal)
	{
		if (literal is not null)
			TagQuery.Remove (literal.Literal);
	}

	[RelayCommand]
	void RefreshSearch ()
		=> ReloadPhotos ();
}

public sealed class QueryGroupViewModel (bool showOr)
{
	public bool ShowOr { get; } = showOr;
	public ObservableCollection<QueryLiteralViewModel> Literals { get; } = [];
}

public sealed class QueryLiteralViewModel (TagQueryLiteral literal)
{
	public TagQueryLiteral Literal { get; } = literal;
	public string Name => Literal.Tag.Name;
	public bool IsNegated => Literal.IsNegated;
	public string ToolTip => IsNegated ? string.Format (FSpot.Resources.Lang.Strings.NotSpaceX, Name) : Name;
	public string IconPath => TagIcons.UriFor (Literal.Tag.ThemeIconName);
	public string AutomationId => "query.tag." + Literal.Tag.Id;
}

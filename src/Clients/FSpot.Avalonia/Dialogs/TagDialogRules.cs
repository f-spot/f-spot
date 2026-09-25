// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// One entry in a Parent Tag combo.
/// </summary>
public sealed record TagParentOption (Category Category, string Label)
{
	public override string ToString () => Label;
}

/// <summary>
/// The shared rules of the GTK CreateTagDialog and EditTagDialog.
/// </summary>
public static class TagDialogRules
{
	/// <summary>
	/// The root category followed by every category, depth first. Create Tag indents each
	/// entry two spaces per level below the top; Edit Tag lists names flat and leaves out
	/// the edited tag and its descendants, which cannot become its parent.
	/// </summary>
	public static IReadOnlyList<TagParentOption> ParentChoices (Category root, Tag? editing = null)
	{
		var result = new List<TagParentOption> { new (root, root.Name ?? string.Empty) };
		Add (result, root, editing);
		return result;
	}

	static void Add (List<TagParentOption> result, Category parent, Tag? editing)
	{
		foreach (var tag in parent.Children) {
			if (tag is not Category category)
				continue;
			if (editing is not null && (ReferenceEquals (category, editing) || editing.IsAncestorOf (category)))
				continue;
			string label = editing is null ? new string (' ', Depth (category) * 2) + category.Name : category.Name;
			result.Add (new TagParentOption (category, label));
			Add (result, category, editing);
		}
	}

	static int Depth (Category category)
	{
		int depth = 0;
		for (var parent = category.Category; parent?.Category is not null; parent = parent.Category)
			depth++;
		return depth;
	}

	public static int IndexOf (IReadOnlyList<TagParentOption> options, Category? category)
	{
		if (category is null)
			return 0;
		for (int i = 0; i < options.Count; i++) {
			if (options[i].Category.Id == category.Id)
				return i;
		}
		return 0;
	}

	/// <summary>
	/// The category a new tag goes under: the first highlighted tag if it is a category,
	/// otherwise that tag's parent, otherwise the root.
	/// </summary>
	public static Category DefaultParent (Category root, Tag? highlighted)
		=> highlighted as Category ?? highlighted?.Category ?? root;

	/// <summary>
	/// Tag names are unique across the whole tree, compared without case.
	/// </summary>
	public static bool NameExists (Category root, string name)
	{
		foreach (var tag in root.Children) {
			if (string.Equals (tag.Name, name, StringComparison.OrdinalIgnoreCase))
				return true;
			if (tag is Category category && NameExists (category, name))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Whether the name box shows "This name is already in use". Edit Tag lets a tag keep
	/// its own name, in any case.
	/// </summary>
	public static bool NameInUse (Category root, string name, string? originalName = null)
	{
		if (string.IsNullOrEmpty (name))
			return false;
		if (originalName is not null && string.Equals (name, originalName, StringComparison.OrdinalIgnoreCase))
			return false;
		return NameExists (root, name);
	}

	public static bool CanAccept (Category root, string? name, string? originalName = null)
		=> !string.IsNullOrEmpty (name) && !NameInUse (root, name, originalName);
}

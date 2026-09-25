// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// Which tags Merge Tags folds together and which one survives, as in the GTK
/// HandleMergeTagsCommand: selected categories bring their descendants along, the list
/// is ordered children first, and the last tag (never a child of another) survives.
/// </summary>
public sealed class TagMergePlan
{
	TagMergePlan (Tag survivor, IReadOnlyList<Tag> merged)
	{
		Survivor = survivor;
		Merged = merged;
	}

	public Tag Survivor { get; }

	/// <summary>
	/// The tags removed once their photos carry the survivor. The survivor is not in it.
	/// </summary>
	public IReadOnlyList<Tag> Merged { get; }

	public static TagMergePlan? For (IReadOnlyList<Tag> selected)
	{
		if (selected.Count < 2)
			return null;
		var all = new List<Tag> (selected.Count);
		foreach (var tag in selected) {
			if (all.Contains (tag))
				continue;
			all.Add (tag);
			(tag as Category)?.AddDescendentsTo (all);
		}
		all = all.Distinct ().ToList ();
		if (all.Count < 2)
			return null;
		// GTK sorted with TagRemoveComparer, which only orders ancestor pairs and so is not a
		// total order. Ordering deepest first (stable) keeps every child ahead of its
		// ancestors and guarantees the last tag is not a descendant of any other.
		all = all.OrderByDescending (Depth).ToList ();
		var survivor = all[^1];
		return new TagMergePlan (survivor, all.Take (all.Count - 1).ToList ());
	}

	static int Depth (Tag tag)
	{
		int depth = 0;
		for (var parent = tag.Category; parent is not null; parent = parent.Category)
			depth++;
		return depth;
	}
}

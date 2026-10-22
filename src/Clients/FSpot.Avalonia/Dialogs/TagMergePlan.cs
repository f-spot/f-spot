// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot;
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
		if (selected == null || selected.Count < 2)
			return null;
		try {
			TagMerge plan = TagMerge.Plan (selected);
			return new TagMergePlan (plan.Survivor, plan.Merged);
		} catch (ArgumentException) {
			return null;
		}
	}
}

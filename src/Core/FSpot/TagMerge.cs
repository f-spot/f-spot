// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot
{
	/// <summary>
	/// The GTK "Merge Tags" plan: the selected tags plus every sub-tag collapse into one
	/// surviving tag that is not a descendant of any other merged tag.
	/// </summary>
	public sealed class TagMerge
	{
		TagMerge (Tag survivor, IReadOnlyList<Tag> merged)
		{
			Survivor = survivor;
			Merged = merged;
		}

		public Tag Survivor { get; }

		// The tags that disappear, children before parents so they are safe to delete in order.
		public IReadOnlyList<Tag> Merged { get; }

		public static bool CanMerge (IReadOnlyCollection<Tag> tags)
			=> tags != null && tags.Where (tag => tag != null).Select (tag => tag.Id).Distinct ().Count () >= 2;

		public static string ConfirmHeading (int count)
			=> string.Format (Strings.MergeTheSelectedTags, count);

		public static string ConfirmBody
			=> Strings.ThisWillMergeSelectedTagsAndSubTagsIntoSingleTag;

		public static TagMerge Plan (IEnumerable<Tag> selected)
		{
			if (selected == null)
				throw new ArgumentNullException (nameof (selected));

			var all = new List<Tag> ();
			foreach (var tag in selected) {
				if (tag == null || all.Contains (tag))
					continue;
				all.Add (tag);
				(tag as Category)?.AddDescendentsTo (all);
			}
			if (all.Count < 2)
				throw new ArgumentException ("Merging needs at least two tags.", nameof (selected));

			// Deepest first; the shallowest tag that comes last survives, as in GTK's TagRemoveComparer.
			var ordered = all.Select ((tag, index) => (tag, index, depth: Depth (tag)))
				.OrderByDescending (entry => entry.depth)
				.ThenBy (entry => entry.index)
				.Select (entry => entry.tag)
				.ToList ();
			var survivor = ordered[ordered.Count - 1];
			ordered.RemoveAt (ordered.Count - 1);
			return new TagMerge (survivor, ordered);
		}

		// Tags every photo that carries a merged tag with the survivor. Returns the photos it changed.
		public IReadOnlyList<T> Retag<T> (IEnumerable<T> photos) where T : Photo
		{
			var changed = new List<T> ();
			var ids = Merged.Select (tag => tag.Id).ToHashSet ();
			foreach (var photo in photos ?? Enumerable.Empty<T> ()) {
				if (photo?.Tags == null || !photo.Tags.Any (tag => tag != null && ids.Contains (tag.Id)))
					continue;
				photo.AddTag (Survivor);
				changed.Add (photo);
			}
			return changed;
		}

		static int Depth (Tag tag)
		{
			int depth = 0;
			for (var parent = tag.Category; parent != null; parent = parent.Category)
				depth++;
			return depth;
		}
	}
}

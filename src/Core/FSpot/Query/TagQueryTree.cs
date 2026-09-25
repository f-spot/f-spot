//
// TagQueryTree.cs
//
// The model behind the GTK LogicWidget: an OR of AND groups of tag literals.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

namespace FSpot.Query
{
	public sealed class TagQueryLiteral
	{
		internal TagQueryLiteral (Tag tag, TagQueryGroup group)
		{
			Tag = tag;
			Group = group;
		}

		public Tag Tag { get; }
		public bool IsNegated { get; internal set; }
		public TagQueryGroup Group { get; internal set; }

		public bool Matches (IPhoto photo)
			=> FindLiteral.PhotoHasTag (photo, Tag) != IsNegated;
	}

	public sealed class TagQueryGroup
	{
		internal readonly List<TagQueryLiteral> literals = new List<TagQueryLiteral> ();

		public IReadOnlyList<TagQueryLiteral> Literals => literals;

		public bool Matches (IPhoto photo)
		{
			foreach (var literal in literals) {
				if (!literal.Matches (photo))
					return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Tags dropped on the query bar. Each drop on the bar itself starts a new group that is
	/// ORed with the others; a drop on a tag joins that tag's group, ANDed with it.
	/// </summary>
	public sealed class TagQueryTree : IPhotoMatcher
	{
		readonly List<TagQueryGroup> groups = new List<TagQueryGroup> ();

		public event EventHandler Changed;

		public IReadOnlyList<TagQueryGroup> Groups => groups;

		public bool IsEmpty => groups.Count == 0;

		public IEnumerable<TagQueryLiteral> Literals ()
		{
			foreach (var group in groups)
				foreach (var literal in group.literals)
					yield return literal;
		}

		public bool Matches (IPhoto photo)
		{
			if (groups.Count == 0)
				return true;
			foreach (var group in groups) {
				if (group.Matches (photo))
					return true;
			}
			return false;
		}

		public bool TagIncluded (Tag tag)
		{
			foreach (var literal in Literals ()) {
				if (literal.Tag.Id == tag.Id)
					return true;
			}
			return false;
		}

		/// <summary>Adds the tags that are not already in the query, as one new group.</summary>
		public IReadOnlyList<TagQueryLiteral> Include (IEnumerable<Tag> tags)
		{
			var fresh = new List<Tag> ();
			foreach (var tag in tags) {
				if (tag != null && !TagIncluded (tag))
					fresh.Add (tag);
			}
			return fresh.Count == 0 ? Array.Empty<TagQueryLiteral> () : Insert (fresh, null);
		}

		/// <summary>
		/// Inserts <paramref name="tags"/> after <paramref name="after"/> in its group, or as a
		/// new group at the end when <paramref name="after"/> is null. A group never holds a tag twice.
		/// </summary>
		public IReadOnlyList<TagQueryLiteral> Insert (IEnumerable<Tag> tags, TagQueryLiteral after)
		{
			var added = InsertCore (tags, after);
			if (added.Count > 0)
				OnChanged ();
			return added;
		}

		List<TagQueryLiteral> InsertCore (IEnumerable<Tag> tags, TagQueryLiteral after)
		{
			var added = new List<TagQueryLiteral> ();
			TagQueryGroup group = after?.Group;
			if (group != null && !groups.Contains (group))
				group = null;
			int index = group == null ? 0 : group.literals.IndexOf (after) + 1;
			foreach (var tag in tags) {
				if (tag == null)
					continue;
				if (group == null) {
					group = new TagQueryGroup ();
					groups.Add (group);
					index = 0;
				} else if (group.literals.Exists (l => l.Tag.Id == tag.Id)) {
					continue;
				}
				var literal = new TagQueryLiteral (tag, group);
				group.literals.Insert (index++, literal);
				added.Add (literal);
			}
			return added;
		}

		/// <summary>
		/// Moves a literal after <paramref name="after"/> (or into a new group when null),
		/// keeping its negation.
		/// </summary>
		public void Move (TagQueryLiteral literal, TagQueryLiteral after)
		{
			if (literal == null || ReferenceEquals (literal, after) || !RemoveCore (literal))
				return;
			foreach (var moved in InsertCore (new[] { literal.Tag }, after))
				moved.IsNegated = literal.IsNegated;
			OnChanged ();
		}

		public void Remove (TagQueryLiteral literal)
		{
			if (RemoveCore (literal))
				OnChanged ();
		}

		/// <summary>Drops every literal of <paramref name="tag"/>, as when the tag is deleted.</summary>
		public void RemoveTag (Tag tag)
		{
			bool removed = false;
			foreach (var literal in new List<TagQueryLiteral> (Literals ())) {
				if (literal.Tag.Id == tag.Id)
					removed |= RemoveCore (literal);
			}
			if (removed)
				OnChanged ();
		}

		public void ToggleNegated (TagQueryLiteral literal)
		{
			if (literal?.Group == null || !groups.Contains (literal.Group))
				return;
			literal.IsNegated = !literal.IsNegated;
			OnChanged ();
		}

		public void Clear ()
		{
			if (groups.Count == 0)
				return;
			groups.Clear ();
			OnChanged ();
		}

		bool RemoveCore (TagQueryLiteral literal)
		{
			var group = literal?.Group;
			if (group == null || !groups.Contains (group) || !group.literals.Remove (literal))
				return false;
			if (group.literals.Count == 0)
				groups.Remove (group);
			return true;
		}

		void OnChanged ()
			=> Changed?.Invoke (this, EventArgs.Empty);
	}
}

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot.Query
{
	/// <summary>
	/// One tag in a <see cref="TagTermQuery"/> term, optionally negated.
	/// </summary>
	public sealed class TagLiteral
	{
		public TagLiteral (Tag tag, bool negated = false)
		{
			Tag = tag ?? throw new ArgumentNullException (nameof (tag));
			IsNegated = negated;
		}

		public Tag Tag { get; }
		public bool IsNegated { get; internal set; }

		public string Label => IsNegated ? string.Format (Strings.NotSpaceX, Tag.Name) : Tag.Name;
	}

	/// <summary>
	/// The tag part of a find, in the GTK query widget's shape: an OR of terms, where each
	/// term is an AND of literals. Include adds OR terms, Require ANDs into every term and
	/// "Find With" ANDs into one term.
	/// </summary>
	public sealed class TagTermQuery
	{
		readonly List<List<TagLiteral>> terms = new ();

		public event EventHandler Changed;

		public IReadOnlyList<IReadOnlyList<TagLiteral>> Terms => terms;
		public bool IsEmpty => terms.Count == 0;
		public IEnumerable<TagLiteral> Literals => terms.SelectMany (term => term);

		public bool TagIncluded (Tag tag)
			=> tag != null && terms.Any (term => term.Any (literal => literal.Tag.Id == tag.Id));

		public bool TagRequired (Tag tag)
			=> tag != null && terms.Count > 0 && terms.All (term => term.Any (literal => literal.Tag.Id == tag.Id));

		public bool TermContains (int index, Tag tag)
			=> tag != null && index >= 0 && index < terms.Count && terms[index].Any (literal => literal.Tag.Id == tag.Id);

		// Adds each tag that is not already in the query as its own OR term.
		public void Include (IEnumerable<Tag> tags)
		{
			bool changed = false;
			foreach (var tag in Distinct (tags)) {
				if (TagIncluded (tag))
					continue;
				terms.Add (new List<TagLiteral> { new TagLiteral (tag) });
				changed = true;
			}
			if (changed)
				OnChanged ();
		}

		// ANDs the tags into every term; with no terms yet, the tags form one AND term.
		public void Require (IEnumerable<Tag> tags)
		{
			var wanted = Distinct (tags).Where (tag => !TagRequired (tag)).ToList ();
			if (wanted.Count == 0)
				return;
			if (terms.Count == 0)
				terms.Add (new List<TagLiteral> ());
			foreach (var term in terms) {
				foreach (var tag in wanted) {
					if (term.All (literal => literal.Tag.Id != tag.Id))
						term.Add (new TagLiteral (tag));
				}
			}
			OnChanged ();
		}

		// ANDs the tags into one existing term, skipping the ones it already holds.
		public void AddToTerm (int index, IEnumerable<Tag> tags)
		{
			if (index < 0 || index >= terms.Count)
				return;
			var term = terms[index];
			bool changed = false;
			foreach (var tag in Distinct (tags)) {
				if (term.Any (literal => literal.Tag.Id == tag.Id))
					continue;
				term.Add (new TagLiteral (tag));
				changed = true;
			}
			if (changed)
				OnChanged ();
		}

		public void ToggleNegated (TagLiteral literal)
		{
			if (literal == null || !terms.Any (term => term.Contains (literal)))
				return;
			literal.IsNegated = !literal.IsNegated;
			OnChanged ();
		}

		public void Remove (TagLiteral literal)
		{
			if (literal == null)
				return;
			for (int i = 0; i < terms.Count; i++) {
				if (!terms[i].Remove (literal))
					continue;
				if (terms[i].Count == 0)
					terms.RemoveAt (i);
				OnChanged ();
				return;
			}
		}

		// Drops every literal for a tag, for example after the tag was deleted or merged.
		public void RemoveTag (Tag tag)
		{
			if (tag == null)
				return;
			bool changed = false;
			for (int i = terms.Count - 1; i >= 0; i--) {
				changed |= terms[i].RemoveAll (literal => literal.Tag.Id == tag.Id) > 0;
				if (terms[i].Count == 0)
					terms.RemoveAt (i);
			}
			if (changed)
				OnChanged ();
		}

		public void Clear ()
		{
			if (terms.Count == 0)
				return;
			terms.Clear ();
			OnChanged ();
		}

		// The GTK "Find With" label for a term: its literals joined by ", ", negated ones as "Not x".
		public string DescribeTerm (int index)
			=> index < 0 || index >= terms.Count
				? string.Empty
				: string.Join (", ", terms[index].Select (literal => literal.Label));

		public bool Matches (IPhoto photo)
		{
			if (terms.Count == 0)
				return true;
			foreach (var term in terms) {
				if (term.All (literal => HasTag (photo, literal.Tag) != literal.IsNegated))
					return true;
			}
			return false;
		}

		static bool HasTag (IPhoto photo, Tag tag)
		{
			if (photo?.Tags == null)
				return false;
			foreach (var wanted in TagTerm.WithDescendents (tag)) {
				foreach (var candidate in photo.Tags) {
					if (candidate != null && candidate.Id == wanted.Id)
						return true;
				}
			}
			return false;
		}

		static IEnumerable<Tag> Distinct (IEnumerable<Tag> tags)
		{
			var seen = new HashSet<uint> ();
			foreach (var tag in tags ?? Enumerable.Empty<Tag> ()) {
				if (tag != null && seen.Add (tag.Id))
					yield return tag;
			}
		}

		void OnChanged ()
			=> Changed?.Invoke (this, EventArgs.Empty);
	}
}

//
// FindExpression.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

namespace FSpot.Query
{
	/// <summary>
	/// An in-memory condition a photo either satisfies or not. The find bar and the
	/// query bar both produce one of these so the library filter can combine them.
	/// </summary>
	public interface IPhotoMatcher
	{
		bool Matches (IPhoto photo);
	}

	public abstract class FindExpression : IPhotoMatcher
	{
		public abstract bool Matches (IPhoto photo);
	}

	/// <summary>
	/// One word typed into the find bar. Like GTK's OrTerm of a tag Literal and a
	/// TextLiteral, it matches photos carrying the tag of that name (or one of its
	/// descendants) or whose file name, folder, or description contains the text.
	/// </summary>
	public sealed class FindLiteral : FindExpression
	{
		public FindLiteral (string text, Tag tag)
		{
			Text = text ?? string.Empty;
			Tag = tag;
		}

		public string Text { get; }
		public Tag Tag { get; }

		public override bool Matches (IPhoto photo)
		{
			if (photo == null)
				return false;
			if (Tag != null && PhotoHasTag (photo, Tag))
				return true;
			return Contains (photo.Name, Text)
				|| Contains (photo.Description, Text)
				|| Contains (BaseUri (photo), Text);
		}

		internal static bool PhotoHasTag (IPhoto photo, Tag tag)
		{
			if (photo.Tags == null)
				return false;
			foreach (var wanted in TagTerm.WithDescendents (tag)) {
				foreach (var candidate in photo.Tags) {
					if (candidate != null && candidate.Id == wanted.Id)
						return true;
				}
			}
			return false;
		}

		static string BaseUri (IPhoto photo)
		{
			try {
				return photo.DefaultVersion?.BaseUri?.ToString ();
			} catch (InvalidOperationException) {
				return null;
			}
		}

		static bool Contains (string haystack, string needle)
			=> haystack != null && needle.Length > 0 && haystack.IndexOf (needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public sealed class FindNot : FindExpression
	{
		public FindNot (FindExpression inner)
			=> Inner = inner ?? throw new ArgumentNullException (nameof (inner));

		public FindExpression Inner { get; }

		public override bool Matches (IPhoto photo)
			=> !Inner.Matches (photo);
	}

	public sealed class FindAnd : FindExpression
	{
		public FindAnd (IReadOnlyList<FindExpression> terms)
			=> Terms = terms ?? throw new ArgumentNullException (nameof (terms));

		public IReadOnlyList<FindExpression> Terms { get; }

		public override bool Matches (IPhoto photo)
		{
			foreach (var term in Terms) {
				if (!term.Matches (photo))
					return false;
			}
			return true;
		}
	}

	public sealed class FindOr : FindExpression
	{
		public FindOr (IReadOnlyList<FindExpression> terms)
			=> Terms = terms ?? throw new ArgumentNullException (nameof (terms));

		public IReadOnlyList<FindExpression> Terms { get; }

		public override bool Matches (IPhoto photo)
		{
			foreach (var term in Terms) {
				if (term.Matches (photo))
					return true;
			}
			return Terms.Count == 0;
		}
	}
}

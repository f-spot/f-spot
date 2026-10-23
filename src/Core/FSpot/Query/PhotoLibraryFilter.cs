// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

namespace FSpot.Query
{
	public static class PhotoLibraryFilter
	{
		public static bool Matches (IPhoto photo, string findText, DateTime? start, DateTime? end)
			=> MatchesText (photo, findText) && MatchesRange (photo, start, end);

		public static bool Matches (IPhoto photo, LibraryFind find, uint? rollId = null)
		{
			if (find == null)
				return true;
			if (!MatchesText (photo, find.Text))
				return false;
			if (!MatchesRange (photo, find.Start, find.End, find.ExcludeEnd))
				return false;
			if (!MatchesTags (photo, find.Tags, find.RequireAllTags))
				return false;
			if (find.TagQuery != null && !find.TagQuery.Matches (photo))
				return false;
			if (!MatchesRating (photo, find.MinRating, find.MaxRating))
				return false;
			if (!MatchesRoll (rollId ?? RollId (photo), find.RollIds))
				return false;
			if (find.Untagged && photo.Tags != null && photo.Tags.Length > 0)
				return false;
			if (find.Matchers != null) {
				foreach (var matcher in find.Matchers) {
					if (matcher != null && !matcher.Matches (photo))
						return false;
				}
			}
			return true;
		}

		public static bool MatchesTags (IPhoto photo, IReadOnlyList<Tag> tags, bool requireAll)
		{
			if (tags == null || tags.Count == 0)
				return true;
			if (photo.Tags == null || photo.Tags.Length == 0)
				return false;

			if (requireAll) {
				for (int i = 0; i < tags.Count; i++) {
					if (!HasTag (photo, tags[i]))
						return false;
				}
				return true;
			}

			for (int i = 0; i < tags.Count; i++) {
				if (HasTag (photo, tags[i]))
					return true;
			}
			return false;
		}

		public static bool MatchesRating (IPhoto photo, uint? minRating, uint? maxRating)
		{
			if (minRating is null && maxRating is null)
				return true;
			uint rating = photo.Rating;
			if (minRating is uint min && rating < min)
				return false;
			if (maxRating is uint max && rating > max)
				return false;
			return true;
		}

		public static bool MatchesRoll (uint photoRollId, IReadOnlyList<uint> rollIds)
		{
			if (rollIds == null || rollIds.Count == 0)
				return true;
			for (int i = 0; i < rollIds.Count; i++) {
				if (rollIds[i] == photoRollId)
					return true;
			}
			return false;
		}

		static bool HasTag (IPhoto photo, Tag tag)
		{
			if (tag == null || photo.Tags == null)
				return false;
			foreach (var wanted in TagTerm.WithDescendents (tag)) {
				foreach (var candidate in photo.Tags) {
					if (candidate != null && candidate.Id == wanted.Id)
						return true;
				}
			}
			return false;
		}

		static uint RollId (IPhoto photo)
			=> photo is Photo catalog ? catalog.RollId : 0;

		public static bool MatchesText (IPhoto photo, string findText)
		{
			if (string.IsNullOrWhiteSpace (findText))
				return true;

			string needle = findText.Trim ();
			if (ContainsIgnoreCase (photo.Name, needle) || ContainsIgnoreCase (photo.Description, needle))
				return true;

			if (photo.Tags == null)
				return false;

			foreach (var tag in photo.Tags) {
				if (ContainsIgnoreCase (tag.Name, needle))
					return true;
			}

			return false;
		}

		public static bool MatchesRange (IPhoto photo, DateTime? start, DateTime? end)
			=> MatchesRange (photo, start, end, true);

		public static bool MatchesRange (IPhoto photo, DateTime? start, DateTime? end, bool excludeEnd)
		{
			if (start is DateTime rangeStart && photo.Time < rangeStart)
				return false;
			if (end is DateTime rangeEnd && (excludeEnd ? photo.Time >= rangeEnd : photo.Time > rangeEnd))
				return false;
			return true;
		}

		static bool ContainsIgnoreCase (string haystack, string needle)
			=> haystack != null && haystack.IndexOf (needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}

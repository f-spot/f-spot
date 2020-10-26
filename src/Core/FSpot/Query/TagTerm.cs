//
// TagTerm.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using FSpot.Core;

namespace FSpot.Query
{
	public class TagTerm : LogicalTerm
	{
		public Tag Tag { get; private set; }

		public TagTerm (Tag tag)
		{
			Tag = tag;
		}

		public override string SqlClause ()
		{
			return SqlClause (this);
		}

		internal static string SqlClause (params TagTerm[] tags)
		{
			return SqlClause (GetTagIds (tags.Select (t => t.Tag)));
		}

		static IList<string> GetTagIds (IEnumerable<Tag> tags)
		{
			var tagList = new List<Tag> ();
			foreach (var tag in tags) {
				tagList.Add (tag);
				if (tag is Category category) {
					category.AddDescendentsTo (tagList);
				}
			}
			return tagList.Select (t => t.Id.ToString ()).ToList ();
		}

		static string SqlClause (IList<string> tagids)
		{
			if (tagids.Count == 0)
				return null;
			if (tagids.Count == 1)
				return $" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id = {tagids[0]})) ";

			return $" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({string.Join (", ", tagids)}))) ";
		}
	}
}

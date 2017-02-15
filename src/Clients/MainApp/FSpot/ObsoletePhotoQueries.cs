//
// ObsoletePhotoQueries.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//   Mike Gemünde <mike@gemuende.de>
//   Larry Ewing <lewing@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2009-2010 Mike Gemünde
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2008-2010 Ruben Vermeersch
// Copyright (C) 2006-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;
using FSpot.Core;
using FSpot.Query;
using Hyena;

namespace FSpot {
	public static class ObsoletePhotoQueries
	{
		[Obsolete ("drop this, use IQueryCondition correctly instead")]
		public static Photo [] Query (Tag [] tags)
		{
			return Query (tags, null, null, null, null);
		}

		[Obsolete ("drop this, use IQueryCondition correctly instead")]
		public static Photo [] Query (Tag [] tags, string extraCondition, DateRange range, RollSet importidrange)
		{
			return Query (OrTerm.FromTags(tags), extraCondition, range, importidrange, null);
		}

		[Obsolete ("drop this, use IQueryCondition correctly instead")]
		public static Photo [] Query (Tag [] tags, string extraCondition, DateRange range, RollSet importidrange, RatingRange ratingrange)
		{
			return Query (OrTerm.FromTags(tags), extraCondition, range, importidrange, ratingrange);
		}

		[Obsolete ("drop this, use IQueryCondition correctly instead")]
		public static Photo [] Query (Term searchexpression, string extraCondition, DateRange range, RollSet importidrange, RatingRange ratingrange)
		{
			bool hide = (extraCondition == null);

			// The SQL query that we want to construct is:
			//
			// SELECT photos.id
			//		 photos.time
			//		 photos.uri,
			//		 photos.description,
			//		 photos.roll_id,
			//		 photos.default_version_id
			//		 photos.rating
			//				   FROM photos, photo_tags
			//				   WHERE photos.time >= time1 AND photos.time <= time2
			//							   AND photos.rating >= rat1 AND photos.rating <= rat2
			//							   AND photos.id NOT IN (select photo_id FROM photo_tags WHERE tag_id = HIDDEN)
			//							   AND photos.id IN (select photo_id FROM photo_tags where tag_id IN (tag1, tag2..)
			//							   AND extra_condition_string
			//				   GROUP BY photos.id

			var query_builder = new StringBuilder ();
			var where_clauses = new List<string> ();
			query_builder.Append ("SELECT id, " +
				"time, " +
				"base_uri, " +
				"filename, " +
				"description, " +
				"roll_id, " +
				"default_version_id, " +
				"rating " +
				"FROM photos ");

			if (range != null) {
				where_clauses.Add (string.Format ("time >= {0} AND time <= {1}",
					DateTimeUtil.FromDateTime (range.Start),
					DateTimeUtil.FromDateTime (range.End)));

			}

			if (ratingrange != null) {
				where_clauses.Add (ratingrange.SqlClause ());
			}

			if (importidrange != null) {
				where_clauses.Add (importidrange.SqlClause ());
			}

			if (hide && App.Instance.Database.Tags.Hidden != null) {
				where_clauses.Add (string.Format ("id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})",
					App.Instance.Database.Tags.Hidden.Id));
			}

			if (searchexpression != null) {
				where_clauses.Add (searchexpression.SqlCondition ());
			}

			if (extraCondition != null && extraCondition.Trim () != string.Empty) {
				where_clauses.Add (extraCondition);
			}

			if (where_clauses.Count > 0) {
				query_builder.Append (" WHERE ");
				query_builder.Append (string.Join (" AND ", where_clauses.ToArray ()));
			}

			query_builder.Append (" ORDER BY time");
			return App.Instance.Database.Photos.Query (query_builder.ToString ());
		}
	}
}

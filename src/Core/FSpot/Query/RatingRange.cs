//
// RatingRange.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	public class RatingRange : IQueryCondition
	{
		public uint MinRating { get; private set; }
		public uint MaxRating { get; private set; }

		public RatingRange (uint min_rating)
		{
			MinRating = min_rating;
			MaxRating = uint.MaxValue;
		}

		public RatingRange (uint min_rating, uint max_rating)
		{
			MinRating = min_rating;
			MaxRating = max_rating;
		}

		public string SqlClause ()
		{
			return string.Format (" photos.rating >= {0} AND photos.rating <= {1} ", MinRating, MaxRating);
		}
	}
}

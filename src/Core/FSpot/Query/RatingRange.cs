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

		public RatingRange (uint minRating, uint maxRating = uint.MaxValue)
		{
			MinRating = minRating;
			MaxRating = maxRating;
		}

		public string SqlClause ()
		{
			return $" photos.rating >= {MinRating} AND photos.rating <= {MaxRating} ";
		}
	}
}

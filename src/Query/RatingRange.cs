/*
 * RatingRange.cs
 *
 * Author(s):
 *	Bengt Thuree
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot.Query
{
	public class RatingRange : IQueryCondition
	{
		private uint minRating;		
		public uint MinRating {
			get { return minRating; }
			set { minRating = value; }
		}

		private uint maxRating;		
		public uint MaxRating {
			get { return maxRating; }
			set { maxRating = value; }
		}

		public RatingRange (uint min_rating)
		{
			this.minRating = min_rating;
			this.maxRating = System.UInt32.MaxValue;
		}

		public RatingRange (uint min_rating, uint max_rating)
		{
			this.minRating = min_rating;
			this.maxRating = max_rating;
		}

		public string SqlClause ()
		{
			return String.Format (" photos.rating >= {0} AND photos.rating <= {1} ", minRating, maxRating);
		}
	}
}

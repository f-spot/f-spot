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
		public enum RatingType {
			Unrated,
			Rated
		};

		private RatingType ratetype;
		public RatingType RateType {
			get { return ratetype; }
			set { ratetype = value; }
		}

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

		public RatingRange (RatingType ratetype) {
			this.ratetype = ratetype;
		}

		public RatingRange (uint min_rating)
		{
			this.ratetype = RatingType.Rated;
			this.minRating = min_rating;
			this.maxRating = System.UInt32.MaxValue;
		}

		public RatingRange (uint min_rating, uint max_rating)
		{
			this.ratetype = RatingType.Rated;
			this.minRating = min_rating;
			this.maxRating = max_rating;
		}

		public string SqlClause ()
		{
			switch (this.ratetype) {
			case (RatingType.Unrated) :
				return String.Format (" photos.rating is NULL ");
			case (RatingType.Rated) :
				return String.Format (" photos.rating >= {0} AND photos.rating <= {1} ", minRating, maxRating);
			default :
				return String.Empty;
			}
		}
	}
}

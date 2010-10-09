//
// RatingRange.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

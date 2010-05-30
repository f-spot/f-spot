/*
 * FSpot.Query.OrderByTime.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;

namespace FSpot.Query {
	public class OrderByTime : IQueryCondition, IOrderCondition
	{
		public static OrderByTime OrderByTimeAsc = new OrderByTime (true);
		public static OrderByTime OrderByTimeDesc = new OrderByTime (false);

		bool asc;
		public bool Asc {
			get { return asc; }
		}

		public OrderByTime (bool asc)
		{
			this.asc = asc;
		}

		public string SqlClause ()
		{
			// filenames must always appear in alphabetical order if times are the same
			return String.Format (" time {0}, filename ASC ", asc ? "ASC" : "DESC");
		}
	}
}

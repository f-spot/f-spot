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
using FSpot.Utils;

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
			return String.Format (" time {0}, filename {0} ", asc ? "ASC" : "DESC");
		}
	}
}

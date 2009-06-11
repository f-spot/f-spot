/*
 * FSpot.Query.OrderByUri.cs
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
	public class OrderByUri : IQueryCondition, IOrderCondition
	{
		public static OrderByUri OrderByFilenameAsc = new OrderByUri (true);
		public static OrderByUri OrderByFilenameDesc = new OrderByUri (false);

		bool asc;
		public bool Asc {
			get { return asc; }
		}

		public OrderByUri (bool asc)
		{
			this.asc = asc;
		}

		public string SqlClause ()
		{
			return String.Format (" uri {0}", asc ? "ASC" : "DESC");
		}
	}
}

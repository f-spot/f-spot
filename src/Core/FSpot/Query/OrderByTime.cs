//
// OrderByTime.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	public class OrderByTime : IQueryCondition, IOrderCondition
	{
		public static OrderByTime OrderByTimeAsc = new OrderByTime (true);
		public static OrderByTime OrderByTimeDesc = new OrderByTime (false);

		public bool Asc { get; private set; }

		public OrderByTime (bool asc)
		{
			Asc = asc;
		}

		public string SqlClause ()
		{
			// filenames must always appear in alphabetical order if times are the same
			return $" time {(Asc ? "ASC" : "DESC")}, filename ASC ";
		}
	}
}

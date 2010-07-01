/*
 * DateRange.cs
 * 
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using Hyena;

namespace FSpot.Query {
	public class DateRange : IQueryCondition
	{
		private DateTime start;		
		public DateTime Start {
			get { return start; }
		}

		private DateTime end;
		public DateTime End {
			get { return end; }
		}

		public DateRange (DateTime start, DateTime end)
		{
			this.start = start;
			this.end = end;
		}

		public DateRange (int year, int month)
		{
			start = new DateTime (year, month, 1);
			end = new DateTime (month < 12 ? year : year + 1, month < 12 ? month + 1 : 1, 1);
		}

		public string SqlClause ()
		{
			return String.Format (" photos.time >= {0} AND photos.time <= {1} ", 
					DateTimeUtil.FromDateTime (start),
					DateTimeUtil.FromDateTime (end));
		}
	}
}

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

		public string SqlClause ()
		{
			return String.Format (" photos.time >= {0} AND photos.time <= {1} ", 
					DbUtils.UnixTimeFromDateTime (start), 
					DbUtils.UnixTimeFromDateTime (end));
		}
	}
}

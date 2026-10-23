//
// DateRange.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

namespace FSpot.Query
{
	public class DateRange : IQueryCondition
	{
		public DateTime Start { get; private set; }

		public DateTime End { get; private set; }

		public bool ExcludeEnd { get; private set; }

		public DateRange (DateTime start, DateTime end)
			: this (start, end, false)
		{
		}

		public DateRange (DateTime start, DateTime end, bool excludeEnd)
		{
			Start = start;
			End = end;
			ExcludeEnd = excludeEnd;
		}

		public DateRange (int year, int month)
		{
			Start = new DateTime (year, month, 1);
			End = new DateTime (month < 12 ? year : year + 1, month < 12 ? month + 1 : 1, 1);
		}

		public string SqlClause ()
		{
			string endCompare = ExcludeEnd ? "<" : "<=";
			return string.Format (" photos.time >= {0} AND photos.time {1} {2} ",
					DateTimeUtil.FromDateTime (Start),
					endCompare,
					DateTimeUtil.FromDateTime (End));
		}
	}
}

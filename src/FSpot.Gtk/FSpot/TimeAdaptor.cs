//
// TimeAdaptor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2006 Gabriel Burt
// Copyright (C) 2004-2005 Larry Ewing
// Copyright (C) 2006, 2008 Stephane Delcroix
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
using System.Threading;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Query;

using Hyena;

namespace FSpot
{
	public class TimeAdaptor : GroupAdaptor, ILimitable
	{
		Dictionary <int, int[]> years = new Dictionary<int, int[]> ();

		public override event GlassSetHandler GlassSet;

		public TimeAdaptor (PhotoQuery query, bool order_ascending) : base (query, order_ascending)
		{ }

		public override void SetGlass (int min)
		{
			DateTime date = DateFromIndex (min);

			GlassSet?.Invoke (this, query.LookupItem (date));
		}

		public void SetLimits (int min, int max)
		{
			DateTime start = DateFromIndex (min);

			DateTime end = DateFromIndex(max);

			end = order_ascending ? end.AddMonths (1) : end.AddMonths(-1);

			SetLimits (start, end);
		}

		public void SetLimits (DateTime start, DateTime end)
		{
			query.Range = (start > end) ? new DateRange (end, start) : new DateRange (start, end);
		}

		public override int Count ()
		{
			return years.Count * 12;
		}

		public override string GlassLabel (int item)
		{
			return string.Format ("{0} ({1})", DateFromIndex (item).ToString ("MMMM yyyy"), Value (item));
		}

		public override string TickLabel (int item)
		{
			DateTime start = DateFromIndex (item);

			if ((start.Month == 12 && !order_ascending) || (start.Month == 1 && order_ascending))
				return start.Year.ToString ();
		    return null;
		}

		public override int Value (int item)
		{
			if (order_ascending)
				return years [startyear + item/12][item % 12];

			return years [endyear - item/12][11 - item % 12];
		}

		public DateTime DateFromIndex (int item)
		{
			item = Math.Max (item, 0);
			item = Math.Min (years.Count * 12 - 1, item);

			if (order_ascending)
				return DateFromIndexAscending (item);

			return DateFromIndexDescending (item);
		}

		DateTime DateFromIndexAscending (int item)
		{
			int year = startyear + item/12;
			int month = 1 + (item % 12);

			return new DateTime(year, month, 1);
		}

		DateTime DateFromIndexDescending (int item)
		{
			int year = endyear - item/12;
			int month = 12 - (item % 12);

			year = Math.Max(1, year);
			year = Math.Min(year, 9999);
			month = Math.Max(1, month);
			month = Math.Min(month, 12);
		
			int daysInMonth = DateTime.DaysInMonth(year, month);

			return new DateTime (year, month, daysInMonth).AddDays (1.0).AddMilliseconds (-.1);
		}

		public override int IndexFromPhoto (IPhoto photo)
		{
			if (order_ascending)
			       return IndexFromDateAscending (photo.Time);

			return IndexFromDateDescending (photo.Time);
		}

		public int IndexFromDate (DateTime date)
		{
			if (order_ascending)
				return IndexFromDateAscending(date);

			return IndexFromDateDescending(date);
		}

		int IndexFromDateAscending(DateTime date)
		{
			int year = date.Year;
			int min_year = startyear;
			int max_year = endyear;

			if (year < min_year || year > max_year) {
				Log.DebugFormat ("TimeAdaptor.IndexFromDate year out of range[{1},{2}]: {0}", year, min_year, max_year);
				return 0;
			}

			return (year - startyear) * 12 + date.Month - 1 ;
		}

		int IndexFromDateDescending(DateTime date)
		{
			int year = date.Year;
			int min_year = startyear;
			int max_year = endyear;

			if (year < min_year || year > max_year) {
				Log.DebugFormat ("TimeAdaptor.IndexFromPhoto year out of range[{1},{2}]: {0}", year, min_year, max_year);
				return 0;
			}

			return 12 * (endyear - year) + 12 - date.Month;
		}

		public override IPhoto PhotoFromIndex (int item)
		{
			DateTime start = DateFromIndex (item);
			return query [query.LookupItem (start)];

		}

		public override event ChangedHandler Changed;

		uint timer;
		protected override void Reload ()
		{
			timer = Log.DebugTimerStart ();
			Thread reload = new Thread (new ThreadStart (DoReload));
			reload.IsBackground = true;
			reload.Priority = ThreadPriority.Lowest;
			reload.Start ();
		}

		int startyear = Int32.MaxValue, endyear = Int32.MinValue;
		void DoReload ()
		{
			Thread.Sleep (200);
			var years_tmp = query.Store.PhotosPerMonth ();
			int startyear_tmp = Int32.MaxValue;
			int endyear_tmp = Int32.MinValue;

			foreach (int year in years_tmp.Keys) {
				startyear_tmp = Math.Min (year, startyear_tmp);
				endyear_tmp = Math.Max (year, endyear_tmp);
			}

			ThreadAssist.ProxyToMain (() => {
				years = years_tmp;
				startyear = startyear_tmp;
				endyear = endyear_tmp;

				Changed?.Invoke (this);
			});

			Log.DebugTimerPrint (timer, "TimeAdaptor REAL Reload took {0}");
		}
	}
}

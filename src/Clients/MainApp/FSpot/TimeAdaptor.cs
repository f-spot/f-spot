/*
 * FSpot.TimeAdaptor.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 * 	Stephane Delcroix  <stephnae@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Threading;
using System.Collections.Generic;
using FSpot.Core;
using FSpot.Query;
using Hyena;

namespace FSpot {
	public class TimeAdaptor : GroupAdaptor, FSpot.ILimitable {
		Dictionary <int, int[]> years = new Dictionary<int, int[]> ();

		public override event GlassSetHandler GlassSet;
		public override void SetGlass (int min)
		{
			DateTime date = DateFromIndex (min);

			if (GlassSet != null)
				GlassSet (this, query.LookupItem (date));
		}

		public void SetLimits (int min, int max)
		{
			DateTime start = DateFromIndex (min);

			DateTime end = DateFromIndex(max);

			if (order_ascending)
				end = end.AddMonths (1);
			else
				end = end.AddMonths(-1);

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
			return String.Format ("{0} ({1})", DateFromIndex (item).ToString ("MMMM yyyy"), Value (item));
		}

		public override string TickLabel (int item)
		{
			DateTime start = DateFromIndex (item);

			if ((start.Month == 12 && !order_ascending) || (start.Month == 1 && order_ascending))
				return start.Year.ToString ();
			else
				return null;
		}

		public override int Value (int item)
		{
			if (order_ascending)
				return years [startyear + item/12][item % 12];
			else
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

		private DateTime DateFromIndexAscending (int item)
		{
			int year = startyear + item/12;
			int month = 1 + (item % 12);

			return new DateTime(year, month, 1);
		}

		private DateTime DateFromIndexDescending (int item)
		{
			int year = endyear - item/12;
			int month = 12 - (item % 12);

			return new DateTime (year, month, DateTime.DaysInMonth (year, month)).AddDays (1.0).AddMilliseconds (-.1);
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

		private int IndexFromDateAscending(DateTime date)
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

		private int IndexFromDateDescending(DateTime date)
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
			Dictionary <int, int[]> years_tmp = query.Store.PhotosPerMonth ();
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

				if (Changed != null)
					Changed (this);
			});

			Log.DebugTimerPrint (timer, "TimeAdaptor REAL Reload took {0}");
		}

		public TimeAdaptor (PhotoQuery query, bool order_ascending)
			: base (query, order_ascending)
		{ }
	}
}

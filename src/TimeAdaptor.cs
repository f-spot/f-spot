using System;
using System.Collections;

namespace FSpot {
	public class TimeAdaptor : GroupAdaptor, FSpot.ILimitable {
		ArrayList years = new ArrayList ();
		struct YearData {
			public int Year;
			public int [] Months;
		}

		public override event GlassSetHandler GlassSet;
		public override void SetGlass (int min)
		{
			DateTime date = DateFromIndex (min);
			
			if (GlassSet != null)
				GlassSet (this, LookupItem (date));
		}

		public int LookupItem (System.DateTime date)
		{
			if (order_ascending) 
				return LookUpItemAscending (date);
			
			return LookUpItemDescending (date);
		}

		private int LookUpItemAscending (System.DateTime date)
		{
			int i = 0;

			while (i < query.Count && query [i].Time < date)
				i++;

			return i;
		}

		private int LookUpItemDescending (System.DateTime date)
		{
			int i = 0;

			while (i < query.Count && query [i].Time > date)
				i++;

			return i;
		}
		
		public void SetLimits (int min, int max) 
		{
			Console.WriteLine ("min {0} max {1}", min, max);
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
			Console.WriteLine ("{0} {1}", start, end);
			if (start > end)
				query.Range = new PhotoStore.DateRange (end, start);
			else 
				query.Range = new PhotoStore.DateRange (start, end);
		}

		public override int Count ()
		{
			return years.Count * 12;
		}

		public override string GlassLabel (int item)
		{
			DateTime start = DateFromIndex (item);
			
			return String.Format ("{0} ({1})", start.ToString ("MMMM yyyy"), Value (item));
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
			YearData data = (YearData)years [item/12];

			return data.Months [item % 12];
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
			int year = (int)((YearData)years [item / 12]).Year;
			int month = 1 + (item % 12);

			return new DateTime(year, month, 1);
		}

		private DateTime DateFromIndexDescending (int item)
		{
			int year =  (int)((YearData)years [item / 12]).Year;
			int month = 12 - (item % 12);
			
			return new DateTime (year, month, DateTime.DaysInMonth (year, month)).AddDays (1.0).AddMilliseconds (-.1);
		}
		
		public override int IndexFromPhoto (FSpot.IBrowsableItem photo) 
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
			int max_year = ((YearData)years [years.Count - 1]).Year;
			int min_year = ((YearData)years [0]).Year;

			if (year < min_year || year > max_year) {
				Console.WriteLine("TimeAdaptor.IndexFromDate year out of range[{1},{2}]: {0}", year, min_year, max_year);
				return 0;
			}

			int index = date.Month - 1;

			for (int i = 0 ; i < years.Count; i++)
				if (year > ((YearData)years[i]).Year)
					index += 12;

			return index;	
		}

		private int IndexFromDateDescending(DateTime date)
		{
			int year = date.Year;
			int max_year = ((YearData)years [0]).Year;
			int min_year = ((YearData)years [years.Count - 1]).Year;
		
			if (year < min_year || year > max_year) {
				Console.WriteLine("TimeAdaptor.IndexFromPhoto year out of range[{1},{2}]: {0}", year, min_year, max_year);
				return 0;
			}

			int index = 12 - date.Month;

			for (int i = 0; i < years.Count; i++)
				if (year < ((YearData)years[i]).Year)
					index += 12;

			return index;
		}

		public override FSpot.IBrowsableItem PhotoFromIndex (int item)
	       	{
			DateTime start = DateFromIndex (item);
			return query.Items [LookupItem (start)];
		
		}

		public override event ChangedHandler Changed;
		
		protected override void Reload () 
		{
			years.Clear ();

			Photo [] photos = query.Store.Query ((Tag [])null, null, null, null);
			Array.Sort (query.Photos);
			Array.Sort (photos);

			if (!order_ascending) {				
				Array.Reverse (query.Photos);
				Array.Reverse (photos);
			}

			if (photos.Length > 0) {
				YearData data = new YearData ();
				data.Year = 0;

				foreach (Photo photo in photos) {
					int current = photo.Time.Year;
					if (current != data.Year) {
						
						data.Year = current;
						data.Months = new int [12];
						years.Add (data);
						//Console.WriteLine ("Found Year {0}", current);
					}
					if (order_ascending)
						data.Months [photo.Time.Month - 1] += 1;
					else
						data.Months [12 - photo.Time.Month] += 1;
				}
			} else {
				YearData data = new YearData ();
				data.Year = DateTime.Now.Year;
				data.Months = new int [12];
				years.Add (data);
			}

 			if (Changed != null)
				Changed (this);
		}


		public TimeAdaptor (PhotoQuery query) 
			: base (query)
		{ }
	}
}

using System;
using System.Collections;

namespace FSpot {
	public class TimeAdaptor : GroupAdaptor {
		public PhotoQuery query;

		private int span;
		ArrayList years = new ArrayList ();
		struct YearData {
			public int Year;
			public int [] Months;
		}

		public delegate void GlassSetHandler (TimeAdaptor adaptor, int index);
		public event GlassSetHandler GlassSet;

		public override void SetGlass (int min)
		{
			DateTime date = DateFromIndex (min);

			int i = 0;
			while (i < query.Photos.Length && query.Photos [i].Time < date)
				i++;
			
			if (GlassSet != null)
				GlassSet (this, i);
		}

		public override void SetLimits (int min, int max) 
		{
			DateTime start = DateFromIndex (min);
			DateTime end = DateFromIndex (max).AddMonths (1);
			
			Console.WriteLine ("{0} {1}", start, end);
			query.Range = new PhotoStore.DateRange (start, end);
		}

		public override int Count ()
		{
			return years.Count * 12;
		}

		public override string Label (int item)
		{
			DateTime start = DateFromIndex (item);
			
			return start.ToShortTimeString ();
		}

		public override int Value (int item)
		{
			YearData data = (YearData)years [item/12];

			return data.Months[item % 12];
		}
		
		public DateTime DateFromIndex (int item) 
		{
			int year =  (int)((YearData)years [item / 12]).Year;
			int month = (item % 12) + 1;

			return new DateTime (year, month, 1);
		}
		
		public void Load () {
			Photo [] photos = query.Store.Query (null, null);

			if (photos.Length > 0) {
				YearData data = new YearData ();
				data.Year = 0;

				foreach (Photo photo in photos) {
					int current = photo.Time.Year;
					if (current != data.Year) {
						data.Year = current;
						data.Months = new int [12];
						years.Add (data);
						Console.WriteLine ("Found Year {0}", current);
					}
					data.Months [photo.Time.Month - 1] += 1;
				}

			} else {
				years.Add (DateTime.Now.Year);
			}
		}


		public TimeAdaptor (PhotoQuery query) {
			this.query = query;
			
			Load ();
		}
	}
}

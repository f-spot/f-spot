using System;
using System.Collections;

namespace FSpot {
	public class TimeAdaptor : GroupAdaptor, FSpot.ILimitable {
		public PhotoQuery query;

		private int span;
		ArrayList years = new ArrayList ();
		struct YearData {
			public int Year;
			public int [] Months;
		}

		public override event GlassSetHandler GlassSet;
		public override void SetGlass (int min)
		{
			DateTime date = DateFromIndex (min);

			int i = 0;
			while (i < query.Photos.Length && query.Photos [i].Time < date)
				i++;
			
			if (GlassSet != null)
				GlassSet (this, i);
		}

		public void SetLimits (int min, int max) 
		{
			Console.WriteLine ("min {0} max {1}", min, max);
			DateTime start = DateFromIndex (min);
			DateTime end = DateFromIndex (max).AddMonths (1);
			
			Console.WriteLine ("{0} {1}", start, end);
			query.Range = new PhotoStore.DateRange (start, end);
		}

		public override int Count ()
		{
			return years.Count * 12;
		}

		public override string GlassLabel (int item)
		{
			DateTime start = DateFromIndex (item);
			
			return start.ToShortDateString ();
		}

		public override string TickLabel (int item)
		{
			DateTime start = DateFromIndex (item);
			
			if (start.Month == 1)
				return start.Year.ToString ();
			else 
				return null;
		}

		public override int Value (int item)
		{
			YearData data = (YearData)years [item/12];

			return data.Months[item % 12];
		}

		public DateTime DateFromIndex (int item) 
		{
			item = Math.Max (item, 0);
			item = Math.Min (years.Count * 12 - 1, item);

			int year =  (int)((YearData)years [item / 12]).Year;
			int month = (item % 12) + 1;
			
			return new DateTime (year, month, 1);
		}

		private void HandleChanged (IPhotoCollection query)
		{
			Console.WriteLine ("Reloading");
			Reload ();
		}
		
		public override event ChangedHandler Changed;
		public override void Reload () 
		{
			years.Clear ();

			Photo [] photos = query.Store.Query (null, null);
			Array.Sort (query.Photos);

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
				YearData data = new YearData ();
				data.Year = DateTime.Now.Year;
				data.Months = new int [12];
				years.Add (data);
			}

 			if (Changed != null)
				Changed (this);
		}


		public TimeAdaptor (PhotoQuery query) {
			this.query = query;
			this.query.Changed += HandleChanged;
			
			Reload ();
		}
	}
}

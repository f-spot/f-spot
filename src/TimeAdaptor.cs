using System;
using System.Collections;

namespace FSpot {
	public class TimeAdaptor {
		public PhotoQuery query;

		private int span;
		ArrayList years = new ArrayList ();

		public delegate void GlassSetHandler (TimeAdaptor adaptor, int index);
		public event GlassSetHandler GlassSet;

		public void SetGlass (int min)
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
			DateTime start = DateFromIndex (min);
			DateTime end = DateFromIndex (max + 1);
			
			Console.WriteLine ("{0} {1}", start, end);
			query.Range = new PhotoStore.DateRange (start, end);
		}

		public int Count {
			get {
				return years.Count * 12;
			}
		}

		public String Label (int item)
		{
			DateTime start = DateFromIndex (item);
			
			return start.ToShortTimeString ();
		}

		public int Value (int item)
		{
			DateTime start = DateFromIndex (item);
			DateTime end = start.AddMonth (1);
			
			PhotoStore store = query.Store;
			
			Photo [] photos = store.Query (null, new PhotoStore.DateRange (start, end));
			return  photos.Length;
		}
		
		public DateTime DateFromIndex (int item) 
		{
			int year =  (int)years [item / 12];
			int month = (item % 12) + 1;

			return new DateTime (year, month, 1);
		}
		
		public void Load () {
			Photo [] photos = query.Store.Query (null, null);

			if (photos.Length > 0) {
				int last = 0;
				foreach (Photo photo in photos) {
					int current = photo.Time.Year;
					if (current != last) {
						years.Add (current);
						Console.WriteLine ("Found Year {0}", current);
					}

					last = current;
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

using System;

namespace FSpot {
	public class TimeAdaptor {
		public PhotoQuery query;

		private int start_year;
		private int span;

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
				return span * 12;
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
			DateTime end = DateFromIndex (item + 1); 
			
			PhotoStore store = query.Store;
			
			Photo [] photos = store.Query (null, new PhotoStore.DateRange (start, end));
			return  photos.Length;
		}
		
		public DateTime DateFromIndex (int item) 
		{
			int year = start_year + (item / 12);
			int month = (item % 12) + 1;

			return new DateTime (year, month, 1);
		}
		
		public void Load () {
			Photo [] photos = query.Store.Query (null, null);
			
			if (photos.Length > 0) {
				start_year = photos[0].Time.Year;
				span = photos[photos.Length -1].Time.Year - start_year;
			} else {
				start_year = DateTime.Now.Year;
				span = 1;
			}
		}


		public TimeAdaptor (PhotoQuery query) {
			this.query = query;
			
			Load ();
		}
	}
}

using System;

namespace FSpot {
	public class TimeAdaptor {
		PhotoQuery query;

		private int start_year;
		private int span;

		public int Count {
			get {
				return span * 12;
			}
		}

		public String Lable (int item)
		{
			DateTime start = Date (item);
			
			return start.ToShortTimeString ();
		}

		public int Value (int item)
		{
			DateTime start = Date (item);
			DateTime end = Date (item + 1); 
			
			PhotoStore store = query.Store;
			
			Photo [] photos = store.Query (null, new PhotoStore.DateRange (start, end));
			return  photos.Length;
		}
		
		public DateTime Date (int item) 
		{
			int year = start_year + (item / 12);
			int month = (item % 12) + 1;

			return new DateTime (year, month, 1);
		}
		
		public TimeAdaptor (PhotoQuery query, int year, int span) {
			this.query = query;
			this.start_year = year;
			this.span = span;
		}
	}
}

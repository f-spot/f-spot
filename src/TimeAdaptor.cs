using System;

namespace FSpot {
	public class TimeAdaptor {
		PhotoQuery query;

		private int start_year;
		private int span;

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

		public String Lable (int item)
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
		
		public TimeAdaptor (PhotoQuery query, int year, int span) {
			this.query = query;
			this.start_year = year;
			this.span = span;
		}
	}
}

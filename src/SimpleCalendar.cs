namespace FSpot {
	public class SimpleCalendar : Gtk.Calendar {
		private PhotoQuery parent_query;
		private PhotoQuery query;
		
		public SimpleCalendar (PhotoQuery query)
		{
			this.parent_query = query;
			parent_query.Changed += ParentChanged;

			this.query = new PhotoQuery (parent_query.Store);
			this.query.Changed += Changed;

			ParentChanged (parent_query);
			this.Month = System.DateTime.Now;
		}

		private void ParentChanged (IBrowsableCollection query)
		{
			this.query.Tags = ((PhotoQuery)query).Tags;
		}

		private void Changed (IBrowsableCollection query)
		{
			this.ClearMarks ();
			foreach (IBrowsableItem item in query.Items) {
				MarkDay ((uint)item.Time.Day);
			}
		}
			
		public System.DateTime Month
		{
			get {
				uint year;
				uint month;
				uint day;
				GetDate (out year, out month, out day);
				System.Console.WriteLine ("{0}-{1}-{2}", year, month, day);
				return new System.DateTime ((int)year, (int)month + 1, 1);
			}
			set {
				SelectMonth ((uint)value.Month -1, (uint)value.Year);
			}
		}

		protected override void OnMonthChanged ()
		{
			System.DateTime month = this.Month;
			query.Range =  new PhotoStore.DateRange (month, month.AddMonths (1));
			base.OnMonthChanged ();
		}
	}
}

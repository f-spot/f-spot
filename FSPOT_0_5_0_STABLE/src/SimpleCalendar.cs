using FSpot.Query;
#if SHOW_CALENDAR
namespace FSpot {
	public class SimpleCalendar : Gtk.Calendar {
		private PhotoQuery parent_query;
		private PhotoQuery query;
		System.DateTime last;
		
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
			this.query.Terms = ((PhotoQuery)query).Terms;
		}

		private void Changed (IBrowsableCollection query)
		{
			this.ClearMarks ();
			foreach (IBrowsableItem item in query.Items) {
				MarkDay ((uint)item.Time.Day);
			}
		}
			
		new public System.DateTime Month
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
		        System.DateTime current = this.Month;
			if (current.Month != last.Month || current.Year != last.Year) {
				System.Console.WriteLine ("Month thinks is changed {0} {1}", last.ToString (), current.ToString ());
				last = current;
				query.Range =  new DateRange (current, current.AddMonths (1));
			}
			base.OnMonthChanged ();
		}
	}
}
#endif

////
// Author Larry Ewing <lewing@novell.com>
////

using Gtk;
using Gnome;

public class DateCommands {
	public class Set {
		FSpot.PhotoQuery query;
		Gtk.Window parent_window;

		[Glade.Widget]
		private Gtk.Dialog date_range_dialog;

		[Glade.Widget]
		private Button ok_button;

		[Glade.Widget]
		private DateEdit start_dateedit;

		[Glade.Widget] 
		private DateEdit end_dateedit;

		public Set (FSpot.PhotoQuery query, Gtk.Window parent_window)
		{
			this.query = query;
			this.parent_window = parent_window;
		}

		public bool Execute ()
		{
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "date_range_dialog", null);
			xml.Autoconnect (this);
			
			if (query.Range != null) {
				start_dateedit.Time = query.Range.Start;
				end_dateedit.Time = query.Range.End;
			}

			date_range_dialog.DefaultResponse = ResponseType.Ok;
			ResponseType response = (ResponseType) date_range_dialog.Run ();

			bool success = false;

			if (response == ResponseType.Ok) {
				query.Range = new PhotoStore.DateRange (start_dateedit.Time, end_dateedit.Time);
				success = true;
			}
			
			date_range_dialog.Destroy ();
			return success;
		}
	}
}

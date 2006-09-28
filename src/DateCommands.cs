////
// Author Larry Ewing <lewing@novell.com>
////

using Gtk;
using Gnome;

public class DateCommands {
	public class Set : FSpot.GladeDialog {
		FSpot.PhotoQuery query;
		Gtk.Window parent_window;

		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private DateEdit start_dateedit;
		[Glade.Widget] private DateEdit end_dateedit;

		public Set (FSpot.PhotoQuery query, Gtk.Window parent_window)
		{
			this.query = query;
			this.parent_window = parent_window;
		}

		public bool Execute ()
		{
			this.CreateDialog ("date_range_dialog");
			
			if (query.Range != null) {
				start_dateedit.Time = query.Range.Start;
				end_dateedit.Time = query.Range.End;
			}

			Dialog.TransientFor = parent_window;
			Dialog.DefaultResponse = ResponseType.Ok;
			ResponseType response = (ResponseType) this.Dialog.Run ();

			bool success = false;

			if (response == ResponseType.Ok) {
				query.Range = new PhotoStore.DateRange (start_dateedit.Time, end_dateedit.Time.Add(new System.TimeSpan(23,59,59)));
				success = true;
			}
			
			this.Dialog.Destroy ();
			return success;
		}
	}
}

namespace FSpot {
	public class QueryDisplay : Gtk.VBox {
		PhotoQuery query;
		TagView tag_view;
		Gtk.Label label;
		
		public QueryDisplay (PhotoQuery query) {
			this.query = query;
			query.Changed += HandleChanged;

			Gtk.HSeparator sep = new Gtk.HSeparator ();
			sep.Show ();
			this.PackStart (sep, false, false, 0);
			
			Gtk.HBox hbox = new Gtk.HBox ();
			hbox.Show ();
			this.PackStart (hbox, false, false, 0);

			label = new Gtk.Label (Mono.Posix.Catalog.GetString ("Find: "));
			label.Show ();
			hbox.PackStart (label, false, false, 0);
			
			tag_view = new TagView ();
			tag_view.Show ();
			hbox.PackStart (tag_view, false, false, 0);
		}
		
		public void HandleChanged (IBrowsableCollection collection) 
		{
			bool active_search = false;
			
			Tag [] tags = query.Tags;
			tag_view.Tags = tags;
			if (tags != null && tags.Length > 0)
				active_search = true;
			
			this.Visible = active_search;
		}
	}
}

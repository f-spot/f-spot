namespace FSpot {
	public class QueryDisplay : Gtk.VBox {
		PhotoQuery query;
		TagView tag_view;
		Gtk.Label label;
		Gtk.Label untagged;
		Gtk.HBox warning_box;
		Gtk.Button clear_button;
		TagSelectionWidget selector;

		public QueryDisplay (PhotoQuery query, TagSelectionWidget selector) {
			this.query = query;
			query.Changed += HandleChanged;
			this.selector = selector;

			Gtk.HSeparator sep = new Gtk.HSeparator ();
			sep.Show ();
			this.PackStart (sep, false, false, 0);
			
			Gtk.HBox hbox = new Gtk.HBox ();
			hbox.Show ();
			this.PackStart (hbox, false, false, 0);
			
			clear_button = new Gtk.Button ();
			clear_button.Add (new Gtk.Image ("gtk-stop", Gtk.IconSize.Button));
			clear_button.Clicked += HandleClearButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			hbox.PackStart (clear_button, false, false, 0);

			label = new Gtk.Label (Mono.Posix.Catalog.GetString ("Find: "));
			label.Show ();
			hbox.PackStart (label, false, false, 0);
			
			untagged = new Gtk.Label (Mono.Posix.Catalog.GetString ("Untagged photos"));
			untagged.Visible = false;
			hbox.PackStart (untagged, false, false, 0);

			tag_view = new TagView ();
			tag_view.Show ();
			hbox.PackStart (tag_view, false, false, 0);
			
			warning_box = new Gtk.HBox ();
			warning_box.PackStart (new Gtk.Label (""));
			
			Gtk.Image warning_image = new Gtk.Image ("gtk-dialog-warning", Gtk.IconSize.Button);
			warning_image.Show ();
			warning_box.PackStart (warning_image, false, false, 0);
			
			Gtk.Label warning = new Gtk.Label (Mono.Posix.Catalog.GetString ("No matching photos found "));
			warning_box.PackStart (warning, false, false, 0);
			warning_box.ShowAll ();
			warning_box.Spacing = 6;
			warning_box.Visible = false;

			hbox.PackStart (warning_box);				   
		}
		
		public void HandleClearButtonClicked (object sender, System.EventArgs args)
		{
			query.Untagged = false;
			selector.TagSelection = new Tag [] { };
		}

		public void HandleChanged (IBrowsableCollection collection) 
		{
			bool active_search = false;
			
			Tag [] tags = query.Tags;
			tag_view.Tags = tags;
			if ((tags != null && tags.Length > 0) || query.Untagged)
				active_search = true;
			
			this.Visible = active_search;
			untagged.Visible = query.Untagged;
			warning_box.Visible = (query.Count < 1);
		}
	}
}

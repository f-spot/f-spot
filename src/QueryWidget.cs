using FSpot.Query;
using Mono.Posix;

namespace FSpot {
	public class QueryWidget : Gtk.VBox {
		PhotoQuery query;
		LogicWidget logic_widget;
		Gtk.Label label;
		Gtk.Label untagged;
		Gtk.HBox warning_box;
		Gtk.Button clear_button;
		TagSelectionWidget selector;
		Gtk.Tooltips tips = new Gtk.Tooltips ();

		public LogicWidget Logic {
			get {
				return logic_widget;
			}
		}

		public QueryWidget (PhotoQuery query, Db db, TagSelectionWidget selector)
		{
			tips.Enable ();

			this.query = query;
			query.Changed += HandleChanged;
			this.selector = selector;

			Gtk.HSeparator sep = new Gtk.HSeparator ();
			sep.Show ();
			this.PackStart (sep, false, false, 0);
			
			Gtk.HBox hbox = new Gtk.HBox ();
			hbox.Show ();
			this.PackStart (hbox, false, false, 0);
			
			label = new Gtk.Label (Catalog.GetString ("Find: "));
			label.Show ();
			label.Ypad = 9;
			hbox.PackStart (label, false, false, 0);

			untagged = new Gtk.Label (Catalog.GetString ("Untagged photos"));
			untagged.Visible = false;
			hbox.PackStart (untagged, false, false, 0);
			
			logic_widget = new LogicWidget (query, db.Tags, selector);
			logic_widget.Show ();
			hbox.PackStart (logic_widget, true, true, 0);

			warning_box = new Gtk.HBox ();
			warning_box.PackStart (new Gtk.Label (""));
			
			Gtk.Image warning_image = new Gtk.Image ("gtk-info", Gtk.IconSize.Button);
			warning_image.Show ();
			warning_box.PackStart (warning_image, false, false, 0);
			
			clear_button = new Gtk.Button ();
			clear_button.Add (new Gtk.Image ("gtk-close", Gtk.IconSize.Button));
			clear_button.Clicked += HandleClearButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			hbox.PackEnd (clear_button, false, false, 0);
			tips.SetTip (clear_button, Catalog.GetString("Clear search"), null);

			Gtk.Label warning = new Gtk.Label (Catalog.GetString ("No matching photos found"));
			warning_box.PackStart (warning, false, false, 0);
			warning_box.ShowAll ();
			warning_box.Spacing = 6;
			warning_box.Visible = false;

			hbox.PackEnd (warning_box, false, false, 0);
			
			warning_box.Visible = false;
		}
		
		public void HandleClearButtonClicked (object sender, System.EventArgs args)
		{
			query.Untagged = false;
			logic_widget.Clear = true;
			logic_widget.UpdateQuery ();
		}

		public void ClearTags ()
		{
			if (query.Untagged)
				return;

			logic_widget.Clear = true;
			logic_widget.UpdateQuery ();
		}

		public void HandleChanged (IBrowsableCollection collection) 
		{
			if (query.ExtraCondition == null)
				logic_widget.Clear = true;

			if (!logic_widget.Clear || query.Untagged) {
				this.Visible = true;
				Show ();
			} else {
				this.Visible = false;
				Hide ();
			}

			MainWindow.Toplevel.ClearFindByTag.Sensitive = !logic_widget.Clear;

			untagged.Visible = query.Untagged;
			warning_box.Visible = (query.Count < 1);
		}

		public void PhotoTagsChanged (Tag[] tags)
		{
			logic_widget.PhotoTagsChanged (tags);
		}

		public void Include (Tag [] tags)
		{
			logic_widget.Include (tags);
		}
		
		public void UnInclude (Tag [] tags)
		{
			logic_widget.UnInclude (tags);
		}
		
		public void Require (Tag [] tags)
		{
			logic_widget.Require (tags);
		}
		
		public void UnRequire (Tag [] tags)
		{
			logic_widget.UnRequire (tags);
		}
		
		public bool TagIncluded (Tag tag)
		{
			return logic_widget.TagIncluded (tag);
		}
		
		public bool TagRequired (Tag tag)
		{
			return logic_widget.TagRequired (tag);
		}
	}
}

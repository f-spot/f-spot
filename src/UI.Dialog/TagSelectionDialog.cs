using System;
using Gdk;
using Gtk;
using Glade;

namespace FSpot.UI.Dialog {
	public class TagSelectionDialog : GladeDialog 
	{
		[Widget] Gtk.ScrolledWindow tag_selection_scrolled;
		[Widget] Gtk.VBox selection_vbox;
		[Widget] Gtk.Button ok_button;
		[Widget] Gtk.Button cancel_button;
		
		TagSelectionWidget tag_selection_widget;
		
		public TagSelectionDialog (TagStore tags) : base ("tag_selection_dialog")
		{
			tag_selection_widget = new TagSelectionWidget (tags);
			tag_selection_scrolled.Add (tag_selection_widget);
			tag_selection_widget.Show ();
		}
		
		public Tag[] Run ()
		{
			int response = this.Dialog.Run ();
			if ((ResponseType) response == ResponseType.Ok)
				return tag_selection_widget.TagHighlight;
			
			return null;
		}
		
		public void Hide ()
		{
			this.Dialog.Hide ();
		}
	}
}



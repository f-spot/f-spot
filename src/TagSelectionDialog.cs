using System;
using Gdk;
using Gtk;
using Glade;

public class TagSelectionDialog
{
	[Widget] Gtk.Dialog tag_selection_dialog;
	[Widget] Gtk.ScrolledWindow tag_selection_scrolled;
	[Widget] Gtk.VBox selection_vbox;
	[Widget] Gtk.Button ok_button;
	[Widget] Gtk.Button cancel_button;
	
	TagSelectionWidget tag_selection_widget;
	
	public TagSelectionDialog (TagStore tags)
	{
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "tag_selection_dialog", null);
		gui.Autoconnect (this);
		
		tag_selection_widget = new TagSelectionWidget (tags);
		tag_selection_scrolled.Add (tag_selection_widget);
		tag_selection_widget.Show ();
	}
	
	public Tag[] Run ()
	{
		int response = tag_selection_dialog.Run ();
		if ((ResponseType) response == ResponseType.Ok)
			return tag_selection_widget.TagSelection;
		
		return null;
	}
	
	public void Hide ()
	{
		tag_selection_dialog.Hide ();
	}
}



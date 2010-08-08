using System;
using Gdk;
using Gtk;
using Glade;

using FSpot.Core;

namespace FSpot.UI.Dialog {
	public class TagSelectionDialog : BuilderDialog
	{
		[GtkBeans.Builder.Object] Gtk.ScrolledWindow tag_selection_scrolled;

		TagSelectionWidget tag_selection_widget;

		public TagSelectionDialog (TagStore tags) : base ("tag_selection_dialog.ui", "tag_selection_dialog")
		{
			tag_selection_widget = new TagSelectionWidget (tags);
			tag_selection_scrolled.Add (tag_selection_widget);
			tag_selection_widget.Show ();
		}

		public Tag[] Run ()
		{
			int response = base.Run ();
			if ((ResponseType) response == ResponseType.Ok)
				return tag_selection_widget.TagHighlight;

			return null;
		}

		public void Hide ()
		{
			base.Hide ();
		}
	}
}

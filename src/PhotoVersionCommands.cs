using Gtk;
using Glade;
using System;

public class PhotoVersionCommands {

	// Creating a new version.

	public class Create {
		Photo photo;

		[Glade.Widget]
		private Gtk.Dialog create_new_version_dialog;

		[Glade.Widget]
		private Gtk.Button ok_button;

		[Glade.Widget]
		private Gtk.Entry version_name_entry;

		[Glade.Widget]
		private Gtk.Label already_in_use_label;

		private void HandleVersionNameEntryChanged (object obj, EventArgs args)
		{
			if (photo.VersionNameExists (version_name_entry.Text)) {
				already_in_use_label.Markup = "<small>This name is already in use</small>";
				ok_button.Sensitive = false;
				return;
			}

			already_in_use_label.Text = "";
			
			if (version_name_entry.Text.Length == 0)
				ok_button.Sensitive = false;
			else
				ok_button.Sensitive = true;
		}

		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			this.photo = photo;

			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "create_new_version_dialog", null);
			xml.Autoconnect (this);

			ok_button.Sensitive = false;

			// FIXME GTK# bug?  shouldn't need casts from/to int.
			create_new_version_dialog.DefaultResponse = (int) ResponseType.Ok;
			create_new_version_dialog.TransientFor = parent_window;
			ResponseType response = (ResponseType) create_new_version_dialog.Run ();

			if (response == ResponseType.Ok) {
				try {
					photo.CreateVersion (version_name_entry.Text, photo.DefaultVersionId);
					store.Commit (photo);
				} catch {
					// FIXME show error dialog.
					create_new_version_dialog.Destroy ();
					return false;
				}

				create_new_version_dialog.Destroy ();
				return true;
			}

			create_new_version_dialog.Destroy ();
			return false;
		}
	}


	// Deleting a version.

	public class Delete {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			return true;
		}
	}


	// Renaming a version.

	public class Rename {
		public bool RenameVersion (Photo photo, Gtk.Window parent_window)
		{
			return true;
		}
	}
}

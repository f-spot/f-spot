using Gtk;
using Glade;
using System;

public class PhotoVersionCommands {

	private class VersionNameRequest {
		private Photo photo;

		[Glade.Widget]
		private Dialog version_name_dialog;

		[Glade.Widget]
		private Button ok_button;

		[Glade.Widget]
		private Entry version_name_entry;

		[Glade.Widget]
		private Label prompt_label;

		[Glade.Widget]
		private Label already_in_use_label;

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

		public VersionNameRequest (Photo photo, string title, string prompt, Gtk.Window parent_window)
		{
			this.photo = photo;

			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "version_name_dialog", null);
			xml.Autoconnect (this);

			version_name_dialog.Title = title;
			version_name_entry.ActivatesDefault = true;
			prompt_label.Text = prompt;
			version_name_dialog.TransientFor = parent_window;

			// FIXME GTK# bug?  shouldn't need casts from/to int.
			version_name_dialog.DefaultResponse = (int) ResponseType.Ok;

			ok_button.Sensitive = false;
		}

		public ResponseType Run (out string name)
		{
			ResponseType response = (ResponseType) version_name_dialog.Run ();

			name = version_name_entry.Text;
			version_name_dialog.Destroy ();

			return response;
		}
	}

	// Creating a new version.

	public class Create {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			VersionNameRequest request = new VersionNameRequest (photo, "Create New Version", "Name:", parent_window);

			string name;
			ResponseType response = request.Run (out name);

			if (response != ResponseType.Ok)
				return false;

			try {
				photo.DefaultVersionId = photo.CreateVersion (name, photo.DefaultVersionId);
				store.Commit (photo);
			} catch {
				// FIXME show error dialog.
				Console.WriteLine ("error");
				return false;
			}

			return true;
		}
	}


	// Deleting a version.

	public class Delete {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			// FIXME HIG-ify.
			Dialog dialog = new Dialog ();
			dialog.BorderWidth = 6;
			dialog.TransientFor = parent_window;
			dialog.HasSeparator = false;
			dialog.Title = "Really Delete?";
			dialog.AddButton ("Cancel", (int) ResponseType.Cancel);
			dialog.AddButton ("Delete", (int) ResponseType.Ok);
			dialog.DefaultResponse = (int) ResponseType.Ok;

			string version_name = photo.GetVersionName (photo.DefaultVersionId);
			Label label = new Label (String.Format ("Really delete version \"{0}\"?", version_name));
			label.Show ();
			dialog.VBox.PackStart (label, false, true, 6);;

			if (dialog.Run () == (int) ResponseType.Ok) {
				try {
					photo.DeleteVersion (photo.DefaultVersionId);
					store.Commit (photo);
				} catch {
					// FIXME show error dialog.
					dialog.Destroy ();
					Console.WriteLine ("error");
					return false;
				}

				dialog.Destroy ();
				return true;
			}

			dialog.Destroy ();
			return false;
		}
	}


	// Renaming a version.

	public class Rename {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			VersionNameRequest request = new VersionNameRequest (photo,
									     "Rename Version", "New name:",
									     parent_window);

			string new_name;
			ResponseType response = request.Run (out new_name);

			if (response != ResponseType.Ok)
				return false;

			try {
				photo.RenameVersion (photo.DefaultVersionId, new_name);
				store.Commit (photo);
			} catch {
				// FIXME error dialog.
				Console.WriteLine ("error");
				return false;
			}

			return true;
		}
	}
}

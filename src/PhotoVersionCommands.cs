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

		public enum RequestType {
			Create,
			Rename
		}

		private RequestType request_type;

		private void Update ()
		{
			string new_name = version_name_entry.Text;

			if (photo.VersionNameExists (new_name)
			    && ! (request_type == RequestType.Rename
				  && new_name == photo.GetVersionName (photo.DefaultVersionId))) {
				already_in_use_label.Markup = "<small>This name is already in use</small>";
				ok_button.Sensitive = false;
				return;
			}

			already_in_use_label.Text = "";
			
			if (new_name.Length == 0)
				ok_button.Sensitive = false;
			else
				ok_button.Sensitive = true;
		}

		private void HandleVersionNameEntryChanged (object obj, EventArgs args)
		{
			Update ();
		}

		public VersionNameRequest (RequestType request_type, Photo photo, Gtk.Window parent_window)
		{
			this.request_type = request_type;
			this.photo = photo;

			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "version_name_dialog", null);
			xml.Autoconnect (this);

			switch (request_type) {
			case RequestType.Create:
				version_name_dialog.Title = Mono.Posix.Catalog.GetString ("Create New Version");
				prompt_label.Text = Mono.Posix.Catalog.GetString ("Name:");
				break;

			case RequestType.Rename:
				version_name_dialog.Title = Mono.Posix.Catalog.GetString ("Rename Version");
				prompt_label.Text = Mono.Posix.Catalog.GetString ("New name:");
				version_name_entry.Text = photo.GetVersionName (photo.DefaultVersionId);
				version_name_entry.SelectRegion (0, -1);
				break;
			}

			version_name_entry.ActivatesDefault = true;
			version_name_dialog.TransientFor = parent_window;

			version_name_dialog.DefaultResponse = ResponseType.Ok;

			Update ();
		}

		public ResponseType Run (out string name)
		{
			ResponseType response = (ResponseType) version_name_dialog.Run ();

			name = version_name_entry.Text;
			if (request_type == RequestType.Rename && name == photo.GetVersionName (photo.DefaultVersionId))
				response = ResponseType.Cancel;

			version_name_dialog.Destroy ();

			return response;
		}
	}

	// Creating a new version.

	public class Create {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			VersionNameRequest request = new VersionNameRequest (VersionNameRequest.RequestType.Create,
									     photo, parent_window);

			string name;
			ResponseType response = request.Run (out name);

			if (response != ResponseType.Ok)
				return false;

			try {
				photo.DefaultVersionId = photo.CreateVersion (name, photo.DefaultVersionId, true);
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
			dialog.Title = Mono.Posix.Catalog.GetString ("Really Delete?");
			dialog.AddButton ("Cancel", (int) ResponseType.Cancel);
			dialog.AddButton ("Delete", (int) ResponseType.Ok);
			dialog.DefaultResponse = ResponseType.Ok;

			string version_name = photo.GetVersionName (photo.DefaultVersionId);
			Label label = new Label (String.Format (Mono.Posix.Catalog.GetString ("Really delete version \"{0}\"?"), version_name));
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
			VersionNameRequest request = new VersionNameRequest (VersionNameRequest.RequestType.Rename,
									     photo, parent_window);

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

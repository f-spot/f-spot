using Gtk;
using Glade;
using System;
using Mono.Unix;
using FSpot;
using FSpot.UI.Dialog;

public class PhotoVersionCommands
{
	private class VersionNameRequest : GladeDialog {
		private Photo photo;

		[Glade.Widget] private Button ok_button;
		[Glade.Widget] private Entry version_name_entry;
		[Glade.Widget] private Label prompt_label;
		[Glade.Widget] private Label already_in_use_label;

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
				  && new_name == photo.GetVersion (photo.DefaultVersionId).Name)) {
				already_in_use_label.Markup = "<small>This name is already in use</small>";
				ok_button.Sensitive = false;
				return;
			}

			already_in_use_label.Text = String.Empty;
			
			if (new_name.Length == 0)
				ok_button.Sensitive = false;
			else
				ok_button.Sensitive = true;
		}

		private void HandleVersionNameEntryChanged (object obj, EventArgs args)
		{
			Update ();
		}

		public VersionNameRequest (RequestType request_type, Photo photo, Gtk.Window parent_window) : base ("version_name_dialog")
		{
			this.request_type = request_type;
			this.photo = photo;

			switch (request_type) {
			case RequestType.Create:
				this.Dialog.Title = Catalog.GetString ("Create New Version");
				prompt_label.Text = Catalog.GetString ("Name:");
				break;

			case RequestType.Rename:
				this.Dialog.Title = Catalog.GetString ("Rename Version");
				prompt_label.Text = Catalog.GetString ("New name:");
				version_name_entry.Text = photo.GetVersion (photo.DefaultVersionId).Name;
				version_name_entry.SelectRegion (0, -1);
				break;
			}

			version_name_entry.ActivatesDefault = true;

			this.Dialog.TransientFor = parent_window;
			this.Dialog.DefaultResponse = ResponseType.Ok;

			Update ();
		}

		public ResponseType Run (out string name)
		{
			ResponseType response = (ResponseType) this.Dialog.Run ();

			name = version_name_entry.Text;
			if (request_type == RequestType.Rename && name == photo.GetVersion (photo.DefaultVersionId).Name)
				response = ResponseType.Cancel;

			this.Dialog.Destroy ();

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
			} catch (Exception e) {
					string msg = Catalog.GetString ("Could not create a new version");
					string desc = String.Format (Catalog.GetString ("Received exception \"{0}\". Unable to create version \"{1}\""),
								     e.Message, name);
					
					HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent, 
										    Gtk.MessageType.Error, ButtonsType.Ok, 
										    msg,
										    desc);
					md.Run ();
					md.Destroy ();
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
			dialog.Title = Catalog.GetString ("Really Delete?");
			dialog.AddButton (Catalog.GetString ("Cancel"), (int) ResponseType.Cancel);
			dialog.AddButton (Catalog.GetString ("Delete"), (int) ResponseType.Ok);
			dialog.DefaultResponse = ResponseType.Ok;

			string version_name = photo.GetVersion (photo.DefaultVersionId).Name;
			Label label = new Label (String.Format (Catalog.GetString ("Really delete version \"{0}\"?"), version_name));
			label.Show ();
			dialog.VBox.PackStart (label, false, true, 6);;

			if (dialog.Run () == (int) ResponseType.Ok) {
				try {
					photo.DeleteVersion (photo.DefaultVersionId);
					store.Commit (photo);
				} catch (Exception e) {
					// FIXME show error dialog.
					string msg = Catalog.GetString ("Could not delete a version");
					string desc = String.Format (Catalog.GetString ("Received exception \"{0}\". Unable to delete version \"{1}\""),
								     e.Message, photo.Name);
					
					HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent, 
										    Gtk.MessageType.Error, ButtonsType.Ok, 
										    msg,
										    desc);
					md.Run ();
					md.Destroy ();
					dialog.Destroy (); // Delete confirmation window.
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
			} catch (Exception e) {
					string msg = Catalog.GetString ("Could not rename a version");
					string desc = String.Format (Catalog.GetString ("Received exception \"{0}\". Unable to rename version to \"{1}\""),
								     e.Message, new_name);
					
					HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent, 
										    Gtk.MessageType.Error, ButtonsType.Ok, 
										    msg,
										    desc);
					md.Run ();
					md.Destroy ();
					return false;
			}

			return true;
		}
	}

	// Reparenting a photo as version of another one
	public class Reparent {
		public bool Execute (PhotoStore store, Photo [] photos, Photo new_parent, Gtk.Window parent_window)
		{
			foreach (Photo photo in photos) {
				new_parent.AddTag (photo.Tags);
				foreach (uint version_id in photo.VersionIds) {
					try {
						new_parent.DefaultVersionId = new_parent.CreateReparentedVersion (photo.GetVersion (version_id) as PhotoVersion);
						store.Commit (new_parent);
					} catch (Exception e) {
						Console.WriteLine (e);	
					}
				}
				uint [] version_ids = photo.VersionIds;
				Array.Reverse (version_ids);
				foreach (uint version_id in version_ids) {
					try {
						photo.DeleteVersion (version_id, true, true);
					} catch (Exception e) {
						Console.WriteLine(e);
					}
				}
				store.Commit (photo);
				MainWindow.Toplevel.Database.Photos.Remove (photo);
			}
			return true;
		}
	}
}

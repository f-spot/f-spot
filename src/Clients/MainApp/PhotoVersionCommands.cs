using Gtk;
using Glade;
using System;
using Mono.Unix;
using FSpot;
using Hyena;
using Hyena.Widgets;
using FSpot.UI.Dialog;

public class PhotoVersionCommands
{
	private class VersionNameRequest : BuilderDialog {
		private Photo photo;

		[GtkBeans.Builder.Object] private Button ok_button;
		[GtkBeans.Builder.Object] private Entry version_name_entry;
		[GtkBeans.Builder.Object] private Label prompt_label;
		[GtkBeans.Builder.Object] private Label already_in_use_label;

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

		public VersionNameRequest (RequestType request_type, Photo photo, Gtk.Window parent_window) : base ("version_name_dialog.ui", "version_name_dialog")
		{
			this.request_type = request_type;
			this.photo = photo;

			switch (request_type) {
			case RequestType.Create:
				this.Title = Catalog.GetString ("Create New Version");
				prompt_label.Text = Catalog.GetString ("Name:");
				break;

			case RequestType.Rename:
				this.Title = Catalog.GetString ("Rename Version");
				prompt_label.Text = Catalog.GetString ("New name:");
				version_name_entry.Text = photo.GetVersion (photo.DefaultVersionId).Name;
				version_name_entry.SelectRegion (0, -1);
				break;
			}

			version_name_entry.Changed += HandleVersionNameEntryChanged;
			version_name_entry.ActivatesDefault = true;

			this.TransientFor = parent_window;
			this.DefaultResponse = ResponseType.Ok;

			Update ();
		}

		public ResponseType Run (out string name)
		{
			ResponseType response = (ResponseType) this.Run ();

			name = version_name_entry.Text;
			if (request_type == RequestType.Rename && name == photo.GetVersion (photo.DefaultVersionId).Name)
				response = ResponseType.Cancel;

			this.Destroy ();

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
				return true;
			} catch (Exception e) {
				HandleException ("Could not create a new version", e, parent_window);
				return false;
			}
		}
	}


	// Deleting a version.

	public class Delete {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			string ok_caption = Catalog.GetString ("Delete");
			string msg = String.Format (Catalog.GetString ("Really delete version \"{0}\"?"), photo.DefaultVersion.Name);
			string desc = Catalog.GetString ("This removes the version and deletes the corresponding file from disk.");
			try {
				if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(parent_window, DialogFlags.DestroyWithParent,
									   MessageType.Warning, msg, desc, ok_caption)) {
					photo.DeleteVersion (photo.DefaultVersionId);
					store.Commit (photo);
					return true;
				}
			} catch (Exception e) {
				HandleException ("Could not delete a version", e, parent_window);
			}
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
				return true;
			} catch (Exception e) {
				HandleException ("Could not rename a version", e, parent_window);
				return false;
			}
		}
	}

	// Detaching a version (making it a separate photo).

	public class Detach {
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			string ok_caption = Catalog.GetString ("De_tach");
			string msg = String.Format (Catalog.GetString ("Really detach version \"{0}\" from \"{1}\"?"), photo.DefaultVersion.Name, photo.Name.Replace("_", "__"));
			string desc = Catalog.GetString ("This makes the version appear as a separate photo in the library. To undo, drag the new photo back to its parent.");
			try {
				if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(parent_window, DialogFlags.DestroyWithParent,
									   MessageType.Warning, msg, desc, ok_caption)) {
					Photo new_photo = store.CreateFrom (photo, photo.RollId);
					new_photo.CopyAttributesFrom (photo);
					photo.DeleteVersion (photo.DefaultVersionId, false, true);
					store.Commit (new Photo[] {new_photo, photo});
					return true;
				}
			} catch (Exception e) {
				HandleException ("Could not detach a version", e, parent_window);
			}
			return false;
		}
	}

	// Reparenting a photo as version of another one

	public class Reparent {
		public bool Execute (PhotoStore store, Photo [] photos, Photo new_parent, Gtk.Window parent_window)
		{
			string ok_caption = Catalog.GetString ("Re_parent");
			string msg = String.Format (Catalog.GetPluralString ("Really reparent \"{0}\" as version of \"{1}\"?",
			                                                     "Really reparent {2} photos as versions of \"{1}\"?", photos.Length),
			                            new_parent.Name.Replace ("_", "__"), photos[0].Name.Replace ("_", "__"), photos.Length);
			string desc = Catalog.GetString ("This makes the photos appear as a single one in the library. The versions can be detached using the Photo menu.");

			try {
				if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(parent_window, DialogFlags.DestroyWithParent,
									   MessageType.Warning, msg, desc, ok_caption)) {
					uint highest_rating = new_parent.Rating;
					string new_description = new_parent.Description;
					foreach (Photo photo in photos) {
						highest_rating = Math.Max(photo.Rating, highest_rating);
						if (string.IsNullOrEmpty(new_description))
							new_description = photo.Description;
						new_parent.AddTag (photo.Tags);

						foreach (uint version_id in photo.VersionIds) {
							new_parent.DefaultVersionId = new_parent.CreateReparentedVersion (photo.GetVersion (version_id) as PhotoVersion);
							store.Commit (new_parent);
						}
						uint [] version_ids = photo.VersionIds;
						Array.Reverse (version_ids);
						foreach (uint version_id in version_ids) {
							photo.DeleteVersion (version_id, true, true);
						}
						store.Remove (photo);
					}
					new_parent.Rating = highest_rating;
					new_parent.Description = new_description;
					store.Commit (new_parent);
					return true;
				}
			}
			catch (Exception e) {
				HandleException ("Could not reparent photos", e, parent_window);
			}
			return false;
		}
	}

	private static void HandleException (string msg, Exception e, Gtk.Window parent_window) {
		Log.DebugException (e);
		msg = Catalog.GetString (msg);
		string desc = String.Format (Catalog.GetString ("Received exception \"{0}\"."), e.Message);
		HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent,
							    Gtk.MessageType.Error, ButtonsType.Ok, msg, desc);
		md.Run ();
		md.Destroy ();
	}
}

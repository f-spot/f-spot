//
// PhotoVersionCommands.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Anton Keks <anton@azib.net>
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2009-2010 Anton Keks
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Gtk;

using Mono.Unix;

using FSpot;
using FSpot.Database;
using FSpot.UI.Dialog;

using Hyena;
using Hyena.Widgets;

public class PhotoVersionCommands
{
	// Creating a new version.
	public class Create
	{
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			var request = new VersionNameDialog (VersionNameDialog.RequestType.Create, photo, parent_window);

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
	public class Delete
	{
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			string ok_caption = Catalog.GetString ("Delete");
			string msg = string.Format (Catalog.GetString ("Really delete version \"{0}\"?"), photo.DefaultVersion.Name);
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
	public class Rename
	{
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			VersionNameDialog request = new VersionNameDialog (VersionNameDialog.RequestType.Rename,
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
	public class Detach
	{
		public bool Execute (PhotoStore store, Photo photo, Gtk.Window parent_window)
		{
			string ok_caption = Catalog.GetString ("De_tach");
			string msg = string.Format (Catalog.GetString ("Really detach version \"{0}\" from \"{1}\"?"), photo.DefaultVersion.Name, photo.Name.Replace("_", "__"));
			string desc = Catalog.GetString ("This makes the version appear as a separate photo in the library. To undo, drag the new photo back to its parent.");
			try {
				if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(parent_window, DialogFlags.DestroyWithParent,
									   MessageType.Warning, msg, desc, ok_caption)) {
					Photo new_photo = store.CreateFrom (photo, true, photo.RollId);
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
	public class Reparent
	{
		public bool Execute (PhotoStore store, Photo [] photos, Photo new_parent, Gtk.Window parent_window)
		{
			string ok_caption = Catalog.GetString ("Re_parent");
			string msg = string.Format (Catalog.GetPluralString ("Really reparent \"{0}\" as version of \"{1}\"?",
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

	static void HandleException (string msg, Exception e, Gtk.Window parent_window)
	{
		Log.DebugException (e);
		msg = Catalog.GetString (msg);
		string desc = string.Format (Catalog.GetString ("Received exception \"{0}\"."), e.Message);
		HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent,
								Gtk.MessageType.Error, ButtonsType.Ok, msg, desc);
		md.Run ();
		md.Destroy ();
	}
}

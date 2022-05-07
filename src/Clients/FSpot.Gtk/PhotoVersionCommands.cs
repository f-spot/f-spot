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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Database;
using FSpot.Models;
using FSpot.Resources.Lang;
using FSpot.Services;
using FSpot.UI.Dialog;

using Gtk;

using Hyena.Widgets;

namespace FSpot
{
	public class PhotoVersionCommands
	{
		// Creating a new version.
		public class Create
		{
			public bool Execute (PhotoStore store, Photo photo, Window parent_window)
			{
				var request = new VersionNameDialog (VersionNameDialog.RequestType.Create, photo, parent_window);

				var response = request.Run (out var name);

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
			public bool Execute (PhotoStore store, Photo photo, Window parent_window)
			{
				var ok_caption = Strings.Delete;
				var msg = string.Format (Strings.ReallyDeleteVersionXQuestion, photo.DefaultVersion.Name);
				var desc = Strings.ThisRemovesTheVersionAndDeletesTheFileFromDisk;
				try {
					if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation (parent_window, DialogFlags.DestroyWithParent,
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
			public bool Execute (PhotoStore store, Photo photo, Window parent_window)
			{
				using var request = new VersionNameDialog (VersionNameDialog.RequestType.Rename, photo, parent_window);

				var response = request.Run (out var new_name);

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
			public bool Execute (PhotoStore store, Photo photo, Window parent_window)
			{
				var ok_caption = Strings.DetachMnemonic;
				var msg = string.Format (Strings.ReallyDetachVersionXFromY, photo.DefaultVersion.Name, photo.Name.Replace ("_", "__"));
				var desc = Strings.ThisMakesTheVersionAppearAsASeparatePhotoInLibraryToUndoDragNewPhotoBackToParent;
				try {
					if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation (parent_window, DialogFlags.DestroyWithParent,
										   MessageType.Warning, msg, desc, ok_caption)) {
						var new_photo = store.CreateFrom (photo, true, photo.RollId);
						new_photo.CopyAttributesFrom (photo);
						photo.DeleteVersion (photo.DefaultVersionId, false, true);
						store.Commit (new[] { new_photo, photo });
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
			public bool Execute (PhotoStore store, Photo[] photos, Photo new_parent, Window parent_window)
			{
				var ok_caption = Strings.ReparentMnemonic;
				var msg = string.Format (photos.Length <= 1 ? Strings.ReallyReparentXAsVersionOfY : Strings.ReallyReparentZPhotosAsVersionsOfY,
											new_parent.Name.Replace ("_", "__"), photos[0].Name.Replace ("_", "__"), photos.Length);
				var desc = Strings.ThisMakesThePhotosAppearAsASingleOneInLibraryTheVersionsCanBeDetachedUsingThePhotoMenu;

				try {
					if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation (parent_window, DialogFlags.DestroyWithParent,
										   MessageType.Warning, msg, desc, ok_caption)) {
						var highest_rating = new_parent.Rating;
						var new_description = new_parent.Description;
						foreach (var photo in photos) {
							highest_rating = Math.Max (photo.Rating, highest_rating);
							if (string.IsNullOrEmpty (new_description))
								new_description = photo.Description;
							TagService.Instance.Add (new_parent, photo.Tags);

							foreach (var version_id in photo.VersionIds) {
								new_parent.DefaultVersionId = new_parent.CreateReparentedVersion (photo.GetVersion (version_id));
								store.Commit (new_parent);
							}
							var version_ids = photo.VersionIds;
							foreach (var version_id in version_ids.Reverse ()) {
								photo.DeleteVersion (version_id, true, true);
							}
							store.Remove (photo);
						}
						new_parent.Rating = highest_rating;
						new_parent.Description = new_description;
						store.Commit (new_parent);
						return true;
					}
				} catch (Exception e) {
					HandleException ("Could not reparent photos", e, parent_window);
				}
				return false;
			}
		}

		static void HandleException (string msg, Exception e, Window parent_window)
		{
			Logger.Log.Debug (e, "");
			var desc = string.Format (Strings.ReceivedExceptionX, e.Message);
			var md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent,
									MessageType.Error, ButtonsType.Ok, msg, desc);
			md.Run ();
			md.Destroy ();
		}
	}
}
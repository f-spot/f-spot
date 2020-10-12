//
// ThumbnailCommand.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@ximian.com>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2004 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot;
using FSpot.Core;
using FSpot.Thumbnail;
using FSpot.UI.Dialog;

using GLib;

public class ThumbnailCommand
{
	readonly Gtk.Window parent_window;

	public ThumbnailCommand (Gtk.Window parent_window)
	{
		this.parent_window = parent_window;
	}

	public bool Execute (List<Photo> photos)
	{
		ProgressDialog progress_dialog = null;
		var loader = App.Instance.Container.Resolve<IThumbnailLoader> ();
		
		if (photos.Count > 1) {
			progress_dialog = new ProgressDialog (Mono.Unix.Catalog.GetString ("Updating Thumbnails"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Count, parent_window);
		}

		int count = 0;
		foreach (IPhoto photo in photos) {
			if (progress_dialog != null
			    && progress_dialog.Update (string.Format (Mono.Unix.Catalog.GetString ("Updating picture \"{0}\""), photo.Name)))
				break;

			foreach (IPhotoVersion version in photo.Versions) {
				loader.Request (version.Uri, ThumbnailSize.Large, 10);
			}

			count++;
		}

		if (progress_dialog != null)
			progress_dialog.Destroy ();

		return true;
	}
}

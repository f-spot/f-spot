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

using FSpot.Core;
using FSpot.Models;
using FSpot.Resources.Lang;
using FSpot.Thumbnail;
using FSpot.UI.Dialog;

namespace FSpot
{
	public class ThumbnailCommand
	{
		readonly Gtk.Window parent_window;

		public ThumbnailCommand (Gtk.Window parent_window)
		{
			this.parent_window = parent_window;
		}

		public bool Execute (IPhoto[] photos)
		{
			ProgressDialog progress_dialog = null;
			var loader = App.Instance.Container.Resolve<IThumbnailLoader> ();

			if (photos.Length > 1) {
				progress_dialog = new ProgressDialog (Strings.UpdatingThumbnails,
									  ProgressDialog.CancelButtonType.Stop,
									  photos.Length, parent_window);
			}

			var count = 0;
			foreach (var photo in photos) {
				if (progress_dialog != null
					&& progress_dialog.Update (string.Format (Strings.UpdatingPictureX, photo.Name)))
					break;

				foreach (var version in photo.Versions) {
					loader.Request (version.Uri, ThumbnailSize.Large, 10);
				}

				count++;
			}

			progress_dialog?.Destroy ();

			return true;
		}
	}
}
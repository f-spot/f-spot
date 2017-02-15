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

using FSpot;
using FSpot.Core;
using FSpot.Thumbnail;
using FSpot.UI.Dialog;

public class ThumbnailCommand
{
	readonly Gtk.Window parent_window;

	public ThumbnailCommand (Gtk.Window parent_window)
	{
		this.parent_window = parent_window;
	}

	public bool Execute (IPhoto [] photos)
	{
		ProgressDialog progress_dialog = null;
		var loader = App.Instance.Container.Resolve<IThumbnailLoader> ();
		
		if (photos.Length > 1) {
			progress_dialog = new ProgressDialog (Mono.Unix.Catalog.GetString ("Updating Thumbnails"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Length, parent_window);
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

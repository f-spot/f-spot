using System;
using Gtk;
using FSpot;
using FSpot.Utils;
using FSpot.UI.Dialog;

public class ThumbnailCommand {
	
	private Gtk.Window parent_window;

	public ThumbnailCommand (Gtk.Window parent_window)
	{
		this.parent_window = parent_window;
	}

	public bool Execute (IBrowsableItem [] photos)
	{
		ProgressDialog progress_dialog = null;
        var loader = ThumbnailLoader.Default;
		if (photos.Length > 1) {
			progress_dialog = new ProgressDialog (Mono.Unix.Catalog.GetString ("Updating Thumbnails"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Length, parent_window);
		}

		int count = 0;
		foreach (IBrowsableItem photo in photos) {
			if (progress_dialog != null
			    && progress_dialog.Update (String.Format (Mono.Unix.Catalog.GetString ("Updating picture \"{0}\""), photo.Name)))
				break;

			foreach (IBrowsableItemVersion version in photo.Versions) {
				loader.Request (version.Uri, ThumbnailSize.Large, 10);
			}
			
			count++;
		}

		if (progress_dialog != null)
			progress_dialog.Destroy ();

		return true;
	}
}

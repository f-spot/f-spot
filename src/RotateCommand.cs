using System;
using System.IO;
using Gtk;

public class RotateCommand {

	private Gtk.Window parent_window;

	public RotateCommand (Gtk.Window parent_window)
	{
		this.parent_window = parent_window;
	}

	public enum Direction {
		Clockwise,
		Counterclockwise,
	}

	public bool Execute (Direction direction, Photo [] photos)
	{
		ProgressDialog progress_dialog = null;
		
		if (photos.Length > 1) {
			progress_dialog = new ProgressDialog (Mono.Posix.Catalog.GetString ("Rotating pictures"),
							      ProgressDialog.CancelButtonType.Stop,
							      photos.Length, parent_window);
		}

		int count = 0;
		bool has_read_only_selections = false;
		foreach (Photo p in photos) {
			if (progress_dialog != null
			    && progress_dialog.Update (String.Format (Mono.Posix.Catalog.GetString ("Rotating picture \"{0}\""), p.Name)))
				break;

			foreach (uint version_id in p.VersionIds) {
				string original_path = p.GetVersionPath (version_id);
				string temporary_path = original_path + ".tmp";	// FIXME make it unique

				// FIXME exception
				if ((File.GetAttributes(original_path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {

					if (direction == Direction.Clockwise)
						JpegUtils.Transform (original_path, temporary_path, JpegUtils.TransformType.Rotate90);
					else
						JpegUtils.Transform (original_path, temporary_path, JpegUtils.TransformType.Rotate270);

					// FIXME way to do this atomically in .NET?  I think Move() raises an exception
					// if the destination path points to an existing file.
					File.Delete (original_path);
					File.Move (temporary_path, original_path);
					
					FSpot.ThumbnailGenerator.Create (original_path).Dispose ();
				} else {
					has_read_only_selections = true;
				}
			}

			count ++;
		}

		if (progress_dialog != null)
			progress_dialog.Destroy ();
		
		if (has_read_only_selections){ 
				
			MessageDialog md = new MessageDialog (parent_window, 
								DialogFlags.DestroyWithParent,
								MessageType.Error,
								ButtonsType.Close,
								Mono.Posix.Catalog.GetString ("Some images could not be rotated because they are on a read only file system or media such as a CDROM.  Please check the permissions and try again."));
			md.Run();
			md.Destroy();
		}
		
		return true;
	}
}

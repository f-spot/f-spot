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
		ProgressDialog progress_dialog = new ProgressDialog ("Rotating pictures",
								     ProgressDialog.CancelButtonType.Stop,
								     photos.Length, parent_window);

		int count = 0;
		foreach (Photo p in photos) {
			if (progress_dialog.Update (String.Format ("Rotating picture \"{0}\"", p.Name)))
				break;

			foreach (uint version_id in p.VersionIds) {
				string original_path = p.GetVersionPath (version_id);
				string temporary_path = original_path + ".tmp";	// FIXME make it unique

				// FIXME exception

				if (direction == Direction.Clockwise)
					JpegUtils.Transform (original_path, temporary_path, JpegUtils.TransformType.Rotate90);
				else
					JpegUtils.Transform (original_path, temporary_path, JpegUtils.TransformType.Rotate270);

				// FIXME way to do this atomically in .NET?  I think Move() raises an exception
				// if the destination path points to an existing file.
				File.Delete (original_path);
				File.Move (temporary_path, original_path);

				PhotoStore.GenerateThumbnail (original_path);
			}

			count ++;
		}

		progress_dialog.Destroy ();

		return true;
	}
}

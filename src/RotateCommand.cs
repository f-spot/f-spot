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
		int readonly_count = 0;
		foreach (Photo p in photos) {
			if (progress_dialog != null
			    && progress_dialog.Update (String.Format (Mono.Posix.Catalog.GetString ("Rotating picture \"{0}\""), p.Name)))
				break;
			
			foreach (uint version_id in p.VersionIds) {
				string original_path = p.GetVersionPath (version_id);
				string temporary_path = original_path + ".tmp";	// FIXME make it unique
				
				// FIXME exception
				if ((File.GetAttributes(original_path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
					try {
						JpegUtils.Transform (original_path, temporary_path, 
								     direction == Direction.Clockwise ? JpegUtils.TransformType.Rotate90 
								     : JpegUtils.TransformType.Rotate270);
						
						
						// FIXME way to do this atomically in .NET?  I think Move() raises an exception
						// if the destination path points to an existing file.
						File.Delete (original_path);
						File.Move (temporary_path, original_path);
						
						Gdk.Pixbuf thumb = FSpot.ThumbnailGenerator.Create (original_path);
						if (thumb != null)
							thumb.Dispose ();
					} catch (System.Exception e) {
						string longmsg = String.Format (Mono.Posix.Catalog.GetString ("Received exception \"{0}\" while rotating image {1}"),
										e.Message, p.Name);
						
						HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent, 
											    MessageType.Warning, ButtonsType.Ok, 
											    Mono.Posix.Catalog.GetString ("Unknown Error while Rotating Image."),
											    longmsg);
						md.Run ();
						md.Destroy ();
					}
					
				} else {
					readonly_count++;
				}
			}
			
			count ++;
		}
		
		if (progress_dialog != null)
			progress_dialog.Destroy ();
		
		if (readonly_count > 0){ 
			string notice = Mono.Posix.Catalog.GetPluralString ("Unable to rotate image",  "Unable to rotate {0} images",  readonly_count);
			string desc = Mono.Posix.Catalog.GetPluralString ("The image could not be rotated because it is on a read only file system or media such as a CDROM.  Please check the permissions and try again",  
									  "{0} images could not be rotated because they are on a read only file system or media such as a CDROM.  Please check the permissions and try again",  readonly_count);
			
			HigMessageDialog md = new HigMessageDialog (parent_window, 
								    DialogFlags.DestroyWithParent,
								    MessageType.Error,
								    ButtonsType.Close,
								    notice, 
								    desc);
			md.Run();
			md.Destroy();
		}
		
		return true;
	}
}

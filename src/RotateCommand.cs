using System;
using System.IO;
using Gtk;
using FSpot;

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

	private void Transform (Direction direction, string original_path)
	{
		try {
#if false
			string temporary_path = original_path + ".tmp";	// FIXME make it unique
		
			JpegUtils.Transform (original_path, temporary_path, 
					     direction == Direction.Clockwise ? JpegUtils.TransformType.Rotate90 
					     : JpegUtils.TransformType.Rotate270);
			
			
			// FIXME way to do this atomically in .NET?  I think Move() raises an exception
			// if the destination path points to an existing file.
			File.Delete (original_path);
			File.Move (temporary_path, original_path);
#else
			FSpot.ImageFile img = FSpot.ImageFile.Create (original_path);
			FSpot.JpegFile jimg = img as FSpot.JpegFile;
			
			if (jimg != null) {
				PixbufOrientation orientation = direction == Direction.Clockwise
					? PixbufUtils.Rotate90 (img.Orientation)
					: PixbufUtils.Rotate270 (img.Orientation);
				
				jimg.SetOrientation (orientation);
				jimg.SaveMetaData (original_path);
			} else {
				throw new ApplicationException ("Unable to rotate photo type");
			}
#endif					
			Gdk.Pixbuf thumb = FSpot.ThumbnailGenerator.Create (original_path);
			if (thumb != null)
				thumb.Dispose ();
			
		} catch (System.Exception e) {
			System.Console.WriteLine (e.ToString ());
			string longmsg = String.Format (Mono.Posix.Catalog.GetString ("Received error \"{0}\" while attempting to rotate {1}"),
							e.Message, System.IO.Path.GetFileName (original_path));
			
			HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent, 
								    MessageType.Warning, ButtonsType.Ok, 
								    Mono.Posix.Catalog.GetString ("Error while rotating photo."),
								    longmsg);
			md.Run ();
			md.Destroy ();
		}
		
	}
	
	public bool Execute (Direction direction, IBrowsableItem [] items)
	{
		ProgressDialog progress_dialog = null;
		
		if (items.Length > 1) {
			progress_dialog = new ProgressDialog (Mono.Posix.Catalog.GetString ("Rotating photos"),
							      ProgressDialog.CancelButtonType.Stop,
							      items.Length, parent_window);
		}
		
		int count = 0;
		int readonly_count = 0;
		foreach (IBrowsableItem item in items) {
			Photo p = item as Photo;
			if (progress_dialog != null
			    && progress_dialog.Update (String.Format (Mono.Posix.Catalog.GetString ("Rotating photo \"{0}\""), p.Name)))
				break;


			if (p != null) {
				foreach (uint version_id in p.VersionIds) {
					string original_path = p.GetVersionPath (version_id);
					
					if ((File.GetAttributes(original_path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
						Transform (direction, original_path);
					} else {
						readonly_count++;
					}
				}
				count ++;
			} else {
				string original_path = item.DefaultVersionUri.LocalPath;

				if ((File.GetAttributes(original_path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
					Transform (direction, original_path);
				} else {
					readonly_count++;
				}
				count ++;
			}
		}
		
		if (progress_dialog != null)
			progress_dialog.Destroy ();
		
		if (readonly_count > 0){ 
			string notice = Mono.Posix.Catalog.GetPluralString ("Unable to rotate photo",  
									    "Unable to rotate {0} photos",  
									    readonly_count);

			string desc = Mono.Posix.Catalog.GetPluralString ("The photo could not be rotated because it is on a read only file system or media such as a CDROM.  Please check the permissions and try again",  
									  "{0} photos could not be rotated because they are on a read only file system or media such as a CDROM.  Please check the permissions and try again",  readonly_count);
			
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

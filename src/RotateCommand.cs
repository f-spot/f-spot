/*
 * Filters/IFilter.cs
 *
 * Author(s)
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using System.IO;

using Gtk;
using Gdk;

using FSpot;
using FSpot.Png;
using FSpot.UI.Dialog;

using FSpot.Utils;

using Mono.Unix;

namespace FSpot {
	public class RotateException : ApplicationException {
		public string path;
		public bool ReadOnly = false;
		
		public string Path {
			get { return path; }
		}

		public RotateException (string msg, string path) : this (msg, path, false) {}
		
		public RotateException (string msg, string path, bool ro) : base (msg) {
			this.path = path;
			this.ReadOnly = ro;
		}
	}

	public enum RotateDirection {
		Clockwise,
		Counterclockwise,
	}

	public class RotateOperation : IOperation {
		IBrowsableItem item;
		RotateDirection direction;
		bool done;

		public RotateOperation (IBrowsableItem item, RotateDirection direction)
		{
			this.item = item;
			this.direction = direction;
			done = false;
		}

		private static void RotateCoefficients (string original_path, RotateDirection direction)
		{
			string temporary_path = original_path + ".tmp";	// FIXME make it unique
			JpegUtils.Transform (original_path, temporary_path, 
					     direction == RotateDirection.Clockwise ? JpegUtils.TransformType.Rotate90 
					     : JpegUtils.TransformType.Rotate270);
			
			Utils.Unix.Rename (temporary_path, original_path);
		}

		private static void RotateOrientation (string original_path, RotateDirection direction)
		{
			using (FSpot.ImageFile img = FSpot.ImageFile.Create (original_path)) {
				if (img is JpegFile) {
					FSpot.JpegFile jimg = img as FSpot.JpegFile;
					PixbufOrientation orientation = direction == RotateDirection.Clockwise
						? FSpot.Utils.PixbufUtils.Rotate90 (img.Orientation)
						: FSpot.Utils.PixbufUtils.Rotate270 (img.Orientation);
				
					jimg.SetOrientation (orientation);
					jimg.SaveMetaData (original_path);
				} else if (img is PngFile) {
					PngFile png = img as PngFile;
					bool supported = false;

					//FIXME there isn't much png specific here except the check
					//the pixbuf is an accurate representation of the real file
					//by checking the depth.  The check should be abstracted and
					//this code made generic.
					foreach (PngFile.Chunk c in png.Chunks) {
						PngFile.IhdrChunk ihdr = c as PngFile.IhdrChunk;
					
						if (ihdr != null && ihdr.Depth == 8)
							supported = true;
					}

					if (!supported) {
						throw new RotateException (Catalog.GetString ("Unable to rotate this type of photo"), original_path);
					}

					string backup = ImageFile.TempPath (original_path);
					using (Stream stream = File.Open (backup, FileMode.Truncate, FileAccess.Write)) {
						using (Pixbuf pixbuf = img.Load ()) {
							PixbufOrientation fake = (direction == RotateDirection.Clockwise) ? PixbufOrientation.RightTop : PixbufOrientation.LeftBottom;
							using (Pixbuf rotated = FSpot.Utils.PixbufUtils.TransformOrientation (pixbuf, fake)) {
								img.Save (rotated, stream);
							}
						}
					}
					File.Copy (backup, original_path, true);
					File.Delete (backup);
				} else {
					throw new RotateException (Catalog.GetString ("Unable to rotate this type of photo"), original_path);
				}
			}
		}
		       
		private void Rotate (string original_path, RotateDirection dir)
		{
			RotateOrientation (original_path, dir);
		}
		
		public bool Step () {
			string original_path;

			if (done)
				return false;

 			original_path = item.DefaultVersionUri.LocalPath;
 			done = true;

			if ((File.GetAttributes(original_path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
				throw new RotateException (Catalog.GetString ("Unable to rotate readonly file"), original_path, true);
			}

			Rotate (original_path, direction);

			Gdk.Pixbuf thumb = FSpot.ThumbnailGenerator.Create (UriUtils.PathToFileUri (original_path));
			if (thumb != null)
				thumb.Dispose ();
		
			return !done;
		}
	}

	public class RotateMultiple : IOperation {
		RotateDirection direction;
		IBrowsableItem [] items;
		int index;
		RotateOperation op;

		public int Index { 
			get { return index; }
		}

		public IBrowsableItem [] Items {
			get { return items; }
		}

		public RotateMultiple (IBrowsableItem [] items, RotateDirection direction)
		{
			this.direction = direction;
			this.items = items;
			index = 0;
		}
		
		public bool Step ()
		{
			if (index >= items.Length)  
				return false;

			if (op == null) 
				op = new RotateOperation (items [index], direction);
			
			if (op.Step ())
				return true;
			else {
				index++;
				op = null;
			}

			return (index < items.Length);
		}
	}
}

public class RotateCommand {
	private Gtk.Window parent_window;

	public RotateCommand (Gtk.Window parent_window)
	{
		this.parent_window = parent_window;
	}

	public bool Execute (RotateDirection direction, IBrowsableItem [] items)
	{
		ProgressDialog progress_dialog = null;
		
		if (items.Length > 1)
			progress_dialog = new ProgressDialog (Catalog.GetString ("Rotating photos"),
							      ProgressDialog.CancelButtonType.Stop,
							      items.Length, parent_window);
		
	        RotateMultiple op = new RotateMultiple (items, direction);
		int readonly_count = 0;
		bool done = false;
		int index = 0;

		while (!done) {
			if (progress_dialog != null && op.Index != -1 && index < items.Length) 
				if (progress_dialog.Update (String.Format (Catalog.GetString ("Rotating photo \"{0}\""), op.Items [op.Index].Name)))
					break;

			try {
				done = !op.Step ();
			} catch (RotateException re) {
				if (!re.ReadOnly)
					RunGenericError (re, re.Path, re.Message);
				else
					readonly_count++;
			} catch (GLib.GException) {
				readonly_count++;
			} catch (DirectoryNotFoundException e) {
				RunGenericError (e, op.Items [op.Index].DefaultVersionUri.LocalPath, Catalog.GetString ("Directory not found"));
			} catch (FileNotFoundException e) {
				RunGenericError (e, op.Items [op.Index].DefaultVersionUri.LocalPath, Catalog.GetString ("File not found"));
			} catch (Exception e) {
				RunGenericError (e, op.Items [op.Index].DefaultVersionUri.LocalPath);
			}
			index ++;
		}
		
		if (progress_dialog != null)
			progress_dialog.Destroy ();
		
		if (readonly_count > 0)
			RunReadonlyError (readonly_count);
		
		return true;
	}

	private void RunReadonlyError (int readonly_count)
	{
		string notice = Catalog.GetPluralString ("Unable to rotate photo", "Unable to rotate {0} photos", readonly_count);
		string desc = Catalog.GetPluralString (
			"The photo could not be rotated because it is on a read only file system or media such as a CDROM.  Please check the permissions and try again.",
			"{0} photos could not be rotated because they are on a read only file system or media such as a CDROM.  Please check the permissions and try again.",
			readonly_count
		);

		notice = String.Format (notice, readonly_count);
		desc = String.Format (desc, readonly_count);
		
		HigMessageDialog md = new HigMessageDialog (parent_window, 
							    DialogFlags.DestroyWithParent,
							    MessageType.Error,
							    ButtonsType.Close,
							    notice, 
							    desc);
		md.Run();
		md.Destroy();
	}
	
	// FIXME shouldn't need this method, should catch all exceptions explicitly
	// so can present translated error messages.
	private void RunGenericError (System.Exception e, string path)
	{
		RunGenericError (e, path, e.Message);
	}

	private void RunGenericError (System.Exception e, string path, string msg)
	{
		string longmsg = String.Format (Catalog.GetString ("Received error \"{0}\" while attempting to rotate {1}"),
						msg, System.IO.Path.GetFileName (path));

		HigMessageDialog md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent,
							    MessageType.Warning, ButtonsType.Ok,
							    Catalog.GetString ("Error while rotating photo."),
							    longmsg);
		md.Run ();
		md.Destroy ();
	}
}

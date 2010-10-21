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
using FSpot.UI.Dialog;

using Hyena;
using Hyena.Widgets;
using FSpot.Utils;
using FSpot.Core;

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

	public class RotateOperation {
		IPhoto item;
		RotateDirection direction;
		bool done;

		public RotateOperation (IPhoto item, RotateDirection direction)
		{
			this.item = item;
			this.direction = direction;
			done = false;
		}

		private static void RotateOrientation (string original_path, RotateDirection direction)
		{
            try {
                var uri = new SafeUri (original_path);
                using (var metadata = Metadata.Parse (uri)) {
                    metadata.EnsureAvailableTags ();
                    var tag = metadata.ImageTag;
                    var orientation = direction == RotateDirection.Clockwise
                        ? FSpot.Utils.PixbufUtils.Rotate90 (tag.Orientation)
                        : FSpot.Utils.PixbufUtils.Rotate270 (tag.Orientation);

                    tag.Orientation = orientation;
                    var always_sidecar = Preferences.Get<bool> (Preferences.METADATA_ALWAYS_USE_SIDECAR);
                    metadata.SaveSafely (uri, always_sidecar);
                    XdgThumbnailSpec.RemoveThumbnail (uri);
                }
            } catch (Exception e) {
                Log.DebugException (e);
                throw new RotateException (Catalog.GetString ("Unable to rotate this type of photo"), original_path);
            }
        }

        private void Rotate (string original_path, RotateDirection dir)
        {
            RotateOrientation (original_path, dir);
        }

        public bool Step ()
        {
            if (done)
                return false;

            GLib.FileInfo info = GLib.FileFactory.NewForUri (item.DefaultVersion.Uri).QueryInfo ("access::can-write", GLib.FileQueryInfoFlags.None, null);
            if (!info.GetAttributeBoolean("access::can-write")) {
                throw new RotateException (Catalog.GetString ("Unable to rotate readonly file"), item.DefaultVersion.Uri, true);
            }

            Rotate (item.DefaultVersion.Uri, direction);

            done = true;
            return !done;
        }
    }

	public class RotateMultiple {
		RotateDirection direction;
		IPhoto [] items;
		int index;
		RotateOperation op;

		public int Index {
			get { return index; }
		}

		public IPhoto [] Items {
			get { return items; }
		}

		public RotateMultiple (IPhoto [] items, RotateDirection direction)
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

	public bool Execute (RotateDirection direction, IPhoto [] items)
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
				RunGenericError (e, op.Items [op.Index].DefaultVersion.Uri.LocalPath, Catalog.GetString ("Directory not found"));
			} catch (FileNotFoundException e) {
				RunGenericError (e, op.Items [op.Index].DefaultVersion.Uri.LocalPath, Catalog.GetString ("File not found"));
			} catch (Exception e) {
				RunGenericError (e, op.Items [op.Index].DefaultVersion.Uri.LocalPath);
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

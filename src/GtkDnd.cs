// FIXME GTK# stuff that needs to be fixed in the bindings.

using Gtk;
using Gdk;
using System;
using System.Runtime.InteropServices;

public class GtkDnd {

	[DllImport("libgtk-x11-2.0.so")]
	static extern void gtk_drag_dest_set (IntPtr widget, int flags, ref Gtk.TargetList targets, int n_targets, Gdk.DragAction actions);

	public static void SetAsDestination (Widget widget, string [] types)
	{
		Gtk.TargetList target_list = new Gtk.TargetList ();

		int i = 0;
		foreach (string type in types)
			target_list.Add (Atom.Intern (type, false), 0, (uint) i ++);

		gtk_drag_dest_set (widget.Handle, 0 /* GTK_DEST_DEFAULT_MOTION | GDK_DEST_DEFAULT_DROP */,
				   ref target_list, types.Length, Gdk.DragAction.Copy | Gdk.DragAction.Move);
	}
}

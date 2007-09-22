/*
 * Widgets/SaneTreeView.cs: A Gtk# TreeView that doesn't clear your selection
 * if you right click (and in the future when you drag).
 *
 * Author(s)
 *   Gabriel Burt  <gabriel.burt@gmail.com>
 *
 * This is free software. See COPYING for details.
 */
using System;
using Gdk;
using Gtk;

namespace FSpot.Widgets
{
    public class SaneTreeView : TreeView
    {
        public TreePath PathAtPoint(double x, double y) {
            TreePath path_at_pointer = null;
            GetPathAtPos((int) x, (int) y, out path_at_pointer);
            return path_at_pointer;
        }

        // Override the default ButtonPressEvent because it is stupid and
        // returns true, meaning if you want to have another handler for the ButtonPress
        // event you have to Glib.ConnectBefore.  Plus you usually want the default one to
        // run first so it will select/unselect anything necessary before popping up a menu, say.
        protected override bool OnButtonPressEvent (Gdk.EventButton button)
        {
            TreePath path_at_point = PathAtPoint (button.X, button.Y);
            bool selected = (path_at_point == null) ? false : Selection.PathIsSelected (path_at_point);

            if (button.Button != 3 || !selected) {
                base.OnButtonPressEvent (button);
            }

            return false;
        }

        protected override bool OnButtonReleaseEvent (Gdk.EventButton button)
        {
            base.OnButtonReleaseEvent (button);
            return false;
        }

        public SaneTreeView(TreeStore store) : base(store)
        {
            Selection.Mode = SelectionMode.Multiple;
        }
    }
}

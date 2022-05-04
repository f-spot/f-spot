//
// SaneTreeView.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
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

/*
 * A Gtk# TreeView that modifies the selection appropriately when you right
 * click or drag.  Code ported from nautilus, src/file-manager/fm-list-view.c,
 * r13045.
 *
 * If you have multiple rows selected and right click or drag on one of them,
 * in a normal TreeView your selection would change to just the row you
 * clicked/dragged on.
 */

using System;

using Gdk;

using Gtk;

namespace FSpot.Widgets
{
	public class SaneTreeView : TreeView
	{
		protected bool row_selected_on_button_down, ignore_button_release, drag_started;

		protected SaneTreeView (IntPtr raw) : base (raw) { }

		public SaneTreeView (TreeStore store) : base (store)
		{
			Selection.Mode = SelectionMode.Multiple;
		}

		public TreePath PathAtPoint (double x, double y)
		{
			GetPathAtPos ((int)x, (int)y, out var path_at_pointer);
			return path_at_pointer;
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton button)
		{
			bool call_parent = true;
			bool on_expander;
			drag_started = ignore_button_release = false;
			GetPathAtPos ((int)button.X, (int)button.Y, out var path, out var column);

			if (button.Window != BinWindow)
				return false;

			if (path != null) {
				if (button.Type == EventType.TwoButtonPress) {
					ActivateRow (path, Columns[0]);
					base.OnButtonPressEvent (button);
				} else {
					if (button.Button == 3 && Selection.PathIsSelected (path))
						call_parent = false;
					else if ((button.Button == 1 || button.Button == 2) &&
						((button.State & ModifierType.ControlMask) != 0 || (button.State & ModifierType.ShiftMask) == 0)) {
						int expander_size = (int)StyleGetProperty ("expander-size");
						int horizontal_separator = (int)StyleGetProperty ("horizontal-separator");
						// EXPANDER_EXTRA_PADDING from GtkTreeView
						expander_size += 4;
						on_expander = (button.X <= horizontal_separator / 2 + path.Depth * expander_size);
						row_selected_on_button_down = Selection.PathIsSelected (path);
						if (row_selected_on_button_down) {
							call_parent = on_expander;
							ignore_button_release = call_parent;
						} else if ((button.State & ModifierType.ControlMask) != 0) {
							call_parent = false;
							Selection.SelectPath (path);
						} else {
							ignore_button_release = on_expander;
						}
					}

					if (call_parent)
						base.OnButtonPressEvent (button);
					else if (Selection.PathIsSelected (path))
						GrabFocus ();
				}
			} else {
				Selection.UnselectAll ();
				base.OnButtonPressEvent (button);
			}

			return false;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton button)
		{
			if (!drag_started && !ignore_button_release)
				DidNotDrag (button);

			base.OnButtonReleaseEvent (button);
			return false;
		}

		protected override void OnDragBegin (Gdk.DragContext context)
		{
			drag_started = true;
			base.OnDragBegin (context);
		}

		protected void DidNotDrag (Gdk.EventButton button)
		{
			TreePath path = PathAtPoint (button.X, button.Y);

			if (path != null) {
				if ((button.Button == 1 || button.Button == 2)
					&& ((button.State & ModifierType.ControlMask) != 0 ||
					(button.State & ModifierType.ShiftMask) == 0)
					&& row_selected_on_button_down) {
					if (!ButtonEventModifiesSelection (button)) {
						Selection.UnselectAll ();
						Selection.SelectPath (path);
					} else
						Selection.UnselectPath (path);
				}
			}
		}

		protected static bool ButtonEventModifiesSelection (Gdk.EventButton button)
		{
			return (button.State & (ModifierType.ControlMask | ModifierType.ShiftMask)) != 0;
		}
	}
}

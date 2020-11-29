//
// SaneTreeView.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
		protected bool RowSelectedOnButtonDown { get; set; }
		protected bool IgnoreButtonRelease { get; set; }
		protected bool DragStarted { get; set; }

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

		protected override bool OnButtonPressEvent (EventButton button)
		{
			bool call_parent = true;
			bool on_expander;
			DragStarted = IgnoreButtonRelease = false;
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
						RowSelectedOnButtonDown = Selection.PathIsSelected (path);
						if (RowSelectedOnButtonDown) {
							call_parent = on_expander;
							IgnoreButtonRelease = call_parent;
						} else if ((button.State & ModifierType.ControlMask) != 0) {
							call_parent = false;
							Selection.SelectPath (path);
						} else {
							IgnoreButtonRelease = on_expander;
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

		protected override bool OnButtonReleaseEvent (EventButton button)
		{
			if (!DragStarted && !IgnoreButtonRelease)
				DidNotDrag (button);

			base.OnButtonReleaseEvent (button);
			return false;
		}

		protected override void OnDragBegin (DragContext context)
		{
			DragStarted = true;
			base.OnDragBegin (context);
		}

		protected void DidNotDrag (EventButton button)
		{
			using var path = PathAtPoint (button.X, button.Y);

			if (path != null) {
				if ((button.Button == 1 || button.Button == 2)
					&& ((button.State & ModifierType.ControlMask) != 0 ||
					(button.State & ModifierType.ShiftMask) == 0)
					&& RowSelectedOnButtonDown) {
					if (!ButtonEventModifiesSelection (button)) {
						Selection.UnselectAll ();
						Selection.SelectPath (path);
					} else
						Selection.UnselectPath (path);
				}
			}
		}

		protected static bool ButtonEventModifiesSelection (EventButton button)
		{
			return (button.State & (ModifierType.ControlMask | ModifierType.ShiftMask)) != 0;
		}
	}
}

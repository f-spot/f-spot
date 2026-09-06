//
// ComplexMenuItem.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Widgets
{
	public class ComplexMenuItem : MenuItem
	{
		bool is_selected = false;

		public ComplexMenuItem () : base ()
		{
		}

		protected ComplexMenuItem (IntPtr raw) : base (raw)
		{
		}

		// Override OnAdded and OnRemoved so we can work with Gtk.Action/Gtk.UIManager
		// which otherwise would try to replace our child with a Label.
		bool first_add = true;
		protected override void OnAdded (Widget widget)
		{
			if (first_add) {
				first_add = false;
				base.OnAdded (widget);
			}
		}

		protected override void OnRemoved (Widget widget)
		{
		}

		protected void ConnectChildExpose (Widget widget)
		{
			widget.ExposeEvent += OnChildExposeEvent;
		}

		[GLib.ConnectBefore]
		void OnChildExposeEvent (object o, ExposeEventArgs args)
		{
			// NOTE: This is a little insane, but it allows packing of EventBox based widgets
			// into a GtkMenuItem without breaking the theme (leaving an unstyled void in the item).
			// This method is called before the EventBox child does its drawing and the background
			// is filled in with the proper style.

			int x, y, width, height;
			var widget = (Widget)o;

			if (IsSelected) {
				x = Allocation.X - widget.Allocation.X;
				y = Allocation.Y - widget.Allocation.Y;
				width = Allocation.Width;
				height = Allocation.Height;

				var shadow_type = (ShadowType)StyleGetProperty ("selected-shadow-type");
				Gtk.Style.PaintBox (Style, widget.GdkWindow, StateType.Prelight, shadow_type,
					args.Event.Area, widget, "menuitem", x, y, width, height);
			} else {
				// Fill only the visible area in solid color, to be most efficient
				widget.GdkWindow.DrawRectangle (Parent.Style.BackgroundGC (StateType.Normal),
					true, 0, 0, widget.Allocation.Width, widget.Allocation.Height);

				// FIXME: The above should not be necessary, but Clearlooks-based themes apparently
				// don't provide any style for the menu background so we have to fill it first with
				// the correct theme color. Weak.
				//
				// Do a complete style paint based on the size of the entire menu to be compatible with
				// themes that provide a real style for "menu"
				x = Parent.Allocation.X - widget.Allocation.X;
				y = Parent.Allocation.Y - widget.Allocation.Y;
				width = Parent.Allocation.Width;
				height = Parent.Allocation.Height;

				Gtk.Style.PaintBox (Style, widget.GdkWindow, StateType.Normal, ShadowType.Out,
					args.Event.Area, widget, "menu", x, y, width, height);
			}
		}

		protected override void OnSelected ()
		{
			base.OnSelected ();
			is_selected = true;
		}

		protected override void OnDeselected ()
		{
			base.OnDeselected ();
			is_selected = false;
		}

		protected override void OnParentSet (Widget previous_parent)
		{
			if (previous_parent != null) {
				previous_parent.KeyPressEvent -= OnKeyPressEventProxy;
			}

			if (Parent != null) {
				Parent.KeyPressEvent += OnKeyPressEventProxy;
			}
		}

		[GLib.ConnectBefore]
		void OnKeyPressEventProxy (object o, KeyPressEventArgs args)
		{
			if (!IsSelected) {
				return;
			}

			switch (args.Event.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.Down:
			case Gdk.Key.Escape:
				return;
			}

			args.RetVal = OnKeyPressEvent (args.Event);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			return false;
		}

		protected bool IsSelected {
			get { return is_selected; }
		}
	}
}

//
// RatingMenuItem.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Gtk;

namespace Hyena.Widgets
{
	public class RatingMenuItem : ComplexMenuItem
	{
		RatingEntry entry;
		bool can_activate = true;
		Box box;

		protected RatingMenuItem (RatingEntry entry) : base ()
		{
			box = new HBox ();
			box.Spacing = 5;

			var label = new Label ();
			label.Markup = $"<i>{GLib.Markup.EscapeText (Strings.RatingColon)}</i>";
			box.PackStart (label, false, false, 0);
			label.Show ();

			this.entry = entry;
			entry.HasFrame = false;
			entry.PreviewOnHover = true;
			entry.AlwaysShowEmptyStars = true;
			entry.Changed += OnEntryChanged;
			box.PackStart (entry, false, false, 0);

			box.ShowAll ();
			Add (box);
		}

		public RatingMenuItem () : this (new RatingEntry ())
		{
		}

		protected RatingMenuItem (IntPtr raw) : base (raw)
		{
		}

		int TransformX (double inx)
		{
			int x = (int)inx - entry.Allocation.X;

			if (x < 0) {
				x = 0;
			} else if (x > entry.Allocation.Width) {
				x = entry.Allocation.Width;
			}

			return x;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.X == 0 && evnt.Y == 0) {
				return false;
			}
			entry.SetValueFromPosition (TransformX (evnt.X));
			return true;
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			return entry.HandleMotionNotify (evnt.State, TransformX (evnt.X));
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			return entry.HandleLeaveNotify (evnt);
		}

		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			return entry.HandleScroll (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			return entry.HandleKeyPress (evnt);
		}

		void OnEntryChanged (object o, EventArgs args)
		{
			if (can_activate) {
				Activate ();
			}
		}

		public void Reset (int value)
		{
			can_activate = false;
			Value = value;
			entry.ClearHover ();
			can_activate = true;
		}

		public int Value {
			get { return entry.Value; }
			set { entry.Value = value; }
		}

		public RatingEntry RatingEntry {
			get { return entry; }
		}
	}
}

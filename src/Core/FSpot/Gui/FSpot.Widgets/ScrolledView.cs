//
// ScrolledView.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using FSpot.Utils;

namespace FSpot.Widgets
{
	public class ScrolledView : Fixed
	{
		readonly DelayedOperation hide;
		public EventBox ControlBox { get; private set; }
		public ScrolledWindow ScrolledWindow { get; private set; }

		public ScrolledView (IntPtr raw) : base (raw) { }

		public ScrolledView () : base ()
		{
			ScrolledWindow = new ScrolledWindow (null, null);
			Put (ScrolledWindow, 0, 0);
			ScrolledWindow.Show ();

			//ebox = new BlendBox ();
			ControlBox = new EventBox ();
			Put (ControlBox, 0, 0);
			ControlBox.ShowAll ();

			hide = new DelayedOperation (2000, new GLib.IdleHandler (HideControls));
			Destroyed += HandleDestroyed;
		}

		public bool HideControls ()
		{
			return HideControls (false);
		}

		public bool HideControls (bool force)
		{
			if (!force && IsRealized) {
				ControlBox.GdkWindow.GetPointer (out var x, out var y, out var _);
				if (x < ControlBox.Allocation.Width && y < ControlBox.Allocation.Height) {
					hide.Start ();
					return true;
				}
			}

			hide.Stop ();
			ControlBox.Hide ();
			return false;
		}

		public void ShowControls ()
		{
			hide.Stop ();
			hide.Start ();
			ControlBox.Show ();
		}

		void HandleDestroyed (object sender, System.EventArgs args)
		{
			hide.Stop ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			ScrolledWindow.SetSizeRequest (allocation.Width, allocation.Height);
			base.OnSizeAllocated (allocation);
		}
	}
}

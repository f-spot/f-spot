//
// ScrolledView.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
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

using System;

using FSpot.Utils;

using Gtk;

namespace FSpot.Widgets {
	public class ScrolledView : Fixed {
		private DelayedOperation hide;
		public EventBox ControlBox { get; private set; }
		public ScrolledWindow ScrolledWindow { get; private set; }

		public ScrolledView (IntPtr raw) : base (raw) {}

		public ScrolledView ()
		{
			ScrolledWindow = new ScrolledWindow  (null, null);
			this.Put (ScrolledWindow, 0, 0);
			ScrolledWindow.Show ();
			
			//ebox = new BlendBox ();
			ControlBox = new EventBox ();
			this.Put (ControlBox, 0, 0);
			ControlBox.ShowAll ();
			
			hide = new DelayedOperation (2000, new GLib.IdleHandler (HideControls));
			this.Destroyed += HandleDestroyed;
		}

		public bool HideControls ()
		{
			return HideControls (false);
		}

		public bool HideControls (bool force)
		{
			int x, y;
			Gdk.ModifierType type;

			if (!force && IsRealized) {
				ControlBox.GdkWindow.GetPointer (out x, out y, out type);
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

		private void HandleDestroyed (object sender, System.EventArgs args)
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

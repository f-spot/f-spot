using System;
using Gtk;
using FSpot.Utils;

namespace FSpot.Widgets {
	public class ScrolledView : Fixed {
		private EventBox ebox;
		private ScrolledWindow scroll;
		private DelayedOperation hide;

		public ScrolledView (IntPtr raw) : base (raw) {}

		public ScrolledView () : base () {
			scroll = new ScrolledWindow  (null, null);
			this.Put (scroll, 0, 0);
			scroll.Show ();
			
			//ebox = new BlendBox ();
			ebox = new EventBox ();
			this.Put (ebox, 0, 0);
			ebox.ShowAll ();
			
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
				ebox.GdkWindow.GetPointer (out x, out y, out type);
				if (x < ebox.Allocation.Width && y < ebox.Allocation.Height) {
					hide.Start ();
					return true;
				}
			}

			hide.Stop ();
			ebox.Hide ();
			return false;
		}
		
		public void ShowControls ()
		{
			hide.Stop ();
			hide.Start ();
			ebox.Show ();
		}

		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			hide.Stop ();
		}

		public EventBox ControlBox {
			get {
				return ebox;
			}
		}
		public ScrolledWindow ScrolledWindow {
			get {
				return scroll;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			scroll.SetSizeRequest (allocation.Width, allocation.Height);
			base.OnSizeAllocated (allocation);
		}
	}
}

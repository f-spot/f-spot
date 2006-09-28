using System;
using Gtk;

namespace FSpot.Widgets {
	public class ScrolledView : Fixed {
		private EventBox ebox;
		private ScrolledWindow scroll;
		private Delay hide;

		public ScrolledView (IntPtr raw) : base (raw) {}

		public ScrolledView () : base () {
			scroll = new ScrolledWindow  (null, null);
			this.Put (scroll, 0, 0);
			scroll.Show ();
			
			ebox = new BlendBox ();
			this.Put (ebox, 0, 0);
			ebox.ShowAll ();
			
			hide = new Delay (2000, new GLib.IdleHandler (HideControls));
			this.Destroyed += HandleDestroyed;
		}

		public bool HideControls ()
		{
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

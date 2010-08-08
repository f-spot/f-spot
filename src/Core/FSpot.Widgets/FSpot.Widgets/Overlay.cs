using System;
using Gtk;


namespace FSpot.Widgets {
	public class Overlay : Window {
		private bool compositing;

		public Overlay () : base ("F-Spot Overlay") {
			

		}

		protected override void OnRealized ()
		{
			compositing = CompositeUtils.SetRgbaColormap (this);
		}
	}
}

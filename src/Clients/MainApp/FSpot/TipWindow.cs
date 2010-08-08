using Gtk;
using Gdk;

namespace FSpot {
	internal class TipWindow : Gtk.Window {
		public TipWindow () : base (Gtk.WindowType.Popup)
		{
			Name = "gtk-tooltips";
			AppPaintable = true;
			BorderWidth = 4;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Gtk.Style.PaintFlatBox (Style, GdkWindow, State, ShadowType.Out,
						args.Area, this, "tooltip", Allocation.X, Allocation.Y,
						Allocation.Width, Allocation.Height);

			return base.OnExposeEvent (args);
		}
	}
}

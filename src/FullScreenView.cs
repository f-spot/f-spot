namespace FSpot {
	public class FullScreenView : Gtk.Window {
		public FullScreenView (PhotoQuery query) : base ("Full Screen Mode")
		{
			try {
				this.Fullscreen ();
				scroll = new Gtk.ScrolledWindow (null, null);
				this.Add (scroll);
				view = new PhotoImageView (query);
				scroll.Add (view);
				scroll.ShowAll ();
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}		      
		}

		private Gtk.ScrolledWindow scroll;
		private PhotoImageView view;
		public PhotoImageView View {
			get {
				return view;
			}
		}
		
		private Gtk.Button forward_button;
		private Gtk.Button back_button;
		
		protected override bool OnKeyPressEvent (Gdk.EventKey key)
		{
			if (key == null) {
				System.Console.WriteLine ("Key == null", key);
				return false;
			}

			if (view == null) {
				System.Console.WriteLine ("view == null", key);
				return false;
			}

			switch (key.Key) {
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
				view.Prev ();
				break;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
				view.Next ();
				break;
			case Gdk.Key.F:
			case Gdk.Key.f:
				view.ZoomFit();
				break;
			default:
				bool retval = base.OnKeyPressEvent (key);
				if (!retval)
					this.Destroy ();
				break;
			}
			return true;
		}
	}
}

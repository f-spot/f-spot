namespace FSpot {
	public class FullScreenView : Gtk.Window {
		public FullScreenView (PhotoQuery query) : base ("Full Screen Mode")
		{
			try {
				scroll = new Gtk.ScrolledWindow (null, null);
				view = new PhotoImageView (query);
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);

				this.Add (scroll);
				scroll.Add (view);
				scroll.ShowAll ();
				this.Decorated = false;
				this.Fullscreen ();
				this.ButtonPressEvent += HandleButtonPressEvent;
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
		
		private void HandleButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Type == Gdk.EventType.ButtonPress
			    && args.Event.Button == 3) {
				PhotoPopup popup = new PhotoPopup ();
				popup.Activate (args.Event);
			}
		}

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

			bool retval = base.OnKeyPressEvent (key);
			if (!retval)
				this.Destroy ();
			else 
				view.Fit = false;
			return retval;
		}
	}
}

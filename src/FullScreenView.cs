namespace FSpot {
	public class FullScreenView : Gtk.Window {
		public FullScreenView (PhotoQuery query) : base ("Full Screen Mode")
		{
			try {
				scroll = new Gtk.ScrolledWindow (null, null);
				view = new PhotoImageView (query);
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);


		Cms.Profile srgb = Cms.Profile.CreateSRgb ();
		Cms.Profile bchsw = Cms.Profile.CreateAbstract (10, 1.0, 1.0,
								0, 0, 9000, 
								4000);

		Cms.Profile [] list = new Cms.Profile [] { srgb, bchsw, srgb };
		Cms.Transform trans = new Cms.Transform (list, 
							 Cms.Format.Rgb8,
							 Cms.Format.Rgb8,
							 Cms.Intent.Perceptual, 0x0000);

		view.Transform = trans;
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

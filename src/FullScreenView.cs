using Gtk;
namespace FSpot {
	public class ScrolledView : Gtk.Fixed {
		private Gtk.EventBox ebox;
		private Gtk.ScrolledWindow scroll;
		private Delay hide;

		public ScrolledView (System.IntPtr raw) : base (raw) {}

		public ScrolledView () : base () {
			scroll = new Gtk.ScrolledWindow  (null, null);
			this.Put (scroll, 0, 0);
			scroll.Show ();
			
			ebox = new Gtk.EventBox ();
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

		public Gtk.EventBox ControlBox {
			get {
				return ebox;
			}
		}
		public Gtk.ScrolledWindow ScrolledWindow {
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

	public class FullScreenView : Gtk.Window {
		private ScrolledView scroll;
		private PhotoImageView view;
		private Gtk.Button forward_button;
		private Gtk.Button back_button;
		
		public FullScreenView (IBrowsableCollection collection) : base ("Full Screen Mode")
		{
			try {
				//scroll = new Gtk.ScrolledWindow (null, null);
				
				scroll = new ScrolledView ();
				view = new PhotoImageView (collection);
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				view.PointerMode = ImageView.PointerModeType.Scroll;
				this.Add (scroll);
				view.Show ();
				view.MotionNotifyEvent += HandleViewMotion;

				scroll.ScrolledWindow.Add (view);

				Gtk.Button close = ExitButton ();
				scroll.ControlBox.Add (close);
				close.Clicked += HandleExitClicked;
				close.Show ();
				scroll.ShowControls ();
				
				scroll.Show ();
				this.Decorated = false;
				this.Fullscreen ();
				this.ButtonPressEvent += HandleButtonPressEvent;
				
				view.GrabFocus ();
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}		      
		}

		private Gtk.Button ExitButton ()
		{
			Gtk.HBox hbox = new Gtk.HBox ();
			hbox.PackStart (new Gtk.Image (Gtk.Stock.Quit, Gtk.IconSize.Button));
			hbox.PackStart (new Gtk.Label (Mono.Posix.Catalog.GetString ("Exit fullscreen")));
			hbox.ShowAll ();
			return new Gtk.Button (hbox);
		}

		private void HandleExitClicked (object sender, System.EventArgs args)
		{
			this.Destroy ();
		}

		[GLib.ConnectBefore]
		private void HandleViewMotion (object sender, Gtk.MotionNotifyEventArgs args)
		{
			int x, y;
			Gdk.ModifierType type;
			view.GdkWindow.GetPointer (out x, out y, out type);
			scroll.ShowControls ();
		}

		public PhotoImageView View {
			get {
				return view;
			}
		}

		private void HandleButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Type == Gdk.EventType.ButtonPress
			    && args.Event.Button == 3) {
				PhotoPopup popup = new PhotoPopup ();
				popup.Activate (this.Toplevel, args.Event);
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

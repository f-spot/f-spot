using Gtk;
using FSpot.Widgets;
using Cairo;

namespace FSpot {
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
				HBox hhbox = new HBox ();
				Gtk.Button close = ExitButton ();
				hhbox.PackStart (close);
				//hhbox.PackStart (new Gtk.Label ("This is a test"));
				scroll.ControlBox.Add (hhbox);
				hhbox.ShowAll ();
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

#if false
		protected override void OnRealized ()
		{
			CompositeUtils.SetRgbaColormap (this);
			base.OnRealized ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			bool ret = base.OnExposeEvent (args);
			Graphics g = CairoUtils.CreateDrawable (GdkWindow);

			g.Color = new Cairo.Color (0, 0, 0, .5);
			g.Operator = Operator.DestOut;
			g.Rectangle (0, 0, Allocation.Width  * .5, Allocation.Height);
			g.Paint ();

			return ret;
		}
#endif

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

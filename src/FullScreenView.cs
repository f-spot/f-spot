using System;
using Gtk;
using Gdk;
using FSpot.Widgets;
using Cairo;
using Mono.Unix;

namespace FSpot {
	public class FadeIn {
		bool composited;
		Delay fade_delay;
		TimeSpan duration;
		DateTime start;
		Gtk.Window win;
		
		public FadeIn (Gtk.Window win, int sec)
		{
			this.win = win;
			win.Mapped += HandleMapped;
			win.Unmapped += HandleUnmapped;
			win.ExposeEvent += HandleExposeEvent;
			duration = new TimeSpan (0, 0, sec);
		}
		
		[GLib.ConnectBefore]
		public void HandleMapped (object sender, EventArgs args)
		{
			composited = CompositeUtils.SupportsHint (win.Screen, "_NET_WM_WINDOW_OPACITY");
			if (!composited)
				return;
			
			CompositeUtils.SetWinOpacity (win, 0.0);
		}
		
		public void HandleExposeEvent (object sender, ExposeEventArgs args)
		{
			if (fade_delay == null) {
				fade_delay = new Delay (50, new GLib.IdleHandler (Update));
					start = DateTime.Now;
					fade_delay.Start ();
			}
		}
		
		public void HandleUnmapped (object sender, EventArgs args)
		{
			if (fade_delay != null)
				fade_delay.Stop ();
		}
		
		public bool Update ()
		{
			double percent = Math.Min ((DateTime.Now - start).Ticks / (double) duration.Ticks, 1.0);
			double opacity = Math.Sin (percent * Math.PI * 0.2);
			CompositeUtils.SetWinOpacity (win, percent);
			
			bool stop = percent >= 1.0;

			if (stop)
				fade_delay.Stop ();
			
			return !stop;
		}
	}			
	
	[Binding(Gdk.Key.Escape, "Quit")]
#if ENABLE_CRACK
	[Binding(Gdk.Key.D, "PlayPause")]
#endif
	public class FullScreenView : Gtk.Window {
		private ScrolledView scroll;
		private PhotoImageView view;
		private TagView tag_view;
		private Notebook notebook;
		private ImageDisplay display;
		
		ActionGroup actions;
		const string ExitFullScreen = "ExitFullScreen";
		const string NextPicture = "NextPicture";
		const string PreviousPicture = "PreviousPicture";

		public FullScreenView (IBrowsableCollection collection) : base ("Full Screen Mode")
		{
			try {
				//scroll = new Gtk.ScrolledWindow (null, null);
				actions = new ActionGroup ("joe");
				
				actions.Add (new ActionEntry [] {
					new ActionEntry (ExitFullScreen, Stock.Quit, Catalog.GetString ("Exit fullscreen"), null, null, new System.EventHandler (ExitAction)),
					new ActionEntry (NextPicture, Stock.GoForward, Catalog.GetString ("Next"), null, Catalog.GetString ("Next Picture"), new System.EventHandler (NextAction)),
					new ActionEntry (PreviousPicture, Stock.GoBack, Catalog.GetString ("Back"), null, Catalog.GetString ("Previous Picture"), new System.EventHandler (PreviousAction))
				});

				new FadeIn (this, 3);
				notebook = new Notebook ();
				notebook.ShowBorder = false;
				notebook.ShowTabs = false;
				notebook.Show ();

				scroll = new ScrolledView ();
				view = new PhotoImageView (collection);
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				view.PointerMode = ImageView.PointerModeType.Scroll;
				notebook.AppendPage (scroll, null);
				this.Add (notebook);
				view.Show ();
				view.MotionNotifyEvent += HandleViewMotion;

				scroll.ScrolledWindow.Add (view);
				HBox hhbox = new HBox ();
				Gtk.Button close = ExitButton ();
				hhbox.PackStart (close);
				hhbox.PackStart (GetButton ("PreviousPicture"));
				hhbox.PackStart (GetButton ("NextPicture"));

				tag_view = new TagView ();
				tag_view.Show ();
				hhbox.PackStart (tag_view);
				//hhbox.PackStart (new Gtk.Label ("This is a test"));
				scroll.ControlBox.Add (hhbox);
				hhbox.ShowAll ();
				close.Clicked += ExitAction;
				close.Show ();
				//scroll.ShowControls ();
				
				scroll.Show ();
				this.Decorated = false;
				this.Fullscreen ();
				this.ButtonPressEvent += HandleButtonPressEvent;
				
				view.Item.Changed += HandleItemChanged;
				view.GrabFocus ();
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}		      
		}

		private void HandleItemChanged (object sender, BrowsablePointerChangedArgs args)
		{
			tag_view.Current = view.Item.Current;
			actions [NextPicture].Sensitive = view.Item.Index < view.Item.Collection.Count -1;
			actions [PreviousPicture].Sensitive = view.Item.Index > 0;
			if (scroll.ControlBox.Visible)
				scroll.ShowControls ();
		}
#if false
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

		private Button GetButton (string name)
		{
			Action action = actions [name];
			Widget w = action.CreateIcon (IconSize.Button);
			Button button = new Button ();
			button.Add (w);
			w.Show ();

			action.ConnectProxy (button);
			return button;
		}
			
		private void ExitAction (object sender, System.EventArgs args)
		{
			this.Destroy ();
		}

		private void NextAction (object sender, System.EventArgs args)
		{
			view.Item.MoveNext ();
		}

		private void PreviousAction (object sender, System.EventArgs args)
		{
			view.Item.MovePrevious ();
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

		public bool PlayPause ()
		{
			if (display == null) {
				display = new ImageDisplay (view.Item);
				notebook.AppendPage (display, null);
				display.Show ();
			}
			if (notebook.CurrentPage == 0)
				notebook.CurrentPage = 1;
			else
				notebook.CurrentPage = 0;
			return true;
		}

		public void Quit ()
		{
			this.Destroy ();
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
				Quit ();
			else 
				view.Fit = false;
			return retval;
		}
	}
}

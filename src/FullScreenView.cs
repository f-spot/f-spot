using System;
using Gtk;
using Gdk;
using FSpot.Widgets;
using Cairo;
using Mono.Unix;

namespace FSpot {
	[Binding(Gdk.Key.Escape, "Quit")]
	public class FullScreenView : Gtk.Window {
		private ScrolledView scroll;
		private PhotoImageView view;
		private TagView tag_view;
		private Notebook notebook;
		private ControlOverlay controls;
		//		private ImageDisplay display;
		private TextureDisplay display;

		ActionGroup actions;
		const string ExitFullScreen = "ExitFullScreen";
		const string HideToolbar = "HideToolbar";
		const string SlideShow = "SlideShow";
		const string Info = "Info";
		
		public FullScreenView (IBrowsableCollection collection) : base ("Full Screen Mode")
		{
			Name = "FullscreenContainer";
			try {
				//scroll = new Gtk.ScrolledWindow (null, null);
				actions = new ActionGroup ("joe");
				
				actions.Add (new ActionEntry [] {
					new ActionEntry (HideToolbar, Stock.Close, 
							 Catalog.GetString ("Hide"), 
							 null, 
							 Catalog.GetString ("Hide Toolbar"), 
							 HideToolbarAction),
					new ActionEntry (ExitFullScreen, 
							 "f-spot-view-restore", 
							 Catalog.GetString ("Exit fullscreen"), 
							 null, 
							 null, 
							 ExitAction),
					new ActionEntry (SlideShow,
							 "f-spot-slideshow",
							 Catalog.GetString ("Slideshow"),
							 null,
							 Catalog.GetString ("Start slideshow"),
							 SlideShowAction),
						});

				actions.Add (new ToggleActionEntry [] {
					new ToggleActionEntry (Info,
							       Stock.Info,
							       Catalog.GetString ("Info"),
							       null,
							       Catalog.GetString ("Image Information"),
							       InfoAction,
							       false)
						});
				
				new Fader (this, 1.0, 3);
				notebook = new Notebook ();
				notebook.ShowBorder = false;
				notebook.ShowTabs = false;
				notebook.Show ();

				scroll = new ScrolledView ();
				view = new PhotoImageView (collection);
				// FIXME this should be handled by the new style setting code
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				view.PointerMode = ImageView.PointerModeType.Scroll;
				this.Add (notebook);
				view.Show ();
				view.MotionNotifyEvent += HandleViewMotion;
				
				Action rotate_left = new RotateLeftAction (view.Item);
				actions.Add (rotate_left);
				
				Action rotate_right = new RotateRightAction (view.Item);
				actions.Add (rotate_right);

				scroll.ScrolledWindow.Add (view);
				HBox hhbox = new HBox ();
				hhbox.PackEnd (GetButton (HideToolbar), false, true, 0);
				hhbox.PackEnd (GetButton (Info), false, true, 0);
				hhbox.PackStart (GetButton (ExitFullScreen, true), false, false, 0);
				hhbox.PackStart (Add (new PreviousPictureAction (view.Item)), false, false, 0);
				hhbox.PackStart (GetButton (SlideShow), false, true, 0);
				hhbox.PackStart (Add (new NextPictureAction (view.Item)), false, false, 0);
				//hhbox.PackStart (Add (new AutoColor (view.Item)), false, false, 0);

				display = new TextureDisplay (view.Item);
				display.AddEvents ((int) (Gdk.EventMask.PointerMotionMask));
				display.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				display.MotionNotifyEvent += HandleViewMotion;
				Label effect = new Label (Catalog.GetString ("Slide transition: "));
				hhbox.PackStart (effect, false, false, 5);
				hhbox.PackStart (display.GetCombo (), false, false, 0);
				display.Show ();

				hhbox.PackStart (Add (new RotateLeftAction (view.Item)), false, false, 0);
				hhbox.PackStart (Add (new RotateRightAction (view.Item)), false, false, 0);
				hhbox.BorderWidth = 15;

				tag_view = new TagView ();
				hhbox.PackStart (tag_view, false, false, 0);

				//display = new ImageDisplay (view.Item);

				notebook.AppendPage (scroll, null);
				notebook.AppendPage (display, null);

				hhbox.ShowAll ();
				//scroll.ShowControls ();
				
				scroll.Show ();
				this.Decorated = false;
				this.Fullscreen ();
				this.ButtonPressEvent += HandleButtonPressEvent;
				
				view.Item.Changed += HandleItemChanged;
				view.GrabFocus ();
				
				controls = new ControlOverlay (this);
				controls.Add (hhbox);
				controls.Dismiss ();

				notebook.CurrentPage = 0;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}	

		}

		private Widget Add (Action action)
		{
			actions.Add (action);
			return GetButton (action.Name);
		}

		private void HandleItemChanged (object sender, BrowsablePointerChangedArgs args)
		{
			tag_view.Current = view.Item.Current;
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
		private Button GetButton (string name)
		{
			return GetButton (name, false);
		}

		private Button GetButton (string name, bool label)
		{
			Action action = actions [name];
			Widget w = action.CreateIcon (IconSize.LargeToolbar);
			if (label) {
				HBox box = new HBox ();
				box.PackStart (w, false, false, 0);
				Label l = new Label ();
				//l.Markup = "<small>" + action.Label + "</small>";
				l.Text = action.Label;
				box.PackStart (l);
				w = box;
			}
			Button button;
			if (action is ToggleAction) {
				ToggleButton toggle = new ToggleButton ();
				toggle.Active = ((ToggleAction)action).Active;
				button = toggle;
			} else {
				button = new Button ();
			}
			button.Relief = ReliefStyle.None;
			button.Add (w);
			w.ShowAll ();

			action.ConnectProxy (button);
			return button;
		}
			
		private void ExitAction (object sender, System.EventArgs args)
		{
			this.Destroy ();
		}

	        private void HideToolbarAction (object sender, System.EventArgs args)
		{
			scroll.HideControls (true);
			controls.Dismiss ();
		}

		private void SlideShowAction (object sender, System.EventArgs args)
		{
			PlayPause ();
		}

		InfoOverlay info;
		private void InfoAction (object sender, System.EventArgs args)
		{
			ToggleAction action = sender as ToggleAction;

			if (info == null) {
				info = new InfoOverlay (this, view.Item);
			}

			info.Visibility = action.Active ? 
				ControlOverlay.VisibilityType.Partial : 
				ControlOverlay.VisibilityType.None;
		}

		[GLib.ConnectBefore]
		private void HandleViewMotion (object sender, Gtk.MotionNotifyEventArgs args)
		{
			int x, y;
			Gdk.ModifierType type;
			((Gtk.Widget)sender).GdkWindow.GetPointer (out x, out y, out type);
			controls.Visibility = ControlOverlay.VisibilityType.Partial;
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
			switch (key.Key) {
				case Gdk.Key.Up:
				case Gdk.Key.Left:
				case Gdk.Key.KP_Up:
				case Gdk.Key.KP_Left:
				case Gdk.Key.Page_Up:
				case Gdk.Key.Down:
				case Gdk.Key.Right:
				case Gdk.Key.KP_Down:
				case Gdk.Key.KP_Right:
				case Gdk.Key.Page_Down:
					break;
				default:
					controls.Visibility = ControlOverlay.VisibilityType.Partial;
					break;
			}

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

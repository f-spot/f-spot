/*
 * FSpot.FullScreenView
 *
 * Author(s):
 * 	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;
using Gdk;
using FSpot.Widgets;
using Mono.Unix;

namespace FSpot {
	[Binding(Gdk.Key.Escape, "Quit")]
	public class FullScreenView : Gtk.Window {
		private ScrolledView scroll;
		private PhotoImageView view;
		private Notebook notebook;
		private ControlOverlay controls;
		//		private ImageDisplay display;
		private TextureDisplay display;
		private ToolButton play_pause_button;

		ActionGroup actions;
		const string ExitFullScreen = "ExitFullScreen";
		const string HideToolbar = "HideToolbar";
		const string SlideShow = "SlideShow";
		const string Info = "Info";
		
		public FullScreenView (IBrowsableCollection collection, Gtk.Window parent) : base ("Full Screen Mode")
		{
			//going fullscreen on the same screen the parent window
			Gdk.Screen screen = Screen;
			int monitor = screen.GetMonitorAtWindow (parent.GdkWindow);
			Gdk.Rectangle bounds = screen.GetMonitorGeometry(monitor);
			Move(bounds.X, 0);


			string style = "style \"test\" {\n" +
				"GtkToolbar::shadow_type = GTK_SHADOW_NONE\n" +
				"}\n" +
				"class \"GtkToolbar\" style \"test\"";

			Gtk.Rc.ParseString (style);

			Name = "FullscreenContainer";
			try {
				//scroll = new Gtk.ScrolledWindow (null, null);
				actions = new ActionGroup ("joe");
				
				actions.Add (new ActionEntry [] {
					new ActionEntry (HideToolbar, Stock.Close, 
							 Catalog.GetString ("Hide"), 
							 null, 
							 Catalog.GetString ("Hide Toolbar"), 
							 HideToolbarAction)});

				actions.Add (new ToggleActionEntry [] {
					new ToggleActionEntry (Info,
							       Stock.Info,
							       Catalog.GetString ("Info"),
							       null,
							       Catalog.GetString ("Image Information"),
							       InfoAction,
							       false)});

				Gtk.Action exit_full_screen = new Gtk.Action (ExitFullScreen, 
					Catalog.GetString ("Exit fullscreen"),
					null,
					null);
#if GTK_2_10
				exit_full_screen.IconName = "view-restore";
#endif
				exit_full_screen.Activated += ExitAction;
				actions.Add (exit_full_screen);

				Gtk.Action slide_show = new Gtk.Action (SlideShow,
					Catalog.GetString ("Slideshow"),
					Catalog.GetString ("Start slideshow"),
					null);
#if GTK_2_10
				slide_show.IconName = "media-playback-start";
#endif
				slide_show.Activated += SlideShowAction;
				actions.Add (slide_show);

				new Fader (this, 1.0, 3);
				notebook = new Notebook ();
				notebook.ShowBorder = false;
				notebook.ShowTabs = false;
				notebook.Show ();

				scroll = new ScrolledView ();
				scroll.ScrolledWindow.SetPolicy (PolicyType.Never, PolicyType.Never);
				view = new PhotoImageView (collection);
				// FIXME this should be handled by the new style setting code
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				view.PointerMode = ImageView.PointerModeType.Scroll;
				this.Add (notebook);
				view.Show ();
				view.MotionNotifyEvent += HandleViewMotion;
				
				scroll.ScrolledWindow.Add (view);

				Toolbar tbar = new Toolbar ();
				tbar.ToolbarStyle = Gtk.ToolbarStyle.BothHoriz;

				tbar.ShowArrow = false;
				tbar.BorderWidth = 15;

				ToolItem t_item = (actions [ExitFullScreen]).CreateToolItem () as ToolItem;
				t_item.IsImportant = true;
				tbar.Insert (t_item, -1);

				Gtk.Action action = new PreviousPictureAction (view.Item);
				actions.Add (action);
#if GTK_2_10
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);
#else
				t_item = action.CreateToolItem () as ToolItem;
				(t_item as ToolButton).IconName = "gtk-go-back-ltr"; 
				tbar.Insert (t_item, -1);
#endif

				play_pause_button = (actions [SlideShow]).CreateToolItem () as ToolButton;
#if GTK_2_10
				tbar.Insert (play_pause_button, -1);
#else
				play_pause_button.IconName = "media-playback-start";
				tbar.Insert (play_pause_button, -1);
#endif

				action = new NextPictureAction (view.Item);
				actions.Add (action);
#if GTK_2_10
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);
#else
				t_item = action.CreateToolItem () as ToolItem;
				(t_item as ToolButton).IconName = "gtk-go-forward-ltr"; 
				tbar.Insert (t_item, -1);
#endif

				t_item = new ToolItem ();
				t_item.Child = new Label (Catalog.GetString ("Slide transition:"));
				tbar.Insert (t_item, -1);

				display = new TextureDisplay (view.Item);
				display.AddEvents ((int) (Gdk.EventMask.PointerMotionMask));
				display.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				display.MotionNotifyEvent += HandleViewMotion;
				display.Show ();

				t_item = new ToolItem ();
				t_item.Child = display.GetCombo ();
				tbar.Insert (t_item, -1);

				action = new RotateLeftAction (view.Item);
				actions.Add (action);
#if GTK_2_10
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);
#else
				t_item = action.CreateToolItem () as ToolItem;
				(t_item as ToolButton).IconName = "object-rotate-left"; 
				tbar.Insert (t_item, -1);
#endif

				action = new RotateRightAction (view.Item);
				actions.Add (action);
#if GTK_2_10
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);
#else
				t_item = action.CreateToolItem () as ToolItem;
				(t_item as ToolButton).IconName = "object-rotate-right"; 
				tbar.Insert (t_item, -1);
#endif

				tbar.Insert ((actions [Info]).CreateToolItem () as ToolItem, -1);

				tbar.Insert ((actions [HideToolbar]).CreateToolItem () as ToolItem, -1);

				notebook.AppendPage (scroll, null);
				notebook.AppendPage (display, null);

				tbar.ShowAll ();
				
				scroll.Show ();
				this.Decorated = false;
				this.Fullscreen ();
				this.ButtonPressEvent += HandleButtonPressEvent;
				
				view.Item.Changed += HandleItemChanged;
				view.GrabFocus ();
				
				controls = new ControlOverlay (this);
				controls.Add (tbar);
				controls.Dismiss ();

				notebook.CurrentPage = 0;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}	

		}

		private void HandleItemChanged (object sender, BrowsablePointerChangedArgs args)
		{
			if (scroll.ControlBox.Visible)
				scroll.ShowControls ();
		}
#if false
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			bool ret = base.OnExposeEvent (args);
			Graphics g = CairoHelper.Create (GdkWindow);

			g.Color = new Cairo.Color (0, 0, 0, .5);
			g.Operator = Operator.DestOut;
			g.Rectangle (0, 0, Allocation.Width  * .5, Allocation.Height);
			g.Paint ();

			return ret;
		}
#endif
		
		private void ExitAction (object sender, System.EventArgs args)
		{
			display.Dispose ();
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

			if (y > (Allocation.Height * 0.66)) {
				controls.Visibility = ControlOverlay.VisibilityType.Partial;
				scroll.ShowControls ();
			}
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
				PhotoPopup popup = new PhotoPopup (this);
				popup.Activate (this.Toplevel, args.Event);
			}
		}

		public bool PlayPause ()
		{
			if (notebook.CurrentPage == 0) {
				FSpot.Utils.ScreenSaver.Inhibit ("Running slideshow mode");
				notebook.CurrentPage = 1;
				play_pause_button.IconName = "media-playback-pause";
				display.Start ();
			} else {
				FSpot.Utils.ScreenSaver.UnInhibit ();
				notebook.CurrentPage = 0;
				play_pause_button.IconName = "media-playback-start";
				display.Stop ();
			}
			return true;
		}

		public void Quit ()
		{
			FSpot.Utils.ScreenSaver.UnInhibit ();
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

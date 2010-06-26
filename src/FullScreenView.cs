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
using FSpot.Utils;
using Hyena;
using Mono.Unix;

namespace FSpot {
	[Binding(Gdk.Key.Escape, "Quit")]
	public class FullScreenView : Gtk.Window {
		private ScrolledView scroll;
		private PhotoImageView view;
		private Notebook notebook;
		private ControlOverlay controls;
		private SlideShow display;
		private ToolButton play_pause_button;
		private ToggleToolButton info_button;
		private Delay hide_cursor_delay;

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
			Gdk.Rectangle bounds = screen.GetMonitorGeometry (monitor);
			Move (bounds.X, bounds.Y);

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
							 Catalog.GetString ("Hide toolbar"), 
							 HideToolbarAction)});

				actions.Add (new ToggleActionEntry [] {
					new ToggleActionEntry (Info,
							       Stock.Info,
							       Catalog.GetString ("Info"),
							       null,
							       Catalog.GetString ("Image information"),
							       InfoAction,
							       false)});

				Gtk.Action exit_full_screen = new Gtk.Action (ExitFullScreen, 
					Catalog.GetString ("Exit fullscreen"),
					null,
					null);
				exit_full_screen.IconName = "view-restore";
				exit_full_screen.Activated += ExitAction;
				actions.Add (exit_full_screen);

				Gtk.Action slide_show = new Gtk.Action (SlideShow,
					Catalog.GetString ("Slideshow"),
					Catalog.GetString ("Start slideshow"),
					null);
				slide_show.IconName = "media-playback-start";
				slide_show.Activated += SlideShowAction;
				actions.Add (slide_show);

				new Fader (this, 1.0, 600);
				notebook = new Notebook ();
				notebook.ShowBorder = false;
				notebook.ShowTabs = false;
				notebook.Show ();

				scroll = new ScrolledView ();
				scroll.ScrolledWindow.SetPolicy (PolicyType.Never, PolicyType.Never);
				view = new PhotoImageView (collection);
				// FIXME this should be handled by the new style setting code
				view.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				this.Add (notebook);
				view.Show ();
				view.MotionNotifyEvent += HandleViewMotion;
				view.PointerMode = PointerMode.Scroll;
				
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
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);

				play_pause_button = (actions [SlideShow]).CreateToolItem () as ToolButton;
				tbar.Insert (play_pause_button, -1);

				action = new NextPictureAction (view.Item);
				actions.Add (action);
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);

				t_item = new ToolItem ();
				t_item.Child = new Label (Catalog.GetString ("Slide transition:"));
				tbar.Insert (t_item, -1);

				display = new SlideShow (view.Item);
				display.AddEvents ((int) (Gdk.EventMask.PointerMotionMask));
				display.ModifyBg (Gtk.StateType.Normal, this.Style.Black);
				display.MotionNotifyEvent += HandleViewMotion;
				display.Show ();

				t_item = new ToolItem ();
				ComboBox combo = ComboBox.NewText ();
				foreach (var transition in display.Transitions)
					combo.AppendText (transition.Name);
				combo.Active = 0;
				combo.Changed += HandleTransitionChanged;
				t_item.Child = combo;
				tbar.Insert (t_item, -1);

				action = new RotateLeftAction (view.Item);
				actions.Add (action);
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);

				action = new RotateRightAction (view.Item);
				actions.Add (action);
				tbar.Insert (action.CreateToolItem () as ToolItem, -1);

				info_button = (ToggleToolButton) ((actions [Info]).CreateToolItem () as ToolItem);
				tbar.Insert (info_button, -1);

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

				hide_cursor_delay = new Delay (3000, new GLib.IdleHandler (HideCursor));
				hide_cursor_delay.Start ();
				
				controls = new ControlOverlay (this);
				controls.Add (tbar);
				controls.Dismiss ();

				notebook.CurrentPage = 0;
			} catch (System.Exception e) {
				Log.Exception (e);
			}	
		}

		private Gdk.Cursor empty_cursor;
		private bool HideCursor ()
		{
			if (view.Panning) {
				return false;
			}

			if (empty_cursor == null) 
				empty_cursor = GdkUtils.CreateEmptyCursor (GdkWindow.Display);
			
			this.GdkWindow.Cursor = empty_cursor;
			view.GdkWindow.Cursor = empty_cursor;
			return false;
		}
		
		private void ShowCursor () 
		{
			view.PointerMode = PointerMode.Scroll;
			this.GdkWindow.Cursor = null;
		}
		
		private void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			if (scroll.ControlBox.Visible)
				scroll.ShowControls ();
		}

		void HandleTransitionChanged (object sender, EventArgs e)
		{
			ComboBox combo = sender as ComboBox;
			if (combo == null)
				return;
			TreeIter iter;
			if (combo.GetActiveIter (out iter)) {
				string name = combo.Model.GetValue (iter, 0) as string;
				foreach (var transition in display.Transitions)
					if (transition.Name == name)
						display.Transition = transition;
			}


		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			bool ret = base.OnExposeEvent (args);

			HideCursor ();
			return ret;
		}
		
		private void ExitAction (object sender, System.EventArgs args)
		{
			Quit ();
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
			bool active = false;
			if (sender is ToggleToolButton) {
				(sender as ToggleToolButton).Active = ! (sender as ToggleToolButton).Active;
				active = (sender as ToggleToolButton).Active;
			} else 
				active = (sender as ToggleAction).Active;

			if (info == null) {
				info = new InfoOverlay (this, view.Item);
			}

			info.Visibility = active ? 
				ControlOverlay.VisibilityType.Partial : 
				ControlOverlay.VisibilityType.None;
		}

		[GLib.ConnectBefore]
		private void HandleViewMotion (object sender, Gtk.MotionNotifyEventArgs args)
		{
			ShowCursor ();
			hide_cursor_delay.Restart ();
			
			int x, y;
			Gdk.ModifierType type;
			((Gtk.Widget)sender).GdkWindow.GetPointer (out x, out y, out type);

			if (y > (Allocation.Height * 0.75)) {
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
				FSpot.Platform.ScreenSaver.Inhibit ("Running slideshow mode");
				notebook.CurrentPage = 1;
				play_pause_button.IconName = "media-playback-pause";
				display.Start ();
			} else {
				FSpot.Platform.ScreenSaver.UnInhibit ();
				notebook.CurrentPage = 0;
				play_pause_button.IconName = "media-playback-start";
				display.Stop ();
			}
			return true;
		}

		public void Quit ()
		{
			hide_cursor_delay.Stop ();
			FSpot.Platform.ScreenSaver.UnInhibit ();

			this.Destroy ();
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey key)
		{
			switch (key.Key) {
			// quit only on certain keys
			case Gdk.Key.F:
			case Gdk.Key.f:
			case Gdk.Key.Q:
			case Gdk.Key.q:
			case Gdk.Key.F11:
			case Gdk.Key.Escape:
				Quit ();
				return true;
			// display infobox for 'i' key
			case Gdk.Key.i:
			case Gdk.Key.I:
				InfoAction (info_button, null);
				return true;
			case Gdk.Key.bracketleft:
				new RotateLeftAction (view.Item).Activate ();
				return true;
			case Gdk.Key.bracketright:
				new RotateRightAction (view.Item).Activate ();
				return true;
			}

			return base.OnKeyPressEvent (key);
		}
	}
}

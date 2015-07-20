//
// FullScreenView.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Larry Ewing <lewing@src.gnome.org>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2004-2009 Novell, Inc.
// Copyright (C) 2004-2008 Larry Ewing
// Copyright (C) 2007-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using Gtk;

using FSpot.Core;
using FSpot.Widgets;
using FSpot.Gui;
using FSpot.Utils;

using Hyena;

using Mono.Unix;

namespace FSpot
{
	[Binding(Gdk.Key.Escape, "Quit")]
	public class FullScreenView : Window
	{
		readonly ScrolledView scroll;
		readonly PhotoImageView view;
		readonly Notebook notebook;
		readonly ControlOverlay controls;
		readonly SlideShow display;
		readonly ToolButton play_pause_button;
		readonly ToggleToolButton info_button;
		readonly DelayedOperation hide_cursor_delay;

		readonly ActionGroup actions;
		const string ExitFullScreen = "ExitFullScreen";
		const string HideToolbar = "HideToolbar";
		const string SlideShow = "SlideShow";
		const string Info = "Info";

		public FullScreenView (IBrowsableCollection collection, Window parent) : base ("Full Screen Mode")
		{
			//going fullscreen on the same screen the parent window
			Gdk.Screen screen = Screen;
			int monitor = screen.GetMonitorAtWindow (parent.Window);
			Gdk.Rectangle bounds = screen.GetMonitorGeometry (monitor);
			Move (bounds.X, bounds.Y);

			string style = "style \"test\" {\n" +
				"GtkToolbar::shadow_type = GTK_SHADOW_NONE\n" +
				"}\n" +
				"class \"GtkToolbar\" style \"test\"";

			Rc.ParseString (style);

			Name = "FullscreenContainer";
			try {
				//scroll = new Gtk.ScrolledWindow (null, null);
				actions = new ActionGroup ("joe");

				actions.Add (new[] {
					new ActionEntry (HideToolbar, Stock.Close,
							 Catalog.GetString ("Hide"),
							 null,
							 Catalog.GetString ("Hide toolbar"),
							 HideToolbarAction)});

				actions.Add (new[] {
					new ToggleActionEntry (Info,
							       Stock.Info,
							       Catalog.GetString ("Info"),
							       null,
							       Catalog.GetString ("Image information"),
							       InfoAction,
							       false)});

				var exit_full_screen = new Gtk.Action (ExitFullScreen,
					Catalog.GetString ("Exit fullscreen"),
					null,
					null);
				exit_full_screen.IconName = "view-restore";
				exit_full_screen.Activated += ExitAction;
				actions.Add (exit_full_screen);

				var slide_show = new Gtk.Action (SlideShow,
					Catalog.GetString ("Slideshow"),
					Catalog.GetString ("Start slideshow"),
					null);
				slide_show.IconName = "media-playback-start";
				slide_show.Activated += SlideShowAction;
				actions.Add (slide_show);

				new WindowOpacityFader (this, 1.0, 600);
				notebook = new Notebook ();
				notebook.ShowBorder = false;
				notebook.ShowTabs = false;
				notebook.Show ();

				scroll = new ScrolledView ();
				scroll.ScrolledWindow.SetPolicy (PolicyType.Never, PolicyType.Never);
				view = new PhotoImageView (collection);
				// FIXME this should be handled by the new style setting code
				view.ModifyBg (StateType.Normal, Style.Black);
				Add (notebook);
				view.Show ();
				view.MotionNotifyEvent += HandleViewMotion;
				view.PointerMode = PointerMode.Scroll;

				scroll.ScrolledWindow.Add (view);

				Toolbar tbar = new Toolbar ();
				tbar.ToolbarStyle = ToolbarStyle.BothHoriz;

				tbar.ShowArrow = false;
				tbar.BorderWidth = 15;

				var t_item = (actions [ExitFullScreen]).CreateToolItem () as ToolItem;
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
				display.ModifyBg (StateType.Normal, Style.Black);
				display.MotionNotifyEvent += HandleViewMotion;
				display.Show ();

				t_item = new ToolItem ();
				var combo = new ComboBoxText();
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
				Decorated = false;
				Fullscreen ();
				ButtonPressEvent += HandleButtonPressEvent;

				view.Item.Changed += HandleItemChanged;
				view.GrabFocus ();

				hide_cursor_delay = new DelayedOperation (3000, new GLib.IdleHandler (HideCursor));
				hide_cursor_delay.Start ();

				controls = new ControlOverlay (this);
				controls.Add (tbar);
				controls.Dismiss ();

				notebook.CurrentPage = 0;
			} catch (Exception e) {
				Log.Exception (e);
			}
		}

		Gdk.Cursor empty_cursor;
		bool HideCursor ()
		{
			if (view.InPanMotion) {
				return false;
			}

			if (empty_cursor == null)
				empty_cursor = GdkUtils.CreateEmptyCursor (Window.Display);

			Window.Cursor = empty_cursor;
			view.Window.Cursor = empty_cursor;
			return false;
		}

		void ShowCursor ()
		{
			view.PointerMode = PointerMode.Scroll;
			Window.Cursor = null;
		}

		void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			if (scroll.ControlBox.Visible)
				scroll.ShowControls ();
		}

		void HandleTransitionChanged (object sender, EventArgs e)
		{
			var combo = sender as ComboBox;
			if (combo == null)
				return;
			TreeIter iter;
			if (combo.GetActiveIter (out iter))
			{
			    string name = combo.Model.GetValue (iter, 0) as string;
			    foreach (var transition in display.Transitions.Where(transition => transition.Name == name))
			        display.Transition = transition;
			}
		}

		protected override bool OnDrawn (Cairo.Context cr)
		{
			bool ret = base.OnDrawn (cr);

			HideCursor ();
			return ret;
		}

		void ExitAction (object sender, EventArgs args)
		{
			Quit ();
		}

		void HideToolbarAction (object sender, EventArgs args)
		{
			scroll.HideControls (true);
			controls.Dismiss ();
		}

		void SlideShowAction (object sender, EventArgs args)
		{
			PlayPause ();
		}

		InfoOverlay info;
		void InfoAction (object sender, EventArgs args)
		{
			bool active = false;
			var toggleToolButton = sender as ToggleToolButton;
			if (toggleToolButton != null) {
				toggleToolButton.Active = ! toggleToolButton.Active;
				active = toggleToolButton.Active;
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
		void HandleViewMotion (object sender, MotionNotifyEventArgs args)
		{
			ShowCursor ();
			hide_cursor_delay.Restart ();

			int x, y;
			Gdk.ModifierType type;
			((Widget)sender).Window.GetPointer (out x, out y, out type);

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

		void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Type == Gdk.EventType.ButtonPress
			    && args.Event.Button == 3) {
				var popup = new PhotoPopup (this);
				popup.Activate (Toplevel, args.Event);
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

			Destroy ();
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

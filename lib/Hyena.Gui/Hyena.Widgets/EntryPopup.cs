//
// EntryPopup.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using Gtk;

namespace Hyena.Widgets
{
	public class EntryPopup : Gtk.Window
	{
		Entry text_entry;
		HBox hbox;
		uint timeout_id = 0;

		public event EventHandler<EventArgs> Changed;
		public event EventHandler<KeyPressEventArgs> KeyPressed;

		public EntryPopup (string text) : this ()
		{
			Text = text;
		}

		public EntryPopup () : base (Gtk.WindowType.Popup)
		{
			CanFocus = true;
			Resizable = false;
			TypeHint = Gdk.WindowTypeHint.Utility;
			Modal = true;

			var frame = new Frame ();
			frame.Shadow = ShadowType.EtchedIn;
			Add (frame);

			hbox = new HBox () { Spacing = 6 };
			text_entry = new Entry ();
			hbox.PackStart (text_entry, true, true, 0);
			hbox.BorderWidth = 3;

			frame.Add (hbox);
			frame.ShowAll ();

			text_entry.Text = string.Empty;
			text_entry.CanFocus = true;

			//TODO figure out why this event does not get raised
			text_entry.FocusOutEvent += (o, a) => {
				if (hide_when_focus_lost) {
					HidePopup ();
				}
			};

			text_entry.KeyReleaseEvent += delegate (object o, KeyReleaseEventArgs args) {
				if (args.Event.Key == Gdk.Key.Escape ||
					args.Event.Key == Gdk.Key.Return ||
					args.Event.Key == Gdk.Key.Tab) {

					HidePopup ();
				}

				InitializeDelayedHide ();
			};

			text_entry.KeyPressEvent += (o, a) => OnKeyPressed (a);

			text_entry.Changed += (o, a) => {
				if (GdkWindow.IsVisible) {
					OnChanged (a);
				}
			};
		}

		public new bool HasFocus {
			get { return text_entry.HasFocus; }
			set { text_entry.HasFocus = value; }
		}

		public string Text {
			get { return text_entry.Text; }
			set { text_entry.Text = value; }
		}

		public Entry Entry { get { return text_entry; } }
		public HBox Box { get { return hbox; } }

		bool hide_after_timeout = true;
		public bool HideAfterTimeout {
			get { return hide_after_timeout; }
			set { hide_after_timeout = value; }
		}

		uint timeout = 5000;
		public uint Timeout {
			get { return timeout; }
			set { timeout = value; }
		}

		bool hide_when_focus_lost = true;
		public bool HideOnFocusOut {
			get { return hide_when_focus_lost; }
			set { hide_when_focus_lost = value; }
		}

		bool reset_when_hiding = true;
		public bool ResetOnHide {
			get { return reset_when_hiding; }
			set { reset_when_hiding = value; }
		}

		public override void Dispose ()
		{
			text_entry.Dispose ();
			base.Dispose ();
		}

		public new void GrabFocus ()
		{
			text_entry.GrabFocus ();
		}

		public void Position (Gdk.Window eventWindow)
		{
			int x, y;

			Realize ();

			Gdk.Window widget_window = eventWindow;
			Gdk.Screen widget_screen = widget_window.Screen;

			Gtk.Requisition popup_req;

			widget_window.GetOrigin (out var widget_x, out var widget_y);
			widget_window.GetSize (out var widget_width, out var widget_height);

			popup_req = Requisition;

			if (widget_x + widget_width > widget_screen.Width) {
				x = widget_screen.Width - popup_req.Width;
			} else if (widget_x + widget_width - popup_req.Width < 0) {
				x = 0;
			} else {
				x = widget_x + widget_width - popup_req.Width;
			}

			if (widget_y + widget_height + popup_req.Height > widget_screen.Height) {
				y = widget_screen.Height - popup_req.Height;
			} else if (widget_y + widget_height < 0) {
				y = 0;
			} else {
				y = widget_y + widget_height;
			}

			Move (x, y);
		}

		void ResetDelayedHide ()
		{
			if (timeout_id > 0) {
				GLib.Source.Remove (timeout_id);
				timeout_id = 0;
			}
		}

		void InitializeDelayedHide ()
		{
			ResetDelayedHide ();
			timeout_id = GLib.Timeout.Add (timeout, delegate {
				HidePopup ();
				return false;
			});
		}

		void HidePopup ()
		{
			ResetDelayedHide ();
			Hide ();

			if (reset_when_hiding) {
				text_entry.Text = string.Empty;
			}
		}

		protected virtual void OnChanged (EventArgs args)
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnKeyPressed (KeyPressEventArgs args)
		{
			KeyPressed?.Invoke (this, args);
		}

		//TODO figure out why this event does not get raised
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			if (hide_when_focus_lost) {
				HidePopup ();
				return true;
			}

			return base.OnFocusOutEvent (evnt);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			InitializeDelayedHide ();
			return base.OnExposeEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (!text_entry.HasFocus && hide_when_focus_lost) {
				HidePopup ();
				return true;
			}

			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (!text_entry.HasFocus && hide_when_focus_lost) {
				HidePopup ();
				return true;
			}

			return base.OnButtonPressEvent (evnt);
		}
	}
}

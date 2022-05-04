//
// MessageBar.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Widgets
{
	public class MessageBar : Alignment
	{
		HBox box;
		HBox button_box;
		AnimatedImage image;
		WrapLabel label;
		Button close_button;

		Window win;

		Theme theme;

		public event EventHandler CloseClicked {
			add { close_button.Clicked += value; }
			remove { close_button.Clicked -= value; }
		}

		public MessageBar () : base (0.0f, 0.5f, 1.0f, 0.0f)
		{
			win = new Window (WindowType.Popup);
			win.Name = "gtk-tooltips";
			win.EnsureStyle ();
			win.StyleSet += delegate {
				Style = win.Style;
			};

			var shell_box = new HBox ();
			shell_box.Spacing = 10;

			box = new HBox ();
			box.Spacing = 10;

			image = new AnimatedImage ();
			try {
				image.Pixbuf = Gtk.IconTheme.Default.LoadIcon ("process-working", 22, IconLookupFlags.NoSvg);
				image.FrameHeight = 22;
				image.FrameWidth = 22;
				Spinning = false;
				image.Load ();
			} catch {
			}

			label = new WrapLabel ();
			label.Show ();

			box.PackStart (image, false, false, 0);
			box.PackStart (label, true, true, 0);
			box.Show ();

			button_box = new HBox ();
			button_box.Spacing = 3;

			close_button = new Button (new Image (Stock.Close, IconSize.Menu));
			close_button.Relief = ReliefStyle.None;
			close_button.Clicked += delegate { Hide (); };
			close_button.ShowAll ();
			close_button.Hide ();

			shell_box.PackStart (box, true, true, 0);
			shell_box.PackStart (button_box, false, false, 0);
			shell_box.PackStart (close_button, false, false, 0);
			shell_box.Show ();

			Add (shell_box);

			EnsureStyle ();

			BorderWidth = 3;
		}

		protected MessageBar (IntPtr raw) : base (raw)
		{
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			image.Show ();
		}

		protected override void OnHidden ()
		{
			base.OnHidden ();
			image.Hide ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (this);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			QueueDraw ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!IsDrawable) {
				return false;
			}

			Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);

			try {
				Gdk.Color color = Style.Background (StateType.Normal);
				theme.DrawFrame (cr, Allocation, CairoExtensions.GdkColorToCairoColor (color));
				return base.OnExposeEvent (evnt);
			} finally {
				CairoExtensions.DisposeContext (cr);
			}
		}

		bool changing_style = false;
		protected override void OnStyleSet (Gtk.Style previousStyle)
		{
			if (changing_style) {
				return;
			}

			changing_style = true;
			Style = win.Style;
			label.Style = Style;
			changing_style = false;
		}

		public void RemoveButton (Button button)
		{
			button_box.Remove (button);
		}

		public void ClearButtons ()
		{
			foreach (Widget child in button_box.Children) {
				button_box.Remove (child);
			}
		}

		public void AddButton (Button button)
		{
			button_box.Show ();
			button.Show ();
			button_box.PackStart (button, false, false, 0);
		}

		public bool ShowCloseButton {
			set {
				close_button.Visible = value;
				QueueDraw ();
			}
		}

		public string Message {
			set {
				label.Markup = value;
				QueueDraw ();
			}
		}

		public Gdk.Pixbuf Pixbuf {
			set {
				image.InactivePixbuf = value;
				QueueDraw ();
			}
		}

		public bool Spinning {
			get { return image.Active; }
			set { image.Active = value; }
		}
	}
}

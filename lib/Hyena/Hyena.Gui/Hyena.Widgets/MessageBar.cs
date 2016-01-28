//
// MessageBar.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Widgets
{
    public class MessageBar : Alignment
    {
        private HBox box;
        private HBox button_box;
        private AnimatedImage image;
        private WrapLabel label;
        private Button close_button;

        private Window win;

        private Theme theme;

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

            HBox shell_box = new HBox ();
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

        private bool changing_style = false;
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

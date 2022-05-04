//
// MenuButton.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
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

using Gtk;

namespace FSpot.Widgets
{
	public class MenuButton : Button
	{
		Label label;
		Arrow arrow;
		Menu popup_menu;

		public new string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}

		public new Image Image { get; private set; }

		public ArrowType ArrowType {
			get { return arrow.ArrowType; }
			set { arrow.ArrowType = value; }
		}

		public Menu Menu {
			get { return popup_menu; }
			set { popup_menu = value; }
		}

		public MenuButton () : this (null)
		{
		}

		public MenuButton (string label) : this (label, null)
		{
		}

		public MenuButton (string label, Menu menu) : this (label, menu, ArrowType.Down)
		{
		}

		public MenuButton (string label, Menu menu, ArrowType arrow_type) : base ()
		{
			var hbox = new HBox ();

			Image = new Image ();
			hbox.PackStart (Image, false, false, 1);
			Image.Show ();

			this.label = new Label (label);
			this.label.Xalign = 0;
			hbox.PackStart (this.label, true, true, 1);
			this.label.Show ();

			this.arrow = new Arrow (arrow_type, ShadowType.None);
			hbox.PackStart (arrow, false, false, 1);
			arrow.Show ();

			Menu = menu;

			this.Add (hbox);
			hbox.Show ();
		}

		protected override void OnPressed ()
		{
			if (popup_menu == null)
				return;

			popup_menu.Popup (null, null, Position, 0, Gtk.Global.CurrentEventTime);
		}

		void Position (Menu menu, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			x += Allocation.X;
			y += Allocation.Y + Allocation.Height;
			push_in = false;
			menu.WidthRequest = Allocation.Width;
		}
	}
}

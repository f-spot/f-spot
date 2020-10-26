//
// MenuButton.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace FSpot.Widgets
{
	public class MenuButton : Button
	{
		readonly Label label;
		readonly Arrow arrow;

		public new string Label {
			get => label.Text;
			set { label.Text = value; }
		}

		public new Image Image { get; private set; }

		public ArrowType ArrowType {
			get => arrow.ArrowType;
			set { arrow.ArrowType = value; }
		}

		public Menu Menu { get; set; }

		public MenuButton () : this (null)
		{
		}

		public MenuButton (string label) : this (label, null)
		{
		}

		public MenuButton (string label, Menu menu) : this (label, menu, ArrowType.Down)
		{
		}

		public MenuButton (string label, Menu menu, ArrowType arrowType) : base ()
		{
			using var hbox = new HBox ();
			
			Image = new Image ();
			hbox.PackStart (Image, false, false, 1);
			Image.Show ();

			this.label = new Label (label) { Xalign = 0 };
			hbox.PackStart (this.label, true, true, 1);
			this.label.Show ();

			arrow = new Arrow (arrowType, ShadowType.None);
			hbox.PackStart (arrow, false, false, 1);
			arrow.Show ();

			Menu = menu;

			Add (hbox);
			hbox.Show ();
		}

		protected override void OnPressed ()
		{
			if (Menu == null)
				return;
			
			Menu.Popup (null, null, Position, 0, Gtk.Global.CurrentEventTime);
		}

		void Position (Menu menu, out int x, out int y, out bool push_in)
		{
			GdkWindow.GetOrigin (out x, out y);
			x += Allocation.X;
			y += Allocation.Y + Allocation.Height;
			push_in = false;
			menu.WidthRequest = Allocation.Width;
		}
	}
}

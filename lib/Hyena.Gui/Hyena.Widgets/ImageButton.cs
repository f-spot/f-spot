//
// ImageButton.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace Hyena.Widgets
{
	public class ImageButton : Button
	{
		Image image;
		Label label;
		HBox hbox;

		public Image ImageWidget { get { return image; } }
		public Label LabelWidget { get { return label; } }

		public uint InnerPadding {
			get { return hbox.BorderWidth; }
			set { hbox.BorderWidth = value; }
		}

		public int Spacing {
			get { return hbox.Spacing; }
			set { hbox.Spacing = value; }
		}

		public ImageButton (string text, string iconName) : this (text, iconName, Gtk.IconSize.Button)
		{
		}

		public ImageButton (string text, string iconName, Gtk.IconSize iconSize) : base ()
		{
			image = new Image ();
			image.IconName = iconName;
			image.IconSize = (int)iconSize;

			label = new Label ();
			label.MarkupWithMnemonic = text;

			hbox = new HBox ();
			hbox.Spacing = 2;
			hbox.PackStart (image, false, false, 0);
			hbox.PackStart (label, true, true, 0);

			Child = hbox;
			CanDefault = true;
			ShowAll ();
		}
	}
}

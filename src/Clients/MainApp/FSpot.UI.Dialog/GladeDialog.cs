/*
 * FSpot.UI.Dialog.GladeDialog
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;

namespace FSpot.UI.Dialog
{
	public class GladeDialog
	{
		protected string dialog_name;
		protected Glade.XML xml;
		private Gtk.Dialog dialog;

		protected GladeDialog ()
		{
		}

		public GladeDialog (string widget_name) : this (widget_name, "f-spot.glade")
		{
		}

		public GladeDialog (string widget_name, string resource_name)
		{
			if (widget_name == null)
				throw new ArgumentNullException ("widget_name");
			if (resource_name == null)
				throw new ArgumentNullException ("resource_name");

			CreateDialog (widget_name, resource_name);
		}

		protected void CreateDialog (string widget_name)
		{
			CreateDialog (widget_name, "f-spot.glade");
		}

		protected void CreateDialog (string widget_name, string resource_name)
		{
			this.dialog_name = widget_name;
			xml = new Glade.XML (null, resource_name, widget_name, "f-spot");
			xml.Autoconnect (this);
		}

		public Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
			}
		}
	}
}

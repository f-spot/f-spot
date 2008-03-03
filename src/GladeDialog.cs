/*
 * FSpot.GladeDialog.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software, See COPYING for details
 */

using System;
using FSpot.UI.Dialog;
using Gtk;

namespace FSpot {
	public class GladeDialog {
		protected string dialog_name;
		protected Glade.XML xml;
		private Gtk.Dialog dialog;
		
		protected GladeDialog ()
		{
		}

		public GladeDialog (string name)
		{
			CreateDialog (name);
		}

		protected void CreateDialog (string name)
		{
			this.dialog_name = name;		
			xml = new Glade.XML (null, "f-spot.glade", name, "f-spot");
			xml.Autoconnect (this);

			Attribute[] attributeArray = Attribute.GetCustomAttributes (this.GetType ());
			foreach(Attribute attrib in attributeArray) 
			{
				if (attrib is SizeAttribute)
				{
					SizeAttribute a = attrib as SizeAttribute;
 					if ((int) FSpot.Preferences.Get (a.WidthKey) > 0)
 						Dialog.Resize ((int) Preferences.Get (a.WidthKey), (int) Preferences.Get(a.HeightKey));
				}
			}
		}

		public Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);
				return dialog;
			}
		}

		protected void SaveSettings ()
		{
			Attribute[] attributeArray = Attribute.GetCustomAttributes (this.GetType ());
			foreach(Attribute attrib in attributeArray) 
			{
				if (attrib is SizeAttribute)
				{
					SizeAttribute a = attrib as SizeAttribute;
					int width, height;
					Dialog.GetSize (out width, out height);

					Preferences.Set (a.WidthKey, width);
					Preferences.Set (a.HeightKey, height);
				}
			}	
		}
	}
}

/*
 * FSpot.UI.Dialog.AboutDialog.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class AboutDialog : Gtk.AboutDialog
	{
		private static AboutDialog about = null;
		
		private AboutDialog () {
			Artists = new string [] {
				"Jakub Steiner",	
			};
			Authors = new string [] {
				"Aaron Bockover",
				"Alessandro Gervaso",
				"Alex Graveley",
				"Alvaro del Castillo",
				"Ben Monnahan",
				"Bengt Thuree",
				"Chad Files",
				"Ettore Perazzoli",
				"Ewen Cheslack-Postava",
				"Joe Shaw",
				"Joerg Buesse",
				"Jon Trowbridge",
				"Joshua Tauberer",
				"Gabriel Burt",
				"Grahm Orr",
				"Laurence Hygate",
				"Lawrence Ewing",
				"Lee Willis",
				"Lorenzo Milesi",
				"Martin Willemoes Hansen",
				"Matt Jones",
				"Miguel de Icaza",
				"Nat Friedman",
				"Patanjali Somayaji",
				"Peter Johanson",
				"Ruben Vermeersch",
				"Stephane Delcroix",
				"Tambet Ingo",
				"Thomas Van Machelen",
				"Todd Berman",
				"Vincent Moreau",
				"Vladimir Vukicevic",
				"Xavier Bouchoux",
			};
			Comments = "F-Spot is a full-featured personal photo management application for the GNOME desktop.\nIt simplifies digital photography by providing intuitive tools to help you share, touch-up, find and organize your images..";
			Copyright = Catalog.GetString ("Copyright \x00a9 2003-2008 Novell Inc.");
			Documenters = new string[] {
				"Aaron Bockover",
				"Alexandre Prokoudine",	
				"Bengt Thuree",
				"Gabriel Burt",
				"Miguel de Icaza",
				"Stephane Delcroix",
			};
			License = "GPL v2";
			LogoIconName = "f-spot";
	#if !GTK_2_11
			Name = "F-Spot";
	#endif
			TranslatorCredits = Catalog.GetString ("translator-credits");
                	if (System.String.Compare (TranslatorCredits, "translator-credits") == 0)
                		TranslatorCredits = null;
			Version = Defines.VERSION;
			Website = "http://f-spot.org";
			WebsiteLabel = Catalog.GetString ("F-Spot Website");
			WrapLicense = true;
		}

		public static AboutDialog ShowUp ()
		{
			if (about == null)
				about = new AboutDialog ();
			about.Destroyed += delegate (object o, EventArgs e) {about = null;};
			about.Response += delegate (object o, Gtk.ResponseArgs e) {if (about != null) about.Destroy ();};
			about.Show ();
			return about;
		}
	}
}

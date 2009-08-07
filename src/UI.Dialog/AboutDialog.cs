/*
 * FSpot.UI.Dialog.AboutDialog.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class AboutDialog : Gtk.AboutDialog
	{
		private static AboutDialog about = null;
		
		private AboutDialog () {
			Artists = new string [] {
				"Jakub Steiner",	
				"Matthew Paul Thomas",
			};
			Authors = new string [] {
				"Primary Development",
				"\tLawrence Ewing",
				"\tStephane Delcroix",
				"",
				"Active Contributors to this release",
				"\tLorenzo Milesi",					//0.5.0
				"\tMike Gem\x00fcnde",
				"\tRuben Vermeersch",					//0.5.0, 0.6.0
				"",
				"Contributors",
				"\tAaron Bockover",
				"\tAlessandro Gervaso",
				"\tAlex Graveley",
				"\tAlvaro del Castillo",
				"\tAnton Keks",
				"\tBen Monnahan",
				"\tBengt Thuree",
				"\tChad Files",
				"\tEttore Perazzoli",
				"\tEwen Cheslack-Postava",
				"\tJoe Shaw",
				"\tJoerg Buesse",
				"\tJon Trowbridge",
				"\tJoshua Tauberer",
				"\tGabriel Burt",
				"\tGrahm Orr",
				"\tLaurence Hygate",
				"\tLee Willis",
				"\tMartin Willemoes Hansen",
				"\tMatt Jones",
				"\tMiguel de Icaza",
				"\tNat Friedman",
				"\tPatanjali Somayaji",
				"\tPeter Johanson",
				"\tTambet Ingo",
				"\tThomas Van Machelen",				//0.5.0
				"\tTodd Berman",
				"\tVasily Kirilichev (Google Summer of Code)",		//0.5.0
				"\tVincent Moreau",
				"\tVladimir Vukicevic",
				"\tXavier Bouchoux",
				"",
				"In memory Of",
				"\tEttore Perazzoli",
			};
			Comments = Catalog.GetString ("Photo management for GNOME");
			Copyright = Catalog.GetString ("Copyright \x00a9 2003-2009 Novell Inc.");
			Documenters = new string[] {
				"Aaron Bockover",
				"Alexandre Prokoudine",	
				"Bengt Thuree",
				"Gabriel Burt",
				"Miguel de Icaza",
				"Stephane Delcroix",
			};
			//Read license from COPYING
			try {
				System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
				using (Stream s = assembly.GetManifestResourceStream ("COPYING")) {
					StreamReader reader = new StreamReader (s);
					License = reader.ReadToEnd ();
					s.Close ();
				}
			} catch (Exception e) {
				Console.WriteLine (e);
				License = "GPL v2";
			}
			Logo = new Gdk.Pixbuf (System.Reflection.Assembly.GetEntryAssembly (), "f-spot-128.png");
			ProgramName = "F-Spot";
			TranslatorCredits = Catalog.GetString ("translator-credits");
                	if (System.String.Compare (TranslatorCredits, "translator-credits") == 0)
                		TranslatorCredits = null;
			Version = Defines.VERSION;
			Website = "http://f-spot.org";
			WebsiteLabel = Catalog.GetString ("F-Spot Website");
			WrapLicense = true;
		}

		public static void ShowUp ()
		{
			if (about == null) {
				about = new AboutDialog ();
				about.Destroyed += delegate (object o, EventArgs e) {about = null;};
				about.Response += delegate (object o, Gtk.ResponseArgs e) {if (about != null) about.Destroy ();};
			}
			about.Show ();
		}
	}
}

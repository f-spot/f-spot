//
// AboutDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot.UI.Dialog
{
	public class AboutDialog : Gtk.AboutDialog
	{
		static AboutDialog about;

		AboutDialog ()
		{
			Artists = new string[] {
					"Jakub Steiner",
					"Matthew Paul Thomas",
			};
			Authors = new string[] {
				"Primary Development",
					"\tLawrence Ewing",
					"\tStephane Delcroix",
					"\tRuben Vermeersch",
					"\tStephen Shaw",
					"",
					"Active Contributors to this release",
					"\tAdemir Mendoza",
					"\tAlex Launi",
					"\tAnton Keks",
					"\tBertrand Lorentz",
					"\tChristian Krause",
					"\tChristopher Halse Rogers",
					"\tDaniel Köb",
					"\tEric Faehnrich",
					"\tEvan Briones",
					"\tGabriel Burt",
					"\tIain Churcher",
					"\tIain Lane",
					"\tLorenzo Milesi",
					"\tŁukasz Jernaś",
					"\tMike Gem\x00fcnde",
					"\tMike Wallick",
					"\tNick Van Eeckhout",
					"\tPaul Lange",
					"\tPaul Wellner Bou",
					"\tPeter Goetz",
					"\tRuben Vermeersch",
					"\tTim Retout",
					"\tTomas Kovacik",
					"\tTrevor Buchanan",
					"\tVincent Pomey",
					"\tWojciech Dzierżanowski",
					"",
					"Contributors",
					"\tAaron Bockover",
					"\tAlessandro Gervaso",
					"\tAlex Graveley",
					"\tAlvaro del Castillo",
					"\tBengt Thuree",
					"\tBen Monnahan",
					"\tChad Files",
					"\tEttore Perazzoli",
					"\tEwen Cheslack-Postava",
					"\tGrahm Orr",
					"\tJeffrey Finkelstein",
					"\tJeffrey Stedfast",
					"\tJoerg Buesse",
					"\tJoe Shaw",
					"\tJon Trowbridge",
					"\tJoshua Tauberer",
					"\tKarl Mikaelsson",
					"\tLaurence Hygate",
					"\tLee Willis",
					"\tMartin Willemoes Hansen",
					"\tMatt Jones",
					"\tMatt Perry",
					"\tMichal Nánási",
					"\tMiguel de Icaza",
					"\tNat Friedman",
					"\tPascal de Bruijn",
					"\tPatanjali Somayaji",
					"\tPeter Johanson",
					"\tTambet Ingo",
					"\tThomas Van Machelen",
					"\tTodd Berman",
					"\tVasily Kirilichev",
					"\tVincent Moreau",
					"\tVladimir Vukicevic",
					"\tXavier Bouchoux",
					"\tYann Leprince",
					"\tYves Kurz",
					"",
					"In memory Of",
					"\tEttore Perazzoli",
			};
			Comments = Strings.PhotoManagementForGNOME;
			Copyright = Strings.CopyrightNovell;
			Documenters = new string[] {
					"Aaron Bockover",
					"Alexandre Prokoudine",
					"Bengt Thuree",
					"Gabriel Burt",
					"Harold Schreckengost",
					"Miguel de Icaza",
					"Stephane Delcroix",
			};
			//Read license from COPYING
			try {
				var assembly = System.Reflection.Assembly.GetCallingAssembly ();
				using (Stream s = assembly.GetManifestResourceStream ("COPYING")) {
					var reader = new StreamReader (s);
					License = reader.ReadToEnd ();
					s.Close ();
				}
			} catch (Exception e) {
				Logger.Log.Debug (e, "");
				License = "GPL v2";
			}
			Logo = new Gdk.Pixbuf (System.Reflection.Assembly.GetEntryAssembly (), "f-spot-128.png");
			ProgramName = "F-Spot";
			TranslatorCredits = Strings.TranslatorCredits;
			if (string.Compare (TranslatorCredits, "translator-credits") == 0)
				TranslatorCredits = null;
			Version = FSpotConfiguration.Version;
			Website = "http://f-spot.app";
			WebsiteLabel = Strings.FSpotWebsite;
			WrapLicense = true;
		}

		public static void ShowUp ()
		{
			if (about == null) {
				about = new AboutDialog ();
				about.Destroyed += (o, e) => { about = null; };
				about.Response += (o, e) => { if (about != null) about.Destroy (); };
			}

			about.Show ();
		}
	}
}

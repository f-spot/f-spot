/*
 * FSpot.UI.Dialog.AboutDialog.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2008-2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */

using System;
using System.IO;
using Mono.Unix;
using Hyena;

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
                    "\tRuben Vermeersch",
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
                    "\tMartin Slota",
                    "\tMike Gem\x00fcnde",
                    "\tMike Wallick",
                    "\tNick Van Eeckhout",
                    "\tNuno Ferreira",
                    "\tPaul Lange",
                    "\tPaul Wellner Bou",
                    "\tPeter Goetz",
                    "\tRuben Vermeersch",
                    "\tTim Howard",
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
            Comments = Catalog.GetString ("Photo management for GNOME");
            Copyright = Catalog.GetString ("Copyright \x00a9 2003-2010 Novell Inc.");
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
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
                using (Stream s = assembly.GetManifestResourceStream ("COPYING")) {
                    StreamReader reader = new StreamReader (s);
                    License = reader.ReadToEnd ();
                    s.Close ();
                }
            } catch (Exception e) {
                Log.DebugException (e);
                License = "GPL v2";
            }
            Logo = new Gdk.Pixbuf (System.Reflection.Assembly.GetEntryAssembly (), "f-spot-128.png");
            ProgramName = "F-Spot";
            TranslatorCredits = Catalog.GetString ("translator-credits");
            if (System.String.Compare (TranslatorCredits, "translator-credits") == 0)
                TranslatorCredits = null;
            Version = FSpot.Core.Defines.VERSION;
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

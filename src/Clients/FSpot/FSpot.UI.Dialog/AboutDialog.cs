//
// AboutDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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

using System;
using System.IO;
using FSpot.Settings;
using Hyena;
using Mono.Unix;

namespace FSpot.UI.Dialog
{
    public class AboutDialog : Gtk.AboutDialog
    {
        static AboutDialog about;

        AboutDialog ()
		{
            Artists = new string [] {
                    "Jakub Steiner",
                    "Matthew Paul Thomas",
            };
            Authors = new string [] {
                "Primary Development",
                    "\tLawrence Ewing",
                    "\tStephane Delcroix",
                    "\tRuben Vermeersch",
                    "\tStephen  Shaw",
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
            if (string.Compare (TranslatorCredits, "translator-credits") == 0)
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
                about.Destroyed += (o, e) => { about = null; };
                about.Response += (o, e) => { if (about != null) about.Destroy (); };
            }

            about.Show ();
        }
    }
}

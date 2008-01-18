/*
 * FSpot.Utils.GnomeUtil.cs
 *
 * Author(s):
 *
 *
 * This is free software. See COPYING for details
 */

using System;

namespace FSpot.Utils
{
	public class GnomeUtil {
		Gtk.Window window;
		string url;
		
		private GnomeUtil (Gtk.Window window, string url)
		{
			this.window = window;
			this.url = url;
		}
			
		private void Show () 
		{
			try {
				Gnome.Url.Show (url);
			} catch (Exception ge) {
		       		System.Console.WriteLine (ge.ToString ());
		       	}
		}
	
		public static void UrlShow (Gtk.Window owner_window, string url)
		{
			GnomeUtil disp = new GnomeUtil (owner_window, url);
			Gtk.Application.Invoke (disp, null, delegate (object sender, EventArgs args) { ((GnomeUtil) disp).Show (); });
		}
	
		public static void ShowHelp (string filename, string link_id, string help_directory, Gdk.Screen screen, Gtk.Window parent)
		{
			try {
				Gnome.Help.DisplayDesktopOnScreen (
						Gnome.Program.Get (),
						help_directory,
						filename,
						link_id,
						screen);
			} catch {
				string message = Mono.Unix.Catalog.GetString ("The \"F-Spot Manual\" could " +
						"not be found.  Please verify " +
						"that your installation has been " +
						"completed successfully.");
			}
		}
	
	}
}

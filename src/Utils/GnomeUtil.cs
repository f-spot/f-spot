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
	
		public static void ShowHelp (string filename, string link_id, string help_directory, Gdk.Screen screen)
		{
			try {
				Gnome.Help.DisplayDesktopOnScreen (
						Gnome.Program.Get (),
						help_directory,
						filename,
						link_id,
						screen);
			} catch {
				Console.WriteLine (Mono.Unix.Catalog.GetString ("The \"F-Spot Manual\" could " +
						"not be found.  Please verify " +
						"that your installation has been " +
						"completed successfully."));
			}
		}

#if !NOGCONF
		public static void SetBackgroundImage (string path)
		{
			GConf.Client client = new GConf.Client (); 
			client.Set ("/desktop/gnome/background/color_shading_type", "solid");
			client.Set ("/desktop/gnome/background/primary_color", "#000000");
			client.Set ("/desktop/gnome/background/picture_options", "stretched");
			client.Set ("/desktop/gnome/background/picture_opacity", 100);
			client.Set ("/desktop/gnome/background/picture_filename", path);
			client.Set ("/desktop/gnome/background/draw_background", true);
		}
#endif	
	}
}

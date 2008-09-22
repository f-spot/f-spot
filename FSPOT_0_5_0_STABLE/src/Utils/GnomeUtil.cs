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
		string url;
		
		private GnomeUtil (string url)
		{
			this.url = url;
		}
			
		private void Show () 
		{
			try {
				Gnome.Url.Show (url);
			} catch (Exception ge) {
		       		Log.Exception (ge);
		       	}
		}
	
		[Obsolete ("use gtk_show_uri as soon as we can depend on gtk 2.13.1")]
		public static void UrlShow (string url)
		{
			GnomeUtil disp = new GnomeUtil (url);
			Gtk.Application.Invoke (disp, null, delegate (object sender, EventArgs args) { ((GnomeUtil) disp).Show (); });
		}
	
		[Obsolete ("use gtk_show_uri as soon as we can depend on gtk 2.13.1")]
		public static void ShowHelp (string filename, string link_id, string help_directory, Gdk.Screen screen)
		{
			try {
				Gnome.Help.DisplayDesktopOnScreen (
						Gnome.Program.Get (),
						help_directory,
						filename,
						link_id,
						screen);
			} catch (Exception e) {
				Log.Exception (Mono.Unix.Catalog.GetString ("The \"F-Spot Manual\" could " +
						"not be found.  Please verify " +
						"that your installation has been " +
						"completed successfully."), e);
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

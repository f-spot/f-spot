/*
 * FSpot.Platform.Gnome.Desktop.cs
 *
 * Author(s):
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Platform
{
	public static class Desktop
	{
		public static void SetBackgroundImage (string path)
		{
			GConf.Client client = new GConf.Client (); 
			client.Set ("/desktop/gnome/background/color_shading_type", "solid");
			client.Set ("/desktop/gnome/background/primary_color", "#000000");
			client.Set ("/desktop/gnome/background/picture_options", "zoom");
			client.Set ("/desktop/gnome/background/picture_opacity", 100);
			client.Set ("/desktop/gnome/background/picture_filename", path);
			client.Set ("/desktop/gnome/background/draw_background", true);
		}
	}
}

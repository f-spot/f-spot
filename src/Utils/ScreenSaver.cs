/*
 * FSpot.Utils.ScreenSaver.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 * 	Giacomo Rizzo  <alt@free-os.it>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Runtime.InteropServices;

using NDesk.DBus;

namespace FSpot.Utils
{
	[Interface ("org.gnome.ScreenSaver")]
	public interface IScreenSaver
	{
		uint Inhibit (string appname, string reason);
		void UnInhibit (uint cookie);
		void Lock ();
	}

	public static class ScreenSaver
	{
		private static IScreenSaver screensaver;
		private static IScreenSaver GnomeScreenSaver {
			get {
				if (screensaver == null)
					screensaver = Bus.Session.GetObject<IScreenSaver> ("org.gnome.ScreenSaver", new ObjectPath ("/org/gnome/ScreenSaver"));
				return screensaver;
			}
		}

		private static bool inhibited = false;
		private static uint cookie = 0;

		public static uint Inhibit (string reason ) {
			if (inhibited)
				return cookie;

			Console.WriteLine ("Inhibit screensaver for slideshow");
			try {
				cookie = GnomeScreenSaver.Inhibit ("f-spot", reason);
				inhibited = true;
			} catch (Exception ex) {
				Console.WriteLine ("Error Inhibiting the screenserver: {0}", ex.Message);
			}	
			return cookie;
		}

		public static void UnInhibit () {
			if (!inhibited)
				return;

			Console.WriteLine ("UnInhibit screensaver");
			try {
				GnomeScreenSaver.UnInhibit (cookie);
				inhibited = false;
			} catch (Exception ex) {
				Console.WriteLine("Error UnInhibiting the screenserver: {0}", ex.Message);
			}
		}
	}
}

/*
 * FSpot.Platform.Null.ScreenSaver.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;

using FSpot.Utils;

namespace FSpot.Platform
{
	public static class ScreenSaver
	{
		public static uint Inhibit (string reason ) {
			Log.Debug ("No way to inhibit screensaver on this platform (Null Platform)");
			return 1;
		}

		public static void UnInhibit () {
			Log.Debug ("No way to uninhibit screensaver on this platform (Null Platform)");
		}
	}
}

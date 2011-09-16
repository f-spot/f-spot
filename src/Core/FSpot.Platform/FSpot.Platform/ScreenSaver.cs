//
// ScreenSaver.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
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
using System.Runtime.InteropServices;

using DBus;

using Hyena;

namespace FSpot.Platform
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

			Log.InformationFormat ("Inhibit screensaver for {0}", reason);
			try {
				cookie = GnomeScreenSaver.Inhibit ("f-spot", reason);
				inhibited = true;
			} catch (Exception ex) {
				Log.Exception ("Error Inhibiting the screensaver", ex);
			}
			return cookie;
		}

		public static void UnInhibit () {
			if (!inhibited)
				return;

			Log.Information ("UnInhibit screensaver");
			try {
				GnomeScreenSaver.UnInhibit (cookie);
				inhibited = false;
			} catch (Exception ex) {
				Log.Exception ("Error UnInhibiting the screensaver", ex);
			}
		}
	}
}

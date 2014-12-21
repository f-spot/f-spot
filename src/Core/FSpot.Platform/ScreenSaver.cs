//
// ScreenSaver.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
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

using DBus;

using Hyena;

namespace FSpot.Platform
{
	[Interface ("org.gnome.SessionManager")]
	public interface IScreenSaver
	{
		uint Inhibit (string appname, uint toplevel_xid, string reason, uint flags);
		void UnInhibit (uint cookie);
	}

	public static class ScreenSaver
	{
		const string DBUS_INTERFACE = "org.gnome.SessionManager";
		const string DBUS_PATH = "/org/gnome/SessionManager";
		static IScreenSaver screensaver;
		static IScreenSaver GnomeScreenSaver {
			get {
				if (screensaver == null)
					screensaver = Bus.Session.GetObject<IScreenSaver> (DBUS_INTERFACE, new ObjectPath (DBUS_PATH));
				return screensaver;
			}
		}

		static bool inhibited = false;
		static uint cookie = 0;
		static uint toplevel_xid = 0;
		// 8: Inhibit the session being marked as idle
		static uint flags = 8;

		public static uint Inhibit (string reason ) {
			if (inhibited)
				return cookie;

			Log.InformationFormat ("Inhibit screensaver for {0}", reason);
			try {
				cookie = GnomeScreenSaver.Inhibit ("f-spot", toplevel_xid, reason, flags);
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

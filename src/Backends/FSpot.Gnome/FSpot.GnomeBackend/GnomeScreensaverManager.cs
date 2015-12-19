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

using FSpot.PlatformServices;

namespace FSpot.GnomeBackend
{
	[Interface ("org.gnome.SessionManager")]
	interface IGnomeScreensaver
	{
		uint Inhibit (string appname, uint toplevel_xid, string reason, uint flags);
		void UnInhibit (uint cookie);
	}

    class GnomeScreensaverManager : IScreensaverManager
	{
		const string DBUS_INTERFACE = "org.gnome.SessionManager";
		const string DBUS_PATH = "/org/gnome/SessionManager";

        static IGnomeScreensaver manager;
        uint? cookie;
        bool logged_error;
        readonly uint toplevel_xid = 0;
        // 8: Inhibit the session being marked as idle
        readonly uint flags = 8;

        public IGnomeScreensaver Manager {
            get {
                if (manager == null) {
                    if (!Bus.Session.NameHasOwner (DBUS_INTERFACE)) {
                        return null;
                    }

                    manager = Bus.Session.GetObject<IGnomeScreensaver> (DBUS_INTERFACE, new ObjectPath (DBUS_PATH));

                    if (manager == null) {
                        Log.ErrorFormat ("The {0} object could not be located on the DBus interface {1}",
                                         DBUS_PATH, DBUS_INTERFACE);
                    }
                }

                return manager;
            }
        }

		public void Inhibit (string reason)
        {
            Log.InformationFormat ("Inhibit screensaver for {0}", reason);

            try {
                if (!cookie.HasValue && Manager != null) {
                    cookie = Manager.Inhibit ("f-spot", toplevel_xid, reason, flags);
                }
            } catch (Exception e) {
                if (!logged_error) {
                    Log.Error ("Error Inhibiting the screensaver;", e.Message);
                    logged_error = true;
                }
            }
		}

		public void UnInhibit ()
        {
            Log.Information ("UnInhibit screensaver");

            try {
                if (cookie.HasValue && Manager != null) {
                    Manager.UnInhibit (cookie.Value);
                    cookie = null;
                }
            } catch (Exception e) {
                Log.Exception ("Error UnInhibiting the screensaver", e);
            }
		}
	}
}

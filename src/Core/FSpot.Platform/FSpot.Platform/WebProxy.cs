//
// WebProxy.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

using Hyena;

namespace FSpot.Platform
{
	public class WebProxy {

		const string PROXY = "/system/http_proxy/";
		const string PROXY_USE_PROXY = PROXY + "use_http_proxy";
		const string PROXY_HOST = PROXY + "host";
		const string PROXY_PORT = PROXY + "port";
		const string PROXY_USER = PROXY + "authentication_user";
		const string PROXY_PASSWORD = PROXY + "authentication_password";
		const string PROXY_BYPASS_LIST = PROXY + "ignore_hosts";

		public static void Init ()
		{
			LoadPreference (PROXY_USE_PROXY);
		}

		static void OnPreferenceChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		static void LoadPreference (string key)
		{
			switch (key) {
			case PROXY_USE_PROXY :
			case PROXY_HOST :
			case PROXY_PORT :
			case PROXY_USER :
			case PROXY_PASSWORD :
			case PROXY_BYPASS_LIST :
				System.Net.WebRequest.DefaultWebProxy = GetWebProxy () ?? new System.Net.WebProxy ();
				break;
			}
		}

		static System.Net.WebProxy GetWebProxy ()
		{
			System.Net.WebProxy proxy;

			if (!Backend.Get<bool> (PROXY_USE_PROXY))
				return null;

			try {
				string host = Backend.Get<string> (PROXY_HOST);
				int port = Backend.Get<int> (PROXY_PORT);

				string uri = "http://" + host + ":" + port.ToString ();
				proxy = new System.Net.WebProxy (uri);

				string [] bypass_list = Backend.Get<string[]> (PROXY_BYPASS_LIST);
				if (bypass_list != null) {
					for (int i = 0; i < bypass_list.Length; i++) {
						bypass_list [i] = "http://" + bypass_list [i];
					}
					proxy.BypassList = bypass_list;
				}

				string username = Backend.Get<string> (PROXY_USER);
				string password = Backend.Get<string> (PROXY_PASSWORD);

				proxy.Credentials = new System.Net.NetworkCredential (username, password);
			} catch (Exception e) {
				Log.Warning ("Failed to set the web proxy settings");
				Log.DebugException (e);
				return null;
			}

			return proxy;
		}

		static PreferenceBackend backend;
		static EventHandler<NotifyEventArgs> changed_handler;
		static PreferenceBackend Backend {
			get {
				if (backend == null) {
					backend = new PreferenceBackend ();
					changed_handler = new EventHandler<NotifyEventArgs> (OnPreferenceChanged);
					backend.AddNotify (PROXY, changed_handler);
				}
				return backend;
			}
		}
	}
}


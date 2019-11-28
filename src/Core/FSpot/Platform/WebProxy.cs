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

using FSpot.Settings;

using Hyena;

namespace FSpot.Platform
{
	public class WebProxy
	{
		const string ProxyKey = "/system/http_proxy/";
		const string UseProxyKey = ProxyKey + "use_http_proxy";
		const string HostKey = ProxyKey + "host";
		const string PortKey = ProxyKey + "port";
		const string ProxyUserKey = ProxyKey + "authentication_user";
		const string ProxyPasswordKey = ProxyKey + "authentication_password";
		const string ProxyIgnoreHostKey = ProxyKey + "ignore_hosts";

		public static void Init ()
		{
			LoadPreference (UseProxyKey);
		}

		static void OnPreferenceChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		static void LoadPreference (string key)
		{
			switch (key) {
			case UseProxyKey :
			case HostKey :
			case PortKey :
			case ProxyUserKey :
			case ProxyPasswordKey :
			case ProxyIgnoreHostKey :
				System.Net.WebRequest.DefaultWebProxy = GetWebProxy () ?? new System.Net.WebProxy ();
				break;
			}
		}

		static System.Net.WebProxy GetWebProxy ()
		{
			System.Net.WebProxy proxy;

			if (!Preferences.Get<bool> (UseProxyKey))
				return null;

			try {
				string host = Preferences.Get<string> (HostKey);
				int port = Preferences.Get<int> (PortKey);

				string uri = $"http://{host}:{port}";
				proxy = new System.Net.WebProxy (uri);

				string [] bypass_list = Preferences.Get<string[]> (ProxyIgnoreHostKey);
				if (bypass_list != null) {
					for (int i = 0; i < bypass_list.Length; i++) {
						bypass_list [i] = $"http://{bypass_list [i]}";
					}
					proxy.BypassList = bypass_list;
				}

				string username = Preferences.Get<string> (ProxyUserKey);
				string password = Preferences.Get<string> (ProxyPasswordKey);

				proxy.Credentials = new System.Net.NetworkCredential (username, password);
			} catch (Exception e) {
				Log.Warning ("Failed to set the web proxy settings");
				Log.DebugException (e);
				return null;
			}

			return proxy;
		}

		//static PreferenceBackend backend;
		//static EventHandler<NotifyEventArgs> changed_handler;
		//static PreferenceBackend Backend {
		//	get {
		//		if (backend == null) {
		//			backend = new PreferenceBackend ();
		//			changed_handler = new EventHandler<NotifyEventArgs> (OnPreferenceChanged);
		//			//backend.AddNotify (PROXY, changed_handler);
		//		}
		//		return backend;
		//	}
		//}
	}
}


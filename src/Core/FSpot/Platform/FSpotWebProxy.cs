//
// WebProxy.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Net;

using FSpot.Settings;

using Hyena;

namespace FSpot.Platform
{
	public static class FSpotWebProxy
	{
		const string ProxyKey = "HttpProxy/";
		const string UseProxyKey = ProxyKey + "UseHttpProxy";
		const string HostKey = ProxyKey + "Host";
		const string PortKey = ProxyKey + "Port";
		const string ProxyUserKey = ProxyKey + "AuthenticationUser";
		const string ProxyPasswordKey = ProxyKey + "AuthenticationPassword";
		const string ProxyIgnoreHostKey = ProxyKey + "IgnoreHosts";

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
			case UseProxyKey:
			case HostKey:
			case PortKey:
			case ProxyUserKey:
			case ProxyPasswordKey:
			case ProxyIgnoreHostKey:
				WebRequest.DefaultWebProxy = GetWebProxy () ?? new WebProxy ();
				break;
			}
		}

		static WebProxy GetWebProxy ()
		{
			WebProxy proxy;

			if (!Preferences.Get<bool> (UseProxyKey))
				return null;

			try {
				string host = Preferences.Get<string> (HostKey);
				int port = Preferences.Get<int> (PortKey);

				string uri = $"http://{host}:{port}";
				proxy = new WebProxy (uri);

				string[] bypassList = Preferences.Get<string[]> (ProxyIgnoreHostKey);
				if (bypassList != null) {
					for (int i = 0; i < bypassList.Length; i++) {
						bypassList[i] = $"http://{bypassList[i]}";
					}
					proxy.BypassList = bypassList;
				}

				string username = Preferences.Get<string> (ProxyUserKey);
				string password = Preferences.Get<string> (ProxyPasswordKey);

				proxy.Credentials = new NetworkCredential (username, password);
			} catch (Exception e) {
				Log.Warning ("Failed to set the web proxy settings");
				Log.DebugException (e);
				return null;
			}

			return proxy;
		}

		//static PreferenceJsonBackend jsonBackend;
		//static EventHandler<NotifyEventArgs> changed_handler;
		//static PreferenceJsonBackend JsonBackend {
		//	get {
		//		if (jsonBackend == null) {
		//			jsonBackend = new PreferenceJsonBackend ();
		//			changed_handler = new EventHandler<NotifyEventArgs> (OnPreferenceChanged);
		//			//jsonBackend.AddNotify (PROXY, changed_handler);
		//		}
		//		return jsonBackend;
		//	}
		//}
	}
}

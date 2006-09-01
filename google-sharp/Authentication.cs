//
// Mono.Google.Authentication.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Mono.Google {
	class Authentication {
		static string picasa_login_url ="https://www.google.com/accounts/ServiceLoginAuth?service=lh2&passive=true&continue=http://picasaweb.google.com/";
		CookieContainer cookies = new CookieContainer ();

		private Authentication ()
		{
		}

		public static CookieContainer GetAuthCookies (string user, string password, GoogleService service)
		{
			// No auth performed if no useror no password
			if (user == null || password == null)
				return new CookieContainer ();

			// service == Picasa by now
			Authentication auth = new Authentication ();
			return auth.GetCookieContainer (user, password);
		}

		CookieContainer GetCookieContainer (string user, string password)
		{
			StringBuilder content = new StringBuilder ();
			content.AppendFormat ("null=Sign%20in&Email={0}&Passwd={1}&", user, password);
			content.Append ("service=lh2&passive=true&continue=http%3A%2F%2Fpicasaweb.google.com%2F");
			byte [] bytes = Encoding.UTF8.GetBytes (content.ToString ());

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (picasa_login_url);
			request.CookieContainer = cookies;
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = bytes.Length;

			Stream output = request.GetRequestStream ();
			output.Write (bytes, 0, bytes.Length);
			output.Close ();

			HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
			string received = "";
			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				received = sr.ReadToEnd ();
			}
			response.Close ();

			Regex regex = new Regex ("location\\.replace\\(\"(?<location>.*)\"\\)");
			Match match = regex.Match (received);
			if (!match.Success)
				return null;

			string redirect = match.Result ("${location}");
			cookies = RemoveExpiredCookies (cookies);
			request = (HttpWebRequest) WebRequest.Create (redirect);
			request.CookieContainer = cookies;
			response = (HttpWebResponse) request.GetResponse ();
			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				received = sr.ReadToEnd (); // ignored. Just for the cookies.
			}
			response.Close ();
			return cookies;
		}

		static CookieContainer RemoveExpiredCookies (CookieContainer all)
		{
			CookieContainer container = new CookieContainer ();
			Uri secure = new Uri ("https://www.google.com/accounts/");
			CookieCollection c1 = all.GetCookies (secure);
			foreach (Cookie cookie in c1) {
				if (cookie.Expired)
					continue;
				container.Add (secure, cookie);
			}

			Uri pweb = new Uri ("http://picasaweb.google.com/");
			CookieCollection c2 = all.GetCookies (pweb);
			foreach (Cookie cookie in c2) {
				if (cookie.Expired)
					continue;
				container.Add (pweb, cookie);
			}
			
			return container;
		}
	}
}


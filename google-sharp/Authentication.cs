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
using System.Web;
using System.Xml;

namespace Mono.Google {
	class Authentication {
		static string picasa_login_url = "https://www.google.com/accounts/ClientAuth";
		static string picasa_login_url2 = "https://www.google.com/accounts/IssueAuthToken";
		CookieContainer cookies = new CookieContainer ();

		private Authentication ()
		{
		}

		public static CookieContainer GetAuthCookies (GoogleConnection conn, string user, string password, GoogleService service, out string auth)
		{
			// No auth performed if no user or no password
			auth = null;
			if (user == null || password == null)
				return new CookieContainer ();

			// service == Picasa by now
			Authentication authentication = new Authentication ();
			return authentication.GetCookieContainer (conn, user, password, out auth);
		}

		CookieContainer GetCookieContainer (GoogleConnection conn, string user, string password, out string auth)
		{
			user = HttpUtility.UrlEncode (user);
			password = HttpUtility.UrlEncode (password);
			StringBuilder content = new StringBuilder ();
			string appname = HttpUtility.UrlEncode (conn.ApplicationName);
			content.AppendFormat ("Email={0}&Passwd={1}&source={2}&PersistentCookie=0&accountType=HOSTED%5FOR%5FGOOGLE", user, password, appname);
			byte [] bytes = Encoding.UTF8.GetBytes (content.ToString ());

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (picasa_login_url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = bytes.Length;

			Stream output = request.GetRequestStream ();
			output.Write (bytes, 0, bytes.Length);
			output.Close ();

			HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
			string received = "";
			string sid = null;
			string lsid = null;
			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				string s;
				while ((s = sr.ReadLine ()) != null) {
					if (s.StartsWith ("LSID=")) {
						lsid = s.Substring (5);
					} else if (s.StartsWith ("SID=")) {
						sid = s.Substring (4);
					}
				}
			}
			response.Close ();

			string req_input = String.Format ("SID={0}&LSID={1}&service=lh2&Session=true", sid, lsid);
			bytes = Encoding.UTF8.GetBytes (req_input);
			request = (HttpWebRequest) WebRequest.Create (picasa_login_url2);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = bytes.Length;

			output = request.GetRequestStream ();
			output.Write (bytes, 0, bytes.Length);
			output.Close ();

			response = (HttpWebResponse) request.GetResponse ();
			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				received = sr.ReadToEnd ();
			}
			response.Close ();

			Cookie cookie = new Cookie ("SID", sid);
			cookie.Domain = ".google.com";
			cookies.Add (cookie);
			cookie = new Cookie ("LSID", lsid);
			cookie.Secure = true;
			cookie.Domain = "www.google.com";
			cookie.Path = "/accounts";
			cookies.Add (cookie);
			auth = received.Trim ();
			return cookies;
		}
	}
}


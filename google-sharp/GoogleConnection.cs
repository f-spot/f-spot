//
// Mono.Google.GoogleConnection.cs:
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;

namespace Mono.Google {
	public class GoogleConnection {
		CookieContainer cookies;
		string user;
		GoogleService service;
		string auth;
		string appname;

		public GoogleConnection (GoogleService service)
		{
			if (service != GoogleService.Picasa)
				throw new ArgumentException ("Unsupported service.", "service");

			this.service = service;
		}

		public string ApplicationName {
			get {
				if (appname == null) {
					Assembly assembly = Assembly.GetEntryAssembly ();
					if (assembly == null)
						throw new InvalidOperationException ("You need to set GoogleConnection.ApplicationName.");
					AssemblyName aname = assembly.GetName ();
					appname = String.Format ("{0}-{1}", aname.Name, aname.Version);
				}

				return appname;
			}

			set {
				if (value == null || value == "")
					throw new ArgumentException ("Cannot be null or empty", "value");

				appname = value;
			}
		}

		public void Authenticate (string user, string password)
		{
			if (user == null)
				throw new ArgumentNullException ("user");

			if (this.user != null)
				throw new InvalidOperationException (String.Format ("Already authenticated for {0}", this.user));

			this.user = user;
			this.cookies = Authentication.GetAuthCookies (this, user, password, service, null, null, out auth);
			if (this.cookies == null) {
				this.user = null;
				throw new Exception (String.Format ("Authentication failed for user {0}", user));
			}
		}

		public void Authenticate (string user, string password, string token, string captcha)
		{
			if (user == null)
				throw new ArgumentNullException ("user");

			if (token == null)
				throw new ArgumentNullException ("token");

			if (captcha == null)
				throw new ArgumentNullException ("captcha");

			if (this.user != null)
				throw new InvalidOperationException (String.Format ("Already authenticated for {0}", this.user));

			this.user = user;
			this.cookies = Authentication.GetAuthCookies (this, user, password, service, token, captcha, out auth);
			if (this.cookies == null) {
				this.user = null;
				throw new Exception (String.Format ("Authentication failed for user {0}", user));
			}
		}

		public string DownloadString (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			string received = null;
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.CookieContainer = cookies;
			HttpWebResponse response = (HttpWebResponse) req.GetResponse ();
			Encoding encoding = Encoding.UTF8;
			if (response.ContentEncoding != "") {
				try {
					encoding = Encoding.GetEncoding (response.ContentEncoding);
				} catch {}
			}

			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, encoding);
				received = sr.ReadToEnd ();
			}
			AddCookiesFromHeader (req.Address, response.Headers ["set-cookie"]);
			response.Close ();
			return received;
		}

		public byte [] DownloadBytes (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.CookieContainer = cookies;
			HttpWebResponse response = (HttpWebResponse) req.GetResponse ();
			byte [] bytes = null;
			using (Stream stream = response.GetResponseStream ()) {
				if (response.ContentLength != -1) {
					bytes = new byte [response.ContentLength];
					stream.Read (bytes, 0, bytes.Length);
				} else {
					MemoryStream ms = new MemoryStream ();
					bytes = new byte [4096];
					int nread;
					while ((nread = stream.Read (bytes, 0, bytes.Length)) > 0) {
						ms.Write (bytes, 0, nread);
						if (nread < bytes.Length)
							break;
					}
					bytes = ms.ToArray ();
				}
			}
			AddCookiesFromHeader (req.Address, response.Headers ["set-cookie"]);
			response.Close ();

			return bytes;
		}

		public void DownloadToStream (string url, Stream output)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			if (output == null)
				throw new ArgumentNullException ("output");

			if (!output.CanWrite)
				throw new ArgumentException ("The stream is not writeable", "output");

			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.CookieContainer = cookies;
			HttpWebResponse response = (HttpWebResponse) req.GetResponse ();
			byte [] bytes = null;
			using (Stream stream = response.GetResponseStream ()) {
				bytes = new byte [4096];
				int nread;
				while ((nread = stream.Read (bytes, 0, bytes.Length)) > 0) {
					output.Write (bytes, 0, nread);
				}
			}
			AddCookiesFromHeader (req.Address, response.Headers ["set-cookie"]);
			response.Close ();
		}

		/* I don't know why, MS does not set any response.Cookies when it gets a Set-Cookie like:
			lh=DQAAAGwAAAC7YY1_HuCJh7WCvBttpcN5SnC5X6IShPs_9OZB6rZSlLA15xCoyBu_0FHE
			5x9TJ6jqOie9CPOhMprEoYYidr2bA3v5zPnA7lqY8RrOTIBADxHu5nU2KWYISAIPs-7sGA0Dcyatzx0s
			82dG1nl9ntM3;Domain=picasaweb.google.com;Path=/
		*/

		void AddCookiesFromHeader (Uri uri, string header)
		{
			if (header == null || header == "")
				return;

			string name, val;
			Cookie cookie = null;
			CookieParser parser = new CookieParser (header);

			while (parser.GetNextNameValue (out name, out val)) {
				if ((name == null || name == "") && cookie == null)
					continue;

				if (cookie == null) {
					cookie = new Cookie (name, val);
					continue;
				}

				name = name.ToLower (CultureInfo.InvariantCulture);
				switch (name) {
				case "comment":
					if (cookie.Comment == null)
						cookie.Comment = val;
					break;
				case "commenturl":
					if (cookie.CommentUri == null)
						cookie.CommentUri = new Uri (val);
					break;
				case "discard":
					cookie.Discard = true;
					break;
				case "domain":
					if (cookie.Domain == "")
						cookie.Domain = val;
					break;
				case "max-age": // RFC Style Set-Cookie2
					if (cookie.Expires == DateTime.MinValue) {
						try {
						cookie.Expires = cookie.TimeStamp.AddSeconds (UInt32.Parse (val));
						} catch {}
					}
					break;
				case "expires": // Netscape Style Set-Cookie
					if (cookie.Expires != DateTime.MinValue)
						break;
					try {
						cookie.Expires = DateTime.ParseExact (val, "r", CultureInfo.InvariantCulture);
					} catch {
						try { 
						cookie.Expires = DateTime.ParseExact (val,
								"ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'",
								CultureInfo.InvariantCulture);
						} catch {
							cookie.Expires = DateTime.Now.AddDays (1);
						}
					}
					break;
				case "path":
					cookie.Path = val;
					break;
				case "port":
					if (cookie.Port == null)
						cookie.Port = val;
					break;
				case "secure":
					cookie.Secure = true;
					break;
				case "version":
					try {
						cookie.Version = (int) UInt32.Parse (val);
					} catch {}
					break;
				}
			}

			if (cookie.Domain == "")
				cookie.Domain = uri.Host;

			if (cookies == null)
				cookies = new CookieContainer ();

			cookies.Add (cookie);
		}

		public string User {
			get { return user; }
		}

		public GoogleService Service {
			get { return service; }
		}

		internal CookieContainer Cookies {
			get { return cookies; }
		}

		internal string AuthToken {
			get { return auth; }
		}

		class CookieParser {
			string header;
			int pos;
			int length;

			public CookieParser (string header) : this (header, 0)
			{
			}

			public CookieParser (string header, int position)
			{
				this.header = header;
				this.pos = position;
				this.length = header.Length;
			}

			public bool GetNextNameValue (out string name, out string val)
			{
				name = null;
				val = null;

				if (pos >= length)
					return false;

				name = GetCookieName ();
				if (pos < header.Length && header [pos] == '=') {
					pos++;
					val = GetCookieValue ();
				}

				if (pos < length && header [pos] == ';')
					pos++;

				return true;
			}

			string GetCookieName ()
			{
				int k = pos;
				while (k < length && Char.IsWhiteSpace (header [k]))
					k++;

				int begin = k;
				while (k < length && header [k] != ';' &&  header [k] != '=')
					k++;

				pos = k;
				return header.Substring (begin, k - begin).Trim ();
			}

			string GetCookieValue ()
			{
				if (pos >= length)
					return null;

				int k = pos;
				while (k < length && Char.IsWhiteSpace (header [k]))
					k++;

				int begin;
				if (header [k] == '"'){
					int j;
					begin = ++k;

					while (k < length && header [k] != '"')
						k++;

					for (j = k; j < length && header [j] != ';'; j++)
						;
					pos = j;
				} else {
					begin = k;
					while (k < length && header [k] != ';')
						k++;
					pos = k;
				}
					
				return header.Substring (begin, k - begin).Trim ();
			}
		}
	}
}


//
// Mono.Google.GoogleConnection.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Stephane Delcroix (stephane@delcroix.org)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 S. Delcroix
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Check the Google Authentication Page at http://code.google.com/apis/accounts/AuthForInstalledApps.html
//

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;

namespace Mono.Google {
	public class GoogleConnection {
		string user;
		GoogleService service;
		string auth;
		string appname;

		public GoogleConnection (GoogleService service)
		{
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
			this.auth = Authentication.GetAuthorization (this, user, password, service, null, null);
			if (this.auth == null) {
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
			this.auth = Authentication.GetAuthorization (this, user, password, service, token, captcha);
			if (this.auth == null) {
				this.user = null;
				throw new Exception (String.Format ("Authentication failed for user {0}", user));
			}
		}

		public HttpWebRequest AuthenticatedRequest (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			if (auth != null)
				req.Headers.Add ("Authorization: GoogleLogin auth=" + auth);
			return req;
		}

		public string DownloadString (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			string received = null;
			HttpWebRequest req = AuthenticatedRequest (url);
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
			response.Close ();
			return received;
		}

		public byte [] DownloadBytes (string url)
		{
			if (url == null)
				throw new ArgumentNullException ("url");

			HttpWebRequest req = AuthenticatedRequest (url);
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

			HttpWebRequest req = AuthenticatedRequest (url);
			HttpWebResponse response = (HttpWebResponse) req.GetResponse ();
			byte [] bytes = null;
			using (Stream stream = response.GetResponseStream ()) {
				bytes = new byte [4096];
				int nread;
				while ((nread = stream.Read (bytes, 0, bytes.Length)) > 0) {
					output.Write (bytes, 0, nread);
				}
			}
			response.Close ();
		}

		public string User {
			get { return user; }
		}

		public GoogleService Service {
			get { return service; }
		}

		internal string AuthToken {
			get { return auth; }
		}
	}
}

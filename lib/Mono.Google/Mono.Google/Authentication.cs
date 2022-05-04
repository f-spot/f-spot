//
// Mono.Google.Authentication.cs:
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
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Mono.Google {
	class Authentication {
		static string client_login_url = "https://www.google.com/accounts/ClientLogin";

		public static string GetAuthorization (GoogleConnection conn, string email, string password,
				GoogleService service, string token, string captcha)
		{
			if (email == null || email == String.Empty || password == null || password == String.Empty)
				return null;

			email = HttpUtility.UrlEncode (email);
			password = HttpUtility.UrlEncode (password);
			string appname = HttpUtility.UrlEncode (conn.ApplicationName);
			string service_code = service.ServiceCode;

			StringBuilder content = new StringBuilder ();
			content.Append ("accountType=GOOGLE");
			content.AppendFormat ("&Email={0}", email);
			content.AppendFormat ("&Passwd={0}", password);
			content.AppendFormat ("&service={0}", service_code);
			content.AppendFormat ("&source={0}", appname);

			if (token != null) {
				content.AppendFormat ("&logintoken={0}", token);
				content.AppendFormat ("&logincaptcha={0}", captcha);
			}
			byte [] bytes = Encoding.UTF8.GetBytes (content.ToString ());

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (client_login_url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = bytes.Length;

			Stream output = request.GetRequestStream ();
			output.Write (bytes, 0, bytes.Length);
			output.Close ();

			HttpWebResponse response = null;
			try {
				response = (HttpWebResponse) request.GetResponse ();
			} catch (WebException wexc) {
				response = wexc.Response as HttpWebResponse;
				if (response == null)
					throw;
				ThrowOnError (response);
				throw; // if the method above does not throw, we do
			}

			//string sid = null;
			//string lsid = null;
			string auth = null;

			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				string s;
				while ((s = sr.ReadLine ()) != null) {
					if (s.StartsWith ("Auth="))
						auth = s.Substring (5);
					//else if (s.StartsWith ("LSID="))
					//	lsid = s.Substring (5);
					//else if (s.StartsWith ("SID="))
					//	sid = s.Substring (4);
				}
			}
			response.Close ();

			return auth;
		}

		static void ThrowOnError (HttpWebResponse response)
		{
			if (response.StatusCode != HttpStatusCode.Forbidden)
				return;

			string url = null;
			string token = null;
			string captcha_url = null;
			string code = null;
			using (StreamReader reader = new StreamReader (response.GetResponseStream ())) {
				string str;
				while ((str = reader.ReadLine ()) != null) {
					if (str.StartsWith ("Url=")) {
						url = str.Substring (4);
					} else if (str.StartsWith ("Error=")) {
						/* These are the values for Error
							None,
							BadAuthentication,
							NotVerified,
							TermsNotAgreed,
							CaptchaRequired,
							Unknown,
							AccountDeleted,
							AccountDisabled,
							ServiceUnavailable
						*/
						code = str.Substring (6);
					} else if (str.StartsWith ("CaptchaToken=")) {
						token = str.Substring (13);
					} else if (str.StartsWith ("CaptchaUrl=")) {
						captcha_url = str.Substring (11);
					}
				}
			}
			if (code == "CaptchaRequired" && token != null && captcha_url != null) {
				if (url != null) {
					Uri uri = new Uri (url);
					captcha_url = new Uri (uri, captcha_url).ToString ();
				} else if (!captcha_url.StartsWith ("https://")) {
					captcha_url = "https://www.google.com/accounts/" + captcha_url;
				}
				throw new CaptchaException (url, token, captcha_url);
			}

			throw new UnauthorizedAccessException (String.Format ("Access to '{0}' is denied ({1})", url, code));
		}
	}
}

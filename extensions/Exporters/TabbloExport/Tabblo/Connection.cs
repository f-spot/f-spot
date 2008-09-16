//
// Mono.Tabblo.Connection
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2008 Wojciech Dzierzanowski
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

using Mono.Unix;

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

using FSpot.Utils;

namespace Mono.Tabblo {

	public class Connection {

		private const string LoginUrl =
				"https://store.tabblo.com:443/studio/authtoken";
		private const string AuthorizeUrl = "https://store.tabblo.com"
				+ ":443/studio/upload/getposturl";
		private const string RedirUrl =	"http://www.tabblo.com/studio"
				+ "/token/{0}/?url=/studio"
				+ "/report_upload_session";

		private readonly IPreferences preferences;

		private string auth_token = null;
		private string session_upload_url = null;

		private CookieCollection cookies;


		public Connection (IPreferences preferences)
		{
			if (null == preferences) {
				throw new ArgumentNullException ("preferences");
			}
			this.preferences = preferences;
			this.cookies = new CookieCollection ();
		}


		public void UploadFile (string name, Stream data_stream,
		                        string mime_type, string [,] arguments)
		{
			if (!IsAuthenticated ()) {
				Login ();
			}

			Log.DebugFormat ("Uploading " + mime_type + " file " + name);
			DoUploadFile (name, data_stream, mime_type, arguments);
		}


		private void DoUploadFile (string name, Stream data_stream,
		                           string mime_type,
		                           string [,] arguments)
		{
			string upload_url = GetUploadUrl (arguments);
			HttpWebRequest http_request = CreateHttpRequest (
					upload_url, "POST", true);
			MultipartRequest request =
					new MultipartRequest (http_request);

			MemoryStream mem_stream = null;
			if (null != UploadProgressHandler) {
				// "Manual buffering" using a MemoryStream.
				request.Request.AllowWriteStreamBuffering =
						false;
				mem_stream = new MemoryStream ();
				request.OutputStream = mem_stream;
			}

			request.BeginPart (true);
			request.AddHeader ("Content-Disposition",
					"form-data; name=\"filename0\"; "
							+ "filename=\"" + name
							+ '"',
					false);
			request.AddHeader ("Content-Type", mime_type, true);

			byte [] data_buffer = new byte [8192];
			int read_count;
			while ((read_count = data_stream.Read (
					data_buffer, 0, data_buffer.Length))
							> 0) {
				request.WritePartialContent (
						data_buffer, 0, read_count);
			}
			request.EndPartialContent ();
			request.EndPart (true);

			if (null != UploadProgressHandler) {

				int total = (int) request.OutputStream.Length;
				request.Request.ContentLength = total;

				string progress_title = String.Format (
						Catalog.GetString ("Uploading "
								+ "photo "
								+ "\"{0}\""),
						name);

				using (Stream request_stream = request.Request
						.GetRequestStream ()) {
					byte [] buffer =
							mem_stream.GetBuffer ();
					int write_count = 0;
					for (int offset = 0; offset < total;
							offset += write_count) {
						FireUploadProgress (
								progress_title,
								offset, total);
						write_count = System.Math.Min (
								16384,
								total - offset);
						request_stream.Write (buffer,
								offset,
								write_count);
					}
					FireUploadProgress (progress_title,
							total, total);
				}
			}

			SendRequest ("upload", request.Request, true);
		}


		public event UploadProgressEventHandler UploadProgressHandler;

		private void FireUploadProgress (string title, int sent,
		                                 int total)
		{
			if (null != UploadProgressHandler) {
				UploadProgressEventArgs args =
						new UploadProgressEventArgs (
								title, sent,
								total);
				UploadProgressHandler (this, args);
			}
		}


		private bool IsAuthenticated ()
		{
			return null != auth_token;
		}


		private void Login ()
		{
			FireUploadProgress (Catalog.GetString (
						"Logging into Tabblo"),
					0, 0);

			auth_token = null;

			HttpWebRequest request = CreateHttpRequest (
					LoginUrl, "POST");
			request.ContentType =
					"application/x-www-form-urlencoded";

			string [,] arguments = {
				{"username", preferences.Username},
				{"password", preferences.Password}
			};

			try {
				WriteRequestContent (request,
						FormatRequestArguments (
								arguments));
				string response = SendRequest (
						"login", request);
				if ("BAD".Equals (response)) {
					Log.DebugFormat ("Invalid username or password");
					throw new TabbloException (
						"Login failed: Invalid username"
						+ " or password");
				}

				auth_token = response;

			} catch (TabbloException e) {
				// Here's us trying to produce a more
				// descriptive message when we have... trust
				// issues.  This doesn't work, though, at least
				// as long as Mono bug #346635 is not fixed.
				//
				// TODO: When it _starts_ to work, we should
				// think about doing the same for
				// `GetUploadUrl()'.
				WebException we = e.InnerException
						as WebException;
				if (null != we)
					Log.DebugFormat ("Caught a WebException, status=" + we.Status);
				if (null != we
					&& WebExceptionStatus.TrustFailure
							== we.Status) {
					throw new TabbloException (
							"Trust failure", we);
				}
				throw;
			}

			if  (null != auth_token)
				Log.DebugFormat  ("Login successful. Token: " + auth_token);
		}


		private string GetUploadUrl (string [,] arguments)
		{
			FireUploadProgress (Catalog.GetString (
						"Obtaining URL for upload"),
					0, 0);

			if (! IsAuthenticated ())
				Log.DebugFormat ("Not authenticated");

			if (null == session_upload_url) {

				string [,] auth_arguments =
						{ {"auth_token", auth_token} };
				string url = AuthorizeUrl + "/?"
						+ FormatRequestArguments (
								auth_arguments);

				HttpWebRequest request =
						CreateHttpRequest (url, "GET");

				string response = SendRequest (
						"getposturl", request);

				if (response.StartsWith ("@")) {
					session_upload_url =
							response.Substring (1);
				} else {
					throw new TabbloException (
							"Session upload URL "
							+ "retrieval failed");
				}
			}

			string upload_url = session_upload_url;
			upload_url += "&redir=" + String.Format (
					RedirUrl, auth_token);
			if (null != arguments && arguments.GetLength (0) > 0) {
				upload_url += '&' + FormatRequestArguments (
						arguments);
			}

			Log.DebugFormat ("Upload URL: " + upload_url);
			return upload_url;
		}


		private HttpWebRequest CreateHttpRequest (string url,
		                                          string method)
		{
			return CreateHttpRequest (url, method, false);
		}

		private HttpWebRequest CreateHttpRequest (string url,
		                                          string method,
		                                          bool with_cookies)
		{
			HttpWebRequest request = (HttpWebRequest)
					WebRequest.Create (url);
			// For some reason, POST requests are _really_ slow with
			// HTTP 1.1.
			request.ProtocolVersion = HttpVersion.Version10;
			request.Method = method;
			if (with_cookies) {
				HandleRequestCookies (request);
			}
			return request;
		}


		private void HandleRequestCookies (HttpWebRequest request)
		{
			request.CookieContainer = new CookieContainer ();
			// Instead of just doing a
			// `request.CookieContainer.Add(cookies)', add cookies
			// mannually to work around the fact that some cookies
			// are not properly formatted as they are received from
			// the server.
			foreach (Cookie c in cookies) {
				Cookie new_cookie = new Cookie (c.Name, c.Value,
						"/", ".tabblo.com");
				request.CookieContainer.Add (new_cookie);
			}

			string cookie_header = request.CookieContainer
					.GetCookieHeader (request.RequestUri);
			if (cookie_header.Length > 0)
				Log.DebugFormat ("Cookie: " + cookie_header);
		}


		private static void WriteRequestContent (HttpWebRequest request,
		                                         string content)
		{
			byte [] content_bytes =
					Encoding.UTF8.GetBytes (content);

			request.ContentLength = content_bytes.Length;

			try {
				using (Stream request_stream =
						request.GetRequestStream ()) {
					request_stream.Write (content_bytes, 0,
							content_bytes.Length);
				}
			} catch (WebException e) {
				Log.Exception (e);
				throw new TabbloException (
						"HTTP request failure: "
								+ e.Message,
						e);
			}

			char [] content_chars = new char [content_bytes.Length];
			content_bytes.CopyTo (content_chars, 0);
			Log.DebugFormat ("Request content: " + new string (content_chars));
		}


		private static string FormatRequestArguments (
				string [,] arguments)
		{
			StringBuilder content = new StringBuilder ();

			for (int i = 0; i < arguments.GetLength (0); ++i) {
				content.AppendFormat( "{0}={1}&",
						arguments [i, 0],
						arguments [i, 1]);
			}

			if (content.Length > 0) {
				content.Remove (content.Length - 1, 1);
			}

			byte [] content_bytes =	Encoding.UTF8.GetBytes (
					content.ToString ());
			char [] content_chars = new char [content_bytes.Length];
			content_bytes.CopyTo (content_chars, 0);

			return new string (content_chars);
		}


		private string SendRequest (string description,
		                            HttpWebRequest request)
		{
			return SendRequest (description, request, false);
		}

		/// <summary>
		/// Sends an HTTP request.
		/// </summary>
		/// <param name="description"></param>
		/// <param name="request"></param>
		/// <returns>the HTTP response as string</returns>
		private string SendRequest (string description,
		                            HttpWebRequest request,
		                            bool keep_cookies)
		{
			Log.DebugFormat ("Sending " + description + ' ' + request.Method + " request to " + request.Address);

			HttpWebResponse response = null;
			try {
				response = (HttpWebResponse)
						request.GetResponse ();
				if (keep_cookies) {
					cookies.Add (response.Cookies);
					Log.DebugFormat (response.Cookies.Count + " cookie(s)");
					foreach (Cookie c in response.Cookies) {
						Log.DebugFormat ("Set-Cookie: " + c.Name + '=' + c.Value + "; Domain=" + c.Domain + "; expires=" + c.Expires);
					}
				}
				return GetResponseAsString (response);
			} catch (WebException e) {
				Log.Exception (e);
				HttpWebResponse error_response =
						e.Response as HttpWebResponse;
				string response_string = null != error_response
						? GetResponseAsString (
								error_response)
						: "reason unknown";
				throw new TabbloException (description
							+ " failed: "
							+ response_string,
						e);
			} finally {
				if (null != response) {
					response.Close ();
				}
			}
		}


		private static string GetResponseAsString (HttpWebResponse response)
		{
			Log.DebugFormat ("Response: ");

			Encoding encoding = Encoding.UTF8;
			if (response.ContentEncoding.Length > 0) {
				try {
					encoding = Encoding.GetEncoding (
							response
							.ContentEncoding);
				} catch (ArgumentException) {
					// Swallow invalid encoding exception
					// and use the default one.
				}
			}

			string response_string = null;

			using (Stream stream = response.GetResponseStream ()) {
				StreamReader reader = new StreamReader (
						stream, encoding);
				response_string = reader.ReadToEnd ();
				stream.Close ();
			}

			if (null != response_string)
				try {
					Log.DebugFormat (response_string);
				} catch (System.FormatException e) {
					Log.DebugFormat ("Unable to print respose string: not in correct format");
				}
			return response_string;
		}
	}
}

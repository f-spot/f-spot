//
// FormClient.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2004-2008 Novell, Inc.
// Copyright (C) 2004, 2006 Larry Ewing
// Copyright (C) 2008 Stephane Delcroix
//  Copyright (c) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

using FSpot.Core;
using FSpot.Settings;

using Mono.Unix;

namespace FSpot.Exporters.Gallery
{
	public class FormClient
	{
		private struct FormItem {
			public string Name;
			public object Value;
	
			public FormItem (string name, object value) {
				Name = name;
				Value = value;
			}
		}
	
		private StreamWriter stream_writer;
		private List<FormItem> Items;

		private string boundary;
		private string start_boundary;
		private string end_boundary;
	
		private bool multipart = false;
		public bool Multipart {
			set { multipart = value; }
		}
	
		private bool first_item;
	
		public bool Buffer = false;
		public bool SuppressCookiePath = false;
	
		public bool expect_continue = true;
	
		public HttpWebRequest Request;
		public CookieContainer Cookies;
	
		public FSpot.ProgressItem Progress;
	
		public FormClient (CookieContainer cookies) 
		{
			this.Cookies = cookies;
			this.Items = new List<FormItem> ();
		}
		
		public FormClient ()
		{
			this.Items = new List<FormItem> ();
			this.Cookies = new CookieContainer ();
		}
		
		private void GenerateBoundary () 
		{
			Guid guid = Guid.NewGuid ();
			boundary = "--------" + guid.ToString () + "-----";
			start_boundary = "--" + boundary; 
			end_boundary = start_boundary + "--";
		}
		
		public void Add (string name, string value)
		{
			Items.Add (new FormItem (name, value));
		}
		
		public void Add (string name, FileInfo fileinfo)
		{
			multipart = true;
			Items.Add (new FormItem (name, fileinfo));
		}
	
		private void Write (FormItem item) {
			// The types we check here need to match the
			// types we allow in .Add
	
			if (item.Value == null) {
				Write (item.Name, (string)string.Empty);
			} else if (item.Value is FileInfo) {
				Write (item.Name, (FileInfo)item.Value);
			} else if (item.Value is string) {
				Write (item.Name, (string)item.Value);
			} else {
				throw new Exception ("Unknown value type");
			}
		}
	
		private long MultipartLength (FormItem item) {
			// The types we check here need to match the
			// types we allow in .Add
	
			if (item.Value == null) {
				return MultipartLength (item.Name, (string)string.Empty);
			} else if (item.Value is FileInfo) {
				return MultipartLength (item.Name, (FileInfo)item.Value);
			} else if (item.Value is string) {
				return MultipartLength (item.Name, (string)item.Value);
			} else {
				throw new Exception ("Unknown value type");
			}
		}
	
		private string MultipartHeader (string name, string value)
		{
			return string.Format ("{0}\r\n" + 
					      "Content-Disposition: form-data; name=\"{1}\"\r\n" +
					      "\r\n", start_boundary, name);
		}
	
		private long MultipartLength (string name, string value)
		{
			long length = MultipartHeader (name, value).Length;
			length += Encoding.Default.GetBytes (value).Length + 2;
			return length;
		}
	
		private void Write (string name, string value) 
		{
			string cmd;
			
			if (multipart) {
				cmd = string.Format ("{0}"
						     + "{1}\r\n",
						     MultipartHeader (name, value), value);
			} else {
				name = HttpUtility.UrlEncode (name.Replace(" ", "+"));
				value = HttpUtility.UrlEncode (value.Replace(" ", "+"));
				if (first_item) {
					cmd = string.Format ("{0}={1}", name, value);
					first_item = false;
				} else {
					cmd = string.Format ("&{0}={1}", name, value);
				}
			}
			//Console.WriteLine (cmd);
			stream_writer.Write  (cmd);
		}
	
		private string MultipartHeader (string name, FileInfo file)
		{
			string cmd = string.Format ("{0}\r\n"
						    + "Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\n"
						    + "Content-Type: image/jpeg\r\n"
						    + "\r\n", 
						    start_boundary, name, file.Name);
			return cmd;
		}
	
		private long MultipartLength (string name, FileInfo file)
		{
			long length = MultipartHeader (name, file).Length;
			length += file.Length + 2;
			return length;
		}
	
	       	private void Write (string name, FileInfo file)
		{
			if (multipart) {
				stream_writer.Write (MultipartHeader (name, file));
				stream_writer.Flush ();
				Stream stream = stream_writer.BaseStream;
				byte [] data = new byte [32768];
				FileStream fs = file.OpenRead ();
				long total = file.Length;
				long total_read = 0;
	
				int count;			
				while ((count = fs.Read (data, 0, data.Length)) > 0) {
					stream.Write (data, 0, count);
					total_read += count;
					if (Progress != null)
						Progress.Value = total_read / (double)total;
	
				}
				fs.Close ();
	
				stream_writer.Write ("\r\n");
			} else {
				throw new Exception ("Can't write files in url-encoded submissions");
			}
		}
	
	
		public void Clear () 
		{
			Items.Clear ();
			multipart = false;
		}

		public HttpWebResponse Submit (string url, FSpot.ProgressItem progress_item = null)
		{
			return Submit (new Uri (url), progress_item);
		}
		
		public HttpWebResponse Submit (Uri uri, FSpot.ProgressItem progress_item = null)
		{
			this.Progress = progress_item;
			Request = (HttpWebRequest) WebRequest.Create (uri);
			CookieCollection cookie_collection = Cookies.GetCookies (uri);
	
			if (uri.UserInfo != null && uri.UserInfo != string.Empty) {
				NetworkCredential cred = new NetworkCredential ();
				cred.GetCredential (uri, "basic");
				CredentialCache credcache = new CredentialCache();
				credcache.Add(uri, "basic", cred);
				
				Request.PreAuthenticate = true;
				Request.Credentials = credcache;	
			}
	
			Request.ServicePoint.Expect100Continue = expect_continue;
	
			Request.CookieContainer = new CookieContainer ();
			foreach (Cookie c in cookie_collection) {
				if (SuppressCookiePath) 
					Request.CookieContainer.Add (new Cookie (c.Name, c.Value));
				else
					Request.CookieContainer.Add (c);
			}
	
			Request.Method = "POST";
			Request.Headers["Accept-Charset"] = "utf-8;";
			Request.UserAgent = string.Format("F-Spot {0} (http://www.f-spot.org)", Defines.VERSION);
	
			if (multipart) {
				GenerateBoundary ();
				Request.ContentType = "multipart/form-data; boundary=" + boundary;
				Request.Timeout = Request.Timeout * 3;
	
				long length = 0;
				for (int i = 0; i < Items.Count; i++) {
					FormItem item = Items[i];
					
					length += MultipartLength (item);
				}
				length += end_boundary.Length + 2;
				
				//Request.Headers["My-Content-Length"] = length.ToString ();
				if (Buffer == false) {
					Request.ContentLength = length;	
					Request.AllowWriteStreamBuffering = false;
				}
			} else {
				Request.ContentType = "application/x-www-form-urlencoded";
			}
			
			stream_writer = new StreamWriter (Request.GetRequestStream ());
			
			first_item = true;
			for (int i = 0; i < Items.Count; i++) {
				FormItem item = Items[i];
				
				Write (item);
			}
			
			if (multipart)
				stream_writer.Write (end_boundary + "\r\n");
			
			stream_writer.Flush ();
			stream_writer.Close ();
	
			HttpWebResponse response; 
	
			try {
				response = (HttpWebResponse) Request.GetResponse ();
				
				//Console.WriteLine ("found {0} cookies", response.Cookies.Count);
				
				foreach (Cookie c in response.Cookies) {
					Cookies.Add (c);
				}
			} catch (WebException e) {
				if (e.Status == WebExceptionStatus.ProtocolError 
				    && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.ExpectationFailed && expect_continue) {
					e.Response.Close ();
					expect_continue = false;
					return Submit (uri, progress_item);
				}
				
				throw new WebException (Catalog.GetString ("Unhandled exception"), e);
			}
	
			return response;
		}
	}
}

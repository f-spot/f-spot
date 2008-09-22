using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;

namespace FSpot {
	public class FormClient {
		private struct FormItem {
			public string Name;
			public object Value;
	
			public FormItem (string name, object value) {
				Name = name;
				Value = value;
			}
		}
	
		private StreamWriter stream_writer;
		private ArrayList Items;
	
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
			this.Items = new ArrayList ();
		}
		
		public FormClient ()
		{
			this.Items = new ArrayList ();
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
				Write (item.Name, (string)String.Empty);
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
				return MultipartLength (item.Name, (string)String.Empty);
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
			length += value.Length + 2;
			return length;
		}
	
		private void Write (string name, string value) 
		{
			string cmd;
			
			if (multipart) {
				cmd = String.Format ("{0}"
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
	
		public HttpWebResponse Submit (string url)
		{
			return Submit (url, null);
		}
	
		public HttpWebResponse Submit (string url, FSpot.ProgressItem item)
		{
			return Submit (new Uri (url), item);
		}
		
		public HttpWebResponse Submit (Uri uri)
		{
			return Submit (uri, null);
		}
	
		public HttpWebResponse Submit (Uri uri, FSpot.ProgressItem progress_item) 
		{
			this.Progress = progress_item;
			Request = (HttpWebRequest) WebRequest.Create (uri);
			CookieCollection cookie_collection = Cookies.GetCookies (uri);
	
			if (uri.UserInfo != null && uri.UserInfo != String.Empty) {
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
			
			//Request.UserAgent = "F-Spot Gallery Remote Client";
			Request.UserAgent = "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.7) Gecko/20040626 Firefox/0.9.1";
	
			Request.Proxy = WebProxy.GetDefaultProxy ();
	
			if (multipart) {
				GenerateBoundary ();
				Request.ContentType = "multipart/form-data; boundary=" + boundary;
				Request.Timeout = Request.Timeout * 3;
	
				long length = 0;
				for (int i = 0; i < Items.Count; i++) {
					FormItem item = (FormItem)Items[i];
					
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
				FormItem item = (FormItem)Items[i];
				
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
				
				throw new WebException (Mono.Unix.Catalog.GetString ("Unhandled exception"), e);
			}
	
			return response;
		}
	}
}

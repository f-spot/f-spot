using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;

class FormClient {
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
	private bool first_item;

	public HttpWebRequest Request;
	public CookieContainer Cookies;

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
		// FIXME this shouldn't really be hardcoded, look in camel/camel-mime-utils.c
		// for a boundary algo.  camel_header_msgid_generate ()

		boundary = "--------ieoau._._+2_8_GoodLuck8.3-ds0d0J0S0Kl234324jfLdsjfdAuaoei-----";
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

		if (item.Value is FileInfo) {
			Write (item.Name, (FileInfo)item.Value);
		} else if (item.Value is string) {
			Write (item.Name, (string)item.Value);
		} else {
			throw new Exception ("Unknown value type");
		}
	}

	private void Write (string name, string value) 
	{
		string cmd;
		
		if (multipart) {
			cmd = string.Format ("{0}\r\n"
					     + "Content-Disposition: form-data; name=\"{1}\"\r\n"
					     + "\r\n"
					     + "{2}\r\n",
					     start_boundary, name, value);
		} else {
			if (first_item) {
				cmd = string.Format ("{0}={1}", name, HttpUtility.UrlEncode (value));
				first_item = false;
			} else {
				cmd = string.Format ("&{0}={1}", name, HttpUtility.UrlEncode (value));
			}
		}
		//Console.WriteLine (cmd);
		stream_writer.Write  (cmd);
	}

       	private void Write (string name, FileInfo file)
	{
		if (multipart) {
			string cmd = string.Format ("{0}\r\n"
						    + "Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\n"
						    + "Content-Type: image/jpeg\r\n"
						    + "\r\n", 
						    start_boundary, name, file.Name);

			stream_writer.Write (cmd);
			stream_writer.Flush ();

			Stream stream = stream_writer.BaseStream;
			Byte [] data = new Byte [4096];
			FileStream fs = file.OpenRead ();
			int count;
			while ((count = fs.Read (data, 0, 4096)) > 0) {
				stream.Write (data, 0, count);
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
		return Submit (new Uri (url));
	}

	public HttpWebResponse Submit (Uri uri) 
	{
		Request = (HttpWebRequest) WebRequest.Create (uri);
		Request.CookieContainer = Cookies;		
		Request.Method = "POST";
		Request.Headers["Accept-Charset"] = "utf-8";
		Request.UserAgent = "F-Spot Gallery Remote Client";
		
		if (multipart) {
			GenerateBoundary ();
			Request.ContentType = "multipart/form-data; boundary=" + boundary;
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
		
		
		HttpWebResponse response = (HttpWebResponse) Request.GetResponse ();

		foreach (Cookie c in response.Cookies) {
			Cookies.Add (c);
		}
		return response;
	}
}

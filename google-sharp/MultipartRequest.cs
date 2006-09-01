//
// Mono.Google.MultipartRequest
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
using System.Text;
using System.Xml;

namespace Mono.Google {
	class MultipartRequest {
		static byte [] crlf = new byte [] { 13, 10 };
		HttpWebRequest request;
		Stream output_stream;
		byte [] bound_bytes;
		byte [] bound_end_bytes;
		bool output_set;

		public MultipartRequest (string url)
		{
			string hash = (((long) url.GetHashCode () << 32) + GetHashCode ()).ToString ("X");
			string bound_str1 = "---------------------" + hash;
			string bound_head = "--" + bound_str1 + "\r\n";
			string bound_end = "--" + bound_str1 + "--\r\n";
			bound_bytes = Encoding.ASCII.GetBytes (bound_head);
			bound_end_bytes = Encoding.ASCII.GetBytes (bound_end);
			request = (HttpWebRequest) WebRequest.Create (url);
			request.Method = "POST";
			request.ContentType = "multipart/form-data; boundary=" + bound_str1;
		}

		public HttpWebRequest Request {
			get { return request; }
		}

		public Stream OutputStream {
			get { return output_stream; }
			set {
				output_set = true;
				output_stream = value;
			}
		}

		public void BeginPart ()
		{
			if (output_stream == null)
				output_stream = request.GetRequestStream ();

			output_stream.Write (bound_bytes, 0, bound_bytes.Length);
		}

		public void AddHeader (string name, string val)
		{
			AddHeader (name, val, false);
		}

		public void AddHeader (string name, string val, bool last)
		{
			AddHeader (String.Format ("{0}: {1}"), last);
		}

		public void AddHeader (string header)
		{
			AddHeader (header, false);
		}

		public void AddHeader (string header, bool last)
		{
			bool need_crlf = !header.EndsWith ("\r\n");
			byte [] bytes = Encoding.UTF8.GetBytes (header);
			output_stream.Write (bytes, 0, bytes.Length);
			if (need_crlf)
				output_stream.Write (crlf, 0, 2);
			if (last)
				output_stream.Write (crlf, 0, 2);
		}

		public void WriteContent (string content)
		{
			WriteContent (Encoding.UTF8.GetBytes (content));
		}

		public void WriteContent (byte [] content)
		{
			output_stream.Write (content, 0, content.Length);
			output_stream.Write (crlf, 0, crlf.Length);
		}

		public void WritePartialContent (byte [] content, int offset, int nbytes)
		{
			output_stream.Write (content, offset, nbytes);
		}

		public void EndPartialContent ()
		{
			output_stream.Write (crlf, 0, crlf.Length);
		}

		public void EndPart (bool last)
		{
			if (last) {
				output_stream.Write (bound_end_bytes, 0, bound_end_bytes.Length);
				if (!output_set)
					output_stream.Close ();
			} else {
				output_stream.Write (bound_bytes, 0, bound_bytes.Length);
			}
		}

		public string GetResponseAsString ()
		{
			HttpWebResponse response = null;
			response = (HttpWebResponse) request.GetResponse ();
			string received = "";
			// FIXME: use CharacterSet?
			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				received = sr.ReadToEnd ();
			}
			response.Close ();
			return received;
		}
	}
}


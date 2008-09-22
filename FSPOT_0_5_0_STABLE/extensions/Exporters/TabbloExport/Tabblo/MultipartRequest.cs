//
// Mono.Tabblo.MultipartRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Stephane Delcroix (stephane@delcroix.org)
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 S. Delcroix
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

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

using FSpot.Utils;

namespace Mono.Tabblo {

	class MultipartRequest {

		private const string VerboseSymbol =
				"FSPOT_TABBLO_EXPORT_VERBOSE";

		private static readonly byte [] CRLF = { 13, 10 };

		private const string SeparatorString = "PART_SEPARATOR";
		private const string Separator =
				"--" + SeparatorString + "\r\n";
		private const string SeparatorEnd =
				"--" + SeparatorString + "--\r\n";

		private static readonly byte [] SeparatorBytes =
				Encoding.ASCII.GetBytes (Separator);
		private static readonly byte [] SeparatorEndBytes =
				Encoding.ASCII.GetBytes (SeparatorEnd);

		private HttpWebRequest request;
		private Stream output_stream;

		bool output_set;


		public MultipartRequest (HttpWebRequest http_request)
		{
			request = http_request;
			request.ContentType = "multipart/form-data; boundary="
					+ SeparatorString;
		}


		public HttpWebRequest Request {
			get {
				return request;
			}
		}

		public Stream OutputStream {
			get {
				return output_stream;
			}
			set {
				output_set = true;
				output_stream = value;
			}
		}


		public void BeginPart ()
		{
			BeginPart (false);
		}

		public void BeginPart (bool first)
		{
			if (!first) {
				return;
			}

			LogBeginPart ();
			InitContent ();
			AppendContent (SeparatorBytes, 0,
					SeparatorBytes.Length);
		}


		public void AddHeader (string name, string val)
		{
			AddHeader (name, val, false);
		}

		public void AddHeader (string name, string val, bool last)
		{
			AddHeader (String.Format ("{0}: {1}", name, val), last);
		}

		public void AddHeader (string header)
		{
			AddHeader (header, false);
		}

		public void AddHeader (string header, bool last)
		{
			bool need_crlf = !header.EndsWith ("\r\n");
			byte [] bytes = Encoding.UTF8.GetBytes (header);
			AppendContent (bytes, 0, bytes.Length);
			if (need_crlf) {
				AppendContent (CRLF, 0, CRLF.Length);
			}
			if (last) {
				AppendContent (CRLF, 0, CRLF.Length);
			}
		}


		public void WriteContent (string content)
		{
			WriteContent (Encoding.UTF8.GetBytes (content));
		}

		public void WriteContent (byte [] content)
		{
			AppendContent (content, 0, content.Length);
			AppendContent (CRLF, 0, CRLF.Length);
		}

		public void WritePartialContent (byte [] content, int offset,
		                                 int nbytes)
		{
			AppendContent (content, offset, nbytes);
		}


		public void EndPartialContent ()
		{
			AppendContent (CRLF, 0, CRLF.Length);
		}

		public void EndPart (bool last)
		{
			if (last) {
				LogEndPart ();
				AppendContent (SeparatorEndBytes, 0,
						SeparatorEndBytes.Length);
				CloseContent ();
			} else {
				AppendContent (SeparatorBytes, 0,
						SeparatorBytes.Length);
			}
		}


		private void InitContent ()
		{
			if (output_stream == null) {
				output_stream = request.GetRequestStream ();
			}
		}

		private void CloseContent ()
		{
			if (!output_set) {
				output_stream.Close ();
			}
		}

		private void AppendContent (byte [] content, int offset,
		                            int length)
		{
			LogContent (content, offset, length);
			output_stream.Write (content, offset, length);
		}



		[Conditional (VerboseSymbol)]
		private static void LogBeginPart ()
		{
			Log.DebugFormat (">>>START MultipartRequest content");
		}

		[Conditional (VerboseSymbol)]
		private static void LogEndPart ()
		{
			Log.DebugFormat ("<<<END MultipartRequest content");
		}

		[Conditional (VerboseSymbol)]
		private static void LogContent (byte [] content, int offset,
		                                int length)
		{
			char [] content_chars = new char [length];
			Array.Copy (content, offset, content_chars, 0, length);
			Log.DebugFormat (new string (content_chars));
		}
	}
}

//
// Mono.Tabblo.Picture
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

using System;
using System.IO;

namespace Mono.Tabblo {

	public class Picture {

		private readonly string name;
		private readonly Uri uri;
		private readonly string mime_type;
		private readonly string privacy;


		public string Name {
			get {
				return name;
			}
		}
		public Uri Uri {
			get {
				return uri;
			}
		}
		public string MimeType {
			get {
				return mime_type;
			}
		}
		public string Privacy {
			get {
				return privacy;
			}
		}


		public Picture (string name, Uri uri, string mime_type,
		                string privacy)
		{
			if (null == name) {
				throw new ArgumentNullException ("name");
			}
			if (null == uri) {
				throw new ArgumentNullException ("uri");
			}
			if (null == mime_type) {
				throw new ArgumentNullException ("mime_type");
			}
			if (null == privacy) {
				throw new ArgumentNullException ("privacy");
			}
			this.name = name;
			this.uri = uri;
			this.mime_type = mime_type;
			this.privacy = privacy;
		}


		public void Upload (Connection connection)
		{
			if (null == connection) {
				throw new ArgumentNullException ("connection");
			}

			using (Stream data_stream =
					File.OpenRead (Uri.LocalPath)) {
				connection.UploadFile (Name, data_stream,
						MimeType, null);
			}
		}
	}
}

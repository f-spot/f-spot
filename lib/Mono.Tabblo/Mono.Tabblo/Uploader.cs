//
// Mono.Tabblo.Uploader
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2009 Wojciech Dzierzanowski
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
using System.Diagnostics;
using System.IO;

namespace Mono.Tabblo {

	public class Uploader {

		private readonly Connection connection;
		private readonly IPreferences preferences;
		
		
		public Uploader (IPreferences preferences)
		{
			if (null == preferences) {
				throw new ArgumentNullException ("preferences"); 
			}
			connection = new Connection (preferences);
			this.preferences = preferences;
		}


		public event UploadProgressEventHandler ProgressChanged {
			add {
				connection.UploadProgressChanged += value;
			}
			remove {
				connection.UploadProgressChanged -= value;
			}
		}
		
		
		public void Upload (Picture picture)
		{
			if (null == picture) {
				throw new ArgumentNullException ("picture"); 
			}

			string tags = GetTagsAsString (picture);
			string [,] arguments = {
				{"security", preferences.Privacy},
				{"tags", tags},
			};
			
			using (Stream data_stream =
					File.OpenRead (picture.Uri.LocalPath)) {
				Debug.WriteLine ("NEW UPLOAD: "
						+ picture.Uri.LocalPath);
				connection.UploadFile (picture.Name,
						data_stream, picture.MimeType,
						arguments);
			}
		}
		
		
		private static string GetTagsAsString (Picture picture)
		{
			Debug.Assert (null != picture);
			return String.Join (",", picture.Tags);
		}
	}
}

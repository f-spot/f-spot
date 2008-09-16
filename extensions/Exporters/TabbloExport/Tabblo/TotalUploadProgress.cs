//
// Mono.Tabblo.TotalUploadProgress
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
using System.Diagnostics;

using FSpot.Utils;

namespace Mono.Tabblo {

	public class TotalUploadProgress {

		private long prev_bytes_sent = 0;
		private long bytes_sent_total = 0;

		private int total_file_count;
		private ulong total_file_size;


		public TotalUploadProgress (Picture [] pictures)
		{
			if (null == pictures) Log.DebugFormat ("No pictures!");

			total_file_count = pictures.Length;
			total_file_size = GetTotalFileSize (pictures);
		}


		protected int TotalFileCount {
			get {
				return total_file_count;
			}
		}
		protected ulong TotalFileSize {
			get {
				return total_file_size;
			}
		}


		public void HandleProgress (object sender,
		                            UploadProgressEventArgs args)
		{
			bytes_sent_total += args.BytesSent - prev_bytes_sent;
			ShowProgress (args.Title, bytes_sent_total);

			int percent = (int)
					((double) bytes_sent_total * 100
							/ TotalFileSize);
			Log.DebugFormat ("{0}%...", percent);

			if (args.BytesTotal == args.BytesSent) {
				prev_bytes_sent = 0;
			} else {
				prev_bytes_sent = args.BytesSent;
			}
		}


		/// <summary>
		/// Overriden by subclasses to display upload progress.
		/// </summary>
		/// <remarks>
		/// The default implementation does nothing.
		/// </remarks>
		protected virtual void ShowProgress (string title,
		                                     long bytes_sent)
		{
		}


		private static ulong GetTotalFileSize (Picture [] pictures)
		{
			if (null == pictures) Log.DebugFormat ("No pictures!");

			ulong size = 0;

			foreach (Picture picture in pictures) {
				FileInfo info = new FileInfo (
						picture.Uri.LocalPath);
				size += (ulong) info.Length;
			}

			return size;
		}
	}
}

//
// Mono.Google.Picasa.UploadProgressEventArgs
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
namespace Mono.Google.Picasa {
	public sealed class UploadProgressEventArgs : EventArgs {
		string title;
		long bytes_sent;
		long bytes_total;

		internal UploadProgressEventArgs (string title, long bytes_sent, long bytes_total)
		{
			this.title = title;
			this.bytes_sent = bytes_sent;
			this.bytes_total = bytes_total;
		}

		public string Title {
			get { return title; }
		}

		public long BytesSent {
			get { return bytes_sent; }
		}

		public long BytesTotal {
			get { return bytes_total; }
		}
	}
}

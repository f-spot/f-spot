//
// HttpDownloaderState.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Hyena.Downloader
{
	public class HttpDownloaderState
	{
		public DateTime StartTime { get; internal set; }
		public DateTime FinishTime { get; internal set; }
		public double PercentComplete { get; internal set; }
		public double TransferRate { get; internal set; }
		public Buffer Buffer { get; internal set; }
		public long TotalBytesRead { get; internal set; }
		public long TotalBytesExpected { get; internal set; }
		public bool Success { get; internal set; }
		public bool Working { get; internal set; }
		public string ContentType { get; internal set; }
		public string CharacterSet { get; internal set; }
		public Exception FailureException { get; internal set; }

		public override string ToString ()
		{
			if (Working) {
				return string.Format ("HttpDownloaderState: working ({0}% complete)", PercentComplete * 100.0);
			} else {
				return string.Format ("HttpDownloaderState: finished, {0}", Success ? "successful" : "error: " + FailureException.Message);
			}
		}
	}
}

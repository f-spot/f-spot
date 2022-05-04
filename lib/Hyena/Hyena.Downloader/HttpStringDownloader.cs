//
// HttpStringDownloader.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Hyena.Downloader
{
	public class HttpStringDownloader : HttpDownloader
	{
		Encoding detected_encoding;

		public string Content { get; private set; }
		public Encoding Encoding { get; set; }
		public new Action<HttpStringDownloader> Finished { get; set; }

		protected override void OnBufferUpdated ()
		{
			var default_encoding = Encoding.UTF8;

			if (detected_encoding == null && !string.IsNullOrEmpty (State.CharacterSet)) {
				try {
					detected_encoding = Encoding.GetEncoding (State.CharacterSet);
				} catch {
				}
			}

			if (detected_encoding == null) {
				detected_encoding = default_encoding;
			}

			Content += (Encoding ?? detected_encoding).GetString (State.Buffer.Data, 0, State.Buffer.Length);
		}

		protected override void OnFinished ()
		{
			var handler = Finished;
			if (handler != null) {
				try {
					handler (this);
				} catch (Exception e) {
					Log.Exception (string.Format ("HttpStringDownloader.Finished handler ({0})", Uri), e);
				}
			}

			base.OnFinished ();
		}
	}
}

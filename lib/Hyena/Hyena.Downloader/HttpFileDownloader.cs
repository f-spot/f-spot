//
// HttpFileDownloader.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.IO;

namespace Hyena.Downloader
{
	public class HttpFileDownloader : HttpDownloader
	{
		FileStream file_stream;

		public string TempPathRoot { get; set; }
		public string FileExtension { get; set; }
		public string LocalPath { get; private set; }

		public event Action<HttpFileDownloader> FileFinished;

		public HttpFileDownloader ()
		{
			TempPathRoot = System.IO.Path.GetTempPath ();
		}

		protected override void OnStarted ()
		{
			Directory.CreateDirectory (TempPathRoot);
			LocalPath = Path.Combine (TempPathRoot, CryptoUtil.Md5Encode (Uri.AbsoluteUri));
			if (!string.IsNullOrEmpty (FileExtension)) {
				LocalPath += "." + FileExtension;
			}
			base.OnStarted ();
		}

		protected override void OnBufferUpdated ()
		{
			if (file_stream == null) {
				file_stream = new FileStream (LocalPath, FileMode.Create, FileAccess.Write);
			}
			file_stream.Write (State.Buffer.Data, 0, (int)State.Buffer.Length);
			base.OnBufferUpdated ();
		}

		protected override void OnFinished ()
		{
			if (file_stream != null) {
				try {
					file_stream.Close ();
					file_stream = null;
					OnFileFinished ();
				} catch (Exception e) {
					Log.Exception (string.Format ("HttpFileDownloader.OnFinished ({0})", Uri), e);
				}
			}

			base.OnFinished ();
		}

		protected virtual void OnFileFinished ()
		{
			FileFinished?.Invoke (this);
		}
	}
}

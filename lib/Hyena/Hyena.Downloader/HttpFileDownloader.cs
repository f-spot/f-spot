// 
// HttpFileDownloader.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;

namespace Hyena.Downloader
{
    public class HttpFileDownloader : HttpDownloader
    {
        private FileStream file_stream;

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
            if (!String.IsNullOrEmpty (FileExtension)) {
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
                    Log.Exception (String.Format ("HttpFileDownloader.OnFinished ({0})", Uri), e);
                }
            }

            base.OnFinished ();
        }

        protected virtual void OnFileFinished ()
        {
            var handler = FileFinished;
            if (handler != null) {
                handler (this);
            }
        }
    }
}

// 
// HttpStringDownloader.cs
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
using System.Text;

namespace Hyena.Downloader
{
    public class HttpStringDownloader : HttpDownloader
    {
        private Encoding detected_encoding;

        public string Content { get; private set; }
        public Encoding Encoding { get; set; }
        public new Action<HttpStringDownloader> Finished { get; set; }

        protected override void OnBufferUpdated ()
        {
            var default_encoding = Encoding.UTF8;

            if (detected_encoding == null && !String.IsNullOrEmpty (State.CharacterSet)) {
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
                    Log.Exception (String.Format ("HttpStringDownloader.Finished handler ({0})", Uri), e);
                }
            }

            base.OnFinished ();
        }
    }
}

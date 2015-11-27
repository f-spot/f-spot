// 
// HttpDownloaderState.cs
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
                return String.Format ("HttpDownloaderState: working ({0}% complete)", PercentComplete * 100.0);
            } else {
                return String.Format ("HttpDownloaderState: finished, {0}", Success ? "successful" : "error: " + FailureException.Message);
            }
        }
    }
}

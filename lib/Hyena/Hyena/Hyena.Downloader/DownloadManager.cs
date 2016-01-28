// 
// DownloadManager.cs
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
using System.Collections.Generic;

namespace Hyena.Downloader
{
    public class DownloadManager
    {
        private object sync_root = new object ();
        internal object SyncRoot {
            get { return sync_root; }
        }

        private Queue<HttpDownloader> pending_downloaders = new Queue<HttpDownloader> ();
        private List<HttpDownloader> active_downloaders = new List<HttpDownloader> ();

        protected Queue<HttpDownloader> PendingDownloaders {
            get { return pending_downloaders; }
        }

        internal List<HttpDownloader> ActiveDownloaders {
            get { return active_downloaders; }
        }

        public event Action<HttpDownloader> Started;
        public event Action<HttpDownloader> Finished;
        public event Action<HttpDownloader> Progress;
        public event Action<HttpDownloader> BufferUpdated;

        public int MaxConcurrentDownloaders { get; set; }

        public int PendingDownloadCount {
            get { return pending_downloaders.Count; }
        }

        public int ActiveDownloadCount {
            get { return active_downloaders.Count; }
        }

        public int TotalDownloadCount {
            get { return PendingDownloadCount + ActiveDownloadCount; }
        }

        public DownloadManager ()
        {
            MaxConcurrentDownloaders = 2;
        }

        public void QueueDownloader (HttpDownloader downloader)
        {
            lock (SyncRoot) {
                pending_downloaders.Enqueue (downloader);
                Update ();
            }
        }

        public void WaitUntilFinished ()
        {
            while (TotalDownloadCount > 0);
        }

        private void Update ()
        {
            lock (SyncRoot) {
                while (pending_downloaders.Count > 0 && active_downloaders.Count < MaxConcurrentDownloaders) {
                    var downloader = pending_downloaders.Peek ();
                    downloader.Started += OnDownloaderStarted;
                    downloader.Finished += OnDownloaderFinished;
                    downloader.Progress += OnDownloaderProgress;
                    downloader.BufferUpdated += OnDownloaderBufferUpdated;
                    active_downloaders.Add (downloader);
                    pending_downloaders.Dequeue ();
                    downloader.Start ();
                }
            }
        }

        protected virtual void OnDownloaderStarted (HttpDownloader downloader)
        {
            var handler = Started;
            if (handler != null) {
                handler (downloader);
            }
        }

        protected virtual void OnDownloaderFinished (HttpDownloader downloader)
        {
            lock (SyncRoot) {
                active_downloaders.Remove (downloader);
                Update ();
            }

            var handler = Finished;
            if (handler != null) {
                handler (downloader);
            }
        }

        protected virtual void OnDownloaderProgress (HttpDownloader downloader)
        {
            var handler = Progress;
            if (handler != null) {
                handler (downloader);
            }
        }

        protected virtual void OnDownloaderBufferUpdated (HttpDownloader downloader)
        {
            var handler = BufferUpdated;
            if (handler != null) {
                handler (downloader);
            }
        }
    }
}

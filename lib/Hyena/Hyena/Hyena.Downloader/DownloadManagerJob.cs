//
// DownloadManagerJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;

namespace Hyena.Downloader
{
    public class DownloadManagerJob : Hyena.Jobs.Job
    {
        private DownloadManager manager;
        private int finished_count = 0;

        public DownloadManagerJob (DownloadManager manager)
        {
            this.manager = manager;
            manager.Progress += OnDownloaderProgress;
            manager.Finished += OnDownloaderFinished;
        }

        private void OnDownloaderProgress (HttpDownloader downloader)
        {
            FreezeUpdate ();

            lock (manager.SyncRoot) {
                double weight = 1.0 / (manager.TotalDownloadCount + finished_count);
                double progress = finished_count * weight;
                double speed = 0;
                int count = 0;
                foreach (var active_downloader in manager.ActiveDownloaders) {
                    progress += weight * active_downloader.State.PercentComplete;
                    speed = active_downloader.State.TransferRate;
                    count++;
                }
                Progress = progress;

                var human_speed = new Hyena.Query.FileSizeQueryValue ((long)Math.Round (speed)).ToUserQuery ();
                if (manager.PendingDownloadCount == 0) {
                    Status = String.Format (
                        Catalog.GetPluralString (
                            "{0} download at {1}/s",
                            "{0} downloads at {1}/s",
                            count),
                        count, human_speed
                    );
                } else {
                    Status = String.Format (
                        Catalog.GetPluralString (
                            "{0} download at {1}/s ({2} pending)",
                            "{0} downloads at {1}/s ({2} pending)",
                            count),
                        count, human_speed, manager.PendingDownloadCount
                    );
                }
            }

            ThawUpdate (true);
        }

        private void OnDownloaderFinished (HttpDownloader downloader)
        {
            lock (manager.SyncRoot) {
                finished_count++;
                if (manager.TotalDownloadCount <= 0) {
                    OnFinished ();
                }
            }
        }
    }
}

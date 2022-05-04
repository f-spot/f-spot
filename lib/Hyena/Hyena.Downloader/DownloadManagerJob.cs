//
// DownloadManagerJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using FSpot.Resources.Lang;

namespace Hyena.Downloader
{
	public class DownloadManagerJob : Hyena.Jobs.Job
	{
		DownloadManager manager;
		int finished_count = 0;

		public DownloadManagerJob (DownloadManager manager)
		{
			this.manager = manager;
			manager.Progress += OnDownloaderProgress;
			manager.Finished += OnDownloaderFinished;
		}

		void OnDownloaderProgress (HttpDownloader downloader)
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
					Status = string.Format (count <= 1 ? Strings.DownloadXAtYSeconds : Strings.DownloadsXAtYSeconds, count, human_speed);
				} else {
					Status = string.Format (count <= 1 ? Strings.DownloadXAtYSecondsZPending : Strings.DownloadsXAtYSecondsZPending,
						count, human_speed, manager.PendingDownloadCount);
				}
			}

			ThawUpdate (true);
		}

		void OnDownloaderFinished (HttpDownloader downloader)
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

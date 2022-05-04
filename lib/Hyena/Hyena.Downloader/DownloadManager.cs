//
// DownloadManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

namespace Hyena.Downloader
{
	public class DownloadManager
	{
		object sync_root = new object ();
		internal object SyncRoot {
			get { return sync_root; }
		}

		Queue<HttpDownloader> pending_downloaders = new Queue<HttpDownloader> ();
		List<HttpDownloader> active_downloaders = new List<HttpDownloader> ();

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
			while (TotalDownloadCount > 0) ;
		}

		void Update ()
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
			Started?.Invoke (downloader);
		}

		protected virtual void OnDownloaderFinished (HttpDownloader downloader)
		{
			lock (SyncRoot) {
				active_downloaders.Remove (downloader);
				Update ();
			}

			Finished?.Invoke (downloader);
		}

		protected virtual void OnDownloaderProgress (HttpDownloader downloader)
		{
			Progress?.Invoke (downloader);
		}

		protected virtual void OnDownloaderBufferUpdated (HttpDownloader downloader)
		{
			BufferUpdated?.Invoke (downloader);
		}
	}
}

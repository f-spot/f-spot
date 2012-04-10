//
// PixbufCache.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2006 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.Threading;

using FSpot.Utils;

using Hyena;

namespace FSpot
{
	public class PixbufCache
	{
		Dictionary<SafeUri,CacheEntry> items;
		List<CacheEntry> items_mru;
		int total_size;
		int max_size = 256 * 256 * 4 * 30;

		private Thread worker;

		public delegate void PixbufLoadedHandler (PixbufCache cache, CacheEntry entry);
		public event PixbufLoadedHandler OnPixbufLoaded;

		public PixbufCache ()
		{
			items = new Dictionary<SafeUri, CacheEntry> ();
			items_mru = new List<CacheEntry> ();

			worker = new Thread (new ThreadStart (WorkerTask));
			worker.Start ();

			ThumbnailLoader.Default.OnPixbufLoaded += HandleThumbnailLoaded;
		}

		public void HandleThumbnailLoaded (ImageLoaderThread loader, ImageLoaderThread.RequestItem result)
		{
						Reload (result.Uri);
		}

		public void Request (SafeUri uri, object closure, int width, int height)
		{
			lock (items) {
				CacheEntry entry = null;
				if (items.ContainsKey(uri))
					entry = items[uri];

				if (entry == null) {
					entry = new CacheEntry (this, uri, closure, width, height);
					items [uri] = entry;
					items_mru.Add (entry);
				} else {
					MoveForward (entry);
					entry.Data = closure;
				}
				Monitor.Pulse (items);
			}
		}

//		public void Update (SafeUri uri, Gdk.Pixbuf pixbuf)
//		{
//			lock (items) {
//				CacheEntry entry = items [uri];
//				if (entry != null) {
//					entry.SetPixbufExtended (pixbuf, true);
//				}
//			}
//		}

		public void Update (CacheEntry entry, Gdk.Pixbuf pixbuf)
		{
			lock (items) {
				entry.SetPixbufExtended (pixbuf, true);
			}
		}

		public void Reload (CacheEntry entry, object data, int width, int height)
		{
			lock (items) {
				lock (entry) {
					entry.Reload = true;
					entry.Width = width;
					entry.Height = height;
					entry.Data = data;
				}
				Monitor.Pulse (items);
			}
		}

		public void Reload (SafeUri uri)
		{
			CacheEntry entry = null;

			lock (items) {
				if (items.ContainsKey(uri)){
					entry = items [uri];
					lock (entry) {
						entry.Reload = true;
					}
					Monitor.Pulse (items);
				}
			}
		}

		private CacheEntry FindNext ()
		{
			CacheEntry entry;
			int i = items_mru.Count;
			int size = 0;
			if (total_size > max_size * 4) {
				//System.Console.WriteLine ("Hit major limit ({0}) out of {1}",
				//			  total_size, max_size);
				return null;
			}
			while (i-- > 0) {
				entry = items_mru [i];
				lock (entry) {
					if (entry.Reload) {
						entry.Reload = false;
						return entry;
					}

					//if the depth of the queue is so large that we've reached double our limit
					//break out of here and let the queue shrink.
					if (entry.Pixbuf != null)
						size += entry.Size;

					if (size > max_size * 2) {
						//System.Console.WriteLine ("Hit limit ({0},{1}) out of {2}",
						//			  size, total_size,max_size);
						return null;
					}
				}
			}
			return null;
		}

		private bool ShrinkIfNeeded ()
		{
			int num = 0;
			while ((items_mru.Count - num) > 10 && total_size > max_size) {
				CacheEntry entry = items_mru [num++];
				items.Remove (entry.Uri);
				entry.Dispose ();
			}
			if (num > 0) {
				//System.Console.WriteLine ("removing {0} out of {3}  ({1} > {2})",
				//			  num, total_size, max_size, items_mru.Count);
				items_mru.RemoveRange (0, num);
				return true;
			}
			return false;
		}

		private void WorkerTask ()
		{
			CacheEntry current = null;
			//ThumbnailGenerator.Default.PushBlock ();
			while (true) {
				try {
					lock (items) {
						/* find the next item */
						while ((current = FindNext ()) == null) {
							if (!ShrinkIfNeeded ()){
								//ThumbnailGenerator.Default.PopBlock ();
								Monitor.Wait (items);
								//ThumbnailGenerator.Default.PushBlock ();
							}
						}
					}
					ProcessRequest (current);
					QueueLast (current);
				} catch (System.Exception e) {
					Log.Exception (e);
					current = null;
				}
			}
		}

		protected virtual void ProcessRequest (CacheEntry entry)
		{
			Gdk.Pixbuf loaded = null;
			try {
				loaded = XdgThumbnailSpec.LoadThumbnail (entry.Uri, ThumbnailSize.Large);
				this.Update (entry, loaded);
			} catch (GLib.GException){
				if (loaded != null)
					loaded.Dispose ();
			}
		}

		private void QueueLast (CacheEntry entry)
		{
			ThreadAssist.ProxyToMain (() => {
				if (entry.Uri != null && OnPixbufLoaded != null)
					OnPixbufLoaded (this, entry);
			});
		}

		private void MoveForward (CacheEntry entry)
		{
#if true
			int i = items_mru.Count;
			CacheEntry tmp1 = entry;
			CacheEntry tmp2;
			while (i-- > 0) {
				tmp2 = items_mru [i];
				items_mru [i] = tmp1;
				tmp1 = tmp2;
				if (tmp2 == entry)
					return;
			}
			throw new System.Exception ("move forward failed");
#else
			items_mru.Remove (entry);
			items_mru.Add (entry);
#endif
		}


		private CacheEntry ULookup (SafeUri uri)
		{
			CacheEntry entry = null;
			if(items.ContainsKey(uri)) {
				entry = items [uri];
				MoveForward (entry);
			}
			return entry;
		}

		public CacheEntry Lookup (SafeUri uri)
		{
			lock (items) {
				return ULookup (uri);
			}
		}

		private void URemove (SafeUri uri)
		{
			CacheEntry entry = null;
			if (items.ContainsKey (uri)) {
				entry = items[uri];
				items.Remove (uri);
				items_mru.Remove (entry);
				entry.Dispose ();
			}
		}

		public void Remove (SafeUri uri)
		{
			lock (items) {
				URemove (uri);
			}
		}

		public class CacheEntry : System.IDisposable {
			private Gdk.Pixbuf pixbuf;
			private object data;
			private PixbufCache cache;

			public CacheEntry (PixbufCache cache, SafeUri uri, object closure, int width, int height)
			{
				Uri = uri;
				Width = width;
				Height = height;
								// Should this be this.data or Data?
				this.data = closure;
				Reload = true;
				this.cache = cache;
				cache.total_size += this.Size;
			}

			public bool Reload { get; set; }
			public SafeUri Uri { get; private set; }
			public int Width { get; set; }
			public int Height { get; set; }

			public object Data {
				get {
					lock (this) {
						return data;
					}
				}
				set {
					lock (this) {
						data = value;
					}
				}
			}

			public bool IsDisposed {
				get { return Uri == null; }
			}

			public void SetPixbufExtended (Gdk.Pixbuf value, bool ignore_undead)
			{
				lock (this) {
					if (IsDisposed) {
						if (ignore_undead) {
							return;
						} else {
							throw new System.Exception ("I don't want to be undead");
						}
					}

					Gdk.Pixbuf old = this.Pixbuf;
					cache.total_size -= this.Size;
					this.pixbuf = value;
					if (pixbuf != null) {
						Width = pixbuf.Width;
						Height = pixbuf.Height;
					}
					cache.total_size += this.Size;
					Reload = false;

					if (old != null)
						old.Dispose ();
				}
			}

			public Gdk.Pixbuf Pixbuf {
				get {
					lock (this) {
						return pixbuf;
					}
				}
			}

			public Gdk.Pixbuf ShallowCopyPixbuf ()
			{
				lock (this) {
					if (IsDisposed)
						return null;

					if (pixbuf == null)
						return null;

					return pixbuf.ShallowCopy ();
				}
			}

			~CacheEntry ()
			{
				if (!IsDisposed)
					this.Dispose ();
			}

			public void Dispose ()
			{
				lock (this) {
					if (! IsDisposed)
						cache.total_size -= this.Size;

					if (this.pixbuf != null) {
						this.pixbuf.Dispose ();

					}
					this.pixbuf = null;
					this.cache = null;
					Uri = null;
				}
				System.GC.SuppressFinalize (this);
			}

			public int Size {
				get {
					return Width * Height * 3;
				}
			}
		}
	}
}

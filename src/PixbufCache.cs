/*
 * FSpot.PixbufCache.cs
 *
 * Author(s):
 * 	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections;
using System.Threading;

namespace FSpot {
	public class PixbufCache {
		Hashtable items;
		ArrayList items_mru;
		int total_size;
		int max_size = 256 * 256 * 4 * 30;

		private Thread worker;

		public delegate void PixbufLoadedHandler (PixbufCache cache, CacheEntry entry);
		public event PixbufLoadedHandler OnPixbufLoaded;
		
		public PixbufCache ()
		{
			items = new Hashtable ();
			items_mru = new ArrayList ();
			
			worker = new Thread (new ThreadStart (WorkerTask));
			worker.Start ();

			ThumbnailGenerator.Default.OnPixbufLoaded += HandleThumbnailLoaded;
		}
		
		public void HandleThumbnailLoaded (PixbufLoader loader, Uri uri, int order, Gdk.Pixbuf result)
		{
			string thumb_path = ThumbnailGenerator.ThumbnailPath (uri);
			
			if (result != null)
				Reload (thumb_path);
		}
			
		public void Request (string path, object closure, int width, int height)
		{
			lock (items) {
				CacheEntry entry = items[path] as CacheEntry;

				if (entry == null) {
					entry = new CacheEntry (this, path, closure, width, height);
					items [path] = entry;
					items_mru.Add (entry);
				} else {
					MoveForward (entry);
					entry.Data = closure;
				}
				Monitor.Pulse (items);
			}
#if GSD_2_24
			if (!System.IO.File.Exists (path))
				return;
			Utils.Unix.Touch (path);
#endif
		}

		public void Update (string path, Gdk.Pixbuf pixbuf)
		{
			lock (items) {
				CacheEntry entry = (CacheEntry) items [path];
				if (entry != null) {
					entry.SetPixbufExtended (pixbuf, true);
				}
			}
		}

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

		public void Reload (string path)
		{
			CacheEntry entry;

			lock (items) {
				entry = (CacheEntry) items [path];
				if (entry != null) {
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
				entry = (CacheEntry) items_mru [i];
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
				CacheEntry entry = (CacheEntry) items_mru [num++];
				items.Remove (entry.Path);
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
					System.Console.WriteLine (e);
					current = null;
				}
			}
		}
		
		protected virtual void ProcessRequest (CacheEntry entry)
		{
			Gdk.Pixbuf loaded = null;
			try {
				loaded = new Gdk.Pixbuf (entry.Path);
				this.Update (entry, loaded);
			} catch (GLib.GException){
				if (loaded != null)
					loaded.Dispose ();
				return;		
			}
		}
		
		private void QueueLast (CacheEntry entry)
		{
			Gtk.Application.Invoke (delegate (object obj, System.EventArgs args) {
				if (entry.Path != null && OnPixbufLoaded != null)
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
				tmp2 = (CacheEntry) items_mru [i];
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
		       

		private CacheEntry ULookup (string path)
		{
			CacheEntry entry = (CacheEntry) items [path];
			if (entry != null) {
				MoveForward (entry);
			}
			return (CacheEntry) entry;
		}

		public CacheEntry Lookup (string path)
		{
			lock (items) {
				return ULookup (path);
			}
		}

		public void Remove (string path) 
		{
			lock (items) {
				URemove (path);
			}
		}

		private void URemove (string path)
		{
			CacheEntry entry = (CacheEntry) items [path];
			if (entry != null) {
				items.Remove (path);
				items_mru.Remove (entry);
				entry.Dispose ();
			}
		}

		public class CacheEntry : System.IDisposable {
			private Gdk.Pixbuf pixbuf;
			private string path;
			private int width;
			private int height;
			private object data;
			private bool reload;
			private PixbufCache cache;
			
			public CacheEntry (PixbufCache cache, string path, object closure, int width, int height)
			{
				this.path = path;
				this.width = width;
				this.height = height;
				this.data = closure;
				this.Reload = true;
				this.cache = cache;
				cache.total_size += this.Size;
			}

			public bool Reload {
				get { return reload; }
				set { reload = value; }
			}

			public string Path {
				get { return path; }
			}

			public int Width {
				get { return width; }
				set { width = value; }
			}

			public int Height {
				get { return height; }
				set { height = value; }
			}

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
				get { return path == null; }
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
						this.width = pixbuf.Width;
						this.height = pixbuf.Height;
					}
					cache.total_size += this.Size;
					this.Reload = false;
					
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
					
					return PixbufUtils.ShallowCopy (pixbuf);
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
					this.path = null;
				}
				System.GC.SuppressFinalize (this);
			}
			
			public int Size {
				get {
					return width * height * 3;
				}
			}
		}
	}
}

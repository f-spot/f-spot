using System.Collections;
using System.Threading;

namespace FSpot {
	public class PixbufCache {
		Hashtable items;
		ArrayList items_mru;
		int total_size;
		int max_size = 256 * 256 * 4 * 50;

		Gtk.ThreadNotify notify;
		bool  notify_pending;
		Queue pending;

		private Thread worker;

		public delegate void PixbufLoadedHandler (PixbufCache cache, CacheEntry entry);
		public event PixbufLoadedHandler OnPixbufLoaded;
		
		public PixbufCache ()
		{
			pending = new Queue ();
			items = new Hashtable ();
			items_mru = new ArrayList ();
			
			notify = new Gtk.ThreadNotify (new Gtk.ReadyEvent (HandleProcessedRequests));
			
			worker = new Thread (new ThreadStart (WorkerTask));
			worker.Start ();

			ThumbnailGenerator.Default.OnPixbufLoaded += HandleThumbnailLoaded;
		}
		
		public void HandleThumbnailLoaded (PixbufLoader loader, string path, int order, Gdk.Pixbuf result)
		{
			string thumb_path = ThumbnailGenerator.ThumbnailPath (path);
			CacheEntry entry;
			lock (items) {
				entry = ULookup (thumb_path);
			}

			if (entry != null && result != null) {
				int width, height;
				PixbufUtils.Fit (result, entry.Width, entry.Height, false, out width, out height);
				Gdk.Pixbuf down = PixbufUtils.ScaleDown (result, width, height);
				PixbufUtils.CopyThumbnailOptions (result, down);
				Update (entry, down);
			}

			//System.Console.WriteLine ("removing {0}", thumb_path);
		}

		public void Request (string path, object closure, int width, int height)
		{
			lock (items) {
				CacheEntry entry = items[path] as CacheEntry;
				
				if (entry == null) {
					entry = new CacheEntry (path, closure, width, height);
					items [path] = entry;
					items_mru.Add (entry);
					total_size += entry.Size;
					Monitor.Pulse (items);
				} else {
					MoveForward (entry);
					entry.Data = closure;
				}
			}
		}

		public void Update (CacheEntry entry, Gdk.Pixbuf pixbuf)
		{
			lock (items) {
				
				total_size -= entry.Size;
				entry.Pixbuf = pixbuf;
				total_size += entry.Size;
			}
		}
		
		public void Reload (CacheEntry entry, object data, int width, int height)
		{
			lock (items) {
				int size = entry.Size;
				entry.Reload = true;
				entry.Width = width;
				entry.Height = height;
				entry.Data = data;
				total_size += entry.Size - size;
				Monitor.Pulse (items);
			}
		}

		private CacheEntry FindNext ()
		{
			CacheEntry entry;
			int i = items_mru.Count;
			int size = 0;
			while (i-- > 0) {
				entry = (CacheEntry) items_mru [i];
				lock (entry) {
					size += entry.Size;

					if (entry.Reload) {
						entry.Reload = false;
						return entry;
					}

					//if the depth of the queue is so large that we've reached double our limit 
					//break out of here and let the queue shrink.
					if (size / 2  > max_size) {
						//System.Console.WriteLine ("Hit limit");
						return null;
					}
				}
			}
			return null;
		}
		
		
		
		private void WorkerTask ()
		{
			CacheEntry current = null;
			ThumbnailGenerator.Default.PushBlock ();
			while (true) {
				try {
					lock (items) {
						if (current != null) {
							pending.Enqueue (current);
							if (!notify_pending) {
								notify.WakeupMain ();
								notify_pending = true;
							}
						}
						
						/* find the next item */
						while ((current = FindNext ()) == null) {
							int num = 0;
							while ((items_mru.Count - num) > 10 && total_size > max_size) {
								CacheEntry entry = (CacheEntry) items_mru [num++];
								total_size -= entry.Size;
								items.Remove (entry.Path);
								entry.Dispose ();
							}			
							if (num > 0) {
								//System.Console.WriteLine ("removing {0}  ({1} > {2})", num, total_size, max_size);
								items_mru.RemoveRange (0, num);
							} else {
								ThumbnailGenerator.Default.PopBlock ();
								Monitor.Wait (items);
								ThumbnailGenerator.Default.PushBlock ();
							}
						}
					}
					
					ProcessRequest (current);
					
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
				lock (items) {
					loaded = new Gdk.Pixbuf (entry.Path);
					total_size -= entry.Size;
					entry.Pixbuf = loaded;
					total_size += entry.Size;
				}
			} catch (GLib.GException ex){
				if (loaded != null)
					loaded.Dispose ();
				return;		
			}
		}
		
		private void HandleProcessedRequests ()
		{
			Queue entries;
			lock (items) {
				entries = pending.Clone () as Queue;
				pending.Clear ();
				notify_pending = false;
			}
			
			if (OnPixbufLoaded != null) {
				foreach (CacheEntry entry in entries) {
					if (entry.Path != null)
						OnPixbufLoaded (this, entry);
				}
			}
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
			items_mru.Add (entry);
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
			return (CacheEntry) items [path];
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
				total_size -= entry.Size;
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
			
			public CacheEntry (string path, object closure, int width, int height)
			{
				this.path = path;
				this.width = width;
				this.height = height;
				this.data = closure;
				this.Reload = true;
			}

			public bool Reload {
				get {
					return reload;
				}
				set {
					reload = value;
				}
			}

			public string Path {
				get {
					return path;
				}
			}

			public int Width {
				get {
					return width;
				}
				set {
					width = value;
				}
			}

			public int Height {
				get {
					return height;
				}
				set {
					height = value;
				}
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
			
			public Gdk.Pixbuf Pixbuf {
				get {
					lock (this) {
						return pixbuf;
					}
				}
				set {
					Gdk.Pixbuf old;
					lock (this) {
						old = this.Pixbuf;

						this.pixbuf = value;
						if (pixbuf != null) {
							this.width = pixbuf.Width;
							this.height = pixbuf.Height;
						}
						this.Reload = false;
					}

					if (old != null)
						old.Dispose ();
				}
			}
			
			public Gdk.Pixbuf ShallowCopyPixbuf ()
			{
				lock (this) {
					if (pixbuf == null)
						return null;

					return PixbufUtils.ShallowCopy (pixbuf);
				}
			}

			~CacheEntry ()
			{
				this.Dispose ();
			}

			public void Dispose ()
			{
				lock (this) {
					//System.Console.WriteLine ("dispose");

					if (this.pixbuf != null) {
						//System.Console.WriteLine ("entry.Dispose ({0}.{1})", this.path, PixbufUtils.RefCount (this.pixbuf));
						this.pixbuf.Dispose ();
						
					}
					this.pixbuf = null;
					this.path = null;
				}
			}

			public int Size {
				get {
					return width * height * 3;
				}
			}
		}
	}
}

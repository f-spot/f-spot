using System.Collections;
using System.Threading;

namespace FSpot {
	public class PixbufCache {
		Hashtable items;
		ArrayList items_mru;
		int total_size;
		int max_size = 256 * 256 * 4 * 70;

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
				//pending.Enqueue (entry);
			}

			if (entry != null && result != null) {
				Update (entry, PixbufUtils.ScaleToMaxSize (result, entry.Width, entry.Height));
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
					//System.Console.WriteLine ("total ({0} of {1} += {2})", total_size, max_size, entry.Size);
					Monitor.Pulse (items);
				} else {
					MoveForward (entry);
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
			while (i-- > 0) {
				entry = (CacheEntry) items_mru [i];
				
				lock (entry) {
					if (entry.Reload) {
						entry.Reload = false;
						return entry;
					}
				}
			}
			return null;
		}
		
		
		
		private void WorkerTask ()
		{
			CacheEntry current = null;
			while (true) {
				try {
					lock (items) {
						int i = 0;
						if (current != null) {
							if (current.Pixbuf == null)
								URemove (current.Path);
							else {
								pending.Enqueue (current);
								if (!notify_pending) {
									notify.WakeupMain ();
									notify_pending = true;
								}
							}
						}
						
						/* shrink the cache */
						while (items_mru.Count > 10 && total_size > max_size) {
							//System.Console.WriteLine ("remove ({0} > {1})", total_size, max_size);
							URemove (((CacheEntry)items_mru [0]).Path);
						}					
						
						/* find the next item */
						while ((current = FindNext ()) == null) {
							//System.Console.WriteLine ("Waiting");
							Monitor.Wait (items);
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
			try {
				total_size -= entry.Size;
				entry.Pixbuf =  new Gdk.Pixbuf (entry.Path);
				total_size += entry.Size;
			} catch (GLib.GException ex){
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
				foreach (CacheEntry entry in entries)
					OnPixbufLoaded (this, entry);
			}
		}

		private void MoveForward (CacheEntry entry)
		{
#if false		       
			int i = items.Count;
			while (i-- > 0) {
				if (items_mru [i] == entry)
					break;
			}
			
			while (i < items.Count - 1) {
				items_mru [i] = items_mru [i++];
			}

			items_mru [items.Count - 1] = entry;
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

		public Gdk.Pixbuf LookupPixbuf (string path)
		{
			lock (items) {
				CacheEntry entry = ULookup (path);
				if (entry != null)
					return entry.ShallowCopyPixbuf ();
				else
					return null;
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
					lock (this) {
						this.pixbuf = value;
						if (pixbuf != null) {
							this.width = pixbuf.Width;
							this.height = pixbuf.Height;
						}
						this.Reload = false;
					}
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

			public void Dispose ()
			{
				lock (this) {
					if (pixbuf != null)
						pixbuf.Dispose ();
					pixbuf = null;
				}
			}

			public int Size {
				get {
					return (width * height * 3);
				}
			}
		}
	}
}

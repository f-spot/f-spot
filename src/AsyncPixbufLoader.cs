
namespace FSpot {
	public delegate void AreaUpdatedHandler (object sender, Gdk.Rectangle area);
	public delegate void AreaPreparedHandler (object sender, System.EventArgs args);


	public class AsyncPixbufLoader : System.IDisposable {
		System.IO.Stream stream;
		Gdk.PixbufLoader loader;		
		string path;
		bool area_prepared = false;
		bool done_reading = false;
		Gdk.Pixbuf pixbuf;
		PixbufOrientation orientation;

		private Gdk.AreaUpdatedHandler au;
		private System.EventHandler ap;
		private System.EventHandler ev;

		//byte [] buffer = new byte [8192];
		byte [] buffer = new byte [32768];

		public event AreaUpdatedHandler AreaUpdated;
		public event AreaPreparedHandler AreaPrepared;
		public event System.EventHandler Done;

		// If the photo we just loaded has an out of date
		// thumbnail save a new one
		bool validate_thumbnail = true;

		// Limit pass control back to the main loop after
		// chunk_timeout miliseconds.
		int  chunk_timeout = 100;

		Delay delay;

		Gdk.Rectangle damage;

		public AsyncPixbufLoader ()
		{
			delay = new Delay (new GLib.IdleHandler (AsyncRead));
			ap = new System.EventHandler (HandleAreaPrepared);
			au = new Gdk.AreaUpdatedHandler (HandleAreaUpdated);
			ev = new System.EventHandler (HandleClosed);
		}
		
		public bool Loading
		{
			get {
				return done_reading;
			}
		}

		public string Path {
			get {
				return path;
			}
		}
		
		public void Load (string filename)
		{
			delay.Stop ();
			path = filename;

			if (!done_reading)
				Close ();

			done_reading = false;
			area_prepared = false;
			damage = Gdk.Rectangle.Zero;

			ImageFile img = ImageFile.Create (filename);
			orientation = img.Orientation;

			stream = img.PixbufStream ();
			if (stream == null) {
				pixbuf = img.Load ();
				done_reading = true;
				if (Done != null)
					Done (this, System.EventArgs.Empty);
				return;
			}

			loader = new Gdk.PixbufLoader ();
			loader.AreaPrepared += ap;
			loader.AreaUpdated += au;
			loader.Closed += ev;

			ThumbnailGenerator.Default.PushBlock ();
			//AsyncIORead (null);
			delay.Start ();
		}			

		public Gdk.PixbufLoader Loader {
			get {
				return loader;
			}
		}

	        public void LoadToAreaPrepared ()
		{
			delay.Stop  ();
			while (!area_prepared && AsyncRead ())
				; //step
		}

		public void LoadToDone ()
		{
			delay.Stop ();
			while (!done_reading && AsyncRead ())
				; //step
		}

		private void Close () 
		{
			ThumbnailGenerator.Default.PopBlock ();
				
			try {
				if (handle != null) {
					stream.EndRead (handle);
					handle = null;
				}
				delay.Stop ();
				if (loader != null) {
					loader.AreaPrepared -= ap;
					loader.AreaUpdated -= au;
					loader.Close ();
					loader.Closed -= ev;
					loader.Dispose ();
				}

				loader = null;
			} catch (System.Exception e) {
				if (pixbuf != null)
					pixbuf.Dispose ();

				pixbuf = null;
			} finally {
				if (stream != null) 
					stream.Close ();
				
				stream = null;
			}
		}

		private void UpdateListeners ()
		{
			Gdk.Rectangle area = damage;
			
			if (pixbuf != null && loader.Pixbuf != null && loader.Pixbuf != pixbuf)
				area = PixbufUtils.TransformAndCopy (loader.Pixbuf, pixbuf, orientation, damage);
			
			if (area.Width != 0 && area.Height != 0 && AreaUpdated != null)
				AreaUpdated (this, area);

			//System.Console.WriteLine ("orig {0} tform {1}", damage.ToString (), area.ToString ());
			damage = Gdk.Rectangle.Zero;
		}

		System.IAsyncResult handle;
		
		private void AsyncIORead (System.IAsyncResult last)
		{
			int len = 0;

			System.DateTime start_time = System.DateTime.Now;
			if (last != null) {
				start_time = (System.DateTime)last.AsyncState;
				System.TimeSpan span = System.DateTime.Now - start_time;

				len = stream.EndRead (last);
				handle = null;
				if (len > 0) {
					try {
						loader.Write (buffer, (uint)len);
						UpdateListeners ();
					} catch (GLib.GException e) {
						pixbuf = null;
					}
				} else {
					UpdateListeners ();
					done_reading = true;
					Close ();
					return;
				}
				/*
				if (span.TotalMilliseconds > chunk_timeout) {
					delay.Start ();
					return;
				}
				*/
			}
			handle = stream.BeginRead (buffer, 0, buffer.Length, new System.AsyncCallback (AsyncIORead), start_time);
		}

		private bool AsyncIORead () 
		{
			AsyncIORead (null);
			delay.Stop ();
			return false;
		}

		private bool AsyncRead () 
		{
			System.DateTime start_time = System.DateTime.Now;
			System.TimeSpan span = start_time - start_time;

			do {
				span = System.DateTime.Now - start_time;

				int len;
				try {
					len = stream.Read (buffer, 0, buffer.Length);
					loader.Write (buffer, (uint)len);
				} catch (GLib.GException e) {
					pixbuf = null;
					len = -1;
				}

				if (len <= 0) {
					UpdateListeners ();
					done_reading = true;
					Close ();
					return false;
				}
			} while (!done_reading && span.TotalMilliseconds <= chunk_timeout);

			UpdateListeners ();
			return true;
		}
		
		private void HandleAreaPrepared (object sender, System.EventArgs args)
		{
			Gdk.Pixbuf old = pixbuf;
			pixbuf = PixbufUtils.TransformOrientation (loader.Pixbuf, orientation, false);

			area_prepared = true;			
			if (AreaUpdated != null)
				AreaPrepared (this, System.EventArgs.Empty);

			if (old != null) {
				old.Dispose ();
			}
		}

		public Gdk.Pixbuf Pixbuf {
			get {
				return pixbuf;
			}
		}
	       
		private void HandleAreaUpdated (object sender, Gdk.AreaUpdatedArgs args)
		{
			Gdk.Rectangle area = new Gdk.Rectangle (args.X, args.Y, args.Width, args.Height);
			
			if (damage.Width == 0 || damage.Height == 0)
				damage = area;
			else 
				damage = area.Union (damage);
		}

		private void HandleClosed (object sender, System.EventArgs args) 
		{
			// FIXME This should probably queue the
			// thumbnail regeneration to a worker thread
			if (validate_thumbnail && done_reading && pixbuf != null) {
				PhotoLoader.ValidateThumbnail (path, pixbuf);
			}

			if (Done != null)
				Done (this, System.EventArgs.Empty);
			
			ThumbnailGenerator.Default.PopBlock ();
		}

		public void Dispose ()
		{
			Close ();

			if (pixbuf != null)
				pixbuf.Dispose ();
		}
	}
}

	       

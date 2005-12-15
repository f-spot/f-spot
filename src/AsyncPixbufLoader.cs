using System;


namespace FSpot {
	public delegate void AreaUpdatedHandler (object sender, AreaUpdatedArgs area);
	public delegate void AreaPreparedHandler (object sender, AreaPreparedArgs args);

	public class AreaPreparedArgs : System.EventArgs {
		bool reduced_resolution;

		public bool ReducedResolution {
			get { return reduced_resolution; }
		}
	
		public AreaPreparedArgs (bool reduced_resolution)
		{
			this.reduced_resolution = reduced_resolution;
		}
	}

	public class AreaUpdatedArgs : System.EventArgs {
		Gdk.Rectangle area;

		public Gdk.Rectangle Area { 
			get { return area; }
		}

		public AreaUpdatedArgs (Gdk.Rectangle area)
		{
			this.area = area;
		}
	}

	public class AsyncPixbufLoader : System.IDisposable {
		System.IO.Stream stream;
		Gdk.PixbufLoader loader;		
		Uri uri;
		bool area_prepared = false;
		bool done_reading = false;
		Gdk.Pixbuf pixbuf;
		Gdk.Pixbuf thumb;
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
				return ! done_reading;
			}
		}

		public bool Prepared
		{
			get {
				return area_prepared;
			}
		}

		public string Path {
			get {
				return uri.LocalPath;
			}
		}

		public Gdk.Pixbuf Pixbuf {
			get {
				return pixbuf;
			}
		}
		
		private void FileLoad (ImageFile img)
		{
			pixbuf = img.Load ();
			done_reading = true;
			if (Done != null)
				Done (this, System.EventArgs.Empty);
		}

		public void Load (Uri uri)
		{
			this.uri = uri;

			delay.Stop ();

			if (!done_reading)
				Close ();

			done_reading = false;
			area_prepared = false;
			damage = Gdk.Rectangle.Zero;

			ImageFile img = ImageFile.Create (Path);
			orientation = img.Orientation;

			try {
			thumb = new Gdk.Pixbuf (ThumbnailGenerator.ThumbnailPath (uri));
			} catch (System.Exception e) {
				FSpot.ThumbnailGenerator.Default.Request (uri.LocalPath, 0, 256, 256);	
				if (!(e is GLib.GException)) 
					System.Console.WriteLine (e.ToString ());
			}

#if false
			if (AreaPrepared != null && thumb != null) {
			
				pixbuf = thumb;
				AreaPrepared (this, new AreaPreparedArgs (true));
			}
#endif


			stream = img.PixbufStream ();
			if (stream == null) {
				FileLoad (img);
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

		private void Close () {
			ThumbnailGenerator.Default.PopBlock ();
				
			try {
				delay.Stop ();
				if (loader != null) { 
					loader.AreaPrepared -= ap;
					loader.AreaUpdated -= au;
					// this can throw exceptions
					loader.Close ();
				}
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
				if (pixbuf != null)
					pixbuf.Dispose ();

				pixbuf = null;
			} finally {
				if (loader != null) {
					loader.Closed -= ev;
					loader.Dispose ();
				}

				loader = null;

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
				AreaUpdated (this, new AreaUpdatedArgs (area));

			//System.Console.WriteLine ("orig {0} tform {1}", damage.ToString (), area.ToString ());
			damage = Gdk.Rectangle.Zero;
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
					loader.Write (buffer, (ulong)len);
				} catch (System.ObjectDisposedException od) {
					len = -1;
				} catch (GLib.GException e) {
					System.Console.WriteLine (e.ToString ());
					pixbuf = null;
					len = -1;
				}

				if (len <= 0) {
					if (loader.Pixbuf == null) {
						if (pixbuf != null)
							pixbuf.Dispose ();

						pixbuf = null;
					}

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
			pixbuf = PixbufUtils.TransformOrientation (loader.Pixbuf, orientation, false);

			if (thumb != null && pixbuf != null)
				thumb.Composite (pixbuf, 0, 0,
						 pixbuf.Width, pixbuf.Height,
						 0.0, 0.0,
						 pixbuf.Width/(double)thumb.Width, pixbuf.Height/(double)thumb.Height,
						 Gdk.InterpType.Bilinear, 0xff);
			
			if (thumb != null)
				if (!ThumbnailGenerator.ThumbnailIsValid (thumb, uri))
					FSpot.ThumbnailGenerator.Default.Request (Path, 0, 256, 256);

			area_prepared = true;			
			if (AreaUpdated != null)
				AreaPrepared (this, new AreaPreparedArgs (false));

			if (thumb != null)
				thumb.Dispose ();
			thumb = null;
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
				PhotoLoader.ValidateThumbnail (Path, pixbuf);
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

	       


namespace FSpot {
	public delegate void AreaUpdatedHandler (object sender, Gdk.Rectangle area);
	
	public class AsyncPixbufLoader {
		System.IO.Stream stream;
		Gdk.PixbufLoader loader;		
		string path;
		bool area_prepared = false;
		bool done_reading = false;
		System.Exception error;
		Gdk.Pixbuf pixbuf;
		PixbufOrientation orientation;

		//byte [] buffer = new byte [8192];
		byte [] buffer = new byte [32768];

		public event AreaUpdatedHandler AreaUpdated;
		public event System.EventHandler Done;

		Delay delay;

		public AsyncPixbufLoader ()
		{
			delay = new Delay (new GLib.IdleHandler (AsyncRead));
		}
				
		public Gdk.Pixbuf Load (string filename)
		{
			delay.Stop ();
			path = filename;

			if (!done_reading && loader != null)
				loader.Close ();

			if (loader != null)
				loader.Dispose ();

			done_reading = false;
			area_prepared = false;

			if (stream != null)
				stream.Close ();
			
			orientation = PixbufUtils.GetOrientation (filename);

			stream = new System.IO.FileStream (filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			
			loader = new Gdk.PixbufLoader ();
			loader.AreaPrepared += HandleAreaPrepared;
			loader.AreaUpdated += HandleAreaUpdated;
			loader.Closed += HandleClosed;
			
			LoadToAreaPrepared ();
			delay.Start ();
			
			return pixbuf;
		}

		public Gdk.PixbufLoader Loader {
			get {
				return loader;
			}
		}

		private void LoadToAreaPrepared ()
		{
			int len;
			do {
				len = stream.Read (buffer, 0, buffer.Length);
				loader.Write (buffer, (uint)len);
			} while (len > 0 && !area_prepared);

			if (len <= 0) {
				done_reading = true;
				stream.Close ();
				loader.Close ();
			}
		}

		private bool AsyncRead () 
		{
			System.DateTime start_time = System.DateTime.Now;
			System.TimeSpan span = start_time - start_time;

			do {
				span = System.DateTime.Now - start_time;

				int len = stream.Read (buffer, 0, buffer.Length);
				loader.Write (buffer, (uint)len);
				
				if (len <= 0) {
					done_reading = true;
					stream.Close ();
					loader.Close ();
					return false;
				}
			} while (!done_reading && span.TotalMilliseconds <= 300);
			return true;
		}
		
		private void HandleAreaPrepared (object sender, System.EventArgs args)
		{
			loader.Pixbuf.Fill (0x00000000);
			pixbuf = PixbufUtils.TransformOrientation (loader.Pixbuf, orientation);
			area_prepared = true;			
		}

	       
		private void HandleAreaUpdated (object sender, Gdk.AreaUpdatedArgs args)
		{
			Gdk.Rectangle area = new Gdk.Rectangle (args.X, args.Y, args.Width, args.Height);

			if (pixbuf != null && loader.Pixbuf != pixbuf)
				area = PixbufUtils.TransformAndCopy (loader.Pixbuf, pixbuf, orientation, area);

			if (AreaUpdated != null)
				AreaUpdated (this, area);
		}

		private void HandleClosed (object sender, System.EventArgs args) 
		{
			if (done_reading && pixbuf != null) {
				PhotoLoader.ValidateThumbnail (path, pixbuf);
			}

			if (Done != null)
				Done (this, System.EventArgs.Empty);
		}
	}
}

	       

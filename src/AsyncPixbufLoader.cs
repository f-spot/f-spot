
namespace FSpot {
	public class AsyncPixbufLoader {
		public AsyncPixbufLoader ()
		{
			delay = new Delay (new GLib.IdleHandler (AsyncRead));
		}
				
		System.IO.Stream stream;
		Gdk.PixbufLoader loader;
		
		string path;
		bool area_prepared = false;
		bool done_reading = false;
		System.Exception error;

		//byte [] buffer = new byte [8192];
		byte [] buffer = new byte [32768];

		Delay delay;

		public Gdk.Pixbuf Load (string filename)
		{
			delay.Stop ();
			path = filename;
			done_reading = false;
			area_prepared = false;
			
			if (stream != null)
				stream.Close ();
			
			stream = new System.IO.FileStream (filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			
			if (loader != null) {
				loader.Close ();
				loader.Dispose ();
			}
			
			loader = new Gdk.PixbufLoader ();
			loader.AreaPrepared += HandleAreaPrepared;
			loader.AreaUpdated += HandleAreaUpdated;
			loader.Closed += HandleClosed;
			
			LoadToAreaPrepared ();
			delay.Start ();
			
			return loader.Pixbuf;
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
			System.Console.WriteLine ("AreaPrepared");
			area_prepared = true;			
		}

	       
		private void HandleAreaUpdated (object sender, Gdk.AreaUpdatedArgs args)
		{
			if (done_reading) {
				
			}
		}

		private void HandleClosed (object sender, System.EventArgs args) 
		{
			System.Console.WriteLine ("Closed");
			if (done_reading && loader.Pixbuf != null) {
				PhotoLoader.ValidateThumbnail (path, loader.Pixbuf);
			}
		}
	}
}

	       

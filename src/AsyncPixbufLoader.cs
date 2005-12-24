using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Collections;

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
		StreamWrapper stream;
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
		//byte [] buffer = new byte [1 << 14];

		public event AreaUpdatedHandler AreaUpdated;
		public event AreaPreparedHandler AreaPrepared;
		public event System.EventHandler Done;

		// If the photo we just loaded has an out of date
		// thumbnail save a new one
		bool validate_thumbnail = true;

		// Limit pass control back to the main loop after
		// chunk_timeout miliseconds.
		int  chunk_timeout = 100;

		Queue completed;
		
		Gdk.Rectangle damage;

		public AsyncPixbufLoader ()
		{
			ap = new System.EventHandler (HandleAreaPrepared);
			au = new Gdk.AreaUpdatedHandler (HandleAreaUpdated);
			ev = new System.EventHandler (HandleClosed);
			completed = Queue.Synchronized (new Queue ());
			update = new Delay (new GLib.IdleHandler (ReadDone));
		}
		
		Delay update;
		
		public bool Loading
		{
			get { return ! done_reading; }
		}

		public bool Prepared
		{
			get { return area_prepared; }
		}

		public string Path {
			get { return uri.LocalPath; }
		}

		public Gdk.Pixbuf Pixbuf {
			get { return pixbuf; }
		}

		public Gdk.PixbufLoader Loader {
			get { return loader; }
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


			if (!done_reading)
				Close ();

			done_reading = false;
			area_prepared = false;
			damage = Gdk.Rectangle.Zero;
			completed.Clear ();
			
			ImageFile img = ImageFile.Create (Path);
			orientation = img.Orientation;
			

			try {
				thumb = new Gdk.Pixbuf (ThumbnailGenerator.ThumbnailPath (uri));
			} catch (System.Exception e) {
				FSpot.ThumbnailGenerator.Default.Request (uri.LocalPath, 0, 256, 256);	
				if (!(e is GLib.GException)) 
					System.Console.WriteLine (e.ToString ());
			}

#if true
			if (AreaPrepared != null && thumb != null) {
				pixbuf = thumb;
				AreaPrepared (this, new AreaPreparedArgs (true));
			}
#endif

			System.IO.Stream nstream = img.PixbufStream ();
			if (nstream == null) {
				FileLoad (img);
				return;
			} else
				stream = new StreamWrapper (nstream);
			
			loader = new Gdk.PixbufLoader ();
			loader.AreaPrepared += ap;
			loader.AreaUpdated += au;
			loader.Closed += ev;

			ThumbnailGenerator.Default.PushBlock ();
			AsyncRead ();
		}			

		public void LoadToDone ()
		{
			while (!done_reading) {
				Console.WriteLine ("todone");
			}
		}

		private void Close () {
			ThumbnailGenerator.Default.PopBlock ();
				
			try {
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

		private class StreamWrapper {
			private delegate int ReadDelegate (byte [] buffer, int offset, int count);
			System.IO.Stream stream;
			IAsyncResult iar;

			public System.IO.Stream Stream {
				get { return stream; }
			}

			public StreamWrapper (System.IO.Stream stream)
			{
				this.stream = stream;
			}

			public int Read (byte[] buffer, int offset, int count)
			{
				return stream.Read (buffer, offset, count);
			}

			public IAsyncResult BeginRead (byte [] buffer, int offset, int count, AsyncCallback cb, object state)
			{
				ReadDelegate del = new ReadDelegate (Read);
				return iar = del.BeginInvoke (buffer, offset, count, cb, state);
			}
			
			public int EndRead (IAsyncResult result)
			{
				AsyncResult art = result as AsyncResult;
				ReadDelegate del = art.AsyncDelegate as ReadDelegate;
				int i = del.EndInvoke (result);
				return i;
			}
			
			public void Close ()
			{
				IDisposable d = stream as IDisposable;
				if (d != null)
					d.Dispose ();
				else
					stream.Close ();
				
			}
		}

		private class Packet {
			public byte [] Buffer;
			public int Length;
			public StreamWrapper Stream;

			public Packet (StreamWrapper stream, int length)
			{
				Stream = stream;
				Buffer = new byte [length];
			}
		}

		private void AsyncIORead (StreamWrapper wstream)
		{
			try {
				if (stream == null)
					return;

				if (wstream != stream) {
					Console.WriteLine ("bailing");
					return;
				}
				Packet p = new Packet (wstream, 1 << 14);
				p.Stream.BeginRead (p.Buffer, 0, p.Buffer.Length, AsyncIOEnd, p);
			} catch (System.Exception e) {
				System.Console.WriteLine ("In read got {0}", e);
			}
		}

		private void AsyncIOEnd (IAsyncResult iar)
		{
			Packet p = (Packet) iar.AsyncState;
			p.Length = p.Stream.EndRead (iar);
			completed.Enqueue (p);
			//Gtk.Application.Invoke (CallDone);
			update.Start ();
			if (p.Length > 0)
				AsyncIORead (p.Stream);
		}
		
		private Packet GetPacket () {
			try {
				return (Packet) completed.Dequeue ();
			} catch (System.Exception e) {
			        return null;
			}
		}

		public void CallDone (object sender, EventArgs args)
		{
			update.Start ();
		}

		public bool ReadDone ()
		{
			if (loader == null) {
				//UpdateListeners ();
				return false;
			}

 			if (System.Threading.Thread.CurrentThread.IsThreadPoolThread) 
				Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Wrong thread");

			int len = 500;
			bool found = false;
			try {
				Packet p = GetPacket ();
				while (p != null) {
					if (p.Stream != stream)
						continue;

					len = p.Length;

					loader.Write (p.Buffer, (ulong)p.Length);
					p = GetPacket ();
				}
			} catch (System.ObjectDisposedException od) {
				System.Console.WriteLine ("error in endread {0}", od);
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
				
			UpdateListeners ();
			return false;
		}
	
		private void AsyncRead () 
		{
			AsyncIORead (stream);
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

	       

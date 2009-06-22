//
// Fspot.ImageLoader.cs
//
// Copyright (c) 2009 Novell, Inc.
//
// Author(s)
//	Stephane Delcroix  <sdelcroix@novell.com>
//
// This is free software. See COPYING for details
//

using System;

using Gdk;

using FSpot.Utils;

namespace FSpot {
	public class AreaPreparedEventArgs : EventArgs
	{
		bool reduced_resolution;

		public bool ReducedResolution {
			get { return reduced_resolution; }
		}
	
		public AreaPreparedEventArgs (bool reduced_resolution) : base ()
		{
			this.reduced_resolution = reduced_resolution;
		}
	}

	public class AreaUpdatedEventArgs : EventArgs
	{
		Gdk.Rectangle area;
		public Gdk.Rectangle Area { 
			get { return area; }
		}

		public AreaUpdatedEventArgs (Gdk.Rectangle area) : base ()
		{
			this.area = area;
		}
	}

	public class ImageLoader : Gdk.PixbufLoader
	{
#region public api
		public ImageLoader () : base ()
		{	
		}

		public void Load (Uri uri)
		{
			if (is_disposed)
				return;

			using (ImageFile image_file = ImageFile.Create (uri)) {
				image_stream = image_file.PixbufStream ();
				pixbuf_orientation = image_file.Orientation;
			}
			image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
			loading = true;
		}

		new public event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		new public event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		public event EventHandler Completed;

		bool loading = false;
		public bool Loading {
			get { return loading; }
		}

		bool notify_prepared = false;
		bool prepared = false;
		public bool Prepared {
			get { return prepared; }
		}

		PixbufOrientation pixbuf_orientation = PixbufOrientation.TopLeft;
		public PixbufOrientation PixbufOrientation {
			get { return pixbuf_orientation; }
		}

		bool is_disposed = false;
		public override void Dispose ()
		{
			is_disposed = true;
			if (image_stream != null)
				image_stream.Close ();
			try {
				Close ();
			} catch (GLib.GException) {
				//it's normal to get an exception here if we're closing in the early loading stages, and it's safe to ignore
				// that exception as we don't want the loading to finish but want to cancel it.
			}
			base.Dispose ();
		}
#endregion

#region event handlers
		protected override void OnAreaPrepared ()
		{
			if (is_disposed)
				return;

			prepared = notify_prepared = true;
			damage = Rectangle.Zero;
			base.OnAreaPrepared ();
		}

		protected override void OnAreaUpdated (int x, int y, int width, int height)
		{
			if (is_disposed)
				return;

			Rectangle area = new Rectangle (x, y, width, height);
			damage = damage == Rectangle.Zero ? area : damage.Union (area);
			base.OnAreaUpdated (x, y, width, height);
		}

		protected virtual void OnCompleted ()
		{
			if (is_disposed)
				return;

			EventHandler eh = Completed;
			if (eh != null)
				eh (this, EventArgs.Empty);
			Close ();
		}
#endregion

#region private stuffs
		System.IO.Stream image_stream;
		const int count = 1 << 20;
		byte [] buffer = new byte [count];
		bool notify_completed = false;
		Rectangle damage;

		void HandleReadDone (IAsyncResult ar)
		{
			if (is_disposed)
				return;

			int byte_read = image_stream.EndRead (ar);
			if (byte_read == 0) {
				image_stream.Close ();
				loading = false;
				notify_completed = true;
			}

			Gtk.Application.Invoke (this, null, delegate (object sender, EventArgs e) {
				if (loading)
					try {
						if (!is_disposed && Write (buffer, (ulong)byte_read))
							image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
					} catch (System.ObjectDisposedException od) {
					} catch (GLib.GException ge) {
					}

				//Send the AreaPrepared event
				if (notify_prepared) {
					notify_prepared = false;
					EventHandler<AreaPreparedEventArgs> eh = AreaPrepared;
					if (eh != null)
						eh (this, new AreaPreparedEventArgs (false));
				}

				//Send the AreaUpdated events
				if (damage != Rectangle.Zero) {
					EventHandler<AreaUpdatedEventArgs> eh = AreaUpdated;
					if (eh != null)
						eh (this, new AreaUpdatedEventArgs (damage));
					damage = Rectangle.Zero;
				}

				//Send the Completed event
				if (notify_completed) {
					notify_completed = false;
					OnCompleted ();
				}
			});
		}
#endregion
	}
}	       

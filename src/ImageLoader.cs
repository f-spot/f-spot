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
using System.Threading;
using Gdk;
using FSpot.Utils;
using FSpot.Platform;

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

			//First, send a thumbnail if we have one
			if ((thumb = ThumbnailFactory.LoadThumbnail (uri)) != null) {
				pixbuf_orientation = PixbufOrientation.TopLeft;
				EventHandler<AreaPreparedEventArgs> prep = AreaPrepared;
				if (prep != null)
					prep (this, new AreaPreparedEventArgs (true));
				EventHandler<AreaUpdatedEventArgs> upd = AreaUpdated;
				if (upd != null)
					upd (this, new AreaUpdatedEventArgs (new Rectangle (0, 0, thumb.Width, thumb.Height)));
			}

			using (ImageFile image_file = ImageFile.Create (uri)) {
				image_stream = image_file.PixbufStream ();
				pixbuf_orientation = image_file.Orientation;
			}

			// The ThreadPool.QueueUserWorkItem hack is there cause, as the bytes to read are present in the stream,
			// the Read is CompletedAsynchronously, blocking the mainloop
			image_stream.BeginRead (buffer, 0, count, delegate (IAsyncResult r) {
				ThreadPool.QueueUserWorkItem (delegate {HandleReadDone (r);});
			}, null);
			loading = true;
		}

		new public event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		new public event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		public event EventHandler Completed;


		Pixbuf thumb;
		new public Pixbuf Pixbuf {
			get {
				if (thumb != null)
					return thumb;
				return base.Pixbuf;
			}
		}

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
			if (thumb != null) {
				thumb.Dispose ();
				thumb = null;
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
			} else {
				try {
					if (!is_disposed && Write (buffer, (ulong)byte_read))
						image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
				} catch (System.ObjectDisposedException od) {
				} catch (GLib.GException ge) {
				}
			}

			GLib.Idle.Add (delegate {
				//Send the AreaPrepared event
				if (notify_prepared) {
					notify_prepared = false;
					if (thumb != null) {
						thumb.Dispose ();
						thumb = null;
					}

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

				return false;
			});
		}
#endregion
	}
}	       

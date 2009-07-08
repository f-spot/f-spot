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

namespace FSpot.Loaders {
	public class GdkImageLoader : Gdk.PixbufLoader, IImageLoader
	{
#region public api
		public GdkImageLoader () : base ()
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

			loading = true;
			// The ThreadPool.QueueUserWorkItem hack is there cause, as the bytes to read are present in the stream,
			// the Read is CompletedAsynchronously, blocking the mainloop
			image_stream.BeginRead (buffer, 0, count, delegate (IAsyncResult r) {
				ThreadPool.QueueUserWorkItem (delegate {HandleReadDone (r);});
			}, null);
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
				try {
					image_stream.Close ();
				} catch (GLib.GException)
				{
				}
			Close ();
			if (thumb != null) {
				thumb.Dispose ();
				thumb = null;
			}
			base.Dispose ();
		}

		public new bool Close ()
		{
			lock (sync_handle) {
				return base.Close (true);
			}
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
		object sync_handle = new object ();

		void HandleReadDone (IAsyncResult ar)
		{
			if (is_disposed)
				return;

			int byte_read = image_stream.EndRead (ar);
			lock (sync_handle) {
				if (byte_read == 0) {
					image_stream.Close ();
					loading = false;
					notify_completed = true;
				} else {
					try {
						if (!is_disposed && Write (buffer, (ulong)byte_read))
							image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
					} catch (System.ObjectDisposedException) {
					} catch (GLib.GException) {
					}
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

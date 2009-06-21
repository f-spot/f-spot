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

using Gtk;
using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using FSpot.Platform;
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

			base.OnAreaPrepared ();
			prepared = true;
			EventHandler<AreaPreparedEventArgs> eh = AreaPrepared;
			if (eh != null)
				eh (this, new AreaPreparedEventArgs (false));
		}

		protected override void OnAreaUpdated (int x, int y, int width, int height)
		{
			if (is_disposed)
				return;

			base.OnAreaUpdated (x, y, width, height);
			EventHandler<AreaUpdatedEventArgs> eh = AreaUpdated;
			if (eh != null)
				eh (this, new AreaUpdatedEventArgs (new Gdk.Rectangle (x, y, width, height)));
		}

		protected virtual void OnCompleted ()
		{
			if (is_disposed)
				return;

			loading = false;
			EventHandler eh = Completed;
			if (eh != null)
				eh (this, EventArgs.Empty);
			Close ();
		}
#endregion

#region private stuffs
		System.IO.Stream image_stream;
		const int count = 1 << 16; //64k

		byte [] buffer = new byte [count];

		void HandleReadDone (IAsyncResult ar)
		{
			if (is_disposed)
				return;

			int byte_read = image_stream.EndRead (ar);
			if (byte_read == 0) {
				image_stream.Close ();
				OnCompleted ();
				return;
			}

			Gtk.Application.Invoke (this, null, delegate (object sender, EventArgs e) {
				try {
					if (!is_disposed && Write (buffer, (ulong)byte_read))
						image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
				} catch (System.ObjectDisposedException od) {
				} catch (GLib.GException ge) {
				}
			});
		}
#endregion
	}
}	       

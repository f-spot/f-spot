//
// GdkImageLoader.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2014 Daniel Köb
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Threading;

using Gdk;

using FSpot.Imaging;
using FSpot.Thumbnail;

using Hyena;

using TagLib.Image;

namespace FSpot.Loaders
{
	public class GdkImageLoader : Gdk.PixbufLoader, IImageLoader
	{
#region public api

		// FIXME: Probably really shouldn't be doing this?
		~GdkImageLoader ()
		{
			if (!is_disposed) {
				Dispose ();
			}
		}

		public void Load (SafeUri uri)
		{
			if (is_disposed)
				return;

			//First, send a thumbnail if we have one
			if ((thumb = App.Instance.Container.Resolve<IThumbnailService> ().TryLoadThumbnail (uri, ThumbnailSize.Large)) != null) {
				pixbuf_orientation = ImageOrientation.TopLeft;
				AreaPrepared?.Invoke (this, new AreaPreparedEventArgs (true));
				AreaUpdated?.Invoke (this, new AreaUpdatedEventArgs (new Rectangle (0, 0, thumb.Width, thumb.Height)));
			}

			using (var image_file = App.Instance.Container.Resolve<IImageFileFactory> ().Create (uri)) {
				image_stream = image_file.PixbufStream ();
				pixbuf_orientation = image_file.Orientation;
			}

			Loading = true;
			// The ThreadPool.QueueUserWorkItem hack is there cause, as the bytes to read are present in the stream,
			// the Read is CompletedAsynchronously, blocking the mainloop
			image_stream.BeginRead (buffer, 0, count, delegate (IAsyncResult r) {
				ThreadPool.QueueUserWorkItem (delegate {
					HandleReadDone (r);});
			}, null);
		}

		new public event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		new public event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		public event EventHandler Completed;

		Pixbuf thumb;

		new public Pixbuf Pixbuf {
			get {
				if (thumb != null) {
					return thumb;
				}
				return base.Pixbuf;
			}
		}

		public bool Loading { get; private set; } = false;

		bool notify_prepared;

		public bool Prepared { get; private set; } = false;

		ImageOrientation pixbuf_orientation = ImageOrientation.TopLeft;

		public ImageOrientation PixbufOrientation {
			get { return pixbuf_orientation; }
		}

		bool is_disposed;

		public override void Dispose ()
		{
			is_disposed = true;
			if (image_stream != null) {
				try {
					image_stream.Close ();
				} catch (GLib.GException) {
				}
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
				try {
					return base.Close ();
				}
				catch (GLib.GException) {
					return false;
				}
			}
		}
#endregion

#region event handlers
		protected override void OnAreaPrepared ()
		{
			if (is_disposed)
				return;

			Prepared = notify_prepared = true;
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

			Completed?.Invoke (this, EventArgs.Empty);
			Close ();
		}
#endregion

#region private stuffs
		System.IO.Stream image_stream;
		const int count = 1 << 16;
		byte[] buffer = new byte [count];
		bool notify_completed;
		Rectangle damage;
		readonly object sync_handle = new object ();

		void HandleReadDone (IAsyncResult ar)
		{
			if (is_disposed)
				return;

			int byte_read = image_stream.EndRead (ar);
			lock (sync_handle) {
				if (byte_read == 0) {
					image_stream.Close ();
					Close ();
					Loading = false;
					notify_completed = true;
				} else {
					try {
						if (!is_disposed && Write (buffer, (ulong)byte_read)) {
							image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
						}
					} catch (ObjectDisposedException) {
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

					AreaPrepared?.Invoke (this, new AreaPreparedEventArgs (false));
				}

				//Send the AreaUpdated events
				if (damage != Rectangle.Zero) {
					AreaUpdated?.Invoke (this, new AreaUpdatedEventArgs (damage));
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

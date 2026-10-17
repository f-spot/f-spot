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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Threading;

using FSpot.Imaging;
using FSpot.Thumbnail;
using FSpot.Utils;

using Gdk;

using Hyena;

using TagLib.Image;

namespace FSpot.Loaders
{
	public class GdkImageLoader : Gdk.PixbufLoader, IImageLoader
	{
		public GdkImageLoader ()
		{
			SizePrepared += HandleSizePrepared;
		}

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
			if ((thumb = App.Instance.Container.Resolve<IThumbnailService> ().TryLoadThumbnail (uri, ThumbnailSize.Large).ToPixbuf ()) != null) {
				pixbuf_orientation = ImageOrientation.TopLeft;
				thumbnailHandoff.Hold ();
				AreaPrepared?.Invoke (this, new AreaPreparedEventArgs (true));
				AreaUpdated?.Invoke (this, new AreaUpdatedEventArgs (new Rectangle (0, 0, thumb.Width, thumb.Height)));
			}

			using (var image_file = App.Instance.Container.Resolve<IImageFileFactory> ().Create (uri)) {
				image_stream = image_file.OpenImageStream ();
				pixbuf_orientation = image_file.Orientation;
			}

			Loading = true;
			// The ThreadPool.QueueUserWorkItem hack is there cause, as the bytes to read are present in the stream,
			// the Read is CompletedAsynchronously, blocking the mainloop
			image_stream.BeginRead (buffer, 0, count, delegate (IAsyncResult r) {
				ThreadPool.QueueUserWorkItem (delegate {
					HandleReadDone (r);
				});
			}, null);
		}

		new public event EventHandler<AreaPreparedEventArgs> AreaPrepared;
		new public event EventHandler<AreaUpdatedEventArgs> AreaUpdated;
		public event EventHandler Completed;

		Pixbuf thumb;
		readonly ThumbnailHandoff thumbnailHandoff = new ();

		internal void ClaimThumbnail ()
			=> thumbnailHandoff.Claim ();

		void ReleaseThumbnailForTheView ()
		{
			if (thumbnailHandoff.Release () && thumb != null)
				thumb.Dispose ();
			thumb = null;
		}

		new public Pixbuf Pixbuf {
			get {
				if (over_budget)
					return null;
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

		// Set on the GTK main thread by Dispose and read on the thread pool from the
		// BeginRead completion in HandleReadDone, so it needs a barrier.
		volatile bool is_disposed;

		// Set from SizePrepared on the thread pool during Write, read on the main
		// thread when the idle handler decides whether to show the pixbuf.
		volatile bool over_budget;

		public override void Dispose ()
		{
			is_disposed = true;
			ReleaseImageStream ();
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
				} catch (GLib.GException) {
					return false;
				}
			}
		}
		#endregion

		#region event handlers
		void HandleSizePrepared (object sender, SizePreparedArgs args)
		{
			if (!DecodeBudget.Exceeds (args.Width, args.Height))
				return;

			// Allocate a 1×1 instead of the header size, then hide it. PhotoImageView
			// treats a null Pixbuf as a failed load and shows the error image.
			over_budget = true;
			SetSize (1, 1);
		}

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

			var area = new Rectangle (x, y, width, height);
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
		byte[] buffer = new byte[count];
		bool notify_completed;
		Rectangle damage;
		readonly object sync_handle = new object ();

		void ReleaseImageStream ()
		{
			System.IO.Stream stream;
			lock (sync_handle) {
				stream = image_stream;
				image_stream = null;
			}

			if (stream == null)
				return;

			try {
				stream.Close ();
			} catch (GLib.GException) {
			} catch (ObjectDisposedException) {
			}
		}

		void HandleReadDone (IAsyncResult ar)
		{
			bool disposed;
			bool endOfStream = false;
			bool decoderFailed = false;
			bool stop = false;
			lock (sync_handle) {
				disposed = is_disposed;
				if (disposed) {
					try {
						image_stream?.EndRead (ar);
					} catch (ObjectDisposedException) {
					} catch (System.IO.IOException) {
					} catch (GLib.GException) {
					}
				} else if (image_stream == null) {
					decoderFailed = true;
					Loading = false;
					notify_completed = true;
				} else {
					try {
						int byte_read = image_stream.EndRead (ar);
						if (byte_read == 0) {
							endOfStream = true;
							Loading = false;
							notify_completed = true;
						} else {
							// SizePrepared runs inside Write, so the budget flag is only
							// visible after Write returns.
							bool wrote = !is_disposed && Write (buffer, (ulong)byte_read);
							if (wrote && !over_budget && image_stream != null)
								image_stream.BeginRead (buffer, 0, count, HandleReadDone, null);
							else {
								stop = true;
								Loading = false;
								notify_completed = true;
							}
						}
					} catch (ObjectDisposedException) {
						decoderFailed = true;
						Loading = false;
						notify_completed = true;
					} catch (System.IO.IOException) {
						decoderFailed = true;
						Loading = false;
						notify_completed = true;
					} catch (GLib.GException) {
						// Unrecognized format (SVG on quartz, etc.): without this
						// Completed never fires and PhotoImageView keeps the previous pixbuf.
						decoderFailed = true;
						Loading = false;
						notify_completed = true;
					}
				}
			}

			if (disposed) {
				ReleaseImageStream ();
				return;
			}

			if (endOfStream) {
				ReleaseImageStream ();
			} else if (decoderFailed) {
				// The view may already be showing this thumbnail. Dispose it on
				// the GTK idle, once, after the view has dropped its reference.
				ReleaseImageStream ();
			} else if (stop)
				ReleaseImageStream ();

			GLib.Idle.Add (delegate {
				//Send the AreaPrepared event
				if (notify_prepared && !over_budget) {
					notify_prepared = false;
					ReleaseThumbnailForTheView ();

					AreaPrepared?.Invoke (this, new AreaPreparedEventArgs (false));
				} else if (decoderFailed)
					ReleaseThumbnailForTheView ();

				//Send the AreaUpdated events
				if (damage != Rectangle.Zero && !over_budget) {
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

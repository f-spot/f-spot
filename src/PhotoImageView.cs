//
// FSpot.Widgets.PhotoImageView.cs
//
// Author(s)
//	Larry Ewing  <lewing@novell.com>
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details.
//

using System;
using FSpot.Editors;
using FSpot.Utils;

using Gdk;

namespace FSpot.Widgets {
	public enum ProgressType {
		None,
		Async,
		Full
	}

	public class PhotoImageView : ImageView {
		public delegate void PhotoChangedHandler (PhotoImageView view);
		public event PhotoChangedHandler PhotoChanged;
		
		protected BrowsablePointer item;
		protected FSpot.Loupe loupe;
		protected FSpot.Loupe sharpener;
		ProgressType load_async = ProgressType.Full;
		bool progressive_display;
		public GdkGlx.Context Glx;

		public PhotoImageView (IBrowsableCollection query) : this (new BrowsablePointer (query, -1))
		{
			FSpot.ColorManagement.PhotoImageView = this;
		}

		public PhotoImageView (BrowsablePointer item)
		{
			loader = new FSpot.AsyncPixbufLoader ();
			loader.AreaUpdated += HandlePixbufAreaUpdated;
			loader.AreaPrepared += HandlePixbufPrepared;
			loader.Done += HandleDone;
			
			FSpot.ColorManagement.PhotoImageView = this;
			Transform = FSpot.ColorManagement.StandardTransform (); //for preview windows

			Accelerometer.OrientationChanged += HandleOrientationChanged;

			this.item = item;
			item.Changed += PhotoItemChanged;
			this.Destroyed += HandleDestroyed;
		}
		
		protected override void OnStyleSet (Gtk.Style previous)
		{
			CheckPattern = new CheckPattern (this.Style.Backgrounds [(int)Gtk.StateType.Normal]);
		}

		new public BrowsablePointer Item {
			get { return item; }
		}

		private IBrowsableCollection query;
		public IBrowsableCollection Query {
			get { return item.Collection; }
		}

		public Loupe Loupe {
			get { return loupe; }
		}

		public Gdk.Pixbuf CompletePixbuf ()
		{
			loader.LoadToDone ();
			return this.Pixbuf;
		}

		public void HandleOrientationChanged (object sender, EventArgs e)
		{
			Reload ();
		}

		public void Reload ()
		{
			if (Item == null || !Item.IsValid)
				return;
			
			PhotoItemChanged (Item, null);
		}

		protected override void OnRealized ()
		{
			int [] attr = new int [] {
				(int) GdkGlx.GlxAttribute.Rgba,
				(int) GdkGlx.GlxAttribute.DepthSize, 16,
				(int) GdkGlx.GlxAttribute.DoubleBuffer,
				(int) GdkGlx.GlxAttribute.None
			};

			try {
				Glx = new GdkGlx.Context (Screen, attr);
				Colormap = Glx.GetColormap ();
			} catch (GdkGlx.GlxException e) {
				Console.WriteLine ("Error initializing the OpenGL context:{1} {0}", e, Environment.NewLine);
			}

			base.OnRealized ();
		}

		protected override void OnUnrealized ()
		{
			base.OnUnrealized ();

			if (Glx != null)
				Glx.Destroy ();
		}

		// Display.
		private void HandlePixbufAreaUpdated (object sender, AreaUpdatedEventArgs args)
		{
			if (!ShowProgress)
				return;

			Gdk.Rectangle area = this.ImageCoordsToWindow (args.Area);
			this.QueueDrawArea (area.X, area.Y, area.Width, area.Height);
		}
		

		private void HandlePixbufPrepared (object sender, AreaPreparedEventArgs args)
		{
			if (!ShowProgress)
				return;

			Gdk.Pixbuf prev = this.Pixbuf;
			Gdk.Pixbuf next = loader.Pixbuf;

			this.Pixbuf = next;
			if (prev != null)
				prev.Dispose ();

			this.ZoomFit (args.ReducedResolution);
		}

		private void HandleDone (object sender, System.EventArgs args)
		{
			// FIXME the error hander here needs to provide proper information and we should
			// pass the state and the write exception in the args
			Gdk.Pixbuf prev = this.Pixbuf;
			if (loader.Pixbuf == null) {
				System.Exception ex = null;

				// FIXME in some cases the image passes completely through the
				// pixbuf loader without properly loading... I'm not sure what to do about this other
				// than try to load the image one last time.
				this.Pixbuf = null;
				if (!loader.Loading) {
					try {
						Log.Warning ("Falling back to file loader");

						this.Pixbuf = FSpot.PhotoLoader.Load (item.Collection, 
										      item.Index);
					} catch (System.Exception e) {
						if (!(e is GLib.GException))
							System.Console.WriteLine (e.ToString ());

						ex = e;
					}
				}

				if (this.Pixbuf == null) {
					LoadErrorImage (ex);
				} else {
					this.ZoomFit ();
				}
			} else {
				if (Pixbuf != loader.Pixbuf)
					Pixbuf = loader.Pixbuf;

				if (!loader.Prepared || !ShowProgress) {
					this.ZoomFit ();
				}
			}

			progressive_display = true;

			if (prev != this.Pixbuf && prev != null)
				prev.Dispose ();
		}
		
		private bool ShowProgress {
			get {
				return !(load_async != ProgressType.Full || !progressive_display);
			}
		}
	
		// Zoom scaled between 0.0 and 1.0
		public double NormalizedZoom {
			get {
				return (Zoom - MIN_ZOOM) / (MAX_ZOOM - MIN_ZOOM);
			}
			set {
				Zoom = (value * (MAX_ZOOM - MIN_ZOOM)) + MIN_ZOOM;
			}
		}
		
		FSpot.AsyncPixbufLoader loader;

		private void LoadErrorImage (System.Exception e)
		{
			// FIXME we should check the exception type and do something
			// like offer the user a chance to locate the moved file and
			// update the db entry, but for now just set the error pixbuf
			
			Gdk.Pixbuf old = this.Pixbuf;
			this.Pixbuf = new Gdk.Pixbuf (PixbufUtils.ErrorPixbuf, 0, 0, 
						      PixbufUtils.ErrorPixbuf.Width, 
						      PixbufUtils.ErrorPixbuf.Height);
			if (old != null)
				old.Dispose ();
			
			this.ZoomFit ();
		}

		private void PhotoItemChanged (object sender, BrowsablePointerChangedEventArgs args) 
		{
			// If it is just the position that changed fall out
			if (args != null && 
			    args.PreviousItem != null &&
			    Item.IsValid &&
			    (args.PreviousIndex != item.Index) &&
			    (this.Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri))
				return;

			// Don't reload if the image didn't change at all.
			if (args != null && args.Changes != null &&
			    !args.Changes.DataChanged &&
			    args.PreviousItem != null &&
			    Item.IsValid &&
			    this.Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri)
				return;

			if (args != null &&
			    args.PreviousItem != null && 
			    Item.IsValid && 
			    Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri &&
			    load_async == ProgressType.Full)
				progressive_display = false;

			if (load_async != ProgressType.None) {
				Gdk.Pixbuf old = this.Pixbuf;
				try {
					if (Item.IsValid) {
						System.Uri uri = Item.Current.DefaultVersionUri;
						loader.Load (uri);
					} else
						LoadErrorImage (null);

				} catch (System.Exception e) {
					System.Console.WriteLine (e.ToString ());
					LoadErrorImage (e);
				}
				if (old != null)
					old.Dispose ();
			} else {	
				Gdk.Pixbuf old = this.Pixbuf;
				this.Pixbuf = FSpot.PhotoLoader.Load (item.Collection, 
								      item.Index);
				if (old != null)
					old.Dispose ();

				this.ZoomFit ();
			}
			
			Selection = Gdk.Rectangle.Zero;

			if (PhotoChanged != null)
				PhotoChanged (this);
		}
		

		private void HandleLoupeDestroy (object sender, EventArgs args)
		{
			if (sender == loupe)
				loupe = null;

			if (sender == sharpener)
				sharpener = null;

		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if ((evnt.State & (ModifierType.Mod1Mask | ModifierType.ControlMask)) != 0)
				return base.OnKeyPressEvent (evnt);

			bool handled = true;
		
			// Scroll if image is zoomed in (scrollbars are visible)
			Gtk.ScrolledWindow scrolled_w = this.Parent as Gtk.ScrolledWindow;
			bool scrolled = scrolled_w != null && !this.Fit;
		
			// Go to the next/previous photo when not zoomed (no scrollbars)
			switch (evnt.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
			case Gdk.Key.h:
			case Gdk.Key.H:
			case Gdk.Key.k:
			case Gdk.Key.K:
				if (scrolled)
					handled = false;
				else
					this.Item.MovePrevious ();
				break;
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.BackSpace:
			case Gdk.Key.b:
			case Gdk.Key.B:
				this.Item.MovePrevious ();
				break;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.j:
			case Gdk.Key.J:
			case Gdk.Key.l:
			case Gdk.Key.L:
				if (scrolled)
					handled = false;
				else
					this.Item.MoveNext ();
				break;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
			case Gdk.Key.n:
			case Gdk.Key.N:
				this.Item.MoveNext ();
				break;
			case Gdk.Key.Home:
			case Gdk.Key.KP_Home:
				this.Item.Index = 0;
				break;
			case Gdk.Key.r:
			case Gdk.Key.R:
				this.Item.Index = new Random().Next(0, this.Query.Count - 1);
				break;
			case Gdk.Key.End:
			case Gdk.Key.KP_End:
				this.Item.Index = this.Query.Count - 1;
				break;
			default:
				handled = false;
				break;
			}

			return handled || base.OnKeyPressEvent (evnt);
		}

		public void ShowHideLoupe ()
		{
			if (loupe == null) {
				loupe = new Loupe (this);
				loupe.Destroyed += HandleLoupeDestroy;
				loupe.Show ();
			} else {
				loupe.Destroy ();	
			}
			
		}
		
		public void ShowSharpener ()
		{
			if (sharpener == null) {
				sharpener = new Sharpener (this);
				sharpener.Destroyed += HandleLoupeDestroy;
			}

			sharpener.Show ();	
		}
		
		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			//loader.AreaUpdated -= HandlePixbufAreaUpdated;
			//loader.AreaPrepared -= HandlePixbufPrepared;
			loader.Dispose ();
		}
	}
}

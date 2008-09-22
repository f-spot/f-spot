using System;
using FSpot.Editors;
using FSpot.Utils;

namespace FSpot {
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
		private OldEditor editor;

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
			this.Transform = FSpot.ColorManagement.StandartTransform (); //for preview windows

			Accelerometer.OrientationChanged += HandleOrientationChanged;

			HandleRealized (null, null);

			this.SizeAllocated += HandleSizeAllocated;
			this.KeyPressEvent += HandleKeyPressEvent;
			//this.Realized += HandleRealized;
			this.Unrealized += HandleUnrealized;
			this.ScrollEvent += HandleScrollEvent;
			this.item = item;
			item.Changed += PhotoItemChanged;
			this.Destroyed += HandleDestroyed;
			this.SetTransparentColor (this.Style.BaseColors [(int)Gtk.StateType.Normal]);
		}
		
		protected override void OnStyleSet (Gtk.Style previous)
		{
			this.SetTransparentColor (this.Style.Backgrounds [(int)Gtk.StateType.Normal]);
		}

		new public BrowsablePointer Item {
			get {
				return item;
			}
		}

		private IBrowsableCollection query;
		public IBrowsableCollection Query {
			get {
				return item.Collection;
			}
#if false
			set {
				if (query != null) {
					query.Changed -= HandleQueryChanged;
					query.ItemsChanged -= HandleQueryItemsChanged;
				}

				query = value;
				query.Changed += HandleQueryChanged;
				query.ItemsChanged += HandleQueryItemsChanged;
			}
#endif
		}

		public OldEditor Editor {
			get { return editor; }
			set {
				value.Done += HandleEditorDone;

				if (editor != null)
					editor.Destroy ();
				
				editor = value;
			}
		}

		private void HandleEditorDone (object sender, EventArgs args)
		{
			OldEditor old = sender as OldEditor;

			old.Done -= HandleEditorDone;
				
			if (old == editor)
				editor = null;
		}

		public Loupe Loupe {
			get { return loupe; }
		}

		public Gdk.Pixbuf CompletePixbuf ()
		{
			loader.LoadToDone ();
			return this.Pixbuf;
		}

		public void HandleOrientationChanged (object sender)
		{
			Reload ();
		}

		public void Reload ()
		{
			if (Item == null || !Item.IsValid)
				return;
			
			PhotoItemChanged (Item, null);
		}

		[GLib.ConnectBefore]
		private void HandleRealized (object sender, EventArgs args)
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
		}

		private void HandleUnrealized (object sender, EventArgs args)
		{
			if (Glx != null)
				Glx.Destroy ();
		}

		// Display.
		private void HandlePixbufAreaUpdated (object sender, AreaUpdatedArgs args)
		{
			if (!ShowProgress)
				return;

			Gdk.Rectangle area = this.ImageCoordsToWindow (args.Area);
			this.QueueDrawArea (area.X, area.Y, area.Width, area.Height);
		}
		

		private void HandlePixbufPrepared (object sender, AreaPreparedArgs args)
		{
			if (!ShowProgress)
				return;

			Gdk.Pixbuf prev = this.Pixbuf;
			Gdk.Pixbuf next = loader.Pixbuf;

			this.Pixbuf = next;
			if (prev != null)
				prev.Dispose ();

			UpdateMinZoom ();
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
						System.Console.WriteLine ("Falling back to file loader");

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
					UpdateMinZoom ();
					this.ZoomFit ();
				}
			} else {
				this.Pixbuf = loader.Pixbuf;

				if (!loader.Prepared || !ShowProgress) {
					UpdateMinZoom ();
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

		private bool fit = true;
		public bool Fit {
			get {
				return (Zoom == MIN_ZOOM);
			}
			set {
				if (!fit && value)
					ZoomFit ();
				
				fit = value;
			}
		}


		public double Zoom {
			get {
				double x, y;
				this.GetZoom (out x, out y);
				return x;
			}
			
			set {
				//Console.WriteLine ("Setting zoom to {0}, MIN = {1}", value, MIN_ZOOM);
				value = System.Math.Min (value, MAX_ZOOM);
				value = System.Math.Max (value, MIN_ZOOM);

				double zoom = Zoom;
				if (value == zoom)
					return;

				if (System.Math.Abs (zoom - value) < System.Double.Epsilon)
					return;

				if (value == MIN_ZOOM)
					this.Fit = true;
				else {
					this.Fit = false;
					this.SetZoom (value, value);
				}
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
		
		private void HandleSizeAllocated (object sender, Gtk.SizeAllocatedArgs args)
		{
			if (fit)
				ZoomFit ();
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
			
			UpdateMinZoom ();
			this.ZoomFit ();
		}

		private void PhotoItemChanged (BrowsablePointer item, BrowsablePointerChangedArgs args) 
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

				UpdateMinZoom ();
				this.ZoomFit ();
			}
			
			this.UnsetSelection ();

			if (PhotoChanged != null)
				PhotoChanged (this);
		}
		
		public void ZoomIn ()
		{
			Zoom = Zoom * ZOOM_FACTOR;
		}
		
		public void ZoomOut ()
		{
			Zoom = Zoom / ZOOM_FACTOR;
		}
		
		bool upscale;
		private void ZoomFit ()
		{
			ZoomFit (upscale);
		}

		public void ZoomFit (bool upscale)
		{			
			Gdk.Pixbuf pixbuf = this.Pixbuf;
			Gtk.ScrolledWindow scrolled = this.Parent as Gtk.ScrolledWindow;
			this.upscale = upscale;
			
			if (pixbuf == null)
				return;

			if (scrolled != null)
				scrolled.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Never);

			int available_width = (scrolled != null) ? scrolled.Allocation.Width : this.Allocation.Width;
			int available_height = (scrolled != null) ? scrolled.Allocation.Height : this.Allocation.Height;

			double zoom_to_fit = ZoomUtils.FitToScale ((uint) available_width, 
								   (uint) available_height,
								   (uint) pixbuf.Width, 
								   (uint) pixbuf.Height, 
								   upscale);

			double image_zoom = zoom_to_fit;

			this.SetZoom (image_zoom, image_zoom);
			
			if (scrolled != null)
				scrolled.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic);
		}
		
		private void HandleLoupeDestroy (object sender, EventArgs args)
		{
			if (sender == loupe)
				loupe = null;

			if (sender == sharpener)
				sharpener = null;

		}

		[GLib.ConnectBefore]
		private void HandleKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
		{
			bool alt = Gdk.ModifierType.Mod1Mask == (args.Event.State & Gdk.ModifierType.Mod1Mask);

			// FIXME I really need to figure out why overriding is not working
			// for any of the default handlers.
			args.RetVal = true;
		
			// Scroll if image is zoomed in (scrollbars are visible)
			Gtk.ScrolledWindow scrolled = this.Parent as Gtk.ScrolledWindow;
			if (scrolled != null && !this.Fit) {
				Gtk.Adjustment vadj = scrolled.Vadjustment;
				Gtk.Adjustment hadj = scrolled.Hadjustment;
				switch (args.Event.Key) {					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
				case Gdk.Key.k:
				case Gdk.Key.K:
					vadj.Value -= vadj.StepIncrement;
					if (vadj.Value < vadj.Lower)
						vadj.Value = vadj.Lower;
					return;
				case Gdk.Key.Left:
				case Gdk.Key.KP_Left:
				case Gdk.Key.h:
					hadj.Value -= hadj.StepIncrement;
					if (hadj.Value < hadj.Lower)
						hadj.Value = hadj.Lower;
					return;
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
				case Gdk.Key.j:
				case Gdk.Key.J:
					vadj.Value += vadj.StepIncrement;
					if (vadj.Value > vadj.Upper - vadj.PageSize)
						vadj.Value = vadj.Upper - vadj.PageSize;
					return;
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
				case Gdk.Key.l:
					hadj.Value += hadj.StepIncrement;
					if (hadj.Value > hadj.Upper - hadj.PageSize)
						hadj.Value = hadj.Upper - hadj.PageSize;
					return;
				}
			}
			
			// Go to the next/previous photo when not zoomed (no scrollbars)
			switch (args.Event.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.BackSpace:
			case Gdk.Key.h:
			case Gdk.Key.H:
			case Gdk.Key.k:
			case Gdk.Key.K:
			case Gdk.Key.b:
			case Gdk.Key.B:
				this.Item.MovePrevious ();
				break;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
			case Gdk.Key.j:
			case Gdk.Key.J:
			case Gdk.Key.l:
			case Gdk.Key.L:
			case Gdk.Key.n:
			case Gdk.Key.N:
				this.Item.MoveNext ();
				break;
			case Gdk.Key.Home:
			case Gdk.Key.KP_Home:
				this.Item.Index = 0;
				break;
			case Gdk.Key.End:
			case Gdk.Key.KP_End:
				this.Item.Index = this.Query.Count - 1;
				break;
			case Gdk.Key.Key_0:
			case Gdk.Key.KP_0:
				if (alt) 
					args.RetVal = false;
				else
					this.Fit = true;
				break;
			case Gdk.Key.Key_1:
			case Gdk.Key.KP_1:
				if (alt)
					args.RetVal = false;
				else
					this.Zoom =  1.0;
				break;
			case Gdk.Key.Key_2:
			case Gdk.Key.KP_2:
				if (alt) 
					args.RetVal = false;
				else
					this.Zoom = 2.0;
				break;
			case Gdk.Key.minus:
			case Gdk.Key.KP_Subtract:
				ZoomOut ();
				break;
			case Gdk.Key.equal:
			case Gdk.Key.plus:
			case Gdk.Key.KP_Add:
				ZoomIn ();
				break;
			default:
				args.RetVal = false;
				return;
			}

			return;
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
		
		[GLib.ConnectBefore]
		private void HandleScrollEvent (object sender, Gtk.ScrollEventArgs args)
		{
			//For right now we just disable fit mode and let the parent event handlers deal
			//with the real actions.
			this.Fit = false;
		}
		
		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			//loader.AreaUpdated -= HandlePixbufAreaUpdated;
			//loader.AreaPrepared -= HandlePixbufPrepared;
			loader.Dispose ();
		}
	}
}


namespace FSpot {
	public class PhotoImageView : ImageView {
		public static double ZoomMultipler = 1.1;

		public delegate void PhotoChangedHandler (PhotoImageView view);
		public event PhotoChangedHandler PhotoChanged;
		
		protected BrowsablePointer item;
		protected FSpot.Loupe loupe;
		protected FSpot.Loupe sharpener;
		
		public PhotoImageView (IBrowsableCollection query)
		{
			loader = new FSpot.AsyncPixbufLoader ();
			loader.AreaUpdated += HandlePixbufAreaUpdated;
			loader.AreaPrepared += HandlePixbufPrepared;
			loader.Done += HandleDone;
			
			this.SizeAllocated += HandleSizeAllocated;
			this.KeyPressEvent += HandleKeyPressEvent;
			this.ScrollEvent += HandleScrollEvent;
			this.item = new BrowsablePointer (query, -1);
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

		public Gdk.Pixbuf CompletePixbuf ()
		{
			loader.LoadToDone ();
			return this.Pixbuf;
		}

		public void Reload ()
		{
			if (!Item.IsValid)
				return;
			
			PhotoItemChanged (Item, null);
		}

		// Display.
		private void HandlePixbufAreaUpdated (object sender, Gdk.Rectangle area)
		{
			area = this.ImageCoordsToWindow (area);
			this.QueueDrawArea (area.X, area.Y, area.Width, area.Height);
		}
		
		private void HandlePixbufPrepared (object sender, System.EventArgs args)
		{
			Gdk.Pixbuf prev = this.Pixbuf;
			Gdk.Pixbuf next = loader.Pixbuf;

#if SPEED_COPY_DATA
			if (next != null && prev != null && next.Width == prev.Width && prev.Height == next.Height)
				prev.CopyArea (0, 0, next.Width, next.Height, next, 0, 0);
			else
				next.Fill (0x00000000);
#endif
#if true
			System.Uri uri = Item.Current.DefaultVersionUri;
			try {

				Gdk.Pixbuf thumb = new Gdk.Pixbuf (ThumbnailGenerator.ThumbnailPath (uri));
				if (thumb != null && next != null)
					thumb.Composite (next, 0, 0,
							 next.Width, next.Height,
							 0.0, 0.0,
							 next.Width/(double)thumb.Width, next.Height/(double)thumb.Height,
							 Gdk.InterpType.Bilinear, 0xff);
				
				if (thumb != null) {
					if (!ThumbnailGenerator.ThumbnailIsValid (thumb, uri))
						FSpot.ThumbnailGenerator.Default.Request (uri.LocalPath, 0, 256, 256);
					
					thumb.Dispose ();
				}
			} catch (System.Exception e) {
				FSpot.ThumbnailGenerator.Default.Request (uri.LocalPath, 0, 256, 256);	
				if (!(e is GLib.GException)) 
					System.Console.WriteLine (e.ToString ());
			}
#endif

			this.Pixbuf = next;
			if (prev != null)
				prev.Dispose ();

			this.ZoomFit ();
		}

		private void HandleDone (object sender, System.EventArgs args)
		{
			// FIXME the error hander here needs to provide proper information and we should
			// pass the state and the write exception in the args
			Gdk.Pixbuf prev = this.Pixbuf;
			if (loader.Pixbuf == null) {
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
					}
				}

				if (this.Pixbuf == null)
					this.Pixbuf = new Gdk.Pixbuf (PixbufUtils.ErrorPixbuf, 0, 0, 
								      PixbufUtils.ErrorPixbuf.Width, 
								      PixbufUtils.ErrorPixbuf.Height);

				this.ZoomFit ();
			} else {
				this.Pixbuf = loader.Pixbuf;

				if (!loader.Prepared)
					this.ZoomFit ();
			}

			if (prev != this.Pixbuf && prev != null)
				prev.Dispose ();
		}		
		
		private bool fit = true;
		public bool Fit {
			get {
				return fit;
			}
			set {
				fit = value;
				if (fit)
					ZoomFit ();
			}
		}


		public double Zoom {
			get {
				double x, y;
				this.GetZoom (out x, out y);
				return x;
			}
			
			set {
				this.Fit = false;
				this.SetZoom (value, value);
			}
		}
		
		private void HandleSizeAllocated (object sender, Gtk.SizeAllocatedArgs args)
		{
			if (fit)
				ZoomFit ();
		}	

		bool load_async = true;
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

		private void PhotoItemChanged (BrowsablePointer item, BrowsablePointerChangedArgs args) 
		{
			// If it is just the position that changed fall out
			if (args.PreviousItem != null && 
			    Item.IsValid && 
			    this.Item.Current.DefaultVersionUri == args.PreviousItem.DefaultVersionUri)
				return;

			if (load_async) {
				Gdk.Pixbuf old = this.Pixbuf;
				try {
					if (Item.IsValid) {
						System.Uri uri = Item.Current.DefaultVersionUri;
						loader.Load (uri.LocalPath);
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
			
			this.UnsetSelection ();

			if (PhotoChanged != null)
				PhotoChanged (this);
		}

		private void ZoomFit ()
		{
			Gdk.Pixbuf pixbuf = this.Pixbuf;
			Gtk.ScrolledWindow scrolled = this.Parent as Gtk.ScrolledWindow;
			
			if (pixbuf == null)
				return;

			int available_width = this.Allocation.Width;
			int available_height = this.Allocation.Height;
		
			double zoom_to_fit = ZoomUtils.FitToScale ((uint) available_width, 
								   (uint) available_height,
								   (uint) pixbuf.Width, 
								   (uint) pixbuf.Height, 
								   true);
			
			double image_zoom = zoom_to_fit;
			/*
			System.Console.WriteLine ("Zoom = {0}, {1}, {2}", image_zoom, 
						  available_width, 
						  available_height);
			*/

			if (scrolled != null)
				scrolled.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Never);

			this.SetZoom (image_zoom, image_zoom);
			
			if (scrolled != null)
				scrolled.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic);
		}

		[GLib.ConnectBefore]
		private void HandleKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
		{
			// FIXME I really need to figure out why overriding is not working
			// for any of the default handlers.

			switch (args.Event.Key) {
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
				this.Item.MovePrevious ();
				break;
			case Gdk.Key.Home:
			case Gdk.Key.KP_Home:
				this.Item.Index = 0;
				break;
			case Gdk.Key.End:
			case Gdk.Key.KP_End:
				this.Item.Index = this.Query.Count - 1;
				break;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
				this.Item.MoveNext ();
				break;
			case Gdk.Key.Key_0:
			case Gdk.Key.KP_0:
				this.Fit = true;
				break;
			case Gdk.Key.Key_1:
			case Gdk.Key.KP_1:
				this.Zoom =  1.0;
				break;
			case Gdk.Key.Key_2:
			case Gdk.Key.KP_2:
				this.Zoom = 2.0;
				break;
			case Gdk.Key.minus:
			case Gdk.Key.KP_Subtract:
				this.Zoom /= ZoomMultipler;
				break;
			case Gdk.Key.s:
				if (sharpener == null)
					sharpener = new Sharpener (this);

				sharpener.Show ();
				break;
			case Gdk.Key.v:
				if (loupe == null)
					loupe = new Loupe (this);

				loupe.Show ();
				break;
			case Gdk.Key.plus:
			case Gdk.Key.KP_Add:
				this.Zoom *= ZoomMultipler;
				break;
			default:
				args.RetVal = false;
				return;
			}
			args.RetVal = true;
			return;
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

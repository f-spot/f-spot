
namespace FSpot {
	public class PhotoImageView : ImageView {
		public PhotoImageView (PhotoQuery query)
		{
			loader = new FSpot.AsyncPixbufLoader ();
			this.SizeAllocated += HandleSizeAllocated;
			this.KeyPressEvent += HandleKeyPressEvent;
			this.ScrollEvent += HandleScrollEvent;
			this.Destroyed += HandleDestroy;
			this.Query = query;
		}
		
		public static double ZoomMultipler = 1.1;

		public delegate void PhotoChangedHandler (PhotoImageView view);
		public event PhotoChangedHandler PhotoChanged;
		
		private int current_photo = -1;
		public int CurrentPhoto {
			get {
				return current_photo;
			}
			set {
				if (current_photo == value && this.Pixbuf != null){
					return;
				} else {
					current_photo = value;
					this.PhotoIndexChanged ();
				}
			}
		}

		public Photo Photo {
			get {
				if (CurrentPhotoValid ()) 
					return this.Query.Photos [CurrentPhoto];
				else 
					return null;
			}
		}

		private PhotoQuery query;
		public PhotoQuery Query {
			get {
				return query;
			}
			set {
				if (query != null) {
					query.Changed -= HandleQueryChanged;
					query.ItemChanged -= HandleQueryItemChanged;
				}

				query = value;
				query.Changed += HandleQueryChanged;
				query.ItemChanged += HandleQueryItemChanged;
			}
		}

		public void Reload ()
		{
			if (!CurrentPhotoValid ())
				return;
			
			int idx = CurrentPhoto;
			CurrentPhoto = 0;
			CurrentPhoto = idx;
		}

		private void HandleQueryChanged (IPhotoCollection query)
		{
			if (query == this.query)
				Reload ();
		}

		public void HandleQueryItemChanged (IPhotoCollection query, int item)
		{
			if (item == CurrentPhoto)
				Reload ();
		}

		public bool CurrentPhotoValid ()
		{
			if (query == null ||
			    query.Photos.Length == 0 ||
			    CurrentPhoto >= Query.Photos.Length ||
			    CurrentPhoto < 0) {
				System.Console.WriteLine ("Invalid CurrentPhoto");
				return false;
			}

			return true;
		}

		// Display.
		private void HandlePixbufAreaUpdated (object sender, Gdk.Rectangle area)
		{
			area = this.ImageCoordsToWindow (area);
			this.QueueDrawArea (area.X, area.Y, area.Width, area.Height);
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
		FSpot.AsyncPixbufLoader next_loader;

		private void PhotoIndexChanged () 
		{
			if (!CurrentPhotoValid ())
				return;

			Gdk.Pixbuf old = this.Pixbuf;
			Gdk.Pixbuf current = null;
			
			if (load_async) {
				current = loader.Load (Photo.DefaultVersionPath);
				loader.AreaUpdated += HandlePixbufAreaUpdated;
			} else
				current = FSpot.PhotoLoader.Load (Query, current_photo);
			
			this.Pixbuf = current;
			
			if (old != null)
				old.Dispose ();

			this.UnsetSelection ();
			this.ZoomFit ();

			if (PhotoChanged != null)
				PhotoChanged (this);
		}

		private void ZoomFit ()
		{
			Gdk.Pixbuf pixbuf = this.Pixbuf;
			Gtk.ScrolledWindow scrolled = this.Parent as Gtk.ScrolledWindow;
			
			//System.Console.WriteLine ("ZoomFit");

			if (pixbuf == null) {
				System.Console.WriteLine ("pixbuf == null");
				return;
			}
			int available_width = this.Allocation.Width;
			int available_height = this.Allocation.Height;

		
			double zoom_to_fit = ZoomUtils.FitToScale ((uint) available_width, 
								   (uint) available_height,
								   (uint) pixbuf.Width, 
								   (uint) pixbuf.Height, 
								   false);
			
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

		public void Next () {
			if (CurrentPhoto + 1 < query.Photos.Length)
				CurrentPhoto++;
			else
				CurrentPhoto = 0;
		}
		
		public void Prev () 
		{
			if (CurrentPhoto > 0)
				CurrentPhoto --;
			else
				CurrentPhoto = query.Photos.Length - 1;
		}

		protected override void OnDestroyed ()
		{
			System.Console.WriteLine ("I'm feeling better");
			base.OnDestroyed ();
		}

		[GLib.ConnectBefore]
		private void HandleKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
		{
			// FIXME I really need to figure out why overriding is not working
			// for any of the default handlers.

			switch (args.Event.Key) {
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
				this.Prev ();
				break;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
				this.Next ();
				break;
			case Gdk.Key.Key_0:
				this.Fit = true;
				break;
			case Gdk.Key.Key_1:
				this.Zoom =  1.0;
				break;
			case Gdk.Key.Key_2:
				this.Zoom = 2.0;
				break;
			case Gdk.Key.KP_Subtract:
			case Gdk.Key.minus:
				this.Zoom /= ZoomMultipler;
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
		
		private void HandleDestroy (object sender, System.EventArgs args)
		{
			loader.AreaUpdated -= HandlePixbufAreaUpdated;
		}

		protected override bool OnDestroyEvent (Gdk.Event evnt)
		{
			System.Console.WriteLine ("I'm feeling better");
			return base.OnDestroyEvent (evnt);
		}
	}
}

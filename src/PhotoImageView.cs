
namespace FSpot {
	public class PhotoImageView : ImageView {
		public PhotoImageView (PhotoQuery query)
		{
			this.query = query;
			loader = new FSpot.AsyncPixbufLoader ();
			//scroll_delay = new Delay (new GLib.IdleHandler (IdleUpdateScrollbars));
			this.SizeAllocated += new Gtk.SizeAllocatedHandler (HandleSizeAllocated);
		}
		
		private int current_photo;
		public int CurrentPhoto {
			get {
				return current_photo;
			}
			set {
				if (current_photo == value && this.Pixbuf != null){
					return;
				} else {
					current_photo = value;
					this.PhotoChanged ();
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
					//query.Reload -= HandleQueryReload;
					//query.ItemChanged -= HandleQueryItemChanged;
				}

				query = value;
				//query.Reload += HandleQueryItemReload;
				//query.ItemChanged += HandleQueryItemChanged;
				
				CurrentPhoto = 0;
			}
		}

		public bool CurrentPhotoValid ()
		{
			if (query == null ||
			    query.Photos.Length == 0 ||
			    CurrentPhoto >= Query.Photos.Length) {
				System.Console.WriteLine ("Invalid CurrentPhoto");
				return false;
			}

			return true;
		}

		// Display.
		private void HandlePixbufAreaUpdated (object sender, Gdk.AreaUpdatedArgs args)
		{
			Gdk.Rectangle area = new Gdk.Rectangle (args.X, args.Y, args.Width, args.Height);
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

		private void HandleSizeAllocated (object sender, Gtk.SizeAllocatedArgs args)
		{
			if (fit)
				ZoomFit ();
		}	

		bool load_async = true;
		FSpot.AsyncPixbufLoader loader;
		FSpot.AsyncPixbufLoader next_loader;

		private void PhotoChanged () 
		{
			if (!CurrentPhotoValid ())
				return;

			Gdk.Pixbuf old = this.Pixbuf;
			Gdk.Pixbuf current = null;
			
			if (load_async) {
				current = loader.Load (Photo.DefaultVersionPath);
				loader.Loader.AreaUpdated += HandlePixbufAreaUpdated;
			} else
				current = FSpot.PhotoLoader.Load (Query, current_photo);
			
			this.Pixbuf = current;
			
			if (old != null)
				old.Dispose ();

			this.UnsetSelection ();
			this.ZoomFit ();
		}

		private Delay scroll_delay;
		private bool IdleUpdateScrollbars ()
		{
			(this.Parent as Gtk.ScrolledWindow).SetPolicy (Gtk.PolicyType.Automatic, 
								       Gtk.PolicyType.Automatic);
			return false;
 		}

		private void ZoomFit ()
		{
			Gdk.Pixbuf pixbuf = this.Pixbuf;
			
			System.Console.WriteLine ("ZoomFit");

			if (pixbuf == null) {
				System.Console.WriteLine ("pixbuf == null");
				return;
			}
			int available_width = this.Allocation.Width;
			int available_height = this.Allocation.Height;

		
			double zoom_to_fit = ZoomUtils.FitToScale ((uint) available_width, (uint) available_height,
								   (uint) pixbuf.Width, (uint) pixbuf.Height, false);
			
			double image_zoom = zoom_to_fit;
			System.Console.WriteLine ("Zoom = {0}, {1}, {2}", image_zoom, 
						  available_width, 
						  available_height);
			
			//if (System.Math.Abs (Zoom) < double.Epsilon)
				((Gtk.ScrolledWindow) this.Parent).SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Never);

			this.SetZoom (image_zoom, image_zoom);
			
			((Gtk.ScrolledWindow) this.Parent).SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic);
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
				CurrentPhoto = query.Photos.Length;
		}

		protected override void OnDestroyed ()
		{
			System.Console.WriteLine ("I'm feeling better");
			base.OnDestroyed ();
		}

		protected override bool OnDestroyEvent (Gdk.Event evnt)
		{
			System.Console.WriteLine ("I'm feeling better");
			return base.OnDestroyEvent (evnt);
		}
	}
}

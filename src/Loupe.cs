using Gtk;
using System;
using System.Runtime.InteropServices;
using Cairo;

namespace FSpot {
	public class Sharpener : Loupe {
		Gtk.SpinButton amount_spin = new Gtk.SpinButton (0.5, 100.0, .01);
		Gtk.SpinButton radius_spin = new Gtk.SpinButton (5.0, 50.0, .01);
		Gtk.SpinButton threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01);
		
		public Sharpener (PhotoImageView view) : base (view)
		{	
		}

		protected override void UpdateSample ()
		{
			if (view.Pixbuf == null) {
				System.Console.WriteLine ("no image");
				image.Pixbuf = null;
				return;
			}
			
			Gdk.Pixbuf old = image.Pixbuf;
			Gdk.Pixbuf sample = new Gdk.Pixbuf (view.Pixbuf, region.X, region.Y, region.Width, region.Height);
			if (sample != null) {
				image.Pixbuf = PixbufUtils.UnsharpMask (sample, radius_spin.Value, amount_spin.Value, threshold_spin.Value);
				sample.Dispose ();
			} else 
				image.Pixbuf = null;

			if (old != null)
				old.Dispose ();
		}

		private void HandleSettingsChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}
		
		private void HandleOkClicked ()
		{
			Photo photo = view.Item.Current as Photo;

			if (photo == null)
				return;
			
			Gdk.Pixbuf orig = view.Pixbuf;
			Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig, radius_spin.Value, amount_spin.Value, threshold_spin.Value);
			
			bool create_version = photo.DefaultVersionId == Photo.OriginalVersionId;
			
			try {
				photo.SaveVersion (final, create_version);
			} catch (System.Exception e) {
				string msg = Mono.Posix.Catalog.GetString ("Error saving sharpened photo");
				string desc = String.Format (Mono.Posix.Catalog.GetString ("Received exception \"{0}\". Unable to save image {1}"),
							     e.Message, photo.Name);
				
				HigMessageDialog md = new HigMessageDialog (this, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error, ButtonsType.Ok, 
									    msg,
									    desc);
				md.Run ();
				md.Destroy ();
			}

		}

		protected override void BuildUI ()
		{
			base.BuildUI ();

			this.BorderWidth = 12;
			box.Spacing = 6;

			this.Title = Mono.Posix.Catalog.GetString ("Sharpen");
			Gtk.Table table = new Gtk.Table (3, 2, false);
			table.ColumnSpacing = 6;
			table.RowSpacing = 6;
			
			table.Attach (SetFancyStyle (new Gtk.Label (Mono.Posix.Catalog.GetString ("Amount:"))), 0, 1, 0, 1);
			table.Attach (SetFancyStyle (new Gtk.Label (Mono.Posix.Catalog.GetString ("Radius:"))), 0, 1, 1, 2);
			table.Attach (SetFancyStyle (new Gtk.Label (Mono.Posix.Catalog.GetString ("Threshold:"))), 0, 1, 2, 3);
			
			SetFancyStyle (amount_spin = new Gtk.SpinButton (0.00, 100.0, .01));
			SetFancyStyle (radius_spin = new Gtk.SpinButton (1.0, 50.0, .01));
			SetFancyStyle (threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01));
			amount_spin.Value = .5;
			radius_spin.Value = 5;
			threshold_spin.Value = 0.0;

			amount_spin.ValueChanged += HandleSettingsChanged;
			radius_spin.ValueChanged += HandleSettingsChanged;
			threshold_spin.ValueChanged += HandleSettingsChanged;

			table.Attach (amount_spin, 1, 2, 0, 1);
			table.Attach (radius_spin, 1, 2, 1, 2);
			table.Attach (threshold_spin, 1, 2, 2, 3);
			
			table.ShowAll ();
		        box.PackStart (table);
		}

	}

	public class Loupe : Gtk.Window {
		protected PhotoImageView view;
		protected Image image;
		protected VBox box; 
		protected Gdk.Rectangle region;
		bool use_shape_ext = false;
		Gdk.Pixbuf source;
		private int radius = 128;
		private int inner = 128;
		Gdk.Point start;
		Gdk.Point last;

		public Loupe (PhotoImageView view) : base ("don't peek")
		{ 
			this.view = view;
			Decorated = false;

			Gdk.Visual visual = Gdk.Visual.GetBestWithDepth (32);
			if (visual != null)
				Colormap = new Gdk.Colormap (visual, false);
			else
				use_shape_ext = true;

			BuildUI ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (use_shape_ext) {
				Gdk.Pixmap bitmap = new Gdk.Pixmap (GdkWindow, 
								    allocation.Width, 
								    allocation.Height, 1);

				Graphics g = CreateDrawable (bitmap);
				DrawShape (g, allocation.Width, allocation.Height);
				((IDisposable)g).Dispose ();
				ShapeCombineMask (bitmap, 0, 0);
			} else {
				Realize ();
				Graphics g = CreateDrawable (GdkWindow);
				DrawShape (g, Allocation.Width, Allocation.Height);
				//base.OnExposeEvent (args);
				((IDisposable)g).Dispose ();
			}
		}

		public void SetSamplePoint (Gdk.Point p)
		{
			region.X = p.X;
			region.Y = p.Y;
			region.Width = 256;
			region.Height = 256;
			
			if (view.Pixbuf != null) {
				Gdk.Pixbuf pixbuf = view.Pixbuf;
				
				region.Offset (- Math.Min (region.X, Math.Max (region.Right - pixbuf.Width, 128)), 
					       - Math.Min (region.Y, Math.Max (region.Bottom - pixbuf.Height, 128)));

				region.Intersect (new Gdk.Rectangle (0, 0, pixbuf.Width, pixbuf.Height));
			}
			UpdateSample ();
		}

		protected virtual void UpdateSample ()
		{
			if (source != null)
				source.Dispose ();
			
			source = null;

			if (view.Pixbuf == null)
				return;
			
			inner = (int) (radius * view.Zoom);
			//Resize (2 * (radius + inner), 2 * (radius + inner)); 
			source = new Gdk.Pixbuf (view.Pixbuf,
						 region.X, region.Y,
						 region.Width, region.Height);
			this.QueueDraw ();
		}

		[GLib.ConnectBefore]
		private void HandleImageViewMotion (object sender, MotionNotifyEventArgs args)
		{
			Gdk.Point coords;
			coords = new Gdk.Point ((int) args.Event.X, (int) args.Event.Y);
			
			SetSamplePoint (view.WindowCoordsToImage (coords));
		}
		
		private void DrawShape (Cairo.Graphics g, int width, int height)
		{
			int border = 5;
			int inner_x = radius + border + inner;
			int cx = radius + 2 * border;
			int cy = radius + 2 * border;
			
			g.Operator = Operator.Source;
			g.Color = new Cairo.Color (0,0,0,0);
			g.Rectangle (0, 0, width, height);
			g.Paint ();

			g.NewPath ();
			g.Operator = Operator.Over;
			g.Translate (cx, cy);
			g.Rotate (Math.PI / 4);
			g.Color = new Cairo.Color (0.4, 0.4, 0.4, .7);
			
			g.Rectangle (0, - (border + inner), inner_x, 2 * (border + inner));
			g.Arc (inner_x, 0, inner + border, 0, 2 * Math.PI);
			g.Arc (0, 0, radius + border, 0, 2 * Math.PI);
			g.Fill ();

			g.Color = new Cairo.Color (0, 0, 0, 1.0);
			g.Operator = Operator.DestOut;
			g.Arc (inner_x, 0, inner, 0, 2 * Math.PI);
			g.Fill ();

			g.Operator = Operator.Over;
			g.Matrix = Matrix.Identity;
			g.Translate (cx, cy);
			if (source != null)
				SetSourcePixbuf (g, source, -source.Width / 2, -source.Height / 2);
			g.Arc (0, 0, radius, 0, 2 * Math.PI);
			g.Fill ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			if (!use_shape_ext) {
				Graphics g = CreateDrawable (GdkWindow);
				DrawShape (g, Allocation.Width, Allocation.Height);
				//base.OnExposeEvent (args);
				((IDisposable)g).Dispose ();
			}
			return false;

		}
		
		bool dragging = false;
		private void HandleMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
		{
			double x = args.Event.XRoot;
			double y = args.Event.YRoot;
			
			if (dragging)
				Move ((int)x - start.X, (int)y - start.Y);

			
		}

		private void HandleIndexChanged (BrowsablePointer pointer, IBrowsableItem old)
		{
			UpdateSample ();
		}

		private void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			switch (args.Event.Type) {
			case Gdk.EventType.ButtonPress:
				start = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
				dragging = true;
				break;
			}
		}

		private void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			dragging = false;
		}

		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			view.MotionNotifyEvent -= HandleImageViewMotion;
		}

		protected Widget SetFancyStyle (Widget widget)
		{
			widget.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			widget.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
			return widget;
		}
		
		
		[DllImport("libgdk-x11-2.0.so")]
		extern static void gdk_cairo_set_source_pixbuf (IntPtr handle,
								IntPtr pixbuf,
								double        pixbuf_x,
								double        pixbuf_y);

		static void SetSourcePixbuf (Graphics g, Gdk.Pixbuf pixbuf, double x, double y)
		{
			gdk_cairo_set_source_pixbuf (g.Handle, pixbuf.Handle, x, y);
		}


		[DllImport("libgdk-x11-2.0.so")]
		static extern IntPtr gdk_cairo_create (IntPtr raw);
		
		public static Cairo.Graphics CreateDrawable (Gdk.Drawable drawable)
		{
			Cairo.Graphics g = new Cairo.Graphics (gdk_cairo_create (drawable.Handle));
			if (g == null) 
				throw new Exception ("Couldn't create Cairo Graphics!");
			
			return g;
		}
		
		protected virtual void BuildUI ()
		{
			box = new VBox ();
			this.Add (box);
			image = new Image ();
			box.PackStart (image);
			box.ShowAll ();

			SetFancyStyle (this);
			
			TransientFor = (Gtk.Window) view.Toplevel;
			view.MotionNotifyEvent += HandleImageViewMotion;
			view.Item.IndexChanged += HandleIndexChanged;

			SetSamplePoint (Gdk.Point.Zero);
			box.ShowAll ();
			SetSizeRequest (400, 400);

			AddEvents ((int) (Gdk.EventMask.PointerMotionMask
					  | Gdk.EventMask.ButtonPressMask
					  | Gdk.EventMask.ButtonReleaseMask));

			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			MotionNotifyEvent += HandleMotionNotifyEvent;
		}
	}
}


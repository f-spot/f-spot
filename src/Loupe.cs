using Gtk;
using System;
using System.Runtime.InteropServices;
using Cairo;

namespace FSpot {
	public class Sharpener : Loupe {
		Gtk.SpinButton amount_spin = new Gtk.SpinButton (0.5, 100.0, .01);
		Gtk.SpinButton radius_spin = new Gtk.SpinButton (5.0, 50.0, .01);
		Gtk.SpinButton threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01);
		Gtk.Dialog dialog;
		
		public Sharpener (PhotoImageView view) : base (view)
		{	
		}

		protected override void UpdateSample ()
		{
			base.UpdateSample ();

			if (overlay != null)
				overlay.Dispose ();

			overlay = null;
			if (source != null)
				overlay = PixbufUtils.UnsharpMask (source, 
								   radius_spin.Value, 
								   amount_spin.Value, 
								   threshold_spin.Value);
		}

		private void HandleSettingsChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}
		
		private void HandleOkClicked (object sender, EventArgs args)
		{
			Photo photo = view.Item.Current as Photo;

			if (photo == null)
				return;
			
			try {
				Gdk.Pixbuf orig = view.Pixbuf;
				Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig, radius_spin.Value, amount_spin.Value, threshold_spin.Value);
				
				bool create_version = photo.DefaultVersionId == Photo.OriginalVersionId;

				photo.SaveVersion (final, create_version);
			} catch (System.Exception e) {
				string msg = Mono.Posix.Catalog.GetString ("Error saving sharpened photo");
				string desc = String.Format (Mono.Posix.Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
							     e.Message, photo.Name);
				
				HigMessageDialog md = new HigMessageDialog (this, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error, ButtonsType.Ok, 
									    msg,
									    desc);
				md.Run ();
				md.Destroy ();
			}
			
			Destroy ();
		}

		public void HandleCancelClicked (object sender, EventArgs args)
		{
			Destroy ();
		}

		public void HandleLoupeDestroyed (object sender, EventArgs args)
		{
			dialog.Destroy ();
		}
		
		protected override void BuildUI ()
		{
			base.BuildUI ();

			string title = Mono.Posix.Catalog.GetString ("Sharpen");
			dialog = new Gtk.Dialog (title, (Gtk.Window) this,
						 DialogFlags.DestroyWithParent, new object [0]);
			dialog.BorderWidth = 12;
			dialog.VBox.Spacing = 6;
			
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
			
			Gtk.Button cancel_button = new Gtk.Button (Gtk.Stock.Cancel);
			cancel_button.Clicked += HandleCancelClicked;
			dialog.AddActionWidget (cancel_button, Gtk.ResponseType.Cancel);
			
			Gtk.Button ok_button = new Gtk.Button (Gtk.Stock.Ok);
			ok_button.Clicked += HandleOkClicked;
			dialog.AddActionWidget (ok_button, Gtk.ResponseType.Cancel);

			Destroyed += HandleLoupeDestroyed;
			
			table.ShowAll ();
			dialog.VBox.PackStart (table);
			dialog.ShowAll ();
		}

	}

	public class Loupe : Gtk.Window {
		protected PhotoImageView view;
		protected Gdk.Rectangle region;
		bool use_shape_ext = false;
		protected Gdk.Pixbuf source;
		protected Gdk.Pixbuf overlay;
		private int radius = 128;
		private int inner = 128;
		private int border = 5;
		private double angle = Math.PI / 4;
		Gdk.Point start;
		Gdk.Point start_hot;
		Gdk.Point pos_hot;
		Gdk.Point hotspot;

		public Loupe (PhotoImageView view) : base ("Loupe")
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

		public int Radius {
			get {
				return radius;
			}
			set {
				if (radius != value) {
					radius = value;
					UpdateSample ();
				}
			}
		}

		public int Border {
			get {
				return border;
			}
			set {
				if (border != value) {
					border = value;
					UpdateSample ();
				}
			}
		}

		public double Angle {
			get {
				return angle;
			}
			set {
				Gdk.Point then = GetHotspot ();
				angle = value;
				Gdk.Point now = GetHotspot ();
				System.Console.WriteLine ("{0} now {1}", then, now);
				int x, y;
				GdkWindow.GetOrigin (out x, out y);
				Move (x + then.X - now.X, y + then.Y - now.Y);
				QueueResize ();
			}
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
				bitmap.Dispose ();
			} else {
				Realize ();
				Graphics g = CreateDrawable (GdkWindow);
				DrawShape (g, Allocation.Width, Allocation.Height);
				((IDisposable)g).Dispose ();
			}
		}

		public void SetSamplePoint (Gdk.Point p)
		{
			region.X = p.X;
			region.Y = p.Y;
			region.Width = 2 * radius;
			region.Height = 2 * radius;
			
			if (view.Pixbuf != null) {
				Gdk.Pixbuf pixbuf = view.Pixbuf;
				
				region.Offset (- Math.Min (region.X, Math.Max (region.Right - pixbuf.Width, radius)), 
					       - Math.Min (region.Y, Math.Max (region.Bottom - pixbuf.Height, radius)));

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
			
			int small = (int) (radius * view.Zoom);
			if (small != inner) {
				inner = small;
				this.QueueResize ();
			}

			source = new Gdk.Pixbuf (view.Pixbuf,
						 region.X, region.Y,
						 region.Width, region.Height);
			
			//FIXME sometimes that ctor returns results with a null
			//handle this case ourselves
			if (source.Handle == IntPtr.Zero)
				source = null;

			this.QueueDraw ();
		}

		[GLib.ConnectBefore]
		private void HandleImageViewMotion (object sender, MotionNotifyEventArgs args)
		{
			Gdk.Point coords;
			coords = new Gdk.Point ((int) args.Event.X, (int) args.Event.Y);
			
			SetSamplePoint (view.WindowCoordsToImage (coords));
		}

		private Gdk.Point GetHotspot ()
		{
			Matrix m = new Matrix ();
			m.InitIdentity ();

			int inner_x = radius + border + inner;
			int cx = radius + 2 * border;
			int cy = radius + 2 * border;

			m.Translate (cx, cy);
			m.Rotate (angle);
			
			double hx = inner_x;
			double hy = 0;
			
			m.TransformPoint (ref hx, ref hy);
			
			Gdk.Point p;
			p.X = (int)hx;
			p.Y = (int)hy;
			
			return p;
		}
		
		private void DrawShape (Cairo.Graphics g, int width, int height)
		{
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
			g.Rotate (angle);
			g.Color = new Cairo.Color (0.4, 0.4, 0.4, .7);
			
			g.Rectangle (0, - (border + inner), inner_x, 2 * (border + inner));
			g.Arc (inner_x, 0, inner + border, 0, 2 * Math.PI);
			g.Arc (0, 0, radius + border, 0, 2 * Math.PI);
			g.Fill ();

			double hx = inner_x;
			double hy = 0;
			
			UserToDevice (g, ref hx, ref hy);
			hotspot.X = (int)hx;
			hotspot.Y = (int)hy;

			g.Color = new Cairo.Color (0, 0, 0, 1.0);
			g.Operator = Operator.DestOut;
			g.Arc (inner_x, 0, inner, 0, 2 * Math.PI);
			g.Fill ();

			g.Operator = Operator.Over;
			g.Matrix = new Matrix ();
			g.Translate (cx, cy);
			if (source != null)
				SetSourcePixbuf (g, source, -source.Width / 2, -source.Height / 2);
			g.Arc (0, 0, radius, 0, 2 * Math.PI);
			g.Fill ();

			if (overlay != null) {
				SetSourcePixbuf (g, overlay, -overlay.Width / 2, -overlay.Height / 2);
				g.Arc (0, 0, radius, angle, angle + Math.PI);
				g.ClosePath ();
				g.FillPreserve ();
				g.Color = new Cairo.Color (1.0, 1.0, 1.0, 1.0);
				g.Stroke ();
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Graphics g = CreateDrawable (GdkWindow);
			DrawShape (g, Allocation.Width, Allocation.Height);
			//base.OnExposeEvent (args);
			((IDisposable)g).Dispose ();
			return false;

		}
		
		bool dragging = false;
		bool rotate = false;
		Delay drag;
		Gdk.Point pos;
		double start_angle = 0;
		Gdk.Point root_pos;
		Gdk.Point start_root;

		private void HandleMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
		{
		        pos.X = (int) args.Event.XRoot - start.X;
		        pos.Y = (int) args.Event.YRoot - start.Y;
			root_pos.X = (int) args.Event.XRoot - start_root.X;
			root_pos.Y = (int) args.Event.YRoot - start_root.Y;

			if (dragging)
				drag.Start ();
		}

		private bool DragUpdate ()
		{
			if (!dragging)
				return false;

			if (!rotate) {
				return MoveWindow ();
			} else {
				double dist = Math.Sqrt (root_pos.X * root_pos.X + root_pos.Y * root_pos.Y);
				double angle = Math.Asin (root_pos.X / dist);
				if (root_pos.Y > 0)
					angle += Math.PI;
				System.Console.WriteLine ("angle = {0}", angle);
				Angle = start_angle + angle;
				return false;	
			}
		}

		private bool MoveWindow ()
		{
			Gdk.Point view_coords;
			Gdk.Point top;
			Gdk.Point current;
			
			GdkWindow.GetOrigin (out current.X, out current.Y);
		
			if (current == pos)
				return false;
			
			Move (pos.X, pos.Y);

			pos.Offset (hotspot.X, hotspot.Y);
			Gtk.Window toplevel = (Gtk.Window) view.Toplevel;
			toplevel.GdkWindow.GetOrigin (out top.X, out top.Y);
			toplevel.TranslateCoordinates (view, 
						       pos.X - top.X,  pos.Y - top.Y, 
						       out view_coords.X, out view_coords.Y);

			SetSamplePoint (view.WindowCoordsToImage (view_coords));

			return false;
		}

		private void HandleItemChanged (BrowsablePointer pointer, BrowsablePointerChangedArgs args)
		{
			UpdateSample ();
		}

		private void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			switch (args.Event.Type) {
			case Gdk.EventType.ButtonPress:
				if (args.Event.Button == 1) {
					start = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
					start_root = new Gdk.Point ((int)args.Event.XRoot, (int)args.Event.YRoot);
					dragging = true;
					rotate = (args.Event.State & Gdk.ModifierType.ShiftMask) > 0;
					start_angle = Angle;
				} else {
					Angle += Math.PI /8;
				}
				break;
			case Gdk.EventType.TwoButtonPress:
				dragging = false;
				this.Destroy ();
				break;
			}
		}

		private void HandleViewZoomChanged (object sender, System.EventArgs args)
		{
			UpdateSample ();
		}

		private void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			dragging = false;
		}

		protected override void OnDestroyed ()
		{
			view.MotionNotifyEvent -= HandleImageViewMotion;
			view.Item.Changed -= HandleItemChanged;
			view.ZoomChanged -= HandleViewZoomChanged;
			
			base.OnDestroyed ();
		}

		protected Widget SetFancyStyle (Widget widget)
		{
			//widget.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (127, 127, 127));
			//widget.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0, 0, 0));
			return widget;
		}
		
		
                [DllImport ("libcairo-2.dll")]
                static extern void cairo_user_to_device (IntPtr cr, ref double x, ref double y);

		static void UserToDevice (Graphics g, ref double x, ref double y)
		{
			cairo_user_to_device (g.Handle, ref x, ref y);
		}

		[DllImport("libgdk-2.0-0.dll")]
		extern static void gdk_cairo_set_source_pixbuf (IntPtr handle,
								IntPtr pixbuf,
								double        pixbuf_x,
								double        pixbuf_y);

		static void SetSourcePixbuf (Graphics g, Gdk.Pixbuf pixbuf, double x, double y)
		{
			gdk_cairo_set_source_pixbuf (g.Handle, pixbuf.Handle, x, y);
		}

		[DllImport("libgdk-2.0-0.dll")]
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
			SetFancyStyle (this);
			
			TransientFor = (Gtk.Window) view.Toplevel;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			
			//view.MotionNotifyEvent += HandleImageViewMotion;
			view.Item.Changed += HandleItemChanged;
			view.ZoomChanged += HandleViewZoomChanged;

			SetSamplePoint (Gdk.Point.Zero);
			SetSizeRequest (400, 400);

			AddEvents ((int) (Gdk.EventMask.PointerMotionMask
					  | Gdk.EventMask.ButtonPressMask
					  | Gdk.EventMask.ButtonReleaseMask));

			ButtonPressEvent += HandleButtonPressEvent;
			ButtonReleaseEvent += HandleButtonReleaseEvent;
			MotionNotifyEvent += HandleMotionNotifyEvent;

			drag = new Delay (20, new GLib.IdleHandler (DragUpdate));
		}
	}
}


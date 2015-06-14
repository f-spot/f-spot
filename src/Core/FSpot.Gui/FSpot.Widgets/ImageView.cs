//
// ImageView.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
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

using Gtk;
using Gdk;

using FSpot.Utils;
using TagLib.Image;

using Hyena;

namespace FSpot.Widgets
{
	public partial class ImageView : Container, IScrollableImplementor
	{
#region public API
		protected ImageView (IntPtr raw) : base (raw) { }

		public ImageView (bool canSelect = true) : base ()
		{
			AdjustmentsChanged += ScrollToAdjustments;

			CanFocus = true;
			can_select = canSelect;
		}

		Pixbuf pixbuf;
		public Pixbuf Pixbuf {
			get { return pixbuf; } 
			set {
				if (pixbuf == value)
					return;

				pixbuf = value;
				min_zoom = ComputeMinZoom (upscale);

				ComputeScaledSize ();
				AdjustmentsChanged -= ScrollToAdjustments;

				if (Hadjustment != null)
					Hadjustment.Value = Vadjustment.Value = 0;

				XOffset = YOffset = 0;
				AdjustmentsChanged += ScrollToAdjustments;
				QueueDraw ();
			} 
		}

		ImageOrientation pixbuf_orientation;
		public ImageOrientation PixbufOrientation {
			get { return pixbuf_orientation; }
			set {
				if (value == pixbuf_orientation)
					return;
				pixbuf_orientation = value;
				min_zoom = ComputeMinZoom (upscale);
				ComputeScaledSize ();
				QueueDraw ();
			}
		}

		CheckPattern check_pattern = CheckPattern.Dark;
		public CheckPattern CheckPattern {
			get { return check_pattern; } 
			set { 
				if (check_pattern == value)
					return;
				check_pattern = value;
				if (Pixbuf != null && Pixbuf.HasAlpha)
					QueueDraw ();
			} 
		}

		PointerMode pointer_mode = PointerMode.Select;
		public PointerMode PointerMode {
			get { return pointer_mode; } 
			set { pointer_mode = value; } 
		}

		public ScrollablePolicy HscrollPolicy { get; set; }
		public ScrollablePolicy VscrollPolicy { get; set; }

		Adjustment hadjustment = null;
		public Adjustment Hadjustment {
			get { return hadjustment; }
			set {
				hadjustment = value;
				if (hadjustment == null)
					return;

				hadjustment.Upper = scaled_width;
				hadjustment.ValueChanged += HandleAdjustmentsValueChanged;

				HandleAdjustmentsValueChanged (this, EventArgs.Empty);
			}
		}

		Adjustment vadjustment = null;
		public Adjustment Vadjustment {
			get { return vadjustment; }
			set {
				vadjustment = value;
				if (vadjustment == null)
					return;

				vadjustment.Upper = scaled_height;
				vadjustment.ValueChanged += HandleAdjustmentsValueChanged;

				HandleAdjustmentsValueChanged (this, EventArgs.Empty);
			}
		}

		bool can_select = false;
		public bool CanSelect {
			get { return can_select; }
			set { 
				if (can_select == value)
					return;

				if (!value)
					Selection = Rectangle.Zero;

				can_select = value;
			}
		}

		Rectangle selection = Rectangle.Zero;
		public Rectangle Selection {
			get {
				return can_select ? selection : Rectangle.Zero;
			}
			set { 
				if (!can_select)
					return;

				if (value == selection)
					return;

				selection = value;

				EventHandler eh = SelectionChanged;
				if (eh != null)
					eh (this, EventArgs.Empty);
				QueueDraw ();
			}
		}

		double selection_xy_ratio = 0;
		public double SelectionXyRatio {
			get { return selection_xy_ratio; } 
			set {
				if (selection_xy_ratio == value)
					return;
				selection_xy_ratio = value;

				if (selection_xy_ratio == 0)
					return;

				if (Selection == Rectangle.Zero)
					return;

				Selection = ConstrainSelection (Selection, false, false);
			} 
		}

		InterpType interpolation = InterpType.Bilinear;
		public InterpType Interpolation {
			get { return interpolation; } 
			set { 
				if (interpolation == value)
					return;
				interpolation = value;
				QueueDraw ();
			} 
		}

        double zoom = 1.0;
        public double Zoom {
            get { return zoom; }
            set {
                // Zoom around the center of the image.
                DoZoom (value, Allocation.Width / 2, Allocation.Height / 2);
            }
        }

		public void ZoomIn ()
		{
			Zoom *= ZOOM_FACTOR;
		}

		public void ZoomOut ()
		{
			Zoom *= 1.0 / ZOOM_FACTOR;
		}

		public void ZoomAboutPoint (double zoomIncrement, int x, int y)
		{
			DoZoom (zoom * zoomIncrement, x, y);
		}

        public bool Fit { get; private set; }

        public void ZoomFit (bool upscale)
        {
            ScrolledWindow scrolled = Parent as ScrolledWindow;
            if (scrolled != null)
                scrolled.SetPolicy (PolicyType.Never, PolicyType.Never);

            min_zoom = ComputeMinZoom (upscale);

            this.upscale = upscale;

            Fit = true;
            DoZoom (MIN_ZOOM, Allocation.Width / 2, Allocation.Height / 2);

            if (scrolled != null) {
                ThreadAssist.ProxyToMain (() => {
                        scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
                });
            }
        }

		public Point WindowCoordsToImage (Point win)
		{
			if (Pixbuf == null)
				return Point.Zero;

			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			win.X = Clamp (win.X - x_offset, 0, (int)scaled_width - 1);
			win.Y = Clamp (win.Y - y_offset, 0, (int)scaled_height - 1);

			win = PixbufUtils.TransformOrientation ((int)scaled_width, (int)scaled_height, win, PixbufUtils.ReverseTransformation (pixbuf_orientation));

			return  new Point ((int) Math.Floor (win.X * (double)(((int)PixbufOrientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) / (double)(scaled_width - 1) + .5),
					   (int) Math.Floor (win.Y * (double)(((int)PixbufOrientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) / (double)(scaled_height - 1) + .5));
		}

		public Point ImageCoordsToWindow (Point image)
		{
			if (Pixbuf == null)
				return Point.Zero;
			
			image = PixbufUtils.TransformOrientation (Pixbuf.Width, Pixbuf.Height, image, pixbuf_orientation);
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
			
			return new Point ((int) Math.Floor (image.X * (double) (scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) + x_offset,
			                  (int) Math.Floor (image.Y * (double) (scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) + y_offset);
		}
		
		public Cairo.RectangleInt ImageCoordsToWindow (Rectangle image)
		{
			Cairo.RectangleInt rect;
			if (Pixbuf == null)
			{
				rect = new Cairo.RectangleInt ();
				rect.Height = 0;
				rect.Width = 0;
				rect.X = 0;
				rect.Y = 0;
			}

			image = PixbufUtils.TransformOrientation (Pixbuf.Width, Pixbuf.Height, image, pixbuf_orientation);
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
			
			Cairo.RectangleInt win;
			win = new Cairo.RectangleInt ();
			win.Height = 0;
			win.Width = 0;
			win.X = 0;
			win.Y = 0;

			win.X = (int) Math.Floor (image.X * (double) (scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) + x_offset;
			win.Y = (int) Math.Floor (image.Y * (double) (scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) + y_offset;
			win.Width = (int) Math.Floor ((image.X + image.Width) * (double) (scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) - win.X + x_offset;
			win.Height = (int) Math.Floor ((image.Y + image.Height) * (double) (scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) - win.Y + y_offset;
			
			return win;
		}

		public event EventHandler ZoomChanged;
		public event EventHandler SelectionChanged;
#endregion

#region protected API

		protected static double ZOOM_FACTOR = 1.1;

		protected double max_zoom = 10.0;
		protected double MAX_ZOOM {
			get { return max_zoom; }
		}

		protected double min_zoom = 0.1;
		protected double MIN_ZOOM {
			get { return min_zoom; }
		}

		bool upscale;
		protected void ZoomFit ()
		{
			ZoomFit (upscale);
		}

		protected virtual void ApplyColorTransform (Pixbuf pixbuf)
		{
		}

#endregion

#region GtkWidgetry

        protected override void OnRealized ()
        {
			IsRealized = true;
            Window = new Gdk.Window (ParentWindow,
					new WindowAttr { 
                        WindowType = Gdk.WindowType.Child,
                        X = Allocation.X,
                        Y = Allocation.Y,
                        Width = Allocation.Width,
                        Height = Allocation.Height,
						Wclass = WindowWindowClass.InputOutput,
                        Visual = ParentWindow.Visual,
                        Mask = Events
							| EventMask.ExposureMask
                            | EventMask.ButtonPressMask
                            | EventMask.ButtonReleaseMask
                            | EventMask.PointerMotionMask
                            | EventMask.PointerMotionHintMask
                            | EventMask.ScrollMask
                            | EventMask.KeyPressMask
                            | EventMask.LeaveNotifyMask
                    },
					WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Visual);

			// GTK3
//			GdkWindow.SetBackPixmap (null, false);
            Window.UserData = Handle;

            Style.Attach (Window);
            Style.SetBackground (Window, StateType.Normal);

            OnRealizedChildren ();
        }

        protected override void OnMapped ()
        {
			IsMapped = true;
            OnMappedChildren ();
            Window.Show ();
        }

		protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
		{
			if (Pixbuf == null)
				minimum_height = 0;
			else
				minimum_height = (int) scaled_height;

			natural_height = minimum_height;
		}

		protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
		{
			if (Pixbuf == null)
				minimum_width = 0;
			else
				minimum_width = (int) scaled_width;

			natural_width = minimum_width;
		}

        protected override void OnSizeAllocated (Rectangle allocation)
        {
            min_zoom = ComputeMinZoom (upscale);

            if (Fit || zoom < MIN_ZOOM)
                zoom = MIN_ZOOM;
            // Since this affects the zoom_scale we should alert it
            var handler = ZoomChanged;
            if (handler != null)
                handler (this, EventArgs.Empty);

            ComputeScaledSize ();

            OnSizeAllocatedChildren ();

            if (IsRealized) {
                Window.MoveResize (allocation.X, allocation.Y, allocation.Width, allocation.Height);
            }

            if (Hadjustment != null && XOffset > Hadjustment.Upper - Hadjustment.PageSize)
                ScrollTo ((int)(Hadjustment.Upper - Hadjustment.PageSize), YOffset, false);

            if (Vadjustment != null && YOffset > Vadjustment.Upper - Vadjustment.PageSize)
                ScrollTo (XOffset, (int)(Vadjustment.Upper - Vadjustment.PageSize), false);

            base.OnSizeAllocated (allocation);

            if (Fit)
                ZoomFit (upscale);
        }

		protected override bool OnDrawn (Cairo.Context cr)
		{
			Rectangle area = new Rectangle((int) hadjustment.Value, (int) vadjustment.Value,
						       Parent.Allocation.Width, Parent.Allocation.Height);

			//draw synchronously if InterpType.Nearest or zoom 1:1
			if (Interpolation == InterpType.Nearest || zoom == 1.0) {
				PaintRectangle (Allocation, InterpType.Nearest, cr);
			} else {
				//Do this on idle ???
				PaintRectangle (Allocation, Interpolation, cr);
			}

			if (can_select)
				OnSelectionExposeEvent (cr);

			return base.OnDrawn (cr);
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			bool handled = false;

			if (!HasFocus)
				GrabFocus ();

			if (PointerMode == PointerMode.None)
				return false;

			handled = handled || OnPanButtonPressEvent (evnt);

			if (can_select)
				handled = handled || OnSelectionButtonPressEvent (evnt);

			return handled || base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			bool handled = false;

			handled = handled || OnPanButtonReleaseEvent (evnt);

			if (can_select)
				handled = handled || OnSelectionButtonReleaseEvent (evnt);

			return handled || base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			bool handled = false;

			handled = handled || OnPanMotionNotifyEvent (evnt);

			if (can_select)
				handled = handled || OnSelectionMotionNotifyEvent (evnt);

			return handled || base.OnMotionNotifyEvent (evnt);

		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			if ((evnt.State & ModifierType.ShiftMask) == 0) {//no shift, let's zoom
				ZoomAboutPoint ((evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Right) ? ZOOM_FACTOR : 1.0 / ZOOM_FACTOR,
					(int)evnt.X, (int)evnt.Y);
				return true;
			}

			int x_incr = (int)Hadjustment.PageIncrement / 4;
			int y_incr = (int)Vadjustment.PageIncrement / 4;
			if ((evnt.State & ModifierType.ControlMask) == 0) {//no control scroll
				ScrollBy ((evnt.Direction == ScrollDirection.Left) ? -x_incr : (evnt.Direction == ScrollDirection.Right) ? x_incr : 0,
					(evnt.Direction == ScrollDirection.Up) ? -y_incr : (evnt.Direction == ScrollDirection.Down) ? y_incr : 0);
				return true;
			}
			//invert x and y for scrolling
			ScrollBy ((evnt.Direction == ScrollDirection.Up) ? -y_incr : (evnt.Direction == ScrollDirection.Down) ? y_incr : 0,
				(evnt.Direction == ScrollDirection.Left) ? -x_incr : (evnt.Direction == ScrollDirection.Right) ? x_incr : 0);	
			return true;
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if ((evnt.State & (ModifierType.Mod1Mask | ModifierType.ControlMask)) != 0)
				return base.OnKeyPressEvent (evnt);

			bool handled = true;
			int x, y;
			ModifierType type;

			switch(evnt.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.k:
			case Gdk.Key.K:
				ScrollBy (0, -Vadjustment.StepIncrement);
				break;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.j:
			case Gdk.Key.J:
				ScrollBy (0, Vadjustment.StepIncrement);
				break;
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
			case Gdk.Key.h:
			case Gdk.Key.H:
				ScrollBy (-Hadjustment.StepIncrement, 0);
				break;
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.l:
			case Gdk.Key.L:
				ScrollBy (Hadjustment.StepIncrement, 0);
				break;
			case Gdk.Key.equal:
			case Gdk.Key.plus:
			case Gdk.Key.KP_Add:
				ZoomIn ();
				break;
			case Gdk.Key.minus:
			case Gdk.Key.KP_Subtract:
				ZoomOut ();
				break;
			case Gdk.Key.Key_0:
			case Gdk.Key.KP_0:
				ZoomFit ();
				break;
			case Gdk.Key.KP_1:
			case Gdk.Key.Key_1:
				Window.GetPointer (out x, out y, out type);
				DoZoom (1.0, x, y);
				break;
			case Gdk.Key.Key_2:
			case Gdk.Key.KP_2:
				Window.GetPointer (out x, out y, out type);
				DoZoom (2.0, x, y);
				break;
			default:
				handled = false;
				break;
			}
			
			return handled || base.OnKeyPressEvent (evnt);
		}
#endregion

#region private painting, zooming and misc

        int XOffset { get; set;}
        int YOffset { get; set;}

        /// <summary>
        ///     Zoom to the given factor.
        /// </summary>
        /// <param name='zoom'>
        ///     A zoom factor, expressed as a double.
        /// </param>
        /// <param name='x'>
        ///     The point of the viewport around which to zoom.
        /// </param>
        /// <param name='y'>
        ///     The point of the viewport around which to zoom.
        /// </param>
        void DoZoom (double zoom, int x, int y)
        {
            Fit = zoom == MIN_ZOOM;

            if (zoom == this.zoom || Math.Abs (this.zoom - zoom) < Double.Epsilon) {
                // Don't recalculate if the zoom factor stays the same.
                return;
            }

            // Clamp the zoom factor within the [ MIN_ZOOM , MAX_ZOOM ] interval.
            zoom = Math.Max (Math.Min (zoom, MAX_ZOOM), MIN_ZOOM);

            this.zoom = zoom;

            int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
            int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
            double x_anchor = (double)(x - x_offset) / (double)scaled_width;
            double y_anchor = (double)(y - y_offset) / (double)scaled_height;
            ComputeScaledSize ();

			if (Hadjustment != null && Vadjustment != null) {
				AdjustmentsChanged -= ScrollToAdjustments;

				if (scaled_width < Allocation.Width)
					Hadjustment.Value = XOffset = 0;
				else
					Hadjustment.Value = XOffset = Clamp ((int)(x_anchor * scaled_width - x), 0, (int)(Hadjustment.Upper - Hadjustment.PageSize));

				if (scaled_height < Allocation.Height)
					Vadjustment.Value = YOffset = 0;
				else
					Vadjustment.Value = YOffset = Clamp ((int)(y_anchor * scaled_height - y), 0, (int)(Vadjustment.Upper - Vadjustment.PageSize));

				AdjustmentsChanged += ScrollToAdjustments;
			}

            EventHandler eh = ZoomChanged;
            if (eh != null)
                eh (this, EventArgs.Empty);

            QueueResize ();
        }

		void PaintBackground (Rectangle backgound, Rectangle area)
		{
			// GTK3

//			GdkWindow.DrawRectangle (Style.BackgroundGCs [(int)StateType.Normal], true, area);
		}

		void PaintRectangle (Rectangle area, InterpType interpolation, Cairo.Context cr)
		{
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			//Draw background
			if (y_offset > 0) 	//Top
				PaintBackground (new Rectangle (0, 0, Allocation.Width, y_offset), area);
			if (x_offset > 0) 	//Left
				PaintBackground (new Rectangle (0, y_offset, x_offset, (int)scaled_height), area);
			if (x_offset >= 0)	//Right
				PaintBackground (new Rectangle (x_offset + (int)scaled_width, y_offset, Allocation.Width - x_offset - (int)scaled_width, (int)scaled_height), area);
			if (y_offset >= 0)	//Bottom
				PaintBackground (new Rectangle (0, y_offset + (int)scaled_height, Allocation.Width, Allocation.Height - y_offset - (int)scaled_height), area);

			if (Pixbuf == null)
				return;

			area.Intersect (new Rectangle (x_offset, y_offset, (int)scaled_width, (int)scaled_height));

			if (area.Width <= 0  || area.Height <= 0)
				return;

			//Short circuit for 1:1 zoom
			if (zoom == 1.0 &&
				!Pixbuf.HasAlpha &&
				Pixbuf.BitsPerSample == 8 &&
				pixbuf_orientation == ImageOrientation.TopLeft) {

				cr.Save ();
				Gdk.CairoHelper.SetSourcePixbuf (cr, Pixbuf, area.X, area.Y);
				cr.Rectangle (x_offset, y_offset, scaled_width, scaled_height);
				cr.Fill ();
				cr.Restore ();

				return;
			}

			Rectangle pixbuf_area = PixbufUtils.TransformOrientation ((int)scaled_width,
				(int)scaled_height,
				new Rectangle ((area.X - x_offset),
					(area.Y - y_offset),
					area.Width,
					area.Height),
				PixbufUtils.ReverseTransformation (pixbuf_orientation));
			using (Pixbuf temp_pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, pixbuf_area.Width, pixbuf_area.Height)) {
				if (Pixbuf.HasAlpha)
					temp_pixbuf.Fill (0x00000000);

				Pixbuf.CompositeColor (temp_pixbuf,
					0, 0,
					pixbuf_area.Width, pixbuf_area.Height,
					-pixbuf_area.X, -pixbuf_area.Y,
					zoom, zoom,
					zoom == 1.0 ? InterpType.Nearest : interpolation, 255,
					pixbuf_area.X, pixbuf_area.Y,
					CheckPattern.CheckSize, CheckPattern.Color1, CheckPattern.Color2);


				ApplyColorTransform (temp_pixbuf);

				using (var dest_pixbuf = PixbufUtils.TransformOrientation (temp_pixbuf, pixbuf_orientation)) {
					cr.Save ();
					Gdk.CairoHelper.SetSourcePixbuf (cr, dest_pixbuf, area.X, area.Y);
					cr.Rectangle (x_offset, y_offset, scaled_width, scaled_height);
					cr.Fill ();
					cr.Restore ();
				}
			}
		}

		uint scaled_width, scaled_height;
		void ComputeScaledSize ()
		{
			if (Pixbuf == null)
				scaled_width = scaled_height = 0;
			else {
				double width;
				double height;
				if ((int)pixbuf_orientation <= 4 ) { //TopLeft, TopRight, BottomRight, BottomLeft
					width = Pixbuf.Width;
					height = Pixbuf.Height;
				} else {			//LeftTop, RightTop, RightBottom, LeftBottom
					width = Pixbuf.Height;
					height = Pixbuf.Width;
				}
				scaled_width = (uint)Math.Floor (width * Zoom + .5);
				scaled_height = (uint)Math.Floor (height * Zoom + .5);
			}

			if (Hadjustment != null) {
				Hadjustment.PageSize = Math.Min (scaled_width, Allocation.Width);
				Hadjustment.PageIncrement = scaled_width * .9;
				Hadjustment.StepIncrement = 32;
				Hadjustment.Upper = scaled_width;
				Hadjustment.Lower = 0;
			}

			if (Vadjustment != null) {
				Vadjustment.PageSize = Math.Min (scaled_height, Allocation.Height);
				Vadjustment.PageIncrement = scaled_height * .9;
				Vadjustment.StepIncrement = 32;
				Vadjustment.Upper = scaled_height;
				Vadjustment.Lower = 0;
			}
		}

		event EventHandler AdjustmentsChanged;
		void HandleAdjustmentsValueChanged (object sender, EventArgs e)
		{
			EventHandler eh = AdjustmentsChanged;
			if (eh != null)
				eh (this, EventArgs.Empty);
		}

		void ScrollToAdjustments (object sender, EventArgs e)
		{
			int x = hadjustment == null ? 0 : (int)hadjustment.Value;
			int y = vadjustment == null ? 0 : (int)vadjustment.Value;
			ScrollTo (x, y, false);
		}

		void ScrollTo (int x, int y, bool changeAdjustments)
		{
			x = hadjustment == null ? 0 : Clamp (x, 0, (int)(hadjustment.Upper - hadjustment.PageSize));
			y = vadjustment == null ? 0 : Clamp (y, 0, (int)(vadjustment.Upper - vadjustment.PageSize));

			int xof = x - XOffset;
			int yof = y - YOffset;
			XOffset = x;
			YOffset = y;

			if (IsRealized) {
				Window.Scroll (-xof, -yof);
				Window.ProcessUpdates (true);
			}

			if (changeAdjustments) {
				AdjustmentsChanged -= ScrollToAdjustments;

				if (hadjustment != null)
					hadjustment.Value = XOffset;

				if (vadjustment != null)
					vadjustment.Value = YOffset;

				AdjustmentsChanged += ScrollToAdjustments;
			}
		}

		void ScrollBy (double x, double y)
		{
			ScrollTo ((int)(XOffset + x), (int)(YOffset + y), true);
		}

		static int Clamp (int value, int min, int max)
		{
			return Math.Min (Math.Max (value, min), max);
		}

		double ComputeMinZoom (bool upscale)
		{
			if (Pixbuf == null)
				return 0.1;

			double width;
			double height;
			if ((int)pixbuf_orientation <= 4 ) { //TopLeft, TopRight, BottomRight, BottomLeft
				width = Pixbuf.Width;
				height = Pixbuf.Height;
			} else {			//LeftTop, RightTop, RightBottom, LeftBottom
				width = Pixbuf.Height;
				height = Pixbuf.Width;
			}
			if (upscale)
				return Math.Min ((double)Parent.Allocation.Width / width,
						 (double)Parent.Allocation.Height / height);
			return Math.Min (1.0,
					 Math.Min ((double)Parent.Allocation.Width / width,
						   (double)Parent.Allocation.Height / height));
		}
#endregion


#region selection
		bool OnSelectionExposeEvent (Cairo.Context cr)
		{
			if (selection == Rectangle.Zero)
				return false;

			cr.Save ();
			cr.SetSourceRGBA (.5, .5, .5, .7);
			cr.Rectangle (selection.X, selection.Y, selection.Width, selection.Height);
			cr.Fill ();
			cr.Restore ();

			//			Cairo.RectangleInt win_selection = ImageCoordsToWindow (selection);
			//			using (var evnt_region = evnt.Copy ()) {
			//				using (var r = new Cairo.Region ()) {
			//					r.UnionRectangle (win_selection);
			//					evnt_region.Subtract (r);
			//				}
			//
			//				// GTK3: Figure out Window to Cairo.Context
			////				using (Cairo.Context ctx = new Cairo.Context (Window)) {
			////					ctx.SetSourceRGBA (.5, .5, .5, .7);
			////					Gdk.CairoHelper.Region (ctx, evnt_region);                                                                                                                      
			////					ctx.Fill ();
			////				}
			//			}
			return true;
		}

		enum DragMode {
			None,
			Move,
			Extend,
		}

		const int SELECTION_SNAP_DISTANCE = 8;
		DragMode GetDragMode (int x, int y)
		{
			// GTK3: Cairo stuff
//			Rectangle win_selection = ImageCoordsToWindow (selection);
//			if (Rectangle.Inflate (win_selection, -SELECTION_SNAP_DISTANCE, -SELECTION_SNAP_DISTANCE).Contains (x, y))
//				return DragMode.Move;
//			if (Rectangle.Inflate (win_selection, SELECTION_SNAP_DISTANCE, SELECTION_SNAP_DISTANCE).Contains (x, y))
//				return DragMode.Extend;
			return DragMode.None;
		}

		bool is_dragging_selection = false;
		bool fixed_height = false;
		bool fixed_width = false;
		bool is_moving_selection = false;
		Point selection_anchor = Point.Zero;

		bool OnSelectionButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button != 1)
				return false;

			if (evnt.Type == EventType.TwoButtonPress) {
				is_dragging_selection = false;
				is_moving_selection = false;
				return false;
			}
			
			Point img = WindowCoordsToImage (new Point ((int)evnt.X, (int)evnt.Y));
			switch (GetDragMode ((int)evnt.X, (int)evnt.Y)) {
				case DragMode.None:
					is_dragging_selection = true;
					PointerMode = PointerMode.Select;
					Selection = Rectangle.Zero;
					selection_anchor = img;
					break;

				case DragMode.Extend:
				// GTK3: Stupid Rectangles!
//				Rectangle win_sel = ImageCoordsToWindow (Selection) as Rectangle;
//					is_dragging_selection = true;
//					if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE &&
//					    Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {	 			//TopLeft
//						selection_anchor = new Point (Selection.X + Selection.Width, Selection.Y + Selection.Height);
//					} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE &&
//						   Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) { 			//TopRight
//						selection_anchor = new Point (Selection.X, Selection.Y + Selection.Height);
//					} else if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE &&
//						   Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {	//BottomLeft
//						selection_anchor = new Point (Selection.X + Selection.Width, Selection.Y);
//					} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE &&
//						   Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {	//BottomRight
//						selection_anchor = new Point (Selection.X, Selection.Y);
//					} else if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE) {			//Left
//						selection_anchor = new Point (Selection.X + Selection.Width, Selection.Y);
//						fixed_height = true;
//					} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE) {	//Right
//						selection_anchor = new Point (Selection.X, Selection.Y);
//						fixed_height = true;
//					} else if (Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {			//Top
//						selection_anchor = new Point (Selection.X, Selection.Y + Selection.Height);
//						fixed_width = true;
//					} else if (Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {	//Bottom
//						selection_anchor = new Point (Selection.X, Selection.Y);
//						fixed_width = true;
//					} else {
//						fixed_width = fixed_height = false;
//						is_dragging_selection = false;
//					}
					break;

				case DragMode.Move:
					is_moving_selection = true;
					selection_anchor = img;
					SelectionSetPointer ((int)evnt.X, (int)evnt.Y);
					break;
			}

			return true;
		}

		bool OnSelectionButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button != 1)
				return false;

			is_dragging_selection = false;
			is_moving_selection = false;
			fixed_width = fixed_height = false;

			SelectionSetPointer ((int)evnt.X, (int)evnt.Y);
			return true;
		}

		void SelectionSetPointer (int x, int y)
		{
			if (is_moving_selection)
				Window.Cursor = new Cursor (CursorType.Crosshair);
			else {
				switch (GetDragMode (x, y)) {
				case DragMode.Move:
					Window.Cursor = new Cursor (CursorType.Hand1);
					break;
				default:
					Window.Cursor = null;
					break;
				case DragMode.Extend:
					// GTK3: More rectangles
//					Rectangle win_sel = ImageCoordsToWindow (Selection) as Rectangle;
//					if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE &&
//					    Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {	 			//TopLeft
//						GdkWindow.Cursor = new Cursor (CursorType.TopLeftCorner);
//					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE &&
//						   Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) { 			//TopRight
//						GdkWindow.Cursor = new Cursor (CursorType.TopRightCorner);
//					} else if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE &&
//						   Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {	//BottomLeft
//						GdkWindow.Cursor = new Cursor (CursorType.BottomLeftCorner);
//					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE &&
//						   Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {	//BottomRight
//						GdkWindow.Cursor = new Cursor (CursorType.BottomRightCorner);
//					} else if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE) {			//Left
//						GdkWindow.Cursor = new Cursor (CursorType.LeftSide);
//					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE) {	//Right
//						GdkWindow.Cursor = new Cursor (CursorType.RightSide);
//					} else if (Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {			//Top
//						GdkWindow.Cursor = new Cursor (CursorType.TopSide);
//					} else if (Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {	//Bottom
//						GdkWindow.Cursor = new Cursor (CursorType.BottomSide);
//					}
					break;
				}
			}
		}

		const int SELECTION_THRESHOLD = 5;
		bool OnSelectionMotionNotifyEvent (EventMotion evnt)
		{
			int x, y;
			ModifierType mod;

			if (evnt.IsHint)
				Window.GetPointer (out x, out y, out mod);
			else {
				x = (int)evnt.X;
				y = (int)evnt.Y;
			}


			Point img = WindowCoordsToImage (new Point (x, y));
			if (is_dragging_selection) {
				Point win_anchor = ImageCoordsToWindow (selection_anchor);
				if (Selection == Rectangle.Zero &&
				    Math.Abs (evnt.X - win_anchor.X) < SELECTION_THRESHOLD &&
				    Math.Abs (evnt.Y - win_anchor.Y) < SELECTION_THRESHOLD) {
					SelectionSetPointer (x, y);
					return true;
				}
	
				
				if (selection_xy_ratio == 0)
					Selection = new Rectangle (fixed_width ? Selection.X : Math.Min (selection_anchor.X, img.X),
								   fixed_height ? Selection.Y : Math.Min (selection_anchor.Y, img.Y),
								   fixed_width ? Selection.Width : Math.Abs (selection_anchor.X - img.X),
								   fixed_height ? Selection.Height : Math.Abs (selection_anchor.Y - img.Y));

				else
					Selection = ConstrainSelection (new Rectangle (Math.Min (selection_anchor.X, img.X),
										       Math.Min (selection_anchor.Y, img.Y),
										       Math.Abs (selection_anchor.X - img.X),
										       Math.Abs (selection_anchor.Y - img.Y)),
									fixed_width, fixed_height);

				SelectionSetPointer (x, y);
				return true;
			}

			if (is_moving_selection) {
				Selection = new Rectangle (Clamp (Selection.X + img.X - selection_anchor.X, 0, Pixbuf.Width - Selection.Width),
							   Clamp (Selection.Y + img.Y - selection_anchor.Y, 0, Pixbuf.Height - Selection.Height),
							   Selection.Width, Selection.Height);
				selection_anchor = img;
				SelectionSetPointer (x, y);
				return true;
			}

			SelectionSetPointer (x, y);
			return true;
		}

		Rectangle ConstrainSelection (Rectangle sel, bool fixed_width, bool fixed_height)
		{
			double constrain = selection_xy_ratio;
			if ((double)sel.Width > (double)sel.Height && selection_xy_ratio < 1 ||
			    (double)sel.Width < (double)sel.Height && selection_xy_ratio > 1)
				constrain = 1.0 / constrain;


			double ratio = (double)sel.Width / (double)sel.Height;
			int height = sel.Height;
			int width = sel.Width;
			if (ratio > constrain) {
				height = (int)((double)sel.Width / constrain);
				if (height > Pixbuf.Height) {
					height = sel.Height;
					width = (int)(height * constrain);
				}
			} else {
				width = (int)(height * constrain);
				if (width > Pixbuf.Width) {
					width = sel.Width;
					height = (int)((double)width / constrain);
				}
			}

			return new Rectangle (sel.X + width < Pixbuf.Width ? sel.X : Pixbuf.Width - width,
					      sel.Y + height < Pixbuf.Height ? sel.Y : Pixbuf.Height - height,
					      width, height);
		}
#endregion

    }
}

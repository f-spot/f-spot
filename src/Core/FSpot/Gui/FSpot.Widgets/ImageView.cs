//
// ImageView.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Utils;

using Gdk;

using Gtk;

using Hyena;

using TagLib.Image;

namespace FSpot.Widgets
{
	public partial class ImageView : Container
	{

		#region public API
		protected ImageView (IntPtr raw) : base (raw) { }

		public ImageView (Adjustment hadjustment, Adjustment vadjustment, bool canSelect)
		{
			OnSetScrollAdjustments (hadjustment, vadjustment);
			AdjustmentsChanged += ScrollToAdjustments;
			WidgetFlags &= ~WidgetFlags.NoWindow;
			SetFlag (WidgetFlags.CanFocus);

			can_select = canSelect;
		}

		public ImageView (bool canSelect) : this (null, null, canSelect)
		{
		}

		public ImageView () : this (true)
		{
		}

		Pixbuf pixbuf;
		public Pixbuf Pixbuf {
			get => pixbuf;
			set {
				if (pixbuf == value)
					return;

				pixbuf = value;
				MinZoom = ComputeMinZoom (upscale);

				ComputeScaledSize ();
				AdjustmentsChanged -= ScrollToAdjustments;
				Hadjustment.Value = Vadjustment.Value = 0;
				XOffset = YOffset = 0;
				AdjustmentsChanged += ScrollToAdjustments;
				QueueDraw ();
			}
		}

		ImageOrientation pixbuf_orientation;
		public ImageOrientation PixbufOrientation {
			get => pixbuf_orientation;
			set {
				if (value == pixbuf_orientation)
					return;
				pixbuf_orientation = value;
				MinZoom = ComputeMinZoom (upscale);
				ComputeScaledSize ();
				QueueDraw ();
			}
		}

		CheckPattern check_pattern = CheckPattern.Dark;
		public CheckPattern CheckPattern {
			get => check_pattern;
			set {
				if (check_pattern == value)
					return;
				check_pattern = value;
				if (Pixbuf != null && Pixbuf.HasAlpha)
					QueueDraw ();
			}
		}

		public PointerMode PointerMode { get; set; } = PointerMode.Select;

		public Adjustment Hadjustment { get; private set; }
		public Adjustment Vadjustment { get; private set; }

		bool can_select;
		public bool CanSelect {
			get => can_select;
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
				if (!can_select)
					return Rectangle.Zero;
				return selection;
			}
			set {
				if (!can_select)
					return;

				if (value == selection)
					return;

				selection = value;

				SelectionChanged?.Invoke (this, EventArgs.Empty);
				QueueDraw ();
			}
		}

		double selection_xy_ratio;
		public double SelectionXyRatio {
			get => selection_xy_ratio;
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
			get => interpolation;
			set {
				if (interpolation == value)
					return;
				interpolation = value;
				QueueDraw ();
			}
		}

		double zoom = 1.0;
		public double Zoom {
			get => zoom;
			set {
				// Zoom around the center of the image.
				DoZoom (value, Allocation.Width / 2, Allocation.Height / 2);
			}
		}

		public void ZoomIn ()
		{
			Zoom *= ZoomFactor;
		}

		public void ZoomOut ()
		{
			Zoom *= 1.0 / ZoomFactor;
		}

		public void ZoomAboutPoint (double zoomIncrement, int x, int y)
		{
			DoZoom (zoom * zoomIncrement, x, y);
		}

		public bool Fit { get; private set; }

		public void ZoomFit (bool upscale)
		{
			var scrolled = Parent as ScrolledWindow;
			if (scrolled != null)
				scrolled.SetPolicy (PolicyType.Never, PolicyType.Never);

			MinZoom = ComputeMinZoom (upscale);

			this.upscale = upscale;

			Fit = true;
			DoZoom (MinZoom, Allocation.Width / 2, Allocation.Height / 2);

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

			return new Point ((int)Math.Floor (win.X * (double)(((int)PixbufOrientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) / (double)(scaled_width - 1) + .5),
					   (int)Math.Floor (win.Y * (double)(((int)PixbufOrientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) / (double)(scaled_height - 1) + .5));
		}

		public Point ImageCoordsToWindow (Point image)
		{
			if (Pixbuf == null)
				return Point.Zero;

			image = PixbufUtils.TransformOrientation (Pixbuf.Width, Pixbuf.Height, image, pixbuf_orientation);
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			return new Point ((int)Math.Floor (image.X * (double)(scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) + x_offset,
							  (int)Math.Floor (image.Y * (double)(scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) + y_offset);
		}

		public Rectangle ImageCoordsToWindow (Rectangle image)
		{
			if (Pixbuf == null)
				return Rectangle.Zero;

			image = PixbufUtils.TransformOrientation (Pixbuf.Width, Pixbuf.Height, image, pixbuf_orientation);
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;

			Rectangle win = Rectangle.Zero;
			win.X = (int)Math.Floor (image.X * (double)(scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) + x_offset;
			win.Y = (int)Math.Floor (image.Y * (double)(scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) + y_offset;
			win.Width = (int)Math.Floor ((image.X + image.Width) * (double)(scaled_width - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Width : Pixbuf.Height) - 1) + 0.5) - win.X + x_offset;
			win.Height = (int)Math.Floor ((image.Y + image.Height) * (double)(scaled_height - 1) / (((int)pixbuf_orientation <= 4 ? Pixbuf.Height : Pixbuf.Width) - 1) + 0.5) - win.Y + y_offset;

			return win;
		}

		public event EventHandler ZoomChanged;
		public event EventHandler SelectionChanged;
		#endregion

		#region protected API

		protected static double ZoomFactor { get; } = 1.1;

		protected double MaxZoom { get; } = 10.0;
		protected double MinZoom { get; private set; } = 0.1;

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
			SetFlag (WidgetFlags.Realized);
			GdkWindow = new Gdk.Window (ParentWindow,
					new WindowAttr {
						WindowType = Gdk.WindowType.Child,
						X = Allocation.X,
						Y = Allocation.Y,
						Width = Allocation.Width,
						Height = Allocation.Height,
						Wclass = WindowClass.InputOutput,
						Visual = ParentWindow.Visual,
						Colormap = ParentWindow.Colormap,
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
					WindowAttributesType.X | WindowAttributesType.Y |
					WindowAttributesType.Visual | WindowAttributesType.Colormap);

			GdkWindow.SetBackPixmap (null, false);
			GdkWindow.UserData = Handle;

			Style.Attach (GdkWindow);
			Style.SetBackground (GdkWindow, StateType.Normal);

			OnRealizedChildren ();
		}

		protected override void OnMapped ()
		{
			SetFlag (WidgetFlags.Mapped);
			OnMappedChildren ();
			GdkWindow.Show ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = requisition.Height = 0;
			OnSizeRequestedChildren ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			MinZoom = ComputeMinZoom (upscale);

			if (Fit || zoom < MinZoom)
				zoom = MinZoom;

			// Since this affects the zoom_scale we should alert it
			ZoomChanged?.Invoke (this, EventArgs.Empty);

			ComputeScaledSize ();

			OnSizeAllocatedChildren ();

			if (IsRealized) {
				GdkWindow.MoveResize (allocation.X, allocation.Y, allocation.Width, allocation.Height);
			}

			if (XOffset > Hadjustment.Upper - Hadjustment.PageSize)
				ScrollTo ((int)(Hadjustment.Upper - Hadjustment.PageSize), YOffset, false);
			if (YOffset > Vadjustment.Upper - Vadjustment.PageSize)
				ScrollTo (XOffset, (int)(Vadjustment.Upper - Vadjustment.PageSize), false);

			base.OnSizeAllocated (allocation);

			if (Fit)
				ZoomFit (upscale);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (evnt.Window != GdkWindow)
				return false;

			foreach (Rectangle area in evnt.Region.GetRectangles ()) {
				var p_area = new Rectangle (Math.Max (0, area.X), Math.Max (0, area.Y),
							  Math.Min (Allocation.Width, area.Width), Math.Min (Allocation.Height, area.Height));
				if (p_area == Rectangle.Zero)
					continue;

				//draw synchronously if InterpType.Nearest or zoom 1:1
				if (Interpolation == InterpType.Nearest || zoom == 1.0) {
					PaintRectangle (p_area, InterpType.Nearest);
					continue;
				}

				//Do this on idle ???
				PaintRectangle (p_area, Interpolation);
			}

			if (can_select)
				OnSelectionExposeEvent (evnt);

			return true;
		}

		protected override void OnSetScrollAdjustments (Adjustment hadjustment, Adjustment vadjustment)
		{
			if (hadjustment == null)
				hadjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			if (vadjustment == null)
				vadjustment = new Adjustment (0, 0, 0, 0, 0, 0);

			bool needChange = false;

			if (Hadjustment != hadjustment) {
				Hadjustment = hadjustment;
				Hadjustment.Upper = scaled_width;
				Hadjustment.ValueChanged += HandleAdjustmentsValueChanged;
				needChange = true;
			}
			if (Vadjustment != vadjustment) {
				Vadjustment = vadjustment;
				Vadjustment.Upper = scaled_height;
				Vadjustment.ValueChanged += HandleAdjustmentsValueChanged;
				needChange = true;
			}

			if (needChange)
				HandleAdjustmentsValueChanged (this, EventArgs.Empty);
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
				ZoomAboutPoint ((evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Right) ? ZoomFactor : 1.0 / ZoomFactor,
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
			switch (evnt.Key) {
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
				GdkWindow.GetPointer (out x, out y, out _);
				DoZoom (1.0, x, y);
				break;
			case Gdk.Key.Key_2:
			case Gdk.Key.KP_2:
				GdkWindow.GetPointer (out x, out y, out _);
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

		int XOffset { get; set; }
		int YOffset { get; set; }

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
			Fit = zoom == MinZoom;

			if (zoom == this.zoom || Math.Abs (this.zoom - zoom) < double.Epsilon) {
				// Don't recalculate if the zoom factor stays the same.
				return;
			}

			// Clamp the zoom factor within the [ MIN_ZOOM , MAX_ZOOM ] interval.
			zoom = Math.Max (Math.Min (zoom, MaxZoom), MinZoom);

			this.zoom = zoom;

			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
			double x_anchor = (double)(x - x_offset) / (double)scaled_width;
			double y_anchor = (double)(y - y_offset) / (double)scaled_height;
			ComputeScaledSize ();

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

			ZoomChanged?.Invoke (this, EventArgs.Empty);

			QueueDraw ();
		}

		void PaintBackground (Rectangle backgound, Rectangle area)
		{
			GdkWindow.DrawRectangle (Style.BackgroundGCs[(int)StateType.Normal], true, area);
		}

		void PaintRectangle (Rectangle area, InterpType interpolation)
		{
			int x_offset = scaled_width < Allocation.Width ? (int)(Allocation.Width - scaled_width) / 2 : -XOffset;
			int y_offset = scaled_height < Allocation.Height ? (int)(Allocation.Height - scaled_height) / 2 : -YOffset;
			//Draw background
			if (y_offset > 0)   //Top
				PaintBackground (new Rectangle (0, 0, Allocation.Width, y_offset), area);
			if (x_offset > 0)   //Left
				PaintBackground (new Rectangle (0, y_offset, x_offset, (int)scaled_height), area);
			if (x_offset >= 0)  //Right
				PaintBackground (new Rectangle (x_offset + (int)scaled_width, y_offset, Allocation.Width - x_offset - (int)scaled_width, (int)scaled_height), area);
			if (y_offset >= 0)  //Bottom
				PaintBackground (new Rectangle (0, y_offset + (int)scaled_height, Allocation.Width, Allocation.Height - y_offset - (int)scaled_height), area);

			if (Pixbuf == null)
				return;

			area.Intersect (new Rectangle (x_offset, y_offset, (int)scaled_width, (int)scaled_height));

			if (area.Width <= 0 || area.Height <= 0)
				return;

			//Short circuit for 1:1 zoom
			if (zoom == 1.0 &&
				!Pixbuf.HasAlpha &&
				Pixbuf.BitsPerSample == 8 &&
				pixbuf_orientation == ImageOrientation.TopLeft) {
				GdkWindow.DrawPixbuf (Style.BlackGC,
							  Pixbuf,
							  area.X - x_offset, area.Y - y_offset,
							  area.X, area.Y,
							  area.Width, area.Height,
							  RgbDither.Max,
							  area.X - x_offset, area.Y - y_offset);
				return;
			}

			Rectangle pixbuf_area = PixbufUtils.TransformOrientation ((int)scaled_width,
										  (int)scaled_height,
										  new Rectangle ((area.X - x_offset),
												 (area.Y - y_offset),
												 area.Width,
												 area.Height),
										  PixbufUtils.ReverseTransformation (pixbuf_orientation));

			using var temp_pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, pixbuf_area.Width, pixbuf_area.Height);
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

			using var dest_pixbuf = PixbufUtils.TransformOrientation (temp_pixbuf, pixbuf_orientation);
			GdkWindow.DrawPixbuf (Style.BlackGC,
						  dest_pixbuf,
						  0, 0,
						  area.X, area.Y,
						  area.Width, area.Height,
						  RgbDither.Max,
						  area.X - x_offset, area.Y - y_offset);
		}

		uint scaled_width, scaled_height;
		void ComputeScaledSize ()
		{
			if (Pixbuf == null)
				scaled_width = scaled_height = 0;
			else {
				double width;
				double height;
				if ((int)pixbuf_orientation <= 4) { //TopLeft, TopRight, BottomRight, BottomLeft
					width = Pixbuf.Width;
					height = Pixbuf.Height;
				} else {            //LeftTop, RightTop, RightBottom, LeftBottom
					width = Pixbuf.Height;
					height = Pixbuf.Width;
				}
				scaled_width = (uint)Math.Floor (width * Zoom + .5);
				scaled_height = (uint)Math.Floor (height * Zoom + .5);
			}

			Hadjustment.PageSize = Math.Min (scaled_width, Allocation.Width);
			Hadjustment.PageIncrement = scaled_width * .9;
			Hadjustment.StepIncrement = 32;
			Hadjustment.Upper = scaled_width;
			Hadjustment.Lower = 0;

			Vadjustment.PageSize = Math.Min (scaled_height, Allocation.Height);
			Vadjustment.PageIncrement = scaled_height * .9;
			Vadjustment.StepIncrement = 32;
			Vadjustment.Upper = scaled_height;
			Vadjustment.Lower = 0;

		}

		event EventHandler AdjustmentsChanged;
		void HandleAdjustmentsValueChanged (object sender, EventArgs e)
		{
			AdjustmentsChanged?.Invoke (this, EventArgs.Empty);
		}

		void ScrollToAdjustments (object sender, EventArgs e)
		{
			ScrollTo ((int)Hadjustment.Value, (int)Vadjustment.Value, false);
		}

		void ScrollTo (int x, int y, bool change_adjustments)
		{
			x = Clamp (x, 0, (int)(Hadjustment.Upper - Hadjustment.PageSize));
			y = Clamp (y, 0, (int)(Vadjustment.Upper - Vadjustment.PageSize));

			int xof = x - XOffset;
			int yof = y - YOffset;
			XOffset = x;
			YOffset = y;

			if (IsRealized) {
				GdkWindow.Scroll (-xof, -yof);
				GdkWindow.ProcessUpdates (true);
			}

			if (change_adjustments) {
				AdjustmentsChanged -= ScrollToAdjustments;
				Hadjustment.Value = XOffset;
				Vadjustment.Value = YOffset;
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
			if ((int)pixbuf_orientation <= 4) { //TopLeft, TopRight, BottomRight, BottomLeft
				width = Pixbuf.Width;
				height = Pixbuf.Height;
			} else {            //LeftTop, RightTop, RightBottom, LeftBottom
				width = Pixbuf.Height;
				height = Pixbuf.Width;
			}
			if (upscale)
				return Math.Min ((double)Allocation.Width / width,
						 (double)Allocation.Height / height);
			return Math.Min (1.0,
					 Math.Min ((double)Allocation.Width / width,
						   (double)Allocation.Height / height));
		}
		#endregion


		#region selection
		bool OnSelectionExposeEvent (EventExpose evnt)
		{
			if (selection == Rectangle.Zero)
				return false;

			Rectangle win_selection = ImageCoordsToWindow (selection);
			using (var evnt_region = evnt.Region.Copy ()) {
				using (var r = new Region ()) {
					r.UnionWithRect (win_selection);
					evnt_region.Subtract (r);
				}

				using Cairo.Context ctx = CairoHelper.Create (GdkWindow);
				ctx.SetSourceRGBA (.5, .5, .5, .7);
				CairoHelper.Region (ctx, evnt_region);
				ctx.Fill ();
			}
			return true;
		}

		enum DragMode
		{
			None,
			Move,
			Extend,
		}

		const int SELECTION_SNAP_DISTANCE = 8;
		DragMode GetDragMode (int x, int y)
		{
			Rectangle win_selection = ImageCoordsToWindow (selection);
			if (Rectangle.Inflate (win_selection, -SELECTION_SNAP_DISTANCE, -SELECTION_SNAP_DISTANCE).Contains (x, y))
				return DragMode.Move;
			if (Rectangle.Inflate (win_selection, SELECTION_SNAP_DISTANCE, SELECTION_SNAP_DISTANCE).Contains (x, y))
				return DragMode.Extend;
			return DragMode.None;
		}

		bool isDraggingSelection;
		bool fixedHeight;
		bool fixedWidth;
		bool isMovingSelection;
		Point selectionAnchor = Point.Zero;

		bool OnSelectionButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button != 1)
				return false;

			if (evnt.Type == EventType.TwoButtonPress) {
				isDraggingSelection = false;
				isMovingSelection = false;
				return false;
			}

			Point img = WindowCoordsToImage (new Point ((int)evnt.X, (int)evnt.Y));
			switch (GetDragMode ((int)evnt.X, (int)evnt.Y)) {
			case DragMode.None:
				isDraggingSelection = true;
				PointerMode = PointerMode.Select;
				Selection = Rectangle.Zero;
				selectionAnchor = img;
				break;

			case DragMode.Extend:
				Rectangle win_sel = ImageCoordsToWindow (Selection);
				isDraggingSelection = true;
				if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE &&
					Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {              //TopLeft
					selectionAnchor = new Point (Selection.X + Selection.Width, Selection.Y + Selection.Height);
				} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE &&
					   Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {           //TopRight
					selectionAnchor = new Point (Selection.X, Selection.Y + Selection.Height);
				} else if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE &&
					   Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {  //BottomLeft
					selectionAnchor = new Point (Selection.X + Selection.Width, Selection.Y);
				} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE &&
					   Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {  //BottomRight
					selectionAnchor = new Point (Selection.X, Selection.Y);
				} else if (Math.Abs (win_sel.X - evnt.X) < SELECTION_SNAP_DISTANCE) {           //Left
					selectionAnchor = new Point (Selection.X + Selection.Width, Selection.Y);
					fixedHeight = true;
				} else if (Math.Abs (win_sel.X + win_sel.Width - evnt.X) < SELECTION_SNAP_DISTANCE) {   //Right
					selectionAnchor = new Point (Selection.X, Selection.Y);
					fixedHeight = true;
				} else if (Math.Abs (win_sel.Y - evnt.Y) < SELECTION_SNAP_DISTANCE) {           //Top
					selectionAnchor = new Point (Selection.X, Selection.Y + Selection.Height);
					fixedWidth = true;
				} else if (Math.Abs (win_sel.Y + win_sel.Height - evnt.Y) < SELECTION_SNAP_DISTANCE) {  //Bottom
					selectionAnchor = new Point (Selection.X, Selection.Y);
					fixedWidth = true;
				} else {
					fixedWidth = fixedHeight = false;
					isDraggingSelection = false;
				}
				break;

			case DragMode.Move:
				isMovingSelection = true;
				selectionAnchor = img;
				SelectionSetPointer ((int)evnt.X, (int)evnt.Y);
				break;
			}

			return true;
		}

		bool OnSelectionButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button != 1)
				return false;

			isDraggingSelection = false;
			isMovingSelection = false;
			fixedWidth = fixedHeight = false;

			SelectionSetPointer ((int)evnt.X, (int)evnt.Y);
			return true;
		}

		void SelectionSetPointer (int x, int y)
		{
			if (isMovingSelection)
				GdkWindow.Cursor = new Cursor (CursorType.Crosshair);
			else {
				switch (GetDragMode (x, y)) {
				case DragMode.Move:
					GdkWindow.Cursor = new Cursor (CursorType.Hand1);
					break;
				default:
					GdkWindow.Cursor = null;
					break;
				case DragMode.Extend:
					Rectangle win_sel = ImageCoordsToWindow (Selection);
					if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE &&
						Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {               //TopLeft
						GdkWindow.Cursor = new Cursor (CursorType.TopLeftCorner);
					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {            //TopRight
						GdkWindow.Cursor = new Cursor (CursorType.TopRightCorner);
					} else if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {   //BottomLeft
						GdkWindow.Cursor = new Cursor (CursorType.BottomLeftCorner);
					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE &&
						   Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {   //BottomRight
						GdkWindow.Cursor = new Cursor (CursorType.BottomRightCorner);
					} else if (Math.Abs (win_sel.X - x) < SELECTION_SNAP_DISTANCE) {            //Left
						GdkWindow.Cursor = new Cursor (CursorType.LeftSide);
					} else if (Math.Abs (win_sel.X + win_sel.Width - x) < SELECTION_SNAP_DISTANCE) {    //Right
						GdkWindow.Cursor = new Cursor (CursorType.RightSide);
					} else if (Math.Abs (win_sel.Y - y) < SELECTION_SNAP_DISTANCE) {            //Top
						GdkWindow.Cursor = new Cursor (CursorType.TopSide);
					} else if (Math.Abs (win_sel.Y + win_sel.Height - y) < SELECTION_SNAP_DISTANCE) {   //Bottom
						GdkWindow.Cursor = new Cursor (CursorType.BottomSide);
					}
					break;
				}
			}
		}

		const int SELECTION_THRESHOLD = 5;
		bool OnSelectionMotionNotifyEvent (EventMotion evnt)
		{
			int x, y;
			if (evnt.IsHint)
				GdkWindow.GetPointer (out x, out y, out _);
			else {
				x = (int)evnt.X;
				y = (int)evnt.Y;
			}

			Point img = WindowCoordsToImage (new Point (x, y));
			if (isDraggingSelection) {
				Point win_anchor = ImageCoordsToWindow (selectionAnchor);
				if (Selection == Rectangle.Zero &&
					Math.Abs (evnt.X - win_anchor.X) < SELECTION_THRESHOLD &&
					Math.Abs (evnt.Y - win_anchor.Y) < SELECTION_THRESHOLD) {
					SelectionSetPointer (x, y);
					return true;
				}


				if (selection_xy_ratio == 0)
					Selection = new Rectangle (fixedWidth ? Selection.X : Math.Min (selectionAnchor.X, img.X),
								   fixedHeight ? Selection.Y : Math.Min (selectionAnchor.Y, img.Y),
								   fixedWidth ? Selection.Width : Math.Abs (selectionAnchor.X - img.X),
								   fixedHeight ? Selection.Height : Math.Abs (selectionAnchor.Y - img.Y));

				else
					Selection = ConstrainSelection (new Rectangle (Math.Min (selectionAnchor.X, img.X),
											   Math.Min (selectionAnchor.Y, img.Y),
											   Math.Abs (selectionAnchor.X - img.X),
											   Math.Abs (selectionAnchor.Y - img.Y)),
									fixedWidth, fixedHeight);

				SelectionSetPointer (x, y);
				return true;
			}

			if (isMovingSelection) {
				Selection = new Rectangle (Clamp (Selection.X + img.X - selectionAnchor.X, 0, Pixbuf.Width - Selection.Width),
							   Clamp (Selection.Y + img.Y - selectionAnchor.Y, 0, Pixbuf.Height - Selection.Height),
							   Selection.Width, Selection.Height);
				selectionAnchor = img;
				SelectionSetPointer (x, y);
				return true;
			}

			SelectionSetPointer (x, y);
			return true;
		}

		Rectangle ConstrainSelection (Rectangle sel, bool fixedWidth, bool fixedHeight)
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

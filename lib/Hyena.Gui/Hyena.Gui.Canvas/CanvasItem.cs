//
// CanvasItem.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using Hyena.Gui.Theming;

namespace Hyena.Gui.Canvas
{
	public class CanvasItem
	{
		CanvasManager manager;
		Hyena.Data.IDataBinder binder;
		Theme theme;

		bool prelight_in;
		double prelight_opacity;

		#region Public API

		public event EventHandler<EventArgs> SizeChanged;
		public event EventHandler<EventArgs> LayoutUpdated;

		public CanvasItem ()
		{
			Visible = true;
			Opacity = 1.0;
			Width = double.NaN;
			Height = double.NaN;
			Margin = new Thickness (0);
			Padding = new Thickness (0);
			Foreground = Brush.Black;
			Background = Brush.White;
			MarginStyle = MarginStyle.None;
		}

		public void InvalidateArrange ()
		{
			CanvasItem root = RootAncestor;
			if (root != null && root.Manager != null) {
				root.Manager.QueueArrange (this);
			}
		}

		public void InvalidateMeasure ()
		{
			CanvasItem root = RootAncestor;
			if (root != null && root.Manager != null) {
				root.Manager.QueueMeasure (this);
			}
		}

		public void InvalidateRender ()
		{
			InvalidateRender (InvalidationRect);
		}

		public void Invalidate (Rect area)
		{
			InvalidateRender (area);
		}

		public Hyena.Data.IDataBinder Binder {
			get { return binder ?? (binder = new MemoryDataBinder ()); }
			set { binder = value; }
		}

		public virtual void Bind (object o)
		{
			Binder.Bind (o);
		}

		public virtual void Arrange ()
		{
		}

		public Action<Cairo.Context, Theme, Rect, double> PrelightRenderer { get; set; }

		public virtual Size Measure (Size available)
		{
			double m_x = Margin.X;
			double m_y = Margin.Y;

			double a_w = available.Width - m_x;
			double a_h = available.Height - m_y;

			return DesiredSize = new Size (
				Math.Max (0, Math.Min (a_w, double.IsNaN (Width) ? a_w : Width + m_x)),
				Math.Max (0, Math.Min (a_h, double.IsNaN (Height) ? a_h : Height + m_y))
			);
		}

		public void Render (Hyena.Data.Gui.CellContext context)
		{
			var alloc = ContentAllocation;
			var cr = context.Context;
			double opacity = Opacity;

			if (alloc.Width <= 0 || alloc.Height <= 0 || opacity <= 0) {
				return;
			}

			cr.Save ();

			if (opacity < 1.0) {
				cr.PushGroup ();
			}

			MarginStyle margin_style = MarginStyle;
			if (margin_style != null && margin_style != MarginStyle.None) {
				cr.Translate (Math.Round (Allocation.X), Math.Round (Allocation.Y));
				cr.Save ();
				margin_style.Apply (this, cr);
				cr.Restore ();
				cr.Translate (Math.Round (Margin.Left), Math.Round (Margin.Top));
			} else {
				cr.Translate (Math.Round (alloc.X), Math.Round (alloc.Y));
			}

			cr.Antialias = Cairo.Antialias.Default;

			//cr.Rectangle (0, 0, alloc.Width, alloc.Height);
			//cr.Clip ();

			ClippedRender (context);

			if (PrelightRenderer != null && prelight_opacity > 0) {
				PrelightRenderer (context.Context, context.Theme, new Rect (0, 0, ContentAllocation.Width, ContentAllocation.Height), prelight_opacity);
			}

			//cr.ResetClip ();

			if (opacity < 1.0) {
				cr.PopGroupToSource ();
				cr.PaintWithAlpha (Opacity);
			}

			cr.Restore ();
		}

		public CanvasItem RootAncestor {
			get {
				CanvasItem root = this;
				while (root.Parent != null) {
					root = root.Parent;
				}
				return root;
			}
		}

		public CanvasItem Parent { get; set; }

		public Theme Theme {
			get { return theme ?? (Parent?.Theme); }
			set { theme = value; }
		}

		public virtual bool GetTooltipMarkupAt (Point pt, out string markup, out Rect area)
		{
			markup = TooltipMarkup;
			area = TopLevelAllocation;
			return markup != null;
		}

		protected string TooltipMarkup { get; set; }
		public bool Visible { get; set; }
		public double Opacity { get; set; }
		public Brush Foreground { get; set; }
		public Brush Background { get; set; }

		public Thickness Padding { get; set; }
		public MarginStyle MarginStyle { get; set; }

		public Size DesiredSize { get; protected set; }
		// FIXME need this?
		public Rect VirtualAllocation { get; set; }

		double min_width, max_width;
		public double MinWidth {
			get { return min_width; }
			set {
				min_width = value;
				if (value > max_width) {
					max_width = value;
				}
			}
		}

		public double MaxWidth {
			get { return max_width; }
			set {
				max_width = value;
				if (value < min_width) {
					min_width = value;
				}
			}
		}

		public double Width { get; set; }
		public double Height { get; set; }

		Thickness margin;
		public Thickness Margin {
			get { return margin; }
			set {
				margin = value;
				// Refresh the ContentAllocation etc values
				Allocation = allocation;
			}
		}

		Rect allocation;
		public Rect Allocation {
			get { return allocation; }
			set {
				allocation = value;
				ContentAllocation = new Rect (
					Allocation.X + Margin.Left,
					Allocation.Y + Margin.Top,
					Math.Max (0, Allocation.Width - Margin.X),
					Math.Max (0, Allocation.Height - Margin.Y)
				);
				ContentSize = new Size (ContentAllocation.Width, ContentAllocation.Height);
				RenderSize = new Size (Math.Round (ContentAllocation.Width), Math.Round (ContentAllocation.Height));
			}
		}

		public Rect ContentAllocation { get; private set; }
		public Size ContentSize { get; private set; }
		protected Size RenderSize { get; private set; }

		protected virtual Rect InvalidationRect {
			//get { return Rect.Empty; }
			get { return Allocation; }
		}


		#endregion

		public void Invalidate ()
		{
			InvalidateMeasure ();
			InvalidateArrange ();
			InvalidateRender ();
		}

		protected void InvalidateRender (Rect area)
		{
			if (Parent == null) {
				OnInvalidate (area);
			} else {
				var alloc = Parent.ContentAllocation;
				area.Offset (alloc.X, alloc.Y);
				Parent.Invalidate (area);
			}
		}

		void OnInvalidate (Rect area)
		{
			CanvasItem root = RootAncestor;
			if (root != null && root.Manager != null) {
				root.Manager.QueueRender (this, area);
			} else {
				Hyena.Log.WarningFormat ("Asked to invalidate {0} for {1} but no CanvasManager!", area, this);
			}
		}

		protected object BoundObject {
			get { return Binder.BoundObject; }
			set { Binder.BoundObject = value; }
		}

		Rect TopLevelAllocation {
			get {
				var alloc = ContentAllocation;
				var top = this;
				while (top.Parent != null) {
					alloc.Offset (top.Parent.Allocation);
					top = top.Parent;
				}

				return alloc;
			}
		}

		protected virtual void ClippedRender (Cairo.Context cr)
		{
		}

		protected virtual void ClippedRender (Hyena.Data.Gui.CellContext context)
		{
			ClippedRender (context.Context);
		}

		protected virtual void OnSizeChanged ()
		{
			SizeChanged?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnLayoutUpdated ()
		{
			LayoutUpdated?.Invoke (this, EventArgs.Empty);
		}

		internal CanvasManager Manager {
			get { return manager ?? (Parent?.Manager); }
			set { manager = value; }
		}

		#region Input Events

		//public event EventHandler<EventArgs> Clicked;

		bool pointer_grabbed;
		public virtual bool IsPointerGrabbed {
			get { return pointer_grabbed; }
		}

		protected void GrabPointer ()
		{
			pointer_grabbed = true;
		}

		protected void ReleasePointer ()
		{
			pointer_grabbed = false;
		}

		public virtual bool ButtonEvent (Point press, bool pressed, uint button)
		{
			//GrabPointer ();
			return false;
		}

		/*public virtual void ButtonRelease ()
        {
            ReleasePointer ();
            OnClicked ();
        }*/

		public virtual bool CursorMotionEvent (Point cursor)
		{
			return false;
		}

		public virtual bool CursorEnterEvent ()
		{
			if (PrelightRenderer != null) {
				prelight_in = true;
				prelight_stage.AddOrReset (this);
			}
			return false;
		}

		public virtual bool CursorLeaveEvent ()
		{
			if (PrelightRenderer != null) {
				prelight_in = false;
				prelight_stage.AddOrReset (this);
			}
			return false;
		}

		static Hyena.Gui.Theatrics.Stage<CanvasItem> prelight_stage = new Hyena.Gui.Theatrics.Stage<CanvasItem> (250);
		static CanvasItem ()
		{
			prelight_stage.ActorStep += actor => {
				var alpha = actor.Target.prelight_opacity;
				alpha += actor.Target.prelight_in
					? actor.StepDeltaPercent
					: -actor.StepDeltaPercent;
				actor.Target.prelight_opacity = alpha = Math.Max (0.0, Math.Min (1.0, alpha));
				actor.Target.InvalidateRender ();
				return alpha > 0 && alpha < 1;
			};
		}

		/*protected virtual void OnClicked ()
        {
            var handler = Clicked;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }*/

		#endregion

		class MemoryDataBinder : Hyena.Data.IDataBinder
		{
			public void Bind (object o)
			{
				BoundObject = o;
			}

			public object BoundObject { get; set; }
		}
	}
}

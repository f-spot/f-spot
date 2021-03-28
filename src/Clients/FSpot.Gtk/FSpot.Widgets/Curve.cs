//
// Curve.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gdk;

using Gtk;

namespace FSpot.Widgets
{
	public class Curve : DrawingArea
	{
#region public API
		public Curve ()
		{
			Events |= EventMask.ExposureMask
				| EventMask.PointerMotionMask
				| EventMask.PointerMotionHintMask
				| EventMask.EnterNotifyMask
				| EventMask.ButtonPressMask
				| EventMask.ButtonReleaseMask
				| EventMask.Button1MotionMask;
			ResetVector ();
		}

		public void Reset ()
		{
			CurveType oldType = CurveType;
			CurveType = CurveType.Spline;
			ResetVector ();
			if (oldType != CurveType.Spline)
				CurveTypeChanged?.Invoke (this, EventArgs.Empty);
		}

		float minX;
		public float MinX {
			get { return minX; }
			set { SetRange (value, maxX, minY, maxY); }
		}

		float maxX = 1.0f;
		public float MaxX {
			get { return maxX; }
			set { SetRange (minX, value, minY, maxY); }
		}

		float minY;
		public float MinY {
			get { return minY; }
			set { SetRange (minX, maxX, value, maxY); }
		}

		float maxY = 1.0f;
		public float MaxY {
			get { return maxY; }
			set { SetRange (minX, maxX, minY, value); }
		}

		public void SetRange (float minX, float maxX, float minY, float maxY)
		{
			this.minX = minX;
			this.maxX = maxX;
			this.minY = minY;
			this.maxY = maxY;

			ResetVector ();
			QueueDraw ();
		}

		CurveType curveType = CurveType.Spline;
		public CurveType CurveType {
			get { return curveType; }
			set {
				curveType = value;
				QueueDraw ();
			}
		}

		public float [] GetVector (int len)
		{
			if (len <= 0)
				return null;

			var vector = new float [len];

			var xv = new float [points.Count];
			var yv = new float [points.Count];
			int i = 0;
			foreach (var keyval in points) {
				xv[i] = keyval.Key;
				yv[i] = keyval.Value;
				i++;
			}
			float rx = MinX;
			float dx = (MaxX - MinX) / (len - 1.0f);

			switch (CurveType) {
			case CurveType.Spline:	
				var y2v = SplineSolve (xv, yv);

				for (int x = 0; x < len; x++, rx += dx) {
					float ry = SplineEval (xv, yv, y2v, rx);
					if (ry < MinY)
						ry = MinY;
					if (ry > MaxY)
						ry = MaxY;
					vector[x] = ry;
				}
				break;;
			case CurveType.Linear:
				for (int x = 0; x < len; x++, rx += dx) {
					float ry = LinearEval (xv, yv, rx);
					if (ry < MinY)
						ry = MinY;
					if (ry > MaxY)
						ry = MaxY;
					vector[x] = ry;
				}
				break;
			case CurveType.Free:
				throw new NotImplementedException ();
			}

			return vector;
		}

		public void SetVector (float[] vector)
		{
			throw new NotImplementedException ("FSpot.Gui.Widgets.Curve SetVector does nothing!!!");
		}

		public void AddPoint (float x, float y)
		{
			points.Add (x, y);
			CurveChanged?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler CurveTypeChanged;
		public event EventHandler CurveChanged;
#endregion

#region vector handling
		SortedDictionary<float, float> points;
		void ResetVector ()
		{
			points = new SortedDictionary<float, float> {
				{ minX, minY },
				{ maxX, maxY },
				{ .2f, .1f },
				{ .5f, .5f },
				{ .8f, .9f }
			};
		}
#endregion

#region math helpers
		/* Solve the tridiagonal equation system that determines the second
		   derivatives for the interpolation points. (Based on Numerical
		   Recipies 2nd Edition) */
		static float [] SplineSolve (float[] x, float[] y)
		{
			var y2 = new float [x.Length];
			var u = new float [x.Length - 1];

			y2[0] = u[0] = 0.0f;	//Set lower boundary condition to "natural"

			for (int i = 1; i < x.Length - 1; ++i) {
				float sig = (x[i] - x[i - 1]) / (x[i + 1] - x[i - 1]);
				float p = sig * y2[i - 1] + 2.0f;
				y2[i] = (sig - 1.0f) / p;
				u[i] = ((y[i + 1] - y[i]) / (x[i + 1] - x[i]) - (y[i] - y[i - 1]) / (x[i] - x[i - 1]));
				u[i] = (6.0f * u[i] / (x[i + 1] - x[i - 1]) - sig * u[i - 1]) / p;
			}

			y2[x.Length - 1] = 0.0f;
			for (int k = x.Length - 2; k >= 0; --k)
				y2[k] = y2[k] * y2[k + 1] + u[k];

			return y2;
		}

		/* Returns a y value for val, given x[], y[] and y2[] */
		static float SplineEval (float[] x, float[] y, float[] y2, float val)
		{
			//binary search for the right interval
			int k_lo = 0;
			int k_hi = x.Length - 1;
			while (k_hi - k_lo > 1) {
				int k = (k_hi + k_lo) / 2;
				if (x[k] > val)
					k_hi = k;
				else
					k_lo = k;
			}
			float h = x[k_hi] - x[k_lo];
			float a = (x[k_hi] - val) / h;
			float b = (val - x[k_lo]) / h;
			return a * y[k_lo] + b * y[k_hi] + ((a*a*a - a) * y2[k_lo] + (b*b*b - b) * y2[k_hi]) * (h*h)/6.0f;
		}

		static float LinearEval (float[] x, float[] y, float val)
		{
			//binary search for the right interval
			int k_lo = 0;
			int k_hi = x.Length - 1;
			while (k_hi - k_lo > 1) {
				int k = (k_hi + k_lo) / 2;
				if (x[k] > val)
					k_hi = k;
				else
					k_lo = k;
			}
			float dx = x[k_hi] - x[k_lo];
			float dy = y[k_hi] - y[k_lo];
			return val*dy/dx + y[k_lo] - dy/dx*x[k_lo];
		}

		static int Project (float val, float min, float max, int norm)
		{
			return (int)((norm - 1) * ((val - min) / (max - min)) + .5f);
		}

		static float Unproject (int val, float min, float max, int norm)
		{
			return val / (float) (norm - 1) * (max - min) + min;
		}
#endregion

#region Gtk widgetry
		const int radius = 3;		//radius of the control points
		const int min_distance = 8; //min distance between control points
		readonly int x_offset = radius;
		readonly int y_offset = radius;
		int width, height;		//the real graph

		Pixmap pixmap;

		protected override bool OnConfigureEvent (EventConfigure evnt)
		{
			pixmap = null;
			return base.OnConfigureEvent (evnt);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			pixmap = new Pixmap (GdkWindow, Allocation.Width, Allocation.Height);
			Draw ();
			return base.OnExposeEvent (evnt);
		}

		Gdk.Point [] Interpolate (int width, int height)
		{
			var vector = GetVector (width);
			var retval = new Gdk.Point [width];
			for (int i = 0; i < width; i++) {
				retval[i].X = x_offset + i;
				retval[i].Y = y_offset + height - Project (vector[i], MinY, MaxY, height);
			}
			return retval;
		}

		void Draw ()
		{
			if (pixmap == null)
				return;

			Style style = Style;
			StateType state = Sensitive ? StateType.Normal : StateType.Insensitive;

			if (width <= 0 || height <= 0)
				return;

			//clear the pixmap
			GtkBeans.Style.PaintFlatBox (style, pixmap, StateType.Normal, ShadowType.None, null, this, "curve_bg", 0, 0, Allocation.Width, Allocation.Height);

			//draw the grid lines
			for (int i = 0; i < 5; i++) {
				pixmap.DrawLine (style.DarkGC (state),
						 x_offset,
						 i * (int)(height / 4.0) + y_offset,
						 width + x_offset,
						 i * (int)(height / 4.0) + y_offset);
				pixmap.DrawLine (style.DarkGC (state),
						 i * (int)(width / 4.0) + x_offset,
						 y_offset,
						 i * (int)(width / 4.0) + x_offset,
						 height + y_offset);
			}

			//draw the curve
			pixmap.DrawPoints (style.ForegroundGC (state), Interpolate (width, height));

			//draw the bullets
			if (CurveType != CurveType.Free)
				foreach (var keyval in points) {
					if (keyval.Key < MinX)
						continue;
					int x = Project (keyval.Key, MinX, MaxX, width);
					int y = height - Project (keyval.Value, MinY, MaxY, height);
					pixmap.DrawArc (style.ForegroundGC (state), true, x, y, radius * 2, radius * 2, 0, 360*64);
				}
			GdkWindow.DrawDrawable (style.ForegroundGC (state), pixmap, 0, 0, 0, 0, Allocation.Width, Allocation.Height);
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			width = allocation.Width - 2 * radius;
			height = allocation.Height - 2 * radius;	
			base.OnSizeAllocated (allocation);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = 128 + 2 * x_offset;
			requisition.Height = 128 + 2 * y_offset;
		}

		float? grab_point;
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			int px = (int)evnt.X - x_offset;
			int py = (int)evnt.Y - y_offset;
			if (px < 0) px = 0;
			if (px > width - 1) px = width - 1;
			if (py < 0) py = 0;
			if (py > height - 1) py = height - 1;
			
			//find the closest point
			float closest_x = MinX - 1;
			var distance = int.MaxValue;
			foreach (var point in points) {
				int cx = Project (point.Key, MinX, MaxX, width);
				if (Math.Abs (px - cx) < distance) {
					distance = Math.Abs (px - cx);
					closest_x = point.Key;
				}
			}

			Grab.Add (this);
			CursorType = CursorType.Tcross;
			switch (CurveType) {
			case CurveType.Linear:
			case CurveType.Spline:
				if (distance > min_distance) {
					//insert a new control point
					AddPoint ((closest_x = Unproject (px, MinX, MaxX, width)), MaxY - Unproject (py, MinY, MaxY, height));
					QueueDraw ();
				}
				grab_point = closest_x;
				break;
			case CurveType.Free:
				throw new NotImplementedException ();
			}

			return true;
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			Grab.Remove (this);
			//FIXME: remove inactive points

			CursorType = CursorType.Fleur;
			grab_point = null;
			return true;
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			int px = (int)evnt.X - x_offset;
			int py = (int)evnt.Y - y_offset;
			if (px < 0) px = 0;
			if (px > width - 1) px = width - 1;
			if (py < 0) py = 0;
			if (py > height - 1) py = height - 1;
			
			//find the closest point
			float closest_x = MinX - 1;
			var distance = int.MaxValue;
			foreach (var point in points) {
				int cx = Project (point.Key, MinX, MaxX, width);
				if (Math.Abs (px - cx) < distance) {
					distance = Math.Abs (px - cx);
					closest_x = point.Key;
				}
			}

			switch (CurveType) {
			case CurveType.Spline:
			case CurveType.Linear:
				if (grab_point == null) {		//No grabbed point
					if (distance <= min_distance)
						CursorType = CursorType.Fleur;
					else
						CursorType = CursorType.Tcross;
					return true;
				}

				CursorType = CursorType.Tcross;
				points.Remove (grab_point.Value);
				AddPoint ((closest_x = Unproject (px, MinX, MaxX, width)), MaxY - Unproject (py, MinY, MaxY, height));
				QueueDraw ();
				grab_point = closest_x;

				break;
			case CurveType.Free:
				throw new NotImplementedException ();
			}
			return true;
		}

		Gdk.CursorType cursor_type = Gdk.CursorType.TopLeftArrow;
		CursorType CursorType {
			get { return cursor_type; }
			set {
				if (value == cursor_type)
					return;
				cursor_type = value;
				GdkWindow.Cursor = new Cursor (CursorType);	
			}
		}
#endregion
	}
}

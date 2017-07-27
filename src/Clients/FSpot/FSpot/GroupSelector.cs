//
// GroupSelector.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2004-2007 Larry Ewing
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
using System.Linq;
using Mono.Unix;

using Gtk;
using Gdk;
using GLib;

using FSpot.Utils;
using FSpot.Widgets;
using Layout = Pango.Layout;

namespace FSpot
{
	public class GroupSelector : Fixed
	{
		int border = 6;
		int box_spacing = 2;
		int box_top_padding = 6;
		public static int MIN_BOX_WIDTH = 20;

		Glass glass;
		Limit min_limit;
		Limit max_limit;

		Gtk.Button left;
		Gtk.Button right;
		DelayedOperation left_delay;
		DelayedOperation right_delay;

		Gdk.Window event_window;

		public Gdk.Rectangle background;
		public Gdk.Rectangle legend;
		public Gdk.Rectangle action_area;

		Pango.Layout [] tick_layouts;
		int [] box_counts = new int [0];
		int box_count_max;
		int min_filled;
		int max_filled;
		bool has_limits;

		protected FSpot.GroupAdaptor adaptor;
		public FSpot.GroupAdaptor Adaptor {
			set {
				if (adaptor != null) {
					adaptor.Changed -= HandleAdaptorChanged;
					adaptor.Dispose ();
				}

				adaptor = value;
				HandleAdaptorChanged (adaptor);
				has_limits = adaptor is FSpot.ILimitable;

				if (has_limits) {
					min_limit.SetPosition (0, false);
					max_limit.SetPosition (adaptor.Count () - 1, false);
				}

				if (adaptor is TimeAdaptor) {
					left.TooltipText = Catalog.GetString ("More dates");
					right.TooltipText = Catalog.GetString ("More dates");
				} else {
					left.TooltipText = Catalog.GetString ("More");
					right.TooltipText = Catalog.GetString ("More");
				}

				adaptor.Changed += HandleAdaptorChanged;
			}
			get {
				return adaptor;
			}
		}

		public bool GlassUpdating {
			get {
				return glass.GlassUpdating;
			}
			set {
				glass.GlassUpdating = value;
			}
		}

		public int GlassPosition {
			get {
				return glass.Position;
			}
		}

		void HandleAdaptorChanged (GroupAdaptor adaptor)
		{
			bool size_changed = box_counts.Length != adaptor.Count ();
			int [] box_values = new int [adaptor.Count ()];

			if (tick_layouts != null) {
				foreach (Layout l in tick_layouts.Where(l => l != null))
				{
					l.Dispose ();
				}
			}
			tick_layouts = new Pango.Layout [adaptor.Count ()];

			int i = 0;
			while (i < adaptor.Count ()) {
				box_values [i] = adaptor.Value (i);
				string label = adaptor.TickLabel (i);
				if (label != null) {
					tick_layouts [i] = CreatePangoLayout (label);
				}
				i++;
			}

			if (glass.Position >= adaptor.Count())
				glass.SetPosition (adaptor.Count() - 1);

			Counts = box_values;

			if (has_limits && size_changed) {
				min_limit.SetPosition (0, false);
				max_limit.SetPosition (adaptor.Count () - 1, false);
			}

			for (i = min_limit.Position; i < box_counts.Length; i++)
				if (box_counts [i] > 0)
						break;

			SetPosition (i < box_counts.Length ? i : min_limit.Position);
			ScrollTo (min_limit.Position);

			this.QueueDraw ();
		}

		int [] Counts {
			set {
				bool min_found = false;
				box_count_max = 0;
				min_filled = 0;
				max_filled = 0;

				if (value != null)
					box_counts = value;
				else
					value = new int [0];

				for (int i = 0; i < box_counts.Length; i++){
					int count = box_counts [i];
					box_count_max = Math.Max (count, box_count_max);

					if (count > 0) {
						if (!min_found) {
							min_filled = i;
							min_found = true;
						}
						max_filled = i;
					}
				}
			}
		}

		public enum RangeType {
			All,
			Fixed,
			Min
		}

		RangeType mode = RangeType.Min;
		public RangeType Mode {
			get {
				return mode;
			}
			set {
				mode = value;
				if (Visible)
					GdkWindow.InvalidateRect (Allocation, false);
			}
		}

		void ScrollTo (int position)
		{
			if (position ==  min_filled)
				position = 0;
			else if (position == max_filled)
				position = box_counts.Length - 1;

			Gdk.Rectangle box = new Box (this, position).Bounds;

			// Only scroll to pos if we are not dragging
			if (!glass.Dragging)
			{
				if (box.Right > background.Right)
					Offset -= box.X + box.Width - (background.X + background.Width);
				else if (box.X < background.X)
					Offset += background.X - box.X;
			}
		}

		int scroll_offset;
		public int Offset {
			get {
				return scroll_offset;
			}
			set {
				scroll_offset = value;

				int total_width = (int)(box_counts.Length * BoxWidth);

				if (total_width + scroll_offset < background.Width)
					scroll_offset = background.Width - total_width;

				if (total_width <= background.Width)
					scroll_offset = 0;

				UpdateButtons ();

				if (Visible)
					GdkWindow.InvalidateRect (Allocation, false);
			}
		}

		void UpdateButtons ()
		{
			left.Sensitive = (scroll_offset < 0);
			right.Sensitive = (box_counts.Length * BoxWidth > background.Width - scroll_offset);

			if (!left.Sensitive && left_delay.IsPending)
				left_delay.Stop ();

			if (!right.Sensitive && right_delay.IsPending)
				right_delay.Stop ();
		}

		void BoxXHitFilled (double x, out int out_position)
		{
			x -= BoxX (0);
			double position = (x / BoxWidth);
			position = System.Math.Max (0, position);
			position = System.Math.Min (position, box_counts.Length - 1);

			if (box_counts [(int)position] > 0) {
				out_position = (int)position;
			} else {
				int upper = (int)position;
				while (upper < box_counts.Length && box_counts [upper] == 0)
					upper++;

				int lower = (int)position;
				while (lower >= 0 && box_counts [lower] == 0)
					lower--;

				if (lower == -1 && upper == box_counts.Length) {
					out_position = (int)position;
				} else if (lower == -1 && upper < box_counts.Length) {
					out_position = upper;
				} else if (upper == box_counts.Length && lower > -1){
					out_position = lower;
				} else if (upper + 1 - position > position - lower) {
					out_position = lower;
				} else {
					out_position = upper;
				}
			}
		}

		bool BoxXHit (double x, out int position)
		{
			x -= BoxX (0);
			position = (int) (x / BoxWidth);
			if (position < 0) {
				position = 0;
				return false;
			}

			if (position >= box_counts.Length) {
				position = box_counts.Length -1;
				return false;
			}
			return true;
		}

		bool BoxHit (double x, double y, out int position)
		{
			if (BoxXHit (x, out position)) {
				Box box = new Box (this, position);

				if (box.Bounds.Contains ((int) x, (int) y))
					return true;

				position++;
			}
			return false;
		}

		public void SetPosition (int group)
		{
			if (!glass.Dragging)
				glass.SetPosition(group);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton args)
		{
			if (args.Button == 3)
				return DrawOrderMenu (args);

			double x = args.X + action_area.X;
			double y = args.Y + action_area.Y;

			if (glass.Contains (x, y)) {
				glass.StartDrag (x, y, args.Time);
			} else if (has_limits && min_limit.Contains (x, y)) {
				min_limit.StartDrag (x, y, args.Time);
			} else if (has_limits && max_limit.Contains (x, y)) {
				max_limit.StartDrag (x, y, args.Time);
			} else {
				int position;
				if (BoxHit (x, y, out position)) {
					BoxXHitFilled (x, out position);
					glass.UpdateGlass = true;
					glass.SetPosition (position);
					glass.UpdateGlass = false;
					return true;
				}
			}

			return base.OnButtonPressEvent (args);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton args)
		{
			double x = args.X + action_area.X;
			double y = args.Y + action_area.Y;

			if (glass.Dragging) {
				glass.EndDrag (x, y);
			} else if (min_limit.Dragging) {
				min_limit.EndDrag (x, y);
			} else if (max_limit.Dragging) {
				max_limit.EndDrag (x, y);
			}
			return base.OnButtonReleaseEvent (args);
		}

		public void UpdateLimits ()
		{
			if (adaptor != null && has_limits && min_limit != null && max_limit != null)
				((ILimitable)adaptor).SetLimits (min_limit.Position, max_limit.Position);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion args)
		{
			double x = args.X + action_area.X;
			double y = args.Y + action_area.Y;

			//Rectangle box = glass.Bounds ();
			//Console.WriteLine ("please {0} and {1} in box {2}", x, y, box);

			if (glass == null)
				return base.OnMotionNotifyEvent (args);

			if (glass.Dragging) {
				glass.UpdateDrag (x, y);
			} else if (min_limit.Dragging) {
				min_limit.UpdateDrag (x, y);
			} else if (max_limit.Dragging) {
				max_limit.UpdateDrag (x, y);
			} else {
				glass.State = glass.Contains (x, y) ? StateType.Prelight : StateType.Normal;
				min_limit.State = min_limit.Contains (x, y) ? StateType.Prelight : StateType.Normal;
				max_limit.State = max_limit.Contains (x, y) ? StateType.Prelight : StateType.Normal;
			}

			return base.OnMotionNotifyEvent (args);
		}

		protected override void OnRealized ()
		{
			SetFlag (WidgetFlags.Realized);
			GdkWindow = ParentWindow;

			base.OnRealized ();

			WindowAttr attr = WindowAttr.Zero;
			attr.WindowType = Gdk.WindowType.Child;



			attr.X = action_area.X;
			attr.Y = action_area.Y;
			attr.Width = action_area.Width;
			attr.Height = action_area.Height;
			attr.Wclass = WindowClass.InputOnly;
			attr.EventMask = (int) Events;
			attr.EventMask |= (int) (EventMask.ButtonPressMask |
						 EventMask.KeyPressMask |
						 EventMask.KeyReleaseMask |
						 EventMask.ButtonReleaseMask |
						 EventMask.PointerMotionMask);

			event_window = new Gdk.Window (GdkWindow, attr, (int) (WindowAttributesType.X | WindowAttributesType.Y));
			event_window.UserData = this.Handle;
		}

		protected override void OnUnrealized ()
		{
			event_window.Dispose ();
			event_window = null;
			base.OnUnrealized ();
		}

		Double BoxWidth {
			get {
				switch (mode) {
				case RangeType.All:
					return Math.Max (1.0, box_counts.Length == 0 ? 0 : background.Width / (double) box_counts.Length);
				case RangeType.Fixed:
					return background.Width / (double) 12;
				case RangeType.Min:
					return Math.Max (MIN_BOX_WIDTH, box_counts.Length == 0 ? 0 : background.Width / (double) box_counts.Length);
				default:
					return (double) MIN_BOX_WIDTH;
				}
			}
		}

		int BoxX (int item)
		{
			 return scroll_offset + background.X + (int) Math.Round (BoxWidth * item);
		}

		struct Box {
			Gdk.Rectangle bounds;
			Gdk.Rectangle bar;

			public Box (GroupSelector selector, int item)
			{
				bounds = new Gdk.Rectangle();
				bar = new Rectangle();
				bounds.Height = selector.background.Height;
				bounds.Y = selector.background.Y;
				bounds.X = selector.BoxX (item);
				bounds.Width = Math.Max (selector.BoxX (item + 1) - bounds.X, 1);

				if (item < 0 || item > selector.box_counts.Length - 1)
					return;

				double percent = selector.box_counts [item] / (double) Math.Max (selector.box_count_max, 1);
				bar = bounds;
				bar.Height = (int) Math.Ceiling ((bounds.Height - selector.box_top_padding) * percent);
				bar.Y += bounds.Height - bar.Height - 1;

				bar.Inflate (- selector.box_spacing, 0);
			}

			public Gdk.Rectangle Bounds {
				get {
					return bounds;
				}
			}

			public Gdk.Rectangle Bar {
				get {
					return bar;
				}
			}

		}

		/// <summary>
		/// Draws one bar indicating number of photos on specified tick.
		/// </summary>
		/// <param name='area'>screen area where legend is to be drawn</param>
		/// <param name='item'>index of a tick</param>
		void DrawBox (Rectangle area, int item)
		{
			Box box = new Box (this, item);
			Rectangle bar = box.Bar;

			if (bar.Intersect (area, out area)) {
				if (item < min_limit.Position || item > max_limit.Position) {
					GdkWindow.DrawRectangle (Style.BackgroundGC (StateType.Active), true, area);
				} else {
					GdkWindow.DrawRectangle (Style.BaseGC (StateType.Selected), true, area);
				}
			}
		}

		public Rectangle TickBounds (int item)
		{
			Rectangle bounds = Rectangle.Zero;
			bounds.X = BoxX (item);
			bounds.Y = legend.Y + 3;
			bounds.Width = 1;
			bounds.Height = 6;

			return bounds;
		}

		/// <summary>
		/// Draws one tick mark on GroupSelector widget.
		/// </summary>
		/// <param name='area'>screen area where legend is to be drawn</param>
		/// <param name='item'>index of a tick</param>
		public void DrawTick (Rectangle area, int item)
		{
			Rectangle tick = TickBounds (item);
			Pango.Layout layout = null;

			if (item < tick_layouts.Length) {
				layout = tick_layouts [item];

				if (layout != null) {
					int width, height;
					layout.GetPixelSize (out width, out height);

					Style.PaintLayout (Style, GdkWindow, State, true, area, this,
						   "GroupSelector:Tick", tick.X + 3, tick.Y + tick.Height, layout);
				}
			}

			if (layout == null)
				tick.Height /= 2;

			if (tick.Intersect (area, out area)) {
				GdkWindow.DrawRectangle (Style.ForegroundGC (State), true, area);
			}
		}

		protected override bool OnPopupMenu ()
		{
			DrawOrderMenu (null);
			return true;
		}

		bool DrawOrderMenu (Gdk.EventButton args)
		{
			Gtk.Menu order_menu = new Gtk.Menu();

			order_menu.Append (App.Instance.Organizer.ReverseOrderAction.CreateMenuItem ());

			GtkUtil.MakeMenuItem (order_menu, Catalog.GetString ("_Clear Date Range"),
						App.Instance.Organizer.HandleClearDateRange);

			if (args != null)
				order_menu.Popup (null, null, null, args.Button, args.Time);
			else
				order_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);

			return true;
		}

		public abstract class Manipulator
		{
			protected GroupSelector selector;
			protected DelayedOperation timer;
			public bool Dragging;
			public bool UpdateGlass;
			public bool GlassUpdating;
			public Point DragStart;

			protected Manipulator (GroupSelector selector)
			{
				this.selector = selector;
				timer = new DelayedOperation (50, new GLib.IdleHandler (DragTimeout));
			}

			protected int drag_offset;
			public int DragOffset {
				set {
					Rectangle then = Bounds ();
					drag_offset = value;
					Rectangle now = Bounds ();

					if (selector.Visible) {
						selector.GdkWindow.InvalidateRect (then, false);
						selector.GdkWindow.InvalidateRect (now, false);
					}
				}
				get {
					return Dragging ? drag_offset : 0;
				}
			}

			public virtual void StartDrag (double x, double y, uint time)
			{
				Rectangle bounds = Bounds ();

				State = StateType.Active;
				//timer.Start ();
				Dragging = true;
				DragStart.X = (int)x - (bounds.Width / 2);
				DragStart.Y = (int)y;
				DragOffset = 0;  // InvalidateRect
			}

			bool DragTimeout ()
			{
				int x, y;
				selector.GetPointer (out x, out y);
				x += selector.Allocation.X;
				y += selector.Allocation.Y;
				UpdateDrag ((double) x, (double) y);

				return true;
			}

			protected bool PositionValid (int pos)
			{
				return pos >= 0 && pos <= selector.box_counts.Length - 1;
			}

			public virtual void UpdateDrag (double x, double y)
			{
				Rectangle bounds = Bounds ();
				double drag_lower_limit = (selector.background.Left) - (bounds.Width/2);
				double drag_upper_limit = (selector.background.Right) - (bounds.Width/2);
				double calX = x - (bounds.Width / 2);

				if (calX >= drag_lower_limit && calX <= drag_upper_limit) {
					if (selector.right_delay.IsPending)
						selector.right_delay.Stop();

					if (selector.left_delay.IsPending)
						selector.left_delay.Stop();

					DragOffset = (int)calX - DragStart.X;
				} else if (calX >= drag_upper_limit && selector.right.Sensitive && !selector.right_delay.IsPending) {
					// Ensure selector is at the limit
					if (bounds.Left != drag_upper_limit)
						DragOffset = (int)drag_upper_limit - DragStart.X;
					selector.Offset -= 10;
					selector.right_delay.Start();
				} else if (calX <= drag_lower_limit && selector.left.Sensitive && !selector.left_delay.IsPending) {
					// Ensure selector is at the limit
					if (bounds.Left != drag_lower_limit)
						DragOffset = (int)drag_lower_limit - DragStart.X;
					selector.Offset += 10;
					selector.left_delay.Start();
				}
			}

			public virtual void EndDrag (double x, double y)
			{
				timer.Stop ();

				Rectangle box = Bounds ();
				double middle = box.X + (box.Width / 2.0);

				int position;
				DragOffset = 0;
				Dragging = false;
				if (selector.BoxXHit (middle, out position)) {
					this.SetPosition (position);
					State = StateType.Prelight;
				} else {
					State = selector.State;
				}
			}

			StateType state;
			public StateType State {
				get {
					return state;
				}
				set {
					if (state != value) {
						selector.GdkWindow.InvalidateRect (Bounds (), false);
					}
					state = value;
				}
			}

			public void SetPosition (int position)
			{
				SetPosition (position, true);
			}

			public void SetPosition (int position, bool update)
			{
				if (! PositionValid (position))
					return;

				Rectangle then = Bounds ();
				this.Position = position;
				Rectangle now = Bounds ();

				if (selector.Visible) {
					then = now.Union (then);
					selector.GdkWindow.InvalidateRect (then, false);
					//selector.GdkWindow.InvalidateRect (now, false);
				}

				if (update)
					PositionChanged ();
			}

			public int Position { get; private set; }

			public abstract void Draw (Rectangle area);

			public abstract void PositionChanged ();

			public abstract Rectangle Bounds ();

			public virtual bool Contains (double x, double y)
			{
				return Bounds ().Contains ((int)x, (int)y);
			}
		}

		class Glass : Manipulator
		{
			Gtk.Window popup_window;
			Gtk.Label popup_label;
			int drag_position;

			public Glass (GroupSelector selector) : base (selector)
			{
				popup_window = new ToolTipWindow ();
				popup_label = new Gtk.Label (string.Empty);
				popup_label.Show ();
				popup_window.Add (popup_label);
			}

			public int handle_height = 15;
			int Border {
				get {
					return selector.box_spacing * 2;
				}
			}

			void UpdatePopupPosition ()
			{
				int x = 0, y = 0;
				Rectangle bounds = Bounds ();
				Requisition requisition = popup_window.SizeRequest ();
				popup_window.Resize  (requisition.Width, requisition.Height);
				selector.GdkWindow.GetOrigin (out x, out y);
				x += bounds.X + (bounds.Width - requisition.Width) / 2;
				y += bounds.Y - requisition.Height;
				x = Math.Max (x, 0);
				x = Math.Min (x, selector.Screen.Width - requisition.Width);
				popup_window.Move (x, y);
			}

			public void MaintainPosition()
			{
				Rectangle box = Bounds ();
				double middle = box.X + (box.Width / 2.0);
				int current_position;

				if (selector.BoxXHit (middle, out current_position)) {
					if (current_position != drag_position)
						popup_label.Text = selector.Adaptor.GlassLabel (current_position);
					drag_position = current_position;
				}
				UpdatePopupPosition ();
				selector.ScrollTo (drag_position);
			}

			public override void StartDrag (double x, double y, uint time)
			{
				if (!PositionValid (Position))
					return;

				base.StartDrag (x, y, time);
				popup_label.Text = selector.Adaptor.GlassLabel (this.Position);
				popup_window.Show ();
				UpdatePopupPosition ();
				drag_position = this.Position;
			}

			public override void UpdateDrag (double x, double y)
			{
				base.UpdateDrag (x, y);
				MaintainPosition();
			}

			public override void EndDrag (double x, double y)
			{
				timer.Stop ();

				Rectangle box = Bounds ();
				double middle = box.X + (box.Width / 2.0);

				int position;
				DragOffset = 0;
				Dragging = false;

				selector.BoxXHitFilled (middle, out position);
				UpdateGlass = true;
				this.SetPosition (position);
				UpdateGlass = false;
				State = StateType.Prelight;
				popup_window.Hide ();
			}

			Rectangle InnerBounds ()
			{
				Rectangle box = new Box (selector, Position).Bounds;
				if (Dragging) {
					box.X = DragStart.X + DragOffset + 3;
					// TODO: find out why we need to add 3 to X to set it
					// to middle of mouse cursor while dragging
				} else {
					box.X += DragOffset;
				}
				return box;
			}

			public override Rectangle Bounds ()
			{
				Rectangle box = InnerBounds ();

				box.Inflate  (Border, Border);
				box.Height += handle_height;

				return box;
			}

			public override void Draw (Rectangle area)
			{
				if (! PositionValid (Position))
					return;

				Rectangle inner = InnerBounds ();
				Rectangle bounds = Bounds ();

				if (! bounds.Intersect (area, out area))
					return;

				selector.Style.BackgroundGC (State).ClipRectangle = area;

				int i = 0;

				Rectangle box = inner;
				box.Width -= 1;
				box.Height -= 1;
				while (i < Border) {
					box.Inflate (1, 1);
					selector.GdkWindow.DrawRectangle (selector.Style.BackgroundGC (State),
					                                  false, box);
					i++;
				}

				Style.PaintFlatBox (selector.Style, selector.GdkWindow, State, ShadowType.In,
				                    area, selector, "glass", bounds.X, inner.Y + inner.Height + Border,
				                    bounds.Width, handle_height);

				Style.PaintHandle (selector.Style, selector.GdkWindow, State, ShadowType.In,
				                   area, selector, "glass", bounds.X, inner.Y + inner.Height + Border,
				                   bounds.Width, handle_height, Orientation.Horizontal);

				Style.PaintShadow (selector.Style, selector.GdkWindow, State, ShadowType.Out,
				                   area, selector, null, bounds.X, bounds.Y, bounds.Width, bounds.Height);

				Style.PaintShadow (selector.Style, selector.GdkWindow, State, ShadowType.In,
				                   area, selector, null, inner.X, inner.Y, inner.Width, inner.Height);

			}

			public override void PositionChanged ()
			{
				GlassUpdating = true;
				if (Dragging || UpdateGlass)
					selector.adaptor.SetGlass (Position);
				selector.ScrollTo (Position);
				GlassUpdating = false;
			}


		}

		public class Limit : Manipulator
		{
			int width = 10;
			int handle_height = 10;

			public enum LimitType {
				Min,
				Max
			}

			LimitType limit_type;

			public override Rectangle Bounds ()
			{
				int limit_offset = limit_type == LimitType.Max ? 1 : 0;

				Rectangle bounds = new Rectangle (0, 0, width, selector.background.Height + handle_height);

				if (Dragging) {
					bounds.X = DragStart.X + DragOffset;
				} else {
					bounds.X = DragOffset + selector.BoxX (Position + limit_offset) - bounds.Width /2;
				}
				bounds.Y = selector.background.Y - handle_height/2;
				return bounds;
			}

			public override void Draw (Rectangle area)
			{
				Rectangle bounds = Bounds ();
				Rectangle top = new Rectangle (bounds.X,
				                               bounds.Y,
				                               bounds.Width,
				                               handle_height);

				Rectangle bottom = new Rectangle (bounds.X,
				                                  bounds.Y + bounds.Height - handle_height,
				                                  bounds.Width,
				                                  handle_height);
				Style.PaintBox (selector.Style, selector.GdkWindow, State, ShadowType.Out, area,
				                selector, null, top.X, top.Y, top.Width, top.Height);

				Style.PaintBox (selector.Style, selector.GdkWindow, State, ShadowType.Out, area,
				                selector, null, bottom.X, bottom.Y, bottom.Width, bottom.Height);
			}

			public Limit (GroupSelector selector, LimitType type) : base (selector)
			{
				limit_type = type;
			}

			public override void PositionChanged ()
			{
				selector.UpdateLimits ();
			}
		}

		protected override void OnMapped ()
		{
			base.OnMapped ();
			if (event_window != null)
				event_window.Show ();
		}

		protected override void OnUnmapped ()
		{
			base.OnUnmapped ();
			if (event_window != null)
				event_window.Hide ();

		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Rectangle area;
			//Console.WriteLine ("expose {0}", args.Area);
			foreach (Rectangle sub in args.Region.GetRectangles ()) {
				if (sub.Intersect (background, out area)) {
					Rectangle active = background;
					int min_x = BoxX (min_limit.Position);
					int max_x = BoxX (max_limit.Position + 1);
					active.X = min_x;
					active.Width = max_x - min_x;

					// white background for entire widget
					if (active.Intersect (area, out active))
						GdkWindow.DrawRectangle (Style.BaseGC (State), true, active);

					// draw bars indicating photo counts
					int i;
					BoxXHit (area.X, out i);
					int end;
					BoxXHit (area.X + area.Width, out end);
					while (i <= end)
						DrawBox (area, i++);
				}

				Style.PaintShadow (this.Style, GdkWindow, State, ShadowType.In, area,
						   this, null, background.X, background.Y,
						   background.Width, background.Height);

				//	draw ticks and legend
				if (sub.Intersect (legend, out area)) {
					int i = 0;
					while (i < box_counts.Length)
						DrawTick (area, i++);
				}

				//  draw limit markers and glass if needed (drawing done inside confines of action_area)
				if(sub.Intersect(action_area, out area))
				{
					//	draw limits markers
					if (has_limits) {
						if (min_limit != null) {
							min_limit.Draw (area);
						}

						if (max_limit != null) {
							max_limit.Draw (area);
						}
					}

					//	draw glass
					if (glass != null) {
						glass.Draw (area);
					}
				}
			}
			return base.OnExposeEvent (args);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			left.SizeRequest ();
			right.SizeRequest ();
			requisition.Width = 500;
			requisition.Height = (int) (LegendHeight () + glass.handle_height + 3 * border);
		}

		// FIXME I can't find a c# wrapper for the C PANGO_PIXELS () macro
		// So this Function is for that.
		public static int PangoPixels (int val)
		{
			return val >= 0 ? (val + 1024 / 2) / 1024 :
				(val - 1024 / 2) / 1024;
		}

		int LegendHeight ()
		{
			int max_height = 0;

			Pango.FontMetrics metrics = this.PangoContext.GetMetrics (this.Style.FontDescription,
										  Pango.Language.FromString ("en_US"));
			max_height += PangoPixels (metrics.Ascent + metrics.Descent);

			foreach (Pango.Layout l in tick_layouts) {
				if (l != null) {
					int width, height;

					l.GetPixelSize (out width, out height);
					max_height = Math.Max (height, max_height);
				}
			}

			return (int) (max_height * 1.5);
		}

		bool HandleScrollRight ()
		{
			if (glass.Dragging)
				glass.MaintainPosition ();

			Offset -= 10;
			return true;
		}

		bool HandleScrollLeft ()
		{
			if (glass.Dragging)
				glass.MaintainPosition ();

			Offset += 10;
			return true;
		}

		void HandleLeftPressed (object sender, System.EventArgs ars)
		{
			HandleScrollLeft ();
			left_delay.Start ();
		}

		void HandleRightPressed (object sender, System.EventArgs ars)
		{
			HandleScrollRight ();
			right_delay.Start ();
		}

		[GLib.ConnectBefore]
		void HandleScrollReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			right_delay.Stop ();
			left_delay.Stop ();
		}

		void SetMouseActionArea ()
		{
			Rectangle glass_bounds = glass.Bounds ();
			action_area = background;
			// expand action area to include bounds area
			action_area.Y = glass_bounds.Y;
			action_area.Height = glass_bounds.Height;

			//	resize event window to match new action area
			if (event_window != null)
				event_window.MoveResize (action_area.X, action_area.Y, action_area.Width, action_area.Height);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			base.OnSizeAllocated (alloc);
			int legend_height = LegendHeight ();

			Gdk.Rectangle bar = new Rectangle (alloc.X + border, alloc.Y + border,
							   alloc.Width - 2 *  border,
							   alloc.Height - 2 * border - glass.handle_height);


			if (left.Allocation.Y != bar.Y || left.Allocation.X != bar.X) {
				left.SetSizeRequest (-1, bar.Height);
				this.Move (left, bar.X - Allocation.X, bar.Y - Allocation.Y);
			}

			if (right.Allocation.Y != bar.Y || right.Allocation.X != bar.X + bar.Width - right.Allocation.Width) {
				right.SetSizeRequest (-1, bar.Height);
				this.Move (right,  bar.X - Allocation.X + bar.Width - right.Allocation.Width,
				           bar.Y - Allocation.Y);
			}

			background = new Rectangle (bar.X + left.Allocation.Width, bar.Y,
			                            bar.Width - left.Allocation.Width - right.Allocation.Width,
			                            bar.Height);

			legend = new Rectangle (background.X, background.Y,
			                        background.Width, legend_height);

			SetMouseActionArea ();

			this.Offset = this.Offset;

			UpdateButtons ();
		}

		public void ResetLimits ()
		{
			min_limit.SetPosition(0,false);
			max_limit.SetPosition(adaptor.Count () - 1, false);
		}

		public void SetLimitsToDates(DateTime start, DateTime stop)
		{
			if (((TimeAdaptor)adaptor).OrderAscending) {
				min_limit.SetPosition(((TimeAdaptor)adaptor).IndexFromDate(start),false);
				max_limit.SetPosition(((TimeAdaptor)adaptor).IndexFromDate(stop),false);
			} else {
				min_limit.SetPosition(((TimeAdaptor)adaptor).IndexFromDate(stop),false);
				max_limit.SetPosition(((TimeAdaptor)adaptor).IndexFromDate(start),false);
			}
		}

		public GroupSelector () : base ()
		{
			SetFlag (WidgetFlags.NoWindow);

			background = Rectangle.Zero;
			glass = new Glass (this);
			min_limit = new Limit (this, Limit.LimitType.Min);
			max_limit = new Limit (this, Limit.LimitType.Max);

			left = new Gtk.Button ();
			//left.Add (new Gtk.Image (Gtk.Stock.GoBack, Gtk.IconSize.Button));
			left.Add (new Gtk.Arrow (Gtk.ArrowType.Left, Gtk.ShadowType.None));
			left.Relief = Gtk.ReliefStyle.None;
			//left.Clicked += HandleScrollLeft;
			left.Pressed += HandleLeftPressed;
			left.ButtonReleaseEvent += HandleScrollReleaseEvent;
			left_delay = new DelayedOperation (50, new GLib.IdleHandler (HandleScrollLeft));

			right = new Gtk.Button ();
			//right.Add (new Gtk.Image (Gtk.Stock.GoForward, Gtk.IconSize.Button));
			right.Add (new Gtk.Arrow (Gtk.ArrowType.Right, Gtk.ShadowType.None));
			right.Relief = Gtk.ReliefStyle.None;
			right.Pressed += HandleRightPressed;
			right.ButtonReleaseEvent += HandleScrollReleaseEvent;
			right_delay = new DelayedOperation (50, new GLib.IdleHandler (HandleScrollRight));
			//right.Clicked += HandleScrollRight;

			this.Put (left, 0, 0);
			this.Put (right, 100, 0);
			left.Show ();
			right.Show ();

			CanFocus = true;

			Mode = RangeType.Min;
			UpdateButtons ();
		}

		public GroupSelector (IntPtr raw) : base (raw) {}
	}
}

using System;
using Gtk;
using Gdk;
using GLib;


namespace FSpot {
	public class GroupSelector : Fixed {
		internal static GType groupSelectorGType;

		int border = 6;
		int box_spacing = 2;
		int box_top_padding = 6;
		public static int MIN_BOX_WIDTH = 20;

		private Glass glass;
		private Limit min_limit;
		private Limit max_limit;

		private Gtk.Button left;
		private Gtk.Button right;
		private Delay left_delay;
		private Delay right_delay;

		private Gdk.Window event_window;

		public Gdk.Rectangle background;
		public Gdk.Rectangle legend;
		public Gdk.Rectangle action;

		int    box_count_max;
		int [] box_counts = new int [0];
		Pango.Layout [] tick_layouts;
		bool   has_limits;
		
		protected FSpot.GroupAdaptor adaptor;
		public FSpot.GroupAdaptor Adaptor {
			set {
				if (adaptor != null)
					adaptor.Changed -= HandleAdaptorChanged;

				adaptor = value;
				has_limits = adaptor is FSpot.ILimitable;				

				if (has_limits) {
				    min_limit.SetPosition (0, false);
				    max_limit.SetPosition (adaptor.Count () - 1, false);
				}

				adaptor.Changed += HandleAdaptorChanged;
				HandleAdaptorChanged (adaptor);
			}
			get {
				return adaptor;
			}
		}

		private void HandleAdaptorChanged (GroupAdaptor adaptor)
		{
			bool size_changed = box_counts.Length != adaptor.Count ();
			int [] box_values = new int [adaptor.Count ()];
			
			if (tick_layouts != null) {
				foreach (Pango.Layout l in tick_layouts) {
					if (l != null)
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
			this.QueueDraw ();
		}
		
		private int [] Counts {
			set {
				box_count_max = 0;
				foreach (int count in value)
					box_count_max = Math.Max (count, box_count_max);
				
				if (value != null)
					box_counts = value;
				else
					value = new int [0];
			}
		}

		public enum RangeType {
			All,
			Fixed,
			Min
		}
			
		private RangeType mode = RangeType.Min;
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

		private int scroll_offset;
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
		
		private static Gdk.Rectangle Expand (Gdk.Rectangle src, int width)
		{
			src.X -= width;
			src.Y -= width;
			src.Width += width * 2;
			src.Height += width * 2;
			return src;
		}

		private void UpdateButtons () {
			left.Sensitive = (scroll_offset < 0);
			right.Sensitive = (box_counts.Length * BoxWidth > background.Width - scroll_offset);

			if (!left.Sensitive && left_delay.IsPending) 
				left_delay.Stop ();

			if (!right.Sensitive && right_delay.IsPending)
				right_delay.Stop ();
		}

		static bool IsInside (Rectangle bounds, double x, double y) 
		{
			if (x >= bounds.X && 
			    x < bounds.X + bounds.Width && 
			    y >= bounds.Y &&
			    y < bounds.Y + bounds.Height)
				return true;

			return false;
		}

		private void BoxXHitFilled (double x, out int out_position)
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

		private bool BoxXHit (double x, out int position)
		{
			x -= BoxX (0);
			position = (int) (x / BoxWidth);
			if (position < 0) { 
				position = 0;
				return false;
			} else if (position >= box_counts.Length) {
				position = box_counts.Length -1;
				return false;
			}
			return true;
		}

		private bool BoxHit (double x, double y, out int position) 
		{
			if (BoxXHit (x, out position)) {
				if (IsInside (BoxBounds (position), x, y))
					return true;

				position++;
			}
			return false;
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton args)
		{
			double x = args.X + action.X;
			double y = args.Y + action.Y;

			if (glass.IsInside (x, y)) {
				glass.StartDrag (x, y, args.Time);
			} else if (has_limits && min_limit.IsInside (x, y)) {
				min_limit.StartDrag (x, y, args.Time);
			} else if (has_limits && max_limit.IsInside (x, y)) {
				max_limit.StartDrag (x, y, args.Time);
			} else {
				int position;
				if (BoxHit (x, y, out position)) {
					BoxXHitFilled (x, out position);
					glass.SetPosition (position);
					return true;
				}
			}
			
			return base.OnButtonPressEvent (args);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton args) 
		{
			double x = args.X + action.X;
			double y = args.Y + action.Y;

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
			double x = args.X + action.X;
			double y = args.Y + action.Y;

			//Rectangle box = glass.Bounds ();
			//Console.WriteLine ("please {0} and {1} in box {2}", x, y, box);

			if (glass.Dragging) {
				glass.UpdateDrag (x, y);
			} else if (min_limit.Dragging) {
				min_limit.UpdateDrag (x, y);
			} else if (max_limit.Dragging) {
				max_limit.UpdateDrag (x, y);
			} else {
				glass.State = glass.IsInside (x, y) ? StateType.Prelight : StateType.Normal;
				min_limit.State = min_limit.IsInside (x, y) ? StateType.Prelight : StateType.Normal;
				max_limit.State = max_limit.IsInside (x, y) ? StateType.Prelight : StateType.Normal;
			}

			return base.OnMotionNotifyEvent (args);
		}

		protected override void OnRealized ()
		{
			Flags |= (int)WidgetFlags.Realized;
			GdkWindow = ParentWindow;

			base.OnRealized ();
			
			WindowAttr attr = WindowAttr.Zero;
			attr.WindowType = Gdk.WindowType.Child;
			
			

			attr.X = action.X;
			attr.Y = action.Y;
			attr.Width = action.Width;
			attr.Height = action.Height;
			attr.Wclass = WindowClass.InputOnly;
			attr.EventMask = (int) Events;
			attr.EventMask |= (int) (EventMask.ButtonPressMask | 
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
		
		private Double BoxWidth {
			get {
				switch (mode) {
				case RangeType.All:
					return Math.Max (1.0, background.Width / (double) box_counts.Length);
				case RangeType.Fixed:
					return background.Width / (double) 12;
				case RangeType.Min:
					return Math.Max (MIN_BOX_WIDTH, background.Width / (double) box_counts.Length);
				default:
					return (double) MIN_BOX_WIDTH;
				}
			}
		}

		private int BoxX (int item) 
		{
			 return scroll_offset + background.X + (int) Math.Round (BoxWidth * item);
		}
		
		public Rectangle BoxBounds (int item) 
		{
			Rectangle box = Rectangle.Zero;
			box.Height = background.Height;
			box.Y = background.Y;
			
			box.X = BoxX (item);
			box.Width = Math.Max (BoxX (item + 1) - box.X, 1);

			return box;
		}

	        public Rectangle BoxBarBounds (int item)
		{
			int total_height = background.Height;
			double percent = box_counts [item] / (double) Math.Max (box_count_max, 1);

			Rectangle box = Rectangle.Zero;
			box.Height = (int) Math.Round ((total_height - box_top_padding) * percent + 0.5);

			box.Y = background.Y + total_height - box.Height - 1;
			
			box.X = BoxX (item);
			box.Width = Math.Max (BoxX (item + 1) - box.X, 1);

			return box;
		}

		private void DrawBox (Rectangle area, int item) 
		{
			Rectangle box = BoxBarBounds (item);
			
			box.X += box_spacing;
			box.Width -= box_spacing * 2;
			
			if (box.Intersect (area, out area)) {
				if (item < min_limit.Position || item > max_limit.Position) {
#if false
					box.Height += 1;

					//GdkWindow.DrawRectangle (Style.ForegroundGC (StateType.Normal), false, box);
					Style.PaintShadow (this.Style, GdkWindow, State, ShadowType.In, area, 
							   this, null, box.X, box.Y, 
							   box.Width, box.Height);
#else
					GdkWindow.DrawRectangle (Style.BackgroundGC (StateType.Active), true, area);
#endif
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
						   "GroupSelector:Tick", tick.X, tick.Y + tick.Height, layout); 
				}
			}

			if (layout == null)
				tick.Height /= 2;

			if (tick.Intersect (area, out area)) {
				GdkWindow.DrawRectangle (Style.ForegroundGC (State), true, area);
			}
		}

		public class Manipulator {
			protected GroupSelector selector;
			public bool Dragging;

			public Point DragStart;

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
					if (Dragging)
						return drag_offset;
					else
						return 0;
				}
			}					

			public virtual void StartDrag (double x, double y, uint time) 
			{
				State = StateType.Active;
				Dragging = true;
				DragStart.X = (int)x;
				DragStart.Y = (int)y;	
			}

			public virtual void UpdateDrag (double x, double y)
			{
				DragOffset = (int)x - DragStart.X;
			}

			public virtual void EndDrag (double x, double y)
			{
				Rectangle box = Bounds ();
				double middle = box.X + (box.Width / 2.0);

				int position;
				DragOffset = 0;
				if (selector.BoxXHit (middle, out position)) {
					this.SetPosition (position);
					State = StateType.Prelight;
				} else {
					State = selector.State;
				}
				Dragging = false;				
			}

			private StateType state;
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
				Rectangle then = Bounds ();
				this.position = position;
				Rectangle now = Bounds ();
				
				if (selector.Visible) {
					then = now.Union (then);
					selector.GdkWindow.InvalidateRect (then, false);
					//selector.GdkWindow.InvalidateRect (now, false);
				}

				if (update)
					PositionChanged ();
			}

			private int position;
			public int Position {
				get {
					return position;
				}
			}

			public virtual void Draw (Rectangle area)
			{
				Console.WriteLine ("implement me Draw ({0})", area);
			}
			
			public virtual void PositionChanged ()
			{
				throw new Exception ("Unimplemented");
			}

			public virtual Rectangle Bounds () 
			{
				Console.WriteLine ("implement me Bounds ()");
				return Rectangle.Zero;
			}

			public virtual bool IsInside (double x, double y)
			{
				return GroupSelector.IsInside (Bounds (), x, y);
			}

			public Manipulator (GroupSelector selector) 
			{
				this.selector = selector;
			}
		}
		
		private class Glass : Manipulator {
			Gtk.Window popup_widow;
			Gtk.Label popup_label;
			int drag_position;

			private int handle_height = 15;

			private int border {
				get {
					return selector.box_spacing * 2;
				}
			}
			
			private void UpdatePopupPosition ()
			{
				int x = 0, y = 0;				
				Rectangle bounds = Bounds ();
				Requisition requisition = popup_widow.SizeRequest ();
				popup_widow.Resize  (requisition.Width, requisition.Height);
				selector.GdkWindow.GetOrigin (out x, out y);
				x += bounds.X + (bounds.Width - requisition.Width) / 2;
				y += bounds.Y - requisition.Height;
				x = Math.Max (x, 0);
				x = Math.Min (x, selector.Screen.Width - requisition.Width);
				popup_widow.Move (x, y);
			}

			public override void StartDrag (double x, double y, uint time)
			{
				base.StartDrag (x, y, time);
				popup_label.Text = selector.Adaptor.GlassLabel (this.Position);
				popup_widow.Show ();
				UpdatePopupPosition ();
				drag_position = this.Position;
			}
			
			public override void UpdateDrag (double x, double y)
			{
				Rectangle box = Bounds ();
				double middle = box.X + (box.Width / 2.0);
				int current_position;
				
				base.UpdateDrag (x, y);				
				if (selector.BoxXHit (middle, out current_position)) {
					if (current_position != drag_position)
						popup_label.Text = selector.Adaptor.GlassLabel (current_position);
					drag_position = current_position;
				}
				UpdatePopupPosition ();
			}

			public override void EndDrag (double x, double y)
			{
				Rectangle box = Bounds ();
				double middle = box.X + (box.Width / 2.0);

				int position;
				DragOffset = 0;
				
				selector.BoxXHitFilled (middle, out position);
				this.SetPosition (position);
				State = StateType.Prelight;
				Dragging = false;
				popup_widow.Hide ();
			}

			private Rectangle InnerBounds ()
			{
				Rectangle box = selector.BoxBounds (Position);
				box.X += DragOffset;
				return box;
			}
			
			public override Rectangle Bounds () 
			{
				Rectangle box = InnerBounds ();

				box = Expand (box, border);
				box.Height += handle_height;
				
				return box;
			}

			public override void Draw (Rectangle area)
			{
				Rectangle inner = InnerBounds ();
				Rectangle bounds = Bounds ();
				
				if (bounds.Intersect (area, out area)) {
					int i = 0;

					Rectangle box = inner;
					box.Width -= 1;
					box.Height -= 1;
					while (i < border) {
						box = Expand (box, 1);
						
						selector.Style.BackgroundGC (State).ClipRectangle = area;
						selector.GdkWindow.DrawRectangle (selector.Style.BackgroundGC (State), 
										  false, box);
						i++;
					}

					Style.PaintHandle (selector.Style, selector.GdkWindow, State, ShadowType.In, 
							    area, selector, "glass", bounds.X, inner.Y + inner.Height + border, 
							    bounds.Width, handle_height, Orientation.Horizontal);
					
					Style.PaintShadow (selector.Style, selector.GdkWindow, State, ShadowType.Out, 
							   area, selector, "glass", bounds.X, bounds.Y, bounds.Width, bounds.Height);

					Style.PaintShadow (selector.Style, selector.GdkWindow, State, ShadowType.In, 
							   area, selector, "glass", inner.X, inner.Y, inner.Width, inner.Height);

				}
			}
			
			public override void PositionChanged ()
			{
				selector.adaptor.SetGlass (Position);
			}
			
			public Glass (GroupSelector selector) : base (selector) {
				popup_widow = new Gtk.Window (Gtk.WindowType.Popup);
				popup_label = new Gtk.Label ("");
				popup_label.Show ();
				popup_widow.Add (popup_label);
			}
		}

		public class Limit : Manipulator {
			int width = 10;
			int handle_height = 10;

			public enum LimitType {
				Min,
				Max
			}

			private LimitType limit_type;
			
			public override Rectangle Bounds () 
			{
				int limit_offset = limit_type == LimitType.Max ? 1 : 0;

				Rectangle bounds = new Rectangle (0, 0, width, selector.background.Height + handle_height);
				bounds.X = DragOffset + selector.BoxX (Position + limit_offset) - bounds.Width /2;
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
#if false

				Gdk.Point [] top_points = { new Gdk.Point (top.X, top.Y),
							    new Gdk.Point (top.X, top.Y + top.Height),
							    new Gdk.Point ((int)(top.X + top.Width/2), (int)(top.Y + top.Height * 1.5)),
							    new Gdk.Point (top.X + top.Width, top.Y + top.Height),
							    new Gdk.Point (top.X + top.Width, top.Y),
							    new Gdk.Point (top.X, top.Y) };
				Style.PaintPolygon (selector.Style, selector.GdkWindow, State, ShadowType.In, area,
						    selector, "IconView:Selector", top_points, true);

				Gdk.Point [] bottom_points = { new Gdk.Point (bottom.X, bottom.Y + bottom.Height),
							       new Gdk.Point (bottom.X, bottom.Y),
							       new Gdk.Point ((int)(bottom.X + bottom.Width/2), (int)(bottom.Y - bottom.Height/2)),
							       new Gdk.Point (bottom.X + bottom.Width, bottom.Y),
							       new Gdk.Point (bottom.X + bottom.Width, bottom.Y + bottom.Height),
							       new Gdk.Point (bottom.X, bottom.Y + bottom.Height) };
				Style.PaintPolygon (selector.Style, selector.GdkWindow, State, ShadowType.In, area,
						    selector, "IconView:Selector", bottom_points, true);
#else
				Style.PaintBox (selector.Style, selector.GdkWindow, State, ShadowType.Out, area,
						selector, "limit", top.X, top.Y, top.Width, top.Height);

				Style.PaintBox (selector.Style, selector.GdkWindow, State, ShadowType.Out, area,
						selector, "limit", bottom.X, bottom.Y, bottom.Width, bottom.Height);
#endif
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
					
					if (active.Intersect (area, out active)) {
						GdkWindow.DrawRectangle (Style.BaseGC (State), true, active);
					}
					
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
				
				if (sub.Intersect (legend, out area)) {
					int i = 0;
					
					while (i < box_counts.Length)
						DrawTick (area, i++);
				}
				
				if (has_limits) {
					if (min_limit != null) {
						min_limit.Draw (sub);
					}
					
					if (max_limit != null) {
						max_limit.Draw (sub);
					}
				}
				
				if (glass != null) {
					glass.Draw (sub);
				}
			}
			return base.OnExposeEvent (args);
		}
			
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = 500;
			requisition.Height = 70;
			base.OnSizeRequested (ref requisition);
		}

		// FIXME I can't find a c# wrapper for the C PANGO_PIXELS () macro
		// So this Function is for that.
		public static int PangoPixels (int val)
		{
			return val >= 0 ? (val + 1024 / 2) / 1024 :
				(val - 1024 / 2) / 1024;
		}

		private int LegendHeight ()
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

		private bool HandleScrollRight ()
		{
			Offset -= 10;
			return true;
		}

		private bool HandleScrollLeft ()
		{
			Offset += 10;
			return true;
		}
		
		private void HandleLeftPressed (object sender, System.EventArgs ars)
		{
			HandleScrollLeft ();
			left_delay.Start ();
		}

		private void HandleRightPressed (object sender, System.EventArgs ars)
		{
			HandleScrollRight ();
			right_delay.Start ();
		}

		[GLib.ConnectBefore]
		private void HandleScrollReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			right_delay.Stop ();
			left_delay.Stop ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			base.OnSizeAllocated (alloc);
			int legend_height = LegendHeight ();
	
			Gdk.Rectangle bar = new Rectangle (alloc.X + border, alloc.Y + border,
							   alloc.Width - 2 *  border,
							   alloc.Height - 2 * border - legend_height);
							   
		
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
			
			legend = new Rectangle (background.X, background.Y + background.Height,
						background.Width, legend_height);

			action = background.Union (legend);

			if (event_window != null)
				event_window.MoveResize (action.X, action.Y, action.Width, action.Height);
			
			this.Offset = this.Offset;

			UpdateButtons ();
		}

		public GroupSelector () : base () 
		{
			Flags |= (int)WidgetFlags.NoWindow;

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
			left_delay = new Delay (50, new GLib.IdleHandler (HandleScrollLeft));

			right = new Gtk.Button ();
			//right.Add (new Gtk.Image (Gtk.Stock.GoForward, Gtk.IconSize.Button));
			right.Add (new Gtk.Arrow (Gtk.ArrowType.Right, Gtk.ShadowType.None));
			right.Relief = Gtk.ReliefStyle.None;
			right.Pressed += HandleRightPressed;
			right.ButtonReleaseEvent += HandleScrollReleaseEvent;
			right_delay = new Delay (50, new GLib.IdleHandler (HandleScrollRight));
			//right.Clicked += HandleScrollRight;

			this.Put (left, 0, 0);
			this.Put (right, 100, 0);
			left.Show ();
			right.Show ();
			
			Mode = RangeType.Min;
			UpdateButtons ();
		}

		public GroupSelector (IntPtr raw) : base (raw) {}

#if TEST_MAIN
		private void HandleKeyPressEvent (object sender, KeyPressEventArgs args)
		{		
			switch (args.Event.Key) {
			case Gdk.Key.Left:
				if (glass.Position > 0)
					this.glass.Position--;
				else 
					glass.Position = box_counts.Length - 1;

				break;
			case Gdk.Key.Right:
				if (glass.Position < box_counts.Length - 1)
					glass.Position++;
				else
					glass.Position = 0;

				break;
			case Gdk.Key.Down:
				if (Mode > 0)
					Mode--;
				else 
					Mode = 2;

				break;
			case Gdk.Key.Up:
				if (mode < 2)
					Mode++;
				else 
					Mode = 0;
				break;
			case Gdk.Key.Home:
				Offset += 10;
				break;
			case Gdk.Key.End:
				Offset -= 10;
				break;
			}
		}

		public static int Main (string [] args) {
			
			Application.Init ();
			Gtk.Window win = new Gtk.Window ("testing");

			VBox vbox = new VBox (false, 10);
			GroupSelector gs = new GroupSelector ();
			gs.Counts = new int [] {20, 100, 123, 10, 5, 2, 3, 50, 8, 10, 22, 0, 55, 129, 120, 30, 14, 200, 21, 55};
			gs.Mode = RangeType.Fixed;
			vbox.PackStart (gs);

			gs = new GroupSelector ();
			gs.Counts = new int [] {20, 100, 123, 10, 5, 2, 3, 50, 8, 10, 22, 0, 55, 129, 120, 30, 14, 200, 21, 55};
			gs.Mode = RangeType.Fixed;
			vbox.PackStart (gs);

			win.Add (vbox);
			win.ShowAll ();
			win.AddEvents ((int) EventMask.KeyPressMask);
			win.KeyPressEvent += gs.HandleKeyPressEvent;

			Application.Run ();
			return 0;
		}
#endif
	}

}

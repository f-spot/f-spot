using System;
using Gtk;
using Gdk;
using GLib;

namespace FSpot {
	public class GroupSelector : Bin {
		internal static GType groupSelectorGType;
		int border = 16;
		int box_spacing = 2;
		int box_top_padding = 6;
		public static int MIN_BOX_WIDTH = 20;
		private Glass glass;
		private Limit min_limit;
		private Limit max_limit;

		Gdk.Window event_window;

		public Gdk.Rectangle background;
		public Gdk.Rectangle legend;

		int    box_count_max;
		int [] box_counts = new int [0];
		public int [] Counts {
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

		private int mode;
		public int Mode {
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
				if (Visible)
					GdkWindow.InvalidateRect (Allocation, false);
			}
		}

		protected override void OnRealized ()
		{
			Flags |= (int)WidgetFlags.Realized;
			GdkWindow = ParentWindow;

			base.OnRealized ();
		}

		private Double BoxWidth {
			get {
				switch (mode) {
				case 0:
					return background.Width / (double) box_counts.Length;
				case 1:
					return background.Width / (double) 12;
				case 2:
				default:
					return (double) MIN_BOX_WIDTH;
				}
			}
		}

		private int BoxX (int item) {
			 return scroll_offset + background.X + (int) Math.Round (BoxWidth * item);
		}

	        public Rectangle BoxBounds (int item)
		{
			int total_height = background.Height;
			int count = item;
			double percent = box_counts [item] / (double) box_count_max;

			Rectangle box = Rectangle.Zero;
			box.Height = (int) Math.Round ((total_height - box_top_padding) * percent + 0.5);
			box.Y = background.Y + total_height - box.Height - 1;
			
			box.X = BoxX (item);
			box.Width = Math.Max (BoxX (item + 1) - box.X, 1);

			return box;
		}

		private void DrawBox (Rectangle area, int item) 
		{
			Rectangle box = BoxBounds (item);
			
			box.X += box_spacing;
			box.Width -= box_spacing * 2;
			
			if (box.Intersect (area, out area)) {
				if (item < min_limit.Position || item >= max_limit.Position) {
#if false
					box.Height += 1;

					//GdkWindow.DrawRectangle (Style.ForegroundGC (StateType.Normal), false, box);
					Style.PaintShadow (this.Style, GdkWindow, State, ShadowType.In, area, 
							   this, null, box.X, box.Y, 
							   box.Width, box.Height);
#else
					GdkWindow.DrawRectangle (Style.BackgroundGC (StateType.Prelight), true, area);
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
			if (TickBounds (item).Intersect (area, out area)) {
				GdkWindow.DrawRectangle (Style.ForegroundGC (State), true, area);
			}
		}

		private class Glass {
			private int item;
			private int offset;
			private GroupSelector selector;
			private int handle_height = 15;

			private int border {
				get {
					return selector.box_spacing * 2;
				}
			}

			public int Item {
				set {
					Rectangle then = Bounds ();
					item = value;
					Rectangle now = Bounds ();

					selector.GdkWindow.InvalidateRect (then, false);
					selector.GdkWindow.InvalidateRect (now, false);
				}
				get {
					return item;
				}
			}

			public Rectangle InnerBounds () 
			{
				Rectangle box = selector.BoxBounds (item);

				//box.X += border;
				box.Y = selector.background.Y;
				box.Height = selector.background.Height;
				
				return box;
			}
			
			public Rectangle Bounds () 
			{
				Rectangle box = InnerBounds ();

				box.X -= border;
				box.Y -= border;
				box.Width += 2 * border;
				box.Height += 2 * border + handle_height;
				
				return box;
			}

			public void Draw (Rectangle area)
			{
				Rectangle inner = InnerBounds ();
				Rectangle bounds = Bounds ();
				
				if (bounds.Intersect (area, out area)) {
					
					
					int i = 0;
					Rectangle box = inner;
					box.Width -= 1;
					box.Height -= 1;
					while (i < border) {
						box.X -= 1;
						box.Y -= 1;
						box.Width += 2;
						box.Height += 2;
					
						selector.GdkWindow.DrawRectangle (selector.Style.BackgroundGC (selector.State), 
										  false, box);
						i++;
					}
				
					Style.PaintHandle (selector.Style, selector.GdkWindow, selector.State, ShadowType.In, 
							    area, selector, "glass", bounds.X, inner.Y + inner. Height + border, 
							    bounds.Width, handle_height, Orientation.Horizontal);

					Style.PaintShadow (selector.Style, selector.GdkWindow, selector.State, ShadowType.Out, 
							   area, selector, null, bounds.X, bounds.Y, bounds.Width, bounds.Height);

					Style.PaintShadow (selector.Style, selector.GdkWindow, selector.State, ShadowType.In, 
							   area, selector, null, inner.X, inner.Y, inner.Width, inner.Height);

				}
			}
			
			public Glass (GroupSelector selector) {
				this.selector = selector;
			}
		}

		public class Limit {
			GroupSelector selector;
			private int position;
			int width = 10;
			int handle_height = 10;

			public int Position {
				get {
					return position;
				}
				set {
					Rectangle then = Bounds ();
					position = value;
					Rectangle now = Bounds ();
					
					if (selector.Visible) {
						selector.GdkWindow.InvalidateRect (then, false);
						selector.GdkWindow.InvalidateRect (now, false);
					}
				}
			}

			public Rectangle Bounds () 
			{
				Rectangle bounds = new Rectangle (0, 0, width, selector.background.Height + handle_height);
				bounds.X = selector.BoxX (position) - bounds.Width /2;
				bounds.Y = selector.background.Y - handle_height/2;
				return bounds;
			}

			public void Draw (Rectangle Area) 
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
				selector.GdkWindow.DrawRectangle (selector.Style.TextGC (selector.State), true, top);
				selector.GdkWindow.DrawRectangle (selector.Style.TextGC (selector.State), true, bottom);
			}

			public Limit (GroupSelector selector) 
			{
				this.selector = selector;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Rectangle area; 
			//Console.WriteLine ("expose {0}", args.Area);
			

			if (args.Area.Intersect (background, out area)) {							
				Rectangle active = background;
				int min_x = BoxX (min_limit.Position);
				int max_x = BoxX (max_limit.Position);
				active.X = min_x;
				active.Width = max_x - min_x;

				if (active.Intersect (area, out active)) {
					GdkWindow.DrawRectangle (Style.BaseGC (State), true, active);
				}

				int i = 0;
				while (i < box_counts.Length)
					DrawBox (area, i++);
			}

			Style.PaintShadow (this.Style, GdkWindow, State, ShadowType.In, area, 
					   this, null, background.X, background.Y, 
					   background.Width, background.Height);

			if (args.Area.Intersect (legend, out area)) {
				int i = 0;
				while (i <= box_counts.Length)
					DrawTick (area, i++);
			}
			
			if (min_limit != null) {
				min_limit.Draw (args.Area);
			}
			
			if (max_limit != null) {
				max_limit.Draw (args.Area);
			}
			       
			if (glass != null) {
				glass.Draw (args.Area);
			}

			return base.OnExposeEvent (args);
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			base.OnSizeAllocated (alloc);
			int legend_height = 20;

			background = new Rectangle (border, border, 
						    alloc.Width - 2* border,
						    alloc.Height - 2 * border - legend_height);

			legend = new Rectangle (border, background.Y + background.Height,
						background.Width, legend_height);
		}

		public GroupSelector () : base () 
		{
			Console.WriteLine ("this is a test");

			Flags |= (int)WidgetFlags.NoWindow;

			background = Rectangle.Zero;
			glass = new Glass (this);
			min_limit = new Limit (this);
			min_limit.Position = 3;
			max_limit = new Limit (this);
			max_limit.Position = 12;
		}

		private void HandleKeyPressEvent (object sender, KeyPressEventArgs args)
		{		
			Console.WriteLine ("press");

			switch (args.Event.Key) {
			case Gdk.Key.Left:
				if (glass.Item > 0)
					this.glass.Item--;
				else 
					glass.Item = box_counts.Length - 1;

				break;
			case Gdk.Key.Right:
				if (glass.Item < box_counts.Length - 1)
					glass.Item++;
				else
					glass.Item = 0;

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

			GroupSelector gs = new GroupSelector ();
			gs.Counts = new int [] {20, 10, 5, 2, 3, 50, 8, 10, 22, 0, 55, 129, 120, 30, 14, 200, 21, 55};
			gs.Mode = 2;

			win.Add (gs);
			win.ShowAll ();
			win.AddEvents ((int) EventMask.KeyPressMask);
			win.KeyPressEvent += gs.HandleKeyPressEvent;

			Application.Run ();
			return 0;
		}
	}

}

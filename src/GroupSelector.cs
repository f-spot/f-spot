using System;
using Gtk;
using Gdk;
using GLib;

namespace FSpot {
	public class GroupSelector : Bin {
		internal static GType groupSelectorGType;
		int border = 16;
		int box_top_padding = 6;
		public static int MIN_BOX_WIDTH = 20;
		private Glass glass;
		private Limit top_limit;
		private Limit bottom_limit;

		Gdk.Window back_window;
		
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
			Style = Style.Attach (GdkWindow);
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
			if (BoxBounds (item).Intersect (area, out area))
				GdkWindow.DrawRectangle (Style.BaseGC (StateType.Selected), true, area);
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
			int thickness = 4;

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

			public Rectangle Bounds () 
			{
				Rectangle box = selector.BoxBounds (item);
				
				box.X -= thickness;
				box.Y = selector.background.Y - thickness;
				box.Width += 2 * thickness;
				box.Height = selector.background.Height + 2 * thickness;
				
				return box;
			}

			public void Draw (Rectangle area)
			{
				Rectangle bounds = Bounds ();
				
				if (bounds.Intersect (area, out area)) {
					
					
					int i = thickness - 1;
					while (i > 0) {
						Rectangle border = bounds;
						border.X += i;
						border.Y += i;
						border.Width -= (2 * i) + 1;
						border.Height -= (2 * i) + 1;
					
						selector.GdkWindow.DrawRectangle (selector.Style.BackgroundGC (selector.State), 
										  false, border);
						i--;
					}
				
					Style.PaintShadow (selector.Style, selector.GdkWindow, selector.State, ShadowType.Out, 
							   area, selector, null, bounds.X, bounds.Y, bounds.Width, bounds.Height);
	

					i = thickness;
					bounds.X += i;
					bounds.Y += i;
					bounds.Width -= 2 * i;
					bounds.Height -= 2 * i;
					
					Style.PaintShadow (selector.Style, selector.GdkWindow, selector.State, ShadowType.In, 
							   area, selector, null, bounds.X, bounds.Y, bounds.Width, bounds.Height);

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
				GdkWindow.DrawRectangle (Style.BaseGC (State), true, area);

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
			
			if (top_limit != null) {
				top_limit.Draw (args.Area);
			}
			
			if (bottom_limit != null) {
				bottom_limit.Draw (args.Area);
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
			top_limit = new Limit (this);
			top_limit.Position = 3;
			bottom_limit = new Limit (this);
			bottom_limit.Position = 8;
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
			gs.Counts = new int [] {20, 10, 5, 2, 3, 5, 8, 10, 22, 0, 55, 129, 300, 30, 14, 200, 21, 55};
			gs.Mode = 2;
			gs.Offset = 3;

			win.Add (gs);
			win.ShowAll ();
			win.AddEvents ((int) EventMask.KeyPressMask);
			win.KeyPressEvent += gs.HandleKeyPressEvent;

			Application.Run ();
			return 0;
		}
	}

}

using System;
using Gtk;
using Gdk;
using GLib;

namespace FSpot {
	public class GroupSelector : Bin {
		internal static GType groupSelectorGType;
		int border = 16;
		public static int MIN_BOX_WIDTH = 20;
		private Glass glass;

		Gdk.Window back_window;
		public Gdk.Rectangle background;

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

		public int scroll_offset;
		private int BoxX (int item) {
			 return scroll_offset + background.X + (int) Math.Round (BoxWidth * item);
		}

	        public Rectangle BoxBounds (int item)
		{
			int total_height = background.Height;
			int count = item;
			double percent = box_counts [item] / (double) box_count_max;

			Rectangle box = Rectangle.Zero;

			box.Height = (int) ((total_height - border) * percent);
			box.Y = background.Y + total_height - box.Height;
			
			box.X = BoxX (item);
			box.Width = BoxX (item + 1) - box.X;

			return box;
		}

		private void DrawBox (Rectangle area, int item) 
		{
			if (BoxBounds (item).Intersect (area, out area))
				GdkWindow.DrawRectangle (Style.TextGC (State), true, area);
		}

		private class Glass {
			private int item;
			private int offset;
			private GroupSelector selector;
			int thickness = 4;

			public int Item {
				set {
					Rectangle old = Bounds ();
					item = value;
					Rectangle now = Bounds ();

					selector.GdkWindow.InvalidateRect (old, false);
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
					Style.PaintShadow (selector.Style, selector.GdkWindow, selector.State, ShadowType.Out, 
							   area, selector, null, bounds.X, bounds.Y, bounds.Width, bounds.Height);
	
				
				int i = thickness -1;
				while (i > 0) {
					Rectangle border = bounds;
					border.X += i;
					border.Y += i;
					border.Width -= 2 * i;
					border.Height -= 2 * i;

					selector.GdkWindow.DrawRectangle (selector.Style.BackgroundGC (selector.State), 
									  false, border);
					i--;
				}
				
				Style.PaintShadow (selector.Style, selector.GdkWindow, selector.State, ShadowType.In, 
						   area, selector, null, bounds.X, bounds.Y, bounds.Width, bounds.Height);

				}
			}
			
			public Glass (GroupSelector selector) {
				this.selector = selector;
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Rectangle area; 
			Console.WriteLine ("expose {0}", args.Area);
			
			if (args.Area.Intersect (background, out area)) {			
				GdkWindow.DrawRectangle (Style.BaseGC (State), true, area);

				int i = 0;
				while (i < box_counts.Length)
					DrawBox (area, i++);
			}
			
			Style.PaintShadow (this.Style, GdkWindow, State, ShadowType.In, area, 
					   this, null, background.X, background.Y, 
					   background.Width, background.Height);

			if (glass != null) {
				glass.Draw (args.Area);
			}

			return base.OnExposeEvent (args);
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			base.OnSizeAllocated (alloc);

			background = new Rectangle (border, border, 
						    alloc.Width - 2* border,
						    alloc.Height - 2 * border);

		}

		public GroupSelector () : base () 
		{
			Console.WriteLine ("this is a test");

			Flags |= (int)WidgetFlags.NoWindow;

			background = Rectangle.Zero;
			glass = new Glass (this);
		}

		private void HandleKeyPressEvent (object sender, KeyPressEventArgs args)
		{		
			Console.WriteLine ("press");

			switch (args.Event.Key) {
			case Gdk.Key.Down:
				if (glass.Item > 0)
					this.glass.Item--;
				else 
					glass.Item = box_counts.Length - 1;

				break;
			case Gdk.Key.Up:
				if (glass.Item < box_counts.Length - 1)
					glass.Item++;
				else
					glass.Item = 0;

				break;
			case Gdk.Key.Left:
				if (Mode > 0)
					Mode--;
				else 
					Mode = 2;

				break;
			case Gdk.Key.Right:
				if (mode < 2)
					Mode++;
				else 
					Mode = 0;
				break;
			}
		}

		public static int Main (string [] args) {
			
			Application.Init ();
			Gtk.Window win = new Gtk.Window ("testing");

			GroupSelector gs = new GroupSelector ();
			gs.Counts = new int [] {20, 10, 5, 2, 3, 5, 8, 10, 22, 0, 55, 129, 300};
			gs.Mode = 2;
			gs.scroll_offset = 3;

			win.Add (gs);
			win.ShowAll ();
			win.AddEvents ((int) EventMask.KeyPressMask);
			win.KeyPressEvent += gs.HandleKeyPressEvent;

			Application.Run ();
			return 0;
		}
	}

}

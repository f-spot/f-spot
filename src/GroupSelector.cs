using System;
using Gtk;
using Gdk;
using GLib;

namespace FSpot {
	public class GroupSelector : Bin {
		internal static GType groupSelectorGType;
		int border = 16;
		public static int MIN_BOX_WIDTH = 20;


		Gdk.Window back_window;
		Gdk.Rectangle background;

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

		public int mode;

		protected override void OnRealized ()
		{
			Flags |= (int)WidgetFlags.Realized;
			GdkWindow = ParentWindow;
			Style = Style.Attach (GdkWindow);
		}

		private Double BoxWidth (int mode)
		{
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

		private Rectangle BoxBounds (int item)
		{
			double total_width = BoxWidth (mode);
			int total_height = background.Height;
			int count = item;
			double percent = box_counts [item] / (double) box_count_max;

			Rectangle box = Rectangle.Zero;

			box.Height = (int) ((total_height - border) * percent);
			box.Y = background.Y + total_height - box.Height;
			
			int start_x = (int) Math.Round (total_width * item);
			int end_x = (int) Math.Round (total_width * (item + 1));
			box.X = background.X + start_x;
			box.Width = end_x - start_x;

			return box;
		}

		private void DrawBox (Gdk.EventExpose args, int item) 
		{
			Rectangle area;
			if (args.Area.Intersect (BoxBounds (item), out area) 
			    && background.Intersect (area, out area)) {	
				GdkWindow.DrawRectangle (Style.TextGC (State), true, area);
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
					DrawBox (args, i++);
			}
			
			Style.PaintShadow (this.Style, GdkWindow, State, ShadowType.In, area, 
					   this, null, background.X, background.Y, 
					   background.Width, background.Height);


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
		}
		
		public static int Main (string [] args) {
			
			Application.Init ();
			Gtk.Window win = new Gtk.Window ("testing");

			GroupSelector gs = new GroupSelector ();
			gs.Counts = new int [] {20, 10, 5, 2, 3, 5, 8, 10, 22, 0, 55, 129, 300};
			gs.mode = 2;

			win.Add (gs);
			win.ShowAll ();
			Application.Run ();
			return 0;
		}
	}

}

using Gtk;
using Gnome;
using GtkSharp;
using System;

public class TimelineSelectorWidget : Gtk.Layout {

	// Terminology:
	//
	//  - "Bar": where the histogram is displayed.
	//  - "Limit arrows": the arrows on the sides to that specify which range is shown.
	//  - "Limit rest areas": the areas on the left/right of the Bar where to which the user can drag a limit
	//    arrow to specify no bound on that side.


	// Model
	// FIXME: Should just be an IEnumerator?

	public interface IModel {
		void Get (int item_num, out string label, out int count);
		int Count { get; }
	}

	private IModel model;
	public IModel Model {
		get {
			return model;
		}
		set {
			model = value;
			QueueResize ();
		}
	}

	public void Reload ()
	{
		QueueResize ();
	}


	// Limits.
	// A value of "-1" means "no limit" on that side.

	private int left_limit = -1;
	public int LeftLimit {
		get {
			return left_limit;
		}
		set {
			left_limit = value;
			QueueDraw ();
		}
	}

	private int right_limit = -1;
	public int RightLimit {
		get {
			return right_limit;
		}
		set {
			right_limit = value;
			QueueDraw ();
		}
	}


	// Layout constants.

	const int LIMIT_ARROW_SIZE = 12;
	const int LIMIT_REST_AREA_WIDTH = 16;
	const int BAR_HEIGHT = 32;
	const int BAR_TICK_HEIGHT = 8;
	const int MIN_SLICE_WIDTH = 16;
	const int MAX_SLICE_WIDTH = 32;
	const int SCROLL_BUTTON_WIDTH = 32;
	const int PADDING = 3;
	const int HISTOGRAM_PADDING = 2;

	// Computed layout values.

	private int bar_width;
	private int first_slice_shown;
	private int slice_width;

	private void RecalcLayout (int available_width)
	{
		// (We assume the available width is enough.)

		bar_width = available_width - 2 * LIMIT_REST_AREA_WIDTH - 2 * SCROLL_BUTTON_WIDTH;

		slice_width = bar_width / Model.Count;
		if (slice_width < MIN_SLICE_WIDTH)
			slice_width = MIN_SLICE_WIDTH;
		else if (slice_width > MAX_SLICE_WIDTH)
			slice_width = MAX_SLICE_WIDTH;

		if (first_slice_shown + bar_width / slice_width + 1 >= Model.Count)
			first_slice_shown = Math.Max (0, Model.Count - 1 - (bar_width / slice_width + 1));
	}


	// Drawing.

	private int RightLimitArrowOffset {
		get {
			if (RightLimit == -1)
				return (SCROLL_BUTTON_WIDTH + LIMIT_REST_AREA_WIDTH + bar_width
					+ (LIMIT_REST_AREA_WIDTH - LIMIT_ARROW_SIZE) / 2);

			int x = (RightLimit - first_slice_shown) * slice_width;
			if (x < 0 || x >= bar_width)
				return -1;
			else
				return x + SCROLL_BUTTON_WIDTH + LIMIT_REST_AREA_WIDTH;
		}
	}

	private int LeftLimitArrowOffset {
		get {
			if (LeftLimit == -1)
				return SCROLL_BUTTON_WIDTH + (LIMIT_REST_AREA_WIDTH - LIMIT_ARROW_SIZE) / 2;

			int x = (LeftLimit - first_slice_shown) * slice_width;
			if (x < 0 || x >= bar_width)
				return -1;
			else
				return x + SCROLL_BUTTON_WIDTH + LIMIT_REST_AREA_WIDTH;
		}
	}

	private void DrawLimitArrow (int x, int y, bool pointing_up) 
	{
		Gdk.Point [] points = new Gdk.Point [5];
		points [0].X = x;
		points [0].Y = y;
		points [1].X = x + LIMIT_ARROW_SIZE;
		points [1].Y = y;

		if (pointing_up) {
			points[2].X = x + LIMIT_ARROW_SIZE;
			points[2].Y = y - LIMIT_ARROW_SIZE / 2;
			points[3].X = x + LIMIT_ARROW_SIZE / 2;
			points[3].Y = y - LIMIT_ARROW_SIZE;
			points[4].X = x;
			points[4].Y = y - LIMIT_ARROW_SIZE / 2;
		} else {
			points[2].X = x + LIMIT_ARROW_SIZE;
			points[2].Y = y + LIMIT_ARROW_SIZE / 2;
			points[3].X = x + LIMIT_ARROW_SIZE / 2;
			points[3].Y = y + LIMIT_ARROW_SIZE;
			points[4].X = x;
			points[4].Y = y + LIMIT_ARROW_SIZE / 2;
		}

		// FIXME: GtkSharp bug, it should take a bool not an int for "filled".
		BinWindow.DrawPolygon (Style.ForegroundGC (StateType.Normal), 0, points);
		BinWindow.DrawPolygon (Style.ForegroundGC (StateType.Normal), 1, points);
	}

	private void DrawBar ()
	{
		int slice_max = 0;
		for (int i = 0; i < Model.Count; i ++) {
			string label;
			int count;

			Model.Get (i, out label, out count);
			slice_max = Math.Max (slice_max, count);
		}

		int x = Allocation.X + SCROLL_BUTTON_WIDTH + LIMIT_REST_AREA_WIDTH;

		BinWindow.DrawRectangle (Style.ForegroundGC (StateType.Normal),
					 false,
					 x, Allocation.Y,
					 bar_width, BAR_HEIGHT);

		int histo_width = slice_width - 2 * HISTOGRAM_PADDING;

		for (int i = first_slice_shown, tick_x = x;
		     i < Model.Count && tick_x < x + bar_width;
		     i ++, tick_x += slice_width) {

			string label;
			int count;
			Model.Get (i, out label, out count);

			int histo_height = (int) Math.Round (((double) count / slice_max) * (BAR_HEIGHT - HISTOGRAM_PADDING));

			BinWindow.DrawLine (Style.ForegroundGC (StateType.Normal),
					    tick_x, Allocation.Y + BAR_HEIGHT,
					    tick_x, Allocation.Y + BAR_HEIGHT - BAR_TICK_HEIGHT);

			BinWindow.DrawRectangle (Style.ForegroundGC (StateType.Normal), false,
						 tick_x + HISTOGRAM_PADDING, Allocation.Y + BAR_HEIGHT - histo_height,
						 histo_width, histo_height);
		}
	}

	private void HandleSizeAllocated (object sender, SizeAllocatedArgs args)
	{
		RecalcLayout (Allocation.Width);
	}

	private void HandleExposeEvent (object sender, ExposeEventArgs args)
	{
		DrawLimitArrow (Allocation.X + LeftLimitArrowOffset, Allocation.Y, false);
		DrawLimitArrow (Allocation.X + LeftLimitArrowOffset, Allocation.Y + BAR_HEIGHT, true);

		DrawLimitArrow (Allocation.X + RightLimitArrowOffset, Allocation.Y, false);
		DrawLimitArrow (Allocation.X + RightLimitArrowOffset, Allocation.Y + BAR_HEIGHT, true);

		DrawBar ();
	}

	public TimelineSelectorWidget ()
		: base (null, null)
	{
		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);
		ExposeEvent += new ExposeEventHandler (HandleExposeEvent);
	}


	public TimelineSelectorWidget (IModel model)
		: this ()
	{
		Model = model;
	}


#if true

	private class TimeModel : TimelineSelectorWidget.IModel {
		public void Get (int item_num, out string label, out int count)
		{
			count = item_num * 100;
			if (item_num % 12 == 0)
				label = String.Format ("{0}", item_num / 12 + 2001);
			else
				label = null;
		}

		public int Count {
			get {
				return 24;
			}
		}
	}

	public static void Main (string [] args)
	{
		Program program = new Program ("F-Spot", "0.0", Modules.UI, args);

		Gtk.Window window = new Gtk.Window (WindowType.Toplevel);
		window.Add (new TimelineSelectorWidget (new TimeModel ()));
		window.ShowAll ();

		program.Run ();
	}

#endif
}

using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using System;
using System.Reflection;
using System.Collections;
using System.IO;

public class IconView : Gtk.Layout {

	// Public properties.

	/* Width of the thumbnails. */
	private int thumbnail_width = 128;
	public int ThumbnailWidth {
		get {
			return thumbnail_width;
		}

		set {
			if (thumbnail_width != value) {
				thumbnail_width = value;
				QueueResize ();
			}
		}
	}

	private double thumbnail_ratio = 4.0 / 3.0;
	public double ThumbnailRatio {
		get {
			return thumbnail_ratio;
		}
		set {
			thumbnail_ratio = value;
			QueueResize ();
		}
	}

	public int ThumbnailHeight {
		get {
			return (int) Math.Round ((double) thumbnail_width / ThumbnailRatio);
		}
	}

	private bool display_tags = true;
	public bool DisplayTags {
		get {
			return display_tags;
		}

		set {
			display_tags = value;
			QueueResize ();
		}
	}


	// Size of the frame around the thumbnail.
	private const int CELL_BORDER_WIDTH = 10;

	// Border around the scrolled area.
	private const int BORDER_SIZE = 6;

	// Thickness of the outline used to indicate selected items. 
	private const int SELECTION_THICKNESS = 3;

	// Size of the tag icon in the view.
	private const int TAG_ICON_SIZE = 12;

	// Horizontal spacing between the tag icons
	private const int TAG_ICON_HSPACING = 2;

	// Vertical spacing between the thumbnail and the row of tag icons.
	private const int TAG_ICON_VSPACING = 6;

	// The loader.
	private PixbufLoader pixbuf_loader;

	// Various other layout values.
	private int cells_per_row;
	private int cell_width;
	private int cell_height;

	// Query we are displaying.
	private PhotoQuery query;

	// The first pixel line that is currently on the screen (i.e. in the current
	// scroll region).  Used to compute the area that went offscreen in the "changed"
	// signal handler for the vertical GtkAdjustment.
	private int y_offset;

	// Hash of all the order number of the items that are selected.
	private Hashtable selected_cells;

	// Drag and drop bookkeeping. 
	private bool in_drag;
	private int click_x, click_y;
	private int click_cell = -1;

	// Number of consecutive GDK_BUTTON_PRESS on the same cell, to
	// distinguish the GDK_2BUTTON_PRESS events that we actually care
	// about.
	private int click_count;

	// The pixbuf we use when we can't load a thumbnail.
	static Pixbuf error_pixbuf;


	// Public events.

	public delegate void DoubleClickedHandler (IconView view, int clicked_item);
	public event DoubleClickedHandler DoubleClicked;

	public delegate void SelectionChangedHandler (IconView view);
	public event SelectionChangedHandler SelectionChanged;


	// Public API.

	public IconView () : base (null, null)
	{
		pixbuf_loader = new PixbufLoader (ThumbnailWidth);
		pixbuf_loader.OnPixbufLoaded += new PixbufLoader.PixbufLoadedHandler (HandlePixbufLoaded);

		selected_cells = new Hashtable ();

		ScrollAdjustmentsSet += new ScrollAdjustmentsSetHandler (HandleScrollAdjustmentsSet);
		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);
		ExposeEvent += new ExposeEventHandler (HandleExposeEvent);
		ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
		ButtonReleaseEvent += new ButtonReleaseEventHandler (HandleButtonReleaseEvent);
		MotionNotifyEvent += new MotionNotifyEventHandler (HandleMotionNotifyEvent);

		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);

		string [] types = new string [1];
		types [0] = "text/uri-list";
		GtkDnd.SetAsDestination (this, types);
		DragDrop += new DragDropHandler (HandleDragDrop);
	}

	private void OnReload (PhotoQuery query)
	{
		QueueResize ();
	}

	public IconView (PhotoQuery query) : this ()
	{
		this.query = query;
		query.Reload += new PhotoQuery.ReloadHandler (OnReload);
	}


	public PhotoQuery Query {
		get {
			return query;
		}
	}

	public int [] Selection {
		get {
			int [] selection = new int [selected_cells.Count];

			int i = 0;
			foreach (int cell in selected_cells.Keys)
				selection [i ++] = cell;

			Array.Sort (selection);
			return selection;
		}
	}


	// Updating.

	public void UpdateThumbnail (int thumbnail_num)
	{
		Photo photo = query.Photos [thumbnail_num];
		string thumbnail_path = Thumbnail.PathForUri ("file://" + photo.DefaultVersionPath, ThumbnailSize.Large);
		ThumbnailCache.Default.RemoveThumbnailForPath (thumbnail_path);
		InvalidateCell (thumbnail_num);
	}


	// Private utility methods.

	static private Pixbuf ErrorPixbuf ()
	{
		if (IconView.error_pixbuf == null)
			IconView.error_pixbuf = PixbufUtils.LoadFromAssembly ("f-spot-question-mark.png");

		return IconView.error_pixbuf;
	}

	private int CellAtPosition (int x, int y)
	{
		if (query == null)
			return -1;

		if (x < BORDER_SIZE || x >= BORDER_SIZE + cells_per_row * cell_width)
			return -1;
		if (y < BORDER_SIZE || y >= BORDER_SIZE + (query.Photos.Length / cells_per_row + 1) * cell_height)
			return -1;

		int column = (int) ((x - BORDER_SIZE) / cell_width);
		int row = (int) ((y - BORDER_SIZE) / cell_height);
		int cell_num = column + row * cells_per_row;

		if (cell_num < query.Photos.Length)
			return (int) cell_num;
		else
			return -1;
	}

	private void UnselectAllCells ()
	{
		if (selected_cells.Count == 0)
			return;

		if (selected_cells.Count < 100) {
			foreach (int cell in selected_cells.Keys) {
				InvalidateCell (cell);
			}
		} else {
			QueueDraw ();
		}
		selected_cells.Clear ();		


		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	private bool CellIsSelected (int cell_num)
	{
		return selected_cells.ContainsKey (cell_num);
	}

	private void SelectCell (int cell_num)
	{
		if (CellIsSelected (cell_num))
			return;

		selected_cells.Add (cell_num, cell_num);

		InvalidateCell (cell_num);

		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	private void SelectCellRange (int start, int end)
	{
		if (start == -1 || end == -1)
			return;

		int current = Math.Min (start, end);
		int final = Math.Max (start, end);				
	
		while (current <= final) {
			SelectCell (current);
			current++;
		}
	}

	private void UnselectCell (int cell_num)
	{
		if (! CellIsSelected (cell_num))
			return;

		selected_cells.Remove (cell_num);

		InvalidateCell (cell_num);

		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	private void ToggleCell (int cell_num)
	{
		if (CellIsSelected (cell_num))
			UnselectCell (cell_num);
		else
			SelectCell (cell_num);
	}


	// Layout and drawing.

	private void UpdateLayout ()
	{
		int available_width = Allocation.width - 2 * BORDER_SIZE;

		cell_width = ThumbnailWidth + 2 * CELL_BORDER_WIDTH;
		cell_height = ThumbnailHeight + 2 * CELL_BORDER_WIDTH;

		if (DisplayTags)
			cell_height += TAG_ICON_SIZE + TAG_ICON_VSPACING;

		cells_per_row = (int) (available_width / cell_width);
		if (cells_per_row == 0)
			cells_per_row = 1;

		int num_thumbnails;
		if (query != null)
			num_thumbnails = query.Photos.Length;
		else
			num_thumbnails = 0;

		int num_rows = num_thumbnails / cells_per_row;
		if (num_thumbnails % cells_per_row != 0)
			num_rows ++;

		SetSize ((uint) Allocation.width, (uint) (num_rows * cell_height + 2 * BORDER_SIZE));
	}

	// FIXME Cache the GCs?
	private void DrawCell (int thumbnail_num, int x, int y)
	{
		Gdk.GC gc = new Gdk.GC (BinWindow);
		gc.Copy (Style.ForegroundGC (StateType.Normal));
		gc.SetLineAttributes (1, LineStyle.Solid, CapStyle.NotLast, JoinStyle.Round);

		Photo photo = query.Photos [thumbnail_num];

		string thumbnail_path = Thumbnail.PathForUri ("file://" + photo.DefaultVersionPath, ThumbnailSize.Large);
		Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);

		Gdk.Rectangle area = new Gdk.Rectangle (x, y, cell_width, cell_height);
		Style.PaintBox (Style, BinWindow, StateType.Normal, ShadowType.Out, area, this, null, x, y, cell_width, cell_height);

		if (thumbnail == null) {
			pixbuf_loader.Request (thumbnail_path, thumbnail_num);
		} else {
			int width, height;
			PixbufUtils.Fit (thumbnail, ThumbnailWidth, ThumbnailHeight, false, out width, out height);

			Pixbuf temp_thumbnail;
			if (width == thumbnail.Width)
				temp_thumbnail = thumbnail;
			else
				temp_thumbnail = thumbnail.ScaleSimple (width, height, InterpType.Nearest);

			int dest_x = (int) (x + (cell_width - width) / 2);
			int dest_y;
			if (DisplayTags)
				dest_y = (int) (y + (cell_height - height - (TAG_ICON_SIZE + TAG_ICON_VSPACING)) / 2);
			else
				dest_y = (int) (y + (cell_height - height) / 2);

			temp_thumbnail.RenderToDrawable (BinWindow, Style.WhiteGC,
							 0, 0, dest_x, dest_y, width, height, RgbDither.None, 0, 0);

			if (CellIsSelected (thumbnail_num)) {
				Gdk.GC selection_gc = new Gdk.GC (BinWindow);
				selection_gc.Copy (Style.BackgroundGC (StateType.Selected));
				selection_gc.SetLineAttributes (SELECTION_THICKNESS, LineStyle.Solid, CapStyle.Butt, JoinStyle.Miter);

				BinWindow.DrawRectangle (selection_gc, false,
							 dest_x - SELECTION_THICKNESS, dest_y - SELECTION_THICKNESS,
							 width + 2 * SELECTION_THICKNESS, height + 2 * SELECTION_THICKNESS);
			}
		}

		if (DisplayTags) {
			Tag [] tags = photo.Tags;

			int tag_x, tag_y;

			tag_x = x + CELL_BORDER_WIDTH;
			tag_y = y + cell_height - CELL_BORDER_WIDTH - TAG_ICON_SIZE;
			foreach (Tag t in tags) {
				Pixbuf icon = null;

				if (t.Category.Icon == null) {
					if (t.Icon == null)
						continue;
					icon = t.Icon;
				} else {
					Category category = t.Category;
					while (category.Category.Icon != null)
						category = category.Category;
					icon = category.Icon;
				}

				Pixbuf scaled_icon;
				if (icon.Width == TAG_ICON_SIZE) {
					scaled_icon = icon;
				} else {
					scaled_icon = icon.ScaleSimple (TAG_ICON_SIZE, TAG_ICON_SIZE, InterpType.Bilinear);
				}

				scaled_icon.RenderToDrawable (BinWindow, Style.WhiteGC,
							      0, 0, tag_x, tag_y, TAG_ICON_SIZE, TAG_ICON_SIZE,
							      RgbDither.None, 0, 0);
				tag_x += TAG_ICON_SIZE + TAG_ICON_VSPACING;
			}
		}
	}

	private void DrawAllCells (int x, int y, int width, int height)
	{
		int start_cell_column = Math.Max ((x - BORDER_SIZE) / cell_width, 0);
		int start_cell_row = Math.Max ((y - BORDER_SIZE) / cell_height, 0);
		int start_cell_num = start_cell_column + start_cell_row * cells_per_row;

		int start_cell_x, cell_y;
		GetCellPosition (start_cell_num, out start_cell_x, out cell_y);

		int end_cell_column = Math.Max ((x + width - BORDER_SIZE) / cell_width, 0);
		int end_cell_row = Math.Max ((y + height - BORDER_SIZE) / cell_height, 0);

		int num_rows = end_cell_row - start_cell_row + 1;
		int num_cols = Math.Min (end_cell_column - start_cell_column + 1,
					 cells_per_row - start_cell_column);

		int i, cell_num;
		for (i = 0, cell_num = start_cell_num;
		     i < num_rows && cell_num < query.Photos.Length;
		     i ++) {
			int cell_x = start_cell_x;

			for (int j = 0; j < num_cols && cell_num + j < query.Photos.Length; j ++) {
				DrawCell (cell_num + j, cell_x, cell_y);
				cell_x += cell_width;
			}

			cell_y += cell_height;
			cell_num += cells_per_row;
		}
	}

	private void GetCellPosition (int cell_num, out int x, out int y)
	{
		int row = cell_num / cells_per_row;
		int col = cell_num % cells_per_row;

		x = col * cell_width + BORDER_SIZE;
		y = row * cell_height + BORDER_SIZE;
	}


	// Scrolling.  We do this in an idle loop so we can catch up if the user scrolls quickly.

	private uint scroll_on_idle_id;

	private int idle_count;	// FIXME

	private bool HandleScrollOnIdle ()
	{
		Adjustment adjustment = Vadjustment;

		if (y_offset == adjustment.Value)
			return false;

		int num_thumbnails = query.Photos.Length;
		int num_rows, start;

		if (y_offset < adjustment.Value) {
			int old_first_row = y_offset / cell_height;
			int new_first_row = (int) (adjustment.Value / cell_height);

			num_rows = new_first_row - old_first_row;
			start = old_first_row * cells_per_row;
		} else {
			int old_last_row = (y_offset + Allocation.height) / cell_height;
			int new_last_row = ((int) adjustment.Value + Allocation.height) / cell_height;

			num_rows = old_last_row - new_last_row;
			start = (new_last_row + 1) * cells_per_row;
		}

		for (int i = 0; i < cells_per_row * num_rows; i ++) {
			if (start + i >= num_thumbnails)
				break;

			Photo photo = query.Photos [start + i];
			string thumbnail_path = Thumbnail.PathForUri ("file://" + photo.DefaultVersionPath, ThumbnailSize.Large);
			pixbuf_loader.Cancel (thumbnail_path);
		}

		y_offset = (int) adjustment.Value;

		scroll_on_idle_id = 0;
		return false;
	}

	private void Scroll ()
	{
		if (scroll_on_idle_id == 0)
			scroll_on_idle_id = GLib.Idle.Add (new GLib.IdleHandler (HandleScrollOnIdle));
	}

	private void CancelScroll ()
	{
		if (scroll_on_idle_id != 0)
			GLib.Source.Remove (scroll_on_idle_id);
	}


	// Event handlers.

	private void HandleAdjustmentValueChanged (object sender, EventArgs args)
	{
		Scroll ();
	}

	private void HandlePixbufLoaded (PixbufLoader loader, string path, int order, Pixbuf result)
	{
		if (result == null)
			result = ErrorPixbuf ();

		ThumbnailCache.Default.AddThumbnail (path, result);
		InvalidateCell (order);
	}

	private void InvalidateCell (int order) {
		Rectangle area;
		GetCellPosition (order, out area.x, out area.y);
		area.width = cell_width;
		area.height = cell_height;

		BinWindow.InvalidateRect (area, true);
	}
			
	private void HandleScrollAdjustmentsSet (object sender, ScrollAdjustmentsSetArgs args)
	{
		if (args.Vadjustment != null)
			args.Vadjustment.ValueChanged += new EventHandler (HandleAdjustmentValueChanged);
	}

	private void HandleSizeAllocated (object sender, SizeAllocatedArgs args)
	{
		UpdateLayout ();
	}

	private void HandleExposeEvent (object sender, ExposeEventArgs args)
	{
		DrawAllCells (args.Event.area.x, args.Event.area.y,
			      args.Event.area.width, args.Event.area.height);
	}

 	private void HandleButtonPressEvent (object obj, ButtonPressEventArgs args)
 	{
		int cell_num = CellAtPosition ((int) args.Event.x, (int) args.Event.y);

		if (cell_num < 0)
			return;

		switch (args.Event.type) {
		case EventType.TwoButtonPress:
			if (args.Event.button != 1 || click_count < 2
			    || (args.Event.state & (uint) (ModifierType.ControlMask
							   | ModifierType.ShiftMask)) != 0)
				return;
			if (DoubleClicked != null)
				DoubleClicked (this, cell_num);
			return;

		case EventType.ButtonPress:
			if (args.Event.button != 1)
				return;

			if ((args.Event.state & (uint) ModifierType.ControlMask) != 0) {
				ToggleCell (cell_num);
			} else if ((args.Event.state & (uint) ModifierType.ShiftMask) != 0) {
				SelectCellRange (click_cell, cell_num);
			} else {
				UnselectAllCells ();
				SelectCell (cell_num);
			}

			Gdk.Pointer.Grab (BinWindow, false,
					  EventMask.ButtonReleaseMask | EventMask.Button1MotionMask,
					  null, null, args.Event.time);

			click_x = (int) args.Event.x;
			click_y = (int) args.Event.y;

			if (click_cell == cell_num) {
				click_count ++;
			} else {
				click_cell = cell_num;
				click_count = 1;
			}

			return;

		default:
			return;
		}
	}

	private void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
	{
		Gdk.Pointer.Ungrab (args.Event.time);
		in_drag = false;
	}

 	private void HandleMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
 	{
		if (in_drag)
			return;

		if (! Gtk.Drag.CheckThreshold (this, click_x, click_y, (int) args.Event.x, (int) args.Event.y))
			return;

		TargetList target_list = new TargetList ();
		in_drag = true;
	}

	private void HandleDestroyEvent (object sender, DestroyEventArgs args)
	{
		CancelScroll ();
	}


	// DnD event handlers.

	private void HandleDragDrop (object sender, DragDropArgs args)
	{
		Console.WriteLine ("HandleDragDrop");
	}
}
